using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Implementations
{
    // Công thức báo giá. Mô tả từng bước nằm ở docs/TASKS.md, phụ lục "Báo giá".
    // Mọi mức giá đọc từ bảng pricing.price_items, ở đây chỉ có cách tính.
    public class PricingService : IPricingService
    {
        public const int ExpressThresholdDays = 7;
        public const int HorsesPerGroom = 3;

        private static readonly TimeSpan PriceCacheTtl = TimeSpan.FromMinutes(30);
        private static readonly string[] SurchargeOrder = { "SUR_FUEL", "SUR_EXPRESS", "SUR_CLIMATE" };
        private static readonly string[] ClearanceOrder = { "FEE_DOCS", "FEE_CLEARANCE", "FEE_QUARANTINE" };

        private readonly IRepository<PriceItem> _priceItemRepo;
        private readonly ICacheService _cache;
        private readonly IDistanceService _distanceService;

        public PricingService(IRepository<PriceItem> priceItemRepo, ICacheService cache, IDistanceService distanceService)
        {
            _priceItemRepo = priceItemRepo;
            _cache = cache;
            _distanceService = distanceService;
        }

        public async Task<QuoteModel> CalculateQuoteAsync(QuoteInputModel input)
        {
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
            }

            ValidateInput(input);

            string mode = input.TransportMode;
            string destination = input.DropoffCountryCode.Trim().ToUpperInvariant();
            int horseCount = input.StallClasses.Count;

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
            bool isExpress = (input.DepartureDate - DateTimeOffset.UtcNow).TotalDays < ExpressThresholdDays;
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

            // 4. Phí thủ tục, thông quan, kiểm dịch
            var clearanceFees = prices.Values
                .Where(p => p.Category == PriceCategories.Clearance && p.Value > 0)
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

            // 5. Phí áp tải: 1 groom cho mỗi 3 ngựa; đường bộ tính theo ngày
            int groomCount = (int)Math.Ceiling(horseCount / (double)HorsesPerGroom);
            int transitDays = Math.Max(1, (int)Math.Ceiling((input.DeliveryDate - input.DepartureDate).TotalHours / 24));

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
                DistanceKm = input.TransportMode == TransportModes.Ground ? input.DistanceKm : null
            };
        }

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

            if (input.DeliveryDate <= input.DepartureDate)
            {
                throw new BusinessException("DeliveryDate must be after DepartureDate.");
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
