using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;

namespace RacehorseTransport.Services.Implementations
{
    // R-04: gợi ý lộ trình khi Coordinator chọn ngựa của một hay nhiều đơn để lập chuyến.
    // Kết quả chỉ là gợi ý: Coordinator chép vào bản nháp rồi sửa tên cửa khẩu, thêm mốc nghỉ đêm... trước khi trình.
    public partial class TripPlanningService
    {
        public async Task<RouteSuggestionModel> SuggestRouteAsync(RouteSuggestionRequestModel request)
        {
            var ids = request.BookingHorseIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                throw new BusinessException("Choose at least one horse to suggest a route.");
            }

            var bookingHorses = await _bookingHorseRepo.GetQueryable()
                .AsNoTracking()
                .AsSplitQuery()
                .Include(bh => bh.Horse)
                .Include(bh => bh.Dossier)
                .Include(bh => bh.Booking!).ThenInclude(b => b.TransitCountries)
                .Where(bh => ids.Contains(bh.BookingHorseId))
                .ToListAsync();

            var unknown = ids.Except(bookingHorses.Select(bh => bh.BookingHorseId)).ToList();
            if (unknown.Count > 0)
            {
                throw new BusinessException($"Booking horse(s) with ID {string.Join(", ", unknown)} were not found.");
            }

            var modes = bookingHorses.Select(bh => bh.Booking!.TransportMode).Distinct().ToList();
            if (modes.Count > 1)
            {
                throw new BusinessException("Ground and air bookings cannot be combined on the same trip.");
            }

            var inputs = bookingHorses
                .Select(bh => bh.Booking!)
                .DistinctBy(b => b.BookingId)
                .Select(b => new RouteBookingInput
                {
                    BookingId = b.BookingId,
                    BookingCode = b.BookingCode,
                    PickupAddress = b.PickupAddress,
                    PickupCountryCode = b.PickupCountryCode,
                    DropoffAddress = b.DropoffAddress,
                    DropoffCountryCode = b.DropoffCountryCode,
                    TransitCountryCodes = b.TransitCountries.OrderBy(t => t.SequenceOrder).Select(t => t.CountryCode).ToList(),
                    PickupWindowStart = b.PickupWindowStart,
                    PickupWindowEnd = b.PickupWindowEnd,
                    DeliveryDeadline = b.DeliveryDeadline,
                    DirectDistanceKm = b.DistanceKm
                })
                .ToList();

            var builder = new RouteSuggestionBuilder(_transitOptions, _transitEstimator, DistanceKmAsync);
            var result = await builder.BuildAsync(inputs, modes[0], request.PlannedStartDate.ToUniversalTime(), request.DriverCount);
            await FillApprovedRouteCoordinatesAsync(result, inputs);

            // Hồ sơ chưa thông quan: vẫn lập được kế hoạch, nhưng chuyến sẽ không xuất phát được cho tới khi xong
            foreach (var bh in bookingHorses.Where(bh => bh.Dossier?.Status != DossierStatuses.Cleared).OrderBy(bh => bh.BookingId))
            {
                result.Warnings.Add(new PlanWarningModel
                {
                    Code = PlanWarningCodes.DossierNotCleared,
                    Severity = PlanWarningSeverities.Warning,
                    BookingCode = bh.Booking!.BookingCode,
                    Message = $"Clearance of {bh.Horse!.Name} (booking {bh.Booking.BookingCode}) is {bh.Dossier?.Status ?? "not started"}; the trip cannot depart until it is Cleared.",
                    Params = new Dictionary<string, string>
                    {
                        ["horseName"] = bh.Horse.Name,
                        ["bookingCode"] = bh.Booking.BookingCode,
                        ["status"] = bh.Dossier?.Status ?? "None"
                    }
                });
            }

            return result;
        }

        // Tuyến ghép vẫn do RouteSuggestionBuilder sắp thứ tự; tọa độ điểm đón / trả lấy từ tuyến đã duyệt của từng đơn.
        private async Task FillApprovedRouteCoordinatesAsync(RouteSuggestionModel result, List<RouteBookingInput> bookings)
        {
            var bookingIds = bookings.Select(b => b.BookingId).ToList();
            var plans = await _routePlanRepo.GetQueryable()
                .AsNoTracking()
                .Include(p => p.Stops)
                .Where(p => bookingIds.Contains(p.BookingId) && p.Status == RoutePlanStatuses.Approved)
                .ToListAsync();

            var coordinatesByAddress = new Dictionary<string, (decimal Lat, decimal Lng)>();
            foreach (var plan in plans)
            {
                foreach (var stop in plan.Stops.Where(s => s.StopType == RouteStopTypes.Origin || s.StopType == RouteStopTypes.Destination))
                {
                    coordinatesByAddress[NormalizeAddress(stop.Address)] = (stop.Latitude, stop.Longitude);
                }
            }

            // Cửa khẩu thật trên tuyến đã duyệt, theo cặp nước (nước của mốc trước -> nước của mốc sau)
            var gatesByCountryPair = new Dictionary<string, RacehorseTransport.Repositories.Entities.RoutePlanStop>();
            foreach (var plan in plans)
            {
                var stops = plan.Stops.OrderBy(s => s.SequenceOrder).ToList();
                for (int i = 0; i < stops.Count; i++)
                {
                    if (stops[i].StopType != RouteStopTypes.BorderGate)
                    {
                        continue;
                    }
                    string? from = stops.Take(i).LastOrDefault(s => s.StopType != RouteStopTypes.BorderGate)?.CountryCode;
                    string? to = stops.Skip(i + 1).FirstOrDefault(s => s.StopType != RouteStopTypes.BorderGate)?.CountryCode;
                    if (from != null && to != null)
                    {
                        gatesByCountryPair.TryAdd(CountryPairKey(from, to), stops[i]);
                    }
                }
            }

            foreach (var checkpoint in result.Checkpoints)
            {
                if (coordinatesByAddress.TryGetValue(NormalizeAddress(checkpoint.Address), out var coordinates))
                {
                    checkpoint.Latitude = coordinates.Lat;
                    checkpoint.Longitude = coordinates.Lng;
                }
                else if (checkpoint.CheckpointType == CheckpointTypes.BorderGate
                    && checkpoint.FromCountryCode != null && checkpoint.ToCountryCode != null
                    && gatesByCountryPair.TryGetValue(CountryPairKey(checkpoint.FromCountryCode, checkpoint.ToCountryCode), out var gate))
                {
                    // Thay tên tạm "Border VN - KH" bằng cửa khẩu Manager đã duyệt trên tuyến của đơn
                    checkpoint.CheckpointName = gate.StopName;
                    checkpoint.Address = gate.Address;
                    checkpoint.Latitude = gate.Latitude;
                    checkpoint.Longitude = gate.Longitude;
                }
            }
        }

        private static string CountryPairKey(string from, string to) => $"{from.ToUpperInvariant()}>{to.ToUpperInvariant()}";

        private static string NormalizeAddress(string address) => address.Trim().ToUpperInvariant();

        // Quãng đường đường bộ giữa hai địa điểm (Goong / OSRM, dự phòng bảng cự ly của DistanceService)
        private async Task<int> DistanceKmAsync(RoutePlace from, RoutePlace to)
        {
            var estimate = await _distanceService.EstimateGroundDistanceAsync(new DistanceEstimateInputModel
            {
                PickupAddress = from.Address,
                PickupCountryCode = from.CountryCode,
                DropoffAddress = to.Address,
                DropoffCountryCode = to.CountryCode
            });
            return estimate.DistanceKm;
        }
    }
}
