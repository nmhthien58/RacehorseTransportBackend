using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Helpers;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    // Công thức báo giá. Mô tả từng bước nằm ở docs/TASKS.md, phụ lục "Báo giá".
    // Mọi mức giá đọc từ bảng pricing.price_items, ở đây chỉ có cách tính.
    public class PricingService : IPricingService
    {
        public const int HorsesPerGroom = 3;
        private const string TransitFeeCode = "FEE_TRANSIT";

        private static readonly TimeSpan PriceCacheTtl = TimeSpan.FromMinutes(30);
        private static readonly string[] SurchargeOrder = { "SUR_FUEL", "SUR_EXPRESS", "SUR_CLIMATE" };
        private static readonly string[] ClearanceOrder = { "FEE_DOCS", "FEE_CLEARANCE", "FEE_QUARANTINE" };

        private readonly IRepository<PriceItem> _priceItemRepo;
        private readonly ICacheService _cache;
        private readonly IDistanceService _distanceService;
        private readonly ITransitTimeEstimator _transitTimeEstimator;
        private readonly IClearanceScheduleService _clearanceSchedule;
        private readonly IGeoService _geoService;
        private readonly BookingOptions _bookingOptions;

        public PricingService(
            IRepository<PriceItem> priceItemRepo,
            ICacheService cache,
            IDistanceService distanceService,
            ITransitTimeEstimator transitTimeEstimator,
            IClearanceScheduleService clearanceSchedule,
            IGeoService geoService,
            IOptions<BookingOptions> bookingOptions)
        {
            _priceItemRepo = priceItemRepo;
            _cache = cache;
            _distanceService = distanceService;
            _transitTimeEstimator = transitTimeEstimator;
            _clearanceSchedule = clearanceSchedule;
            _geoService = geoService;
            _bookingOptions = bookingOptions.Value;
        }

        // R-10: khoảng giá ước tính khi khách tạo đơn / xem giá.
        //   Có tọa độ (đường bộ): quãng đường thật A -> B và các nước đường đi qua lấy từ bản đồ (BE-01);
        //   nước quá cảnh khách tự nhập không còn dùng.
        //   min = giá theo quãng đường thật, KHÔNG có phí quá cảnh (lịch / thời gian qua cửa khẩu vẫn tính theo các nước thật);
        //   max = quãng đường × QuoteRangeDistanceFactor + phí quá cảnh cho các nước đường A -> B đi qua (tối đa QuoteMaxTransitCountries).
        public async Task<QuoteModel> CalculateQuoteRangeAsync(QuoteInputModel input)
        {
            bool routed = false;
            bool routeIsEstimate = false;
            string? routeSummary = null;
            if (input.TransportMode == TransportModes.Ground
                && input.PickupLat.HasValue && input.PickupLng.HasValue && input.DropoffLat.HasValue && input.DropoffLng.HasValue)
            {
                string origin = string.IsNullOrWhiteSpace(input.PickupCountryCode) ? "VN" : input.PickupCountryCode.Trim().ToUpperInvariant();
                string destination = input.DropoffCountryCode.Trim().ToUpperInvariant();
                var route = await _geoService.RouteAsync(new[]
                {
                    new GeoPointModel((double)input.PickupLat.Value, (double)input.PickupLng.Value, origin),
                    new GeoPointModel((double)input.DropoffLat.Value, (double)input.DropoffLng.Value, destination)
                }, includeCountries: origin != destination);

                input.DistanceKm = Math.Max(1, (int)Math.Ceiling(route.TotalDistanceKm));
                // Các nước đường đi qua (đã gộp trùng liên tiếp) trừ nước đi và nước đến = nước quá cảnh
                var alongRoute = (route.CountryCodes ?? new List<string>())
                    .Where(c => c != origin && c != destination)
                    .Distinct()
                    .ToList();
                input.TransitCountryCodes = TransitCountryRules.Normalize(alongRoute, origin, destination);
                routeIsEstimate = route.IsEstimate;
                routeSummary = route.CountryCodes is { Count: > 0 }
                    ? $"{input.DistanceKm} km ({string.Join(" → ", route.CountryCodes)})"
                    : $"{input.DistanceKm} km";
                routed = true;
            }
            else
            {
                // Không có tọa độ (app cũ): không còn nhận nước quá cảnh do khách nhập
                input.TransitCountryCodes = new List<string>();
            }

            var detectedTransit = input.TransitCountryCodes.ToList();
            var min = await CalculateQuoteAsync(input);
            if (routed)
            {
                min.RouteSummary = routeSummary;
            }

            // Giá thấp nhất chưa tính phí quá cảnh (tuyến chốt có thể đi thẳng qua cửa khẩu khác)
            var transitLine = min.Lines.FirstOrDefault(l => l.Code == TransitFeeCode);
            if (transitLine != null)
            {
                min.Lines.Remove(transitLine);
                min.Total -= transitLine.Amount;
            }

            decimal max = min.Total;
            if (input.TransportMode == TransportModes.Ground && min.DistanceKm.HasValue)
            {
                var maxQuote = await CalculateQuoteAsync(new QuoteInputModel
                {
                    TransportMode = input.TransportMode,
                    DropoffCountryCode = input.DropoffCountryCode,
                    PickupCountryCode = input.PickupCountryCode,
                    DistanceKm = (int)Math.Ceiling(min.DistanceKm.Value * _bookingOptions.QuoteRangeDistanceFactor),
                    RequiresClimateControl = input.RequiresClimateControl,
                    DeclaredValue = input.DeclaredValue,
                    PickupWindowStart = input.PickupWindowStart,
                    PickupWindowEnd = input.PickupWindowEnd,
                    DeliveryDeadline = input.DeliveryDeadline,
                    TransitCountryCodes = detectedTransit.Take(Math.Max(0, _bookingOptions.QuoteMaxTransitCountries)).ToList(),
                    QuotedAt = input.QuotedAt,
                    StallClasses = input.StallClasses,
                    SkipScheduleCheck = true,
                    ForceExpress = min.IsExpress
                });
                max = maxQuote.Total;
            }

            min.QuoteMin = min.Total;
            min.QuoteMax = Math.Max(max, min.Total);
            min.RouteIsEstimate = routeIsEstimate;
            return min;
        }

        public async Task<QuoteModel> CalculateQuoteAsync(QuoteInputModel input)
        {
            string? routeSummary = null;
            if (input.TransportMode == TransportModes.Ground && (input.DistanceKm == null || input.DistanceKm <= 0))
            {
                var estimate = await _distanceService.EstimateGroundDistanceAsync(new DistanceEstimateInputModel
                {
                    PickupCountryCode = input.PickupCountryCode ?? "VN",
                    PickupCity = input.PickupCity,
                    PickupAddress = input.PickupAddress,
                    DropoffCountryCode = input.DropoffCountryCode,
                    DropoffCity = input.DropoffCity,
                    DropoffAddress = input.DropoffAddress
                });
                input.DistanceKm = estimate.DistanceKm;
                routeSummary = estimate.RouteSummary;
            }

            ValidateInput(input);

            string mode = input.TransportMode;
            string destination = input.DropoffCountryCode.Trim().ToUpperInvariant();
            int horseCount = input.StallClasses.Count;

            string origin = string.IsNullOrWhiteSpace(input.PickupCountryCode) ? "VN" : input.PickupCountryCode.Trim().ToUpperInvariant();
            var transitCountries = TransitCountryRules.Normalize(input.TransitCountryCodes, origin, destination);

            // Thời gian vận chuyển do hệ thống ước tính (không lấy từ ngày khách nhập).
            // Đường bộ khác nước: mỗi nước quá cảnh thêm một lần qua cửa khẩu.
            var transit = _transitTimeEstimator.Estimate(new TransitEstimateInputModel
            {
                TransportMode = mode,
                DistanceKm = input.DistanceKm,
                BorderCrossings = mode == TransportModes.Ground && origin != destination ? transitCountries.Count + 1 : 0
            });

            // Thời gian đặt trước tối thiểu theo thời gian làm hồ sơ của chính tuyến này, rồi kiểm tra lịch khách chọn
            int clearanceDays = await _clearanceSchedule.GetClearanceDaysAsync(origin, transitCountries, destination);
            int leadTimeDays = _bookingOptions.LeadTimeDaysFor(clearanceDays);
            var schedule = input.SkipScheduleCheck
                ? new ScheduleCheck(input.ForceExpress ?? false, default, default, default)
                : CheckSchedule(input, transit, leadTimeDays);

            var prices = await GetApplicablePricesAsync(mode, destination);

            if (!prices.TryGetValue("FREIGHT", out var freight))
            {
                throw new BusinessException($"No freight rate is configured for {mode} transport to '{destination}'.");
            }

            // Cước một con ngựa: đường bay tính theo con, đường bộ tính theo km
            decimal unitFreight = mode == TransportModes.Air
                ? freight.Value
                : Round(input.DistanceKm!.Value * freight.Value);

            var lines = new List<QuoteLineModel>();

            // 1. Cước gốc: mỗi hạng chuồng một dòng, nhân hệ số của hạng đó
            foreach (string stallClass in StallClasses.All)
            {
                int count = input.StallClasses.Count(s => s == stallClass);
                if (count == 0)
                {
                    continue;
                }

                string stallCode = "STALL_" + stallClass.ToUpperInvariant();
                if (!prices.TryGetValue(stallCode, out var stall))
                {
                    throw new BusinessException($"No price multiplier is configured for stall class '{stallClass}'.");
                }

                lines.Add(new QuoteLineModel
                {
                    Code = "FREIGHT_" + stallClass.ToUpperInvariant(),
                    Name = $"Cước vận chuyển - {stall.ItemName}",
                    Quantity = count,
                    UnitPrice = Round(unitFreight * stall.Value),
                    Amount = Round(count * unitFreight * stall.Value)
                });
            }

            decimal freightBase = lines.Sum(l => l.Amount);

            // 2. Chiết khấu số lượng: bậc cao nhất mà số ngựa đạt tới
            var discount = prices.Values
                .Where(p => p.Category == PriceCategories.Discount && p.MinHorses <= horseCount)
                .OrderByDescending(p => p.MinHorses)
                .FirstOrDefault();

            decimal discountAmount = 0;
            if (discount != null)
            {
                discountAmount = Round(freightBase * discount.Value / 100);
                lines.Add(SingleLine(discount, -discountAmount));
            }

            decimal freightNet = freightBase - discountAmount;

            // 3. Phụ phí tính trên cước sau chiết khấu
            bool isExpress = schedule.IsExpress;
            foreach (string code in SurchargeOrder)
            {
                bool applies = code == "SUR_FUEL"
                    || (code == "SUR_EXPRESS" && isExpress)
                    || (code == "SUR_CLIMATE" && input.RequiresClimateControl);

                if (applies && prices.TryGetValue(code, out var surcharge))
                {
                    lines.Add(SingleLine(surcharge, Round(freightNet * surcharge.Value / 100)));
                }
            }

            // 4. Phí thủ tục, thông quan, kiểm dịch (phí quá cảnh tính riêng bên dưới, theo số nước đi qua)
            var clearanceFees = prices.Values
                .Where(p => p.Category == PriceCategories.Clearance && p.Value > 0 && p.ItemCode != TransitFeeCode)
                .OrderBy(p => { int i = Array.IndexOf(ClearanceOrder, p.ItemCode); return i < 0 ? int.MaxValue : i; })
                .ThenBy(p => p.ItemCode);

            foreach (var fee in clearanceFees)
            {
                int quantity = fee.CalcType == PriceCalcTypes.PerHorse ? horseCount : 1;
                lines.Add(new QuoteLineModel
                {
                    Code = fee.ItemCode,
                    Name = fee.ItemName,
                    Quantity = quantity,
                    UnitPrice = Round(fee.Value),
                    Amount = Round(quantity * fee.Value)
                });
            }

            // Phí thủ tục quá cảnh: mỗi nước đi qua một lần (× số ngựa nếu tính theo con)
            if (transitCountries.Count > 0 && prices.TryGetValue(TransitFeeCode, out var transitFee) && transitFee.Value > 0)
            {
                int quantity = transitCountries.Count * (transitFee.CalcType == PriceCalcTypes.PerHorse ? horseCount : 1);
                lines.Add(new QuoteLineModel
                {
                    Code = transitFee.ItemCode,
                    Name = $"{transitFee.ItemName}: {string.Join(", ", transitCountries)}",
                    Quantity = quantity,
                    UnitPrice = Round(transitFee.Value),
                    Amount = Round(quantity * transitFee.Value)
                });
            }

            // 5. Phí áp tải: 1 groom cho mỗi 3 ngựa; đường bộ tính theo số ngày vận chuyển ước tính
            int groomCount = (int)Math.Ceiling(horseCount / (double)HorsesPerGroom);
            int transitDays = transit.TransitDays;

            if (prices.TryGetValue("ESCORT", out var escort))
            {
                int quantity = groomCount * (escort.CalcType == PriceCalcTypes.PerGroomDay ? transitDays : 1);
                lines.Add(new QuoteLineModel
                {
                    Code = escort.ItemCode,
                    Name = escort.ItemName,
                    Quantity = quantity,
                    UnitPrice = Round(escort.Value),
                    Amount = Round(quantity * escort.Value)
                });
            }

            // 6. Bảo hiểm hành trình (chỉ khi khách khai báo giá trị)
            if (input.DeclaredValue.HasValue && prices.TryGetValue("INS_TRANSIT", out var insurance))
            {
                lines.Add(SingleLine(insurance, Round(input.DeclaredValue.Value * insurance.Value / 100)));
            }

            return new QuoteModel
            {
                Lines = lines,
                Total = lines.Sum(l => l.Amount),
                HorseCount = horseCount,
                GroomCount = groomCount,
                TransitDays = transitDays,
                IsExpress = isExpress,
                DistanceKm = input.TransportMode == TransportModes.Ground ? input.DistanceKm : null,
                EstimatedHours = transit.TotalHours,
                RouteSummary = routeSummary,
                LeadTimeDays = leadTimeDays,
                ClearanceDays = clearanceDays,
                TransitCountryCodes = transitCountries,
                EarliestPickupDate = schedule.EarliestPickupDate,
                ExpressUntilDate = schedule.ExpressUntilDate,
                EarliestDeliveryDeadline = schedule.EarliestDeliveryDeadline,
                Transit = transit
            };
        }

        // Kiểm tra khoảng pickup / hạn giao khách chọn và xác định Express.
        // Ngày được so theo lịch của chính múi giờ khách gửi lên (khách chọn "ngày 10" thì là ngày 10 của họ).
        private ScheduleCheck CheckSchedule(QuoteInputModel input, TransitEstimateModel transit, int leadTimeDays)
        {
            var quotedAt = input.QuotedAt ?? DateTimeOffset.UtcNow;
            var earliestPickup = DateOnly.FromDateTime(quotedAt.UtcDateTime).AddDays(leadTimeDays);
            var expressUntil = earliestPickup.AddDays(_bookingOptions.ExpressWindowDays);
            var maxTransit = TimeSpan.FromHours((double)transit.MaxHours);

            if (input.PickupWindowStart == null)
            {
                if (input.PickupWindowEnd != null || input.DeliveryDeadline != null)
                {
                    throw new BusinessException("PickupWindowStart is required when PickupWindowEnd or DeliveryDeadline is given.");
                }

                // Chưa chọn ngày (xem giá trước): chỉ báo lịch sớm nhất có thể, chưa tính Express
                var earliestStart = new DateTimeOffset(earliestPickup.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                return new ScheduleCheck(false, earliestPickup, expressUntil, earliestStart + maxTransit);
            }

            var start = input.PickupWindowStart.Value;
            var end = input.PickupWindowEnd ?? start;
            var startDay = DateOnly.FromDateTime(start.DateTime);

            if (startDay < earliestPickup)
            {
                throw new BusinessException(
                    $"PickupWindowStart must be on or after {earliestPickup:yyyy-MM-dd}: a booking on this route needs about {leadTimeDays} days for review, deposit, quarantine clearance and trip planning.");
            }
            if (end < start)
            {
                throw new BusinessException("PickupWindowEnd must be on or after PickupWindowStart.");
            }
            if ((end - start).TotalDays > _bookingOptions.MaxPickupWindowDays)
            {
                throw new BusinessException($"The pickup window cannot be longer than {_bookingOptions.MaxPickupWindowDays} days.");
            }

            // Ngựa có thể được đón vào bất kỳ lúc nào tới hết ngày cuối của khoảng đón,
            // nên hạn giao phải đủ cho trường hợp đón muộn nhất (cuối ngày cuối) + thời gian chạy chậm nhất
            var latestPickup = new DateTimeOffset(end.Date.AddDays(1), end.Offset);
            var earliestDeadline = latestPickup + maxTransit;
            if (input.DeliveryDeadline.HasValue && input.DeliveryDeadline.Value < earliestDeadline)
            {
                throw new BusinessException(
                    $"DeliveryDeadline must be on or after {earliestDeadline:yyyy-MM-dd HH:mm zzz}: the horses may be picked up as late as the end of {end:yyyy-MM-dd} and this route is estimated to take up to {transit.MaxHours} hours.");
            }

            bool urgentPickup = startDay < expressUntil;
            bool tightDeadline = input.DeliveryDeadline.HasValue
                && (input.DeliveryDeadline.Value - earliestDeadline).TotalHours < _bookingOptions.TightDeadlineHours;

            return new ScheduleCheck(urgentPickup || tightDeadline, earliestPickup, expressUntil, earliestDeadline);
        }

        private sealed record ScheduleCheck(
            bool IsExpress,
            DateOnly EarliestPickupDate,
            DateOnly ExpressUntilDate,
            DateTimeOffset EarliestDeliveryDeadline);

        public string SerializeLines(IEnumerable<QuoteLineModel> lines)
        {
            var stored = lines.Select(l => new StoredQuoteLine
            {
                Code = l.Code,
                Name = l.Name,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                Amount = l.Amount
            });

            return JsonSerializer.Serialize(stored);
        }

        public List<QuoteLineModel> ParseLines(string? quoteBreakdownJson)
        {
            if (string.IsNullOrWhiteSpace(quoteBreakdownJson))
            {
                return new List<QuoteLineModel>();
            }

            var stored = JsonSerializer.Deserialize<List<StoredQuoteLine>>(quoteBreakdownJson) ?? new List<StoredQuoteLine>();
            return stored.Select(s => new QuoteLineModel
            {
                Code = s.Code,
                Name = s.Name,
                Quantity = s.Quantity,
                UnitPrice = s.UnitPrice,
                Amount = s.Amount
            }).ToList();
        }

        // Với mỗi ItemCode lấy đúng MỘT dòng giá khớp phương thức và nước đến;
        // dòng ghi rõ nước đến được ưu tiên hơn dòng dùng chung, rồi tới dòng ghi rõ phương thức.
        private async Task<Dictionary<string, PriceItem>> GetApplicablePricesAsync(string mode, string destination)
        {
            var allActive = await _cache.GetOrSetAsync(CacheKeys.PriceItemsAll, async () =>
            {
                return await _priceItemRepo.GetQueryable().AsNoTracking().Where(p => p.IsActive).ToListAsync();
            }, PriceCacheTtl);

            return allActive
                .Where(p => (p.TransportMode == null || p.TransportMode == mode)
                         && (p.DestCountryCode == null || p.DestCountryCode == destination))
                .GroupBy(p => p.ItemCode)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(p => p.DestCountryCode == null ? 1 : 0)
                          .ThenBy(p => p.TransportMode == null ? 1 : 0)
                          .First());
        }

        private static void ValidateInput(QuoteInputModel input)
        {
            if (!TransportModes.All.Contains(input.TransportMode))
            {
                throw new BusinessException($"TransportMode '{input.TransportMode}' is invalid. Allowed values: {string.Join(", ", TransportModes.All)}.");
            }

            if (input.StallClasses.Count == 0)
            {
                throw new BusinessException("At least one horse is required.");
            }

            var invalidStall = input.StallClasses.FirstOrDefault(s => !StallClasses.All.Contains(s));
            if (invalidStall != null)
            {
                throw new BusinessException($"StallClass '{invalidStall}' is invalid. Allowed values: {string.Join(", ", StallClasses.All)}.");
            }

            if (input.TransportMode == TransportModes.Ground && (input.DistanceKm == null || input.DistanceKm <= 0))
            {
                throw new BusinessException("DistanceKm is required for ground transport.");
            }

            if (input.DeclaredValue < 0)
            {
                throw new BusinessException("DeclaredValue cannot be negative.");
            }
        }

        private static QuoteLineModel SingleLine(PriceItem item, decimal amount)
        {
            return new QuoteLineModel
            {
                Code = item.ItemCode,
                Name = item.ItemName,
                Quantity = 1,
                UnitPrice = amount,
                Amount = amount
            };
        }

        private static decimal Round(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        // Dạng lưu trong cột quote_breakdown (jsonb): [{ "code", "name", "qty", "unitPrice", "amount" }]
        private class StoredQuoteLine
        {
            [JsonPropertyName("code")] public string Code { get; set; } = string.Empty;
            [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
            [JsonPropertyName("qty")] public decimal Quantity { get; set; }
            [JsonPropertyName("unitPrice")] public decimal UnitPrice { get; set; }
            [JsonPropertyName("amount")] public decimal Amount { get; set; }
        }
    }
}
