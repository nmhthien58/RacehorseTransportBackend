using System.Globalization;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    // Gợi ý lộ trình cho một chuyến ghép nhiều đơn (R-04). Không đụng DB; quãng đường lấy qua hàm distance được truyền vào
    // (thực tế là DistanceService, trong test là bảng giả), nên dễ viết unit test.
    //
    // Cách xếp (tham lam "điểm gần nhất tiếp theo", đủ tốt cho 2-4 đơn):
    //   1. Origin = điểm đón của đơn có khoảng pickup sớm nhất. Đơn đón cùng chỗ lên xe ở Origin luôn.
    //   2. Các điểm đón còn lại: lần lượt tới điểm gần nhất -> mốc PickUp.
    //   3. Các điểm trả: lần lượt tới điểm gần nhất -> mốc DropOff; điểm trả cuối cùng là Destination.
    //   4. Đổi nước giữa hai điểm thì chèn mốc BorderGate (theo danh sách nước quá cảnh của đơn nếu có).
    // Giờ dự kiến: giờ lái từng chặng theo TransitTimeEstimator (đã gồm nghỉ giữa chặng / qua đêm) + giờ qua cửa khẩu
    // + giờ xếp / dỡ ngựa ở mỗi điểm đón / trả.
    public class RouteSuggestionBuilder
    {
        private readonly TransitOptions _options;
        private readonly ITransitTimeEstimator _estimator;
        private readonly Func<RoutePlace, RoutePlace, Task<int>> _distance;
        private readonly Dictionary<(RoutePlace, RoutePlace), int> _distanceCache = new();

        public RouteSuggestionBuilder(TransitOptions options, ITransitTimeEstimator estimator, Func<RoutePlace, RoutePlace, Task<int>> distance)
        {
            _options = options;
            _estimator = estimator;
            _distance = distance;
        }

        public async Task<RouteSuggestionModel> BuildAsync(
            IReadOnlyList<RouteBookingInput> bookings, string transportMode, DateTimeOffset plannedStart, int driverCount)
        {
            var result = new RouteSuggestionModel { TransportMode = transportMode, PlannedStartDate = plannedStart };
            if (bookings.Count == 0)
            {
                return result;
            }

            bool isAir = transportMode == TransportModes.Air;
            decimal handlingHours = _options.LoadingHoursPerStop;
            int handlingMinutes = (int)Math.Round(handlingHours * 60);

            var ordered = bookings.OrderBy(b => b.PickupWindowStart).ThenBy(b => b.BookingId).ToList();
            var first = ordered[0];

            var time = plannedStart;
            int totalKm = 0;
            var current = Pickup(first);
            string currentCountry = first.PickupCountryCode;

            var pickupAt = new Dictionary<int, DateTimeOffset>();
            var kmAtPickup = new Dictionary<int, int>();
            var dropAt = new Dictionary<int, DateTimeOffset>();
            var kmAtDrop = new Dictionary<int, int>();

            // ---------- Origin ----------
            result.Checkpoints.Add(new SuggestedCheckpointModel
            {
                CheckpointName = ShortName(first.PickupAddress),
                CheckpointType = CheckpointTypes.Origin,
                Address = first.PickupAddress,
                PlannedTime = time,
                MandatoryRestMinutes = handlingMinutes
            });
            var originBookings = ordered.Where(b => Pickup(b) == current).ToList();
            foreach (var b in originBookings)
            {
                pickupAt[b.BookingId] = time;
                kmAtPickup[b.BookingId] = 0;
            }
            time = time.AddHours((double)handlingHours);

            // ---------- Các điểm đón còn lại ----------
            var pendingPickups = ordered.Except(originBookings).ToList();
            while (pendingPickups.Count > 0)
            {
                var (next, km) = await NearestAsync(current, pendingPickups, Pickup);
                var group = pendingPickups.Where(b => Pickup(b) == Pickup(next)).ToList();

                time = AddBorders(result, currentCountry, next.PickupCountryCode, null, time);
                time = Drive(result, current, Pickup(next), km, time, driverCount);
                totalKm += km;

                foreach (var b in group)
                {
                    result.Checkpoints.Add(new SuggestedCheckpointModel
                    {
                        CheckpointName = $"Pick up {b.BookingCode} - {ShortName(b.PickupAddress)}",
                        CheckpointType = CheckpointTypes.PickUp,
                        Address = b.PickupAddress,
                        PlannedTime = time,
                        MandatoryRestMinutes = handlingMinutes,
                        BookingId = b.BookingId,
                        BookingCode = b.BookingCode,
                        LegDistanceKm = b == group[0] ? km : 0
                    });
                    pickupAt[b.BookingId] = time;
                    kmAtPickup[b.BookingId] = totalKm;
                    time = time.AddHours((double)handlingHours).AddMinutes(1);
                }

                current = Pickup(next);
                currentCountry = next.PickupCountryCode;
                pendingPickups = pendingPickups.Except(group).ToList();
            }

            // ---------- Các điểm trả ----------
            var pendingDrops = ordered.ToList();
            bool firstDropLeg = true;
            while (pendingDrops.Count > 0)
            {
                var (next, km) = await NearestAsync(current, pendingDrops, Dropoff);
                var group = pendingDrops.Where(b => Dropoff(b) == Dropoff(next)).ToList();
                bool isLast = group.Count == pendingDrops.Count;

                // Đường bay: chặng từ điểm đón cuối tới điểm trả đầu tiên là chặng bay (hải quan làm ở sân bay)
                bool flightLeg = isAir && firstDropLeg;
                if (flightLeg)
                {
                    km = group.Max(b => b.DirectDistanceKm) ?? 0;
                    time = Fly(km > 0 ? km : null, time);
                }
                else
                {
                    time = AddBorders(result, currentCountry, next.DropoffCountryCode, next, time);
                    time = Drive(result, current, Dropoff(next), km, time, driverCount);
                }
                totalKm += km;
                firstDropLeg = false;

                if (isLast)
                {
                    result.Checkpoints.Add(new SuggestedCheckpointModel
                    {
                        CheckpointName = ShortName(next.DropoffAddress),
                        CheckpointType = CheckpointTypes.Destination,
                        Address = next.DropoffAddress,
                        PlannedTime = time,
                        LegDistanceKm = km
                    });
                    foreach (var b in group)
                    {
                        dropAt[b.BookingId] = time;
                        kmAtDrop[b.BookingId] = totalKm;
                    }
                    time = time.AddHours((double)handlingHours);
                }
                else
                {
                    foreach (var b in group)
                    {
                        result.Checkpoints.Add(new SuggestedCheckpointModel
                        {
                            CheckpointName = $"Drop off {b.BookingCode} - {ShortName(b.DropoffAddress)}",
                            CheckpointType = CheckpointTypes.DropOff,
                            Address = b.DropoffAddress,
                            PlannedTime = time,
                            MandatoryRestMinutes = handlingMinutes,
                            BookingId = b.BookingId,
                            BookingCode = b.BookingCode,
                            LegDistanceKm = b == group[0] ? km : 0
                        });
                        dropAt[b.BookingId] = time;
                        kmAtDrop[b.BookingId] = totalKm;
                        time = time.AddHours((double)handlingHours).AddMinutes(1);
                    }
                }

                current = Dropoff(next);
                currentCountry = next.DropoffCountryCode;
                pendingDrops = pendingDrops.Except(group).ToList();
            }

            result.SuggestedEndDate = time;
            result.TotalDistanceKm = totalKm;
            result.TotalHours = Math.Round((decimal)(time - plannedStart).TotalHours, 1);

            // ---------- Giờ giao từng đơn + cảnh báo ----------
            foreach (var b in ordered)
            {
                int routeKm = kmAtDrop[b.BookingId] - kmAtPickup[b.BookingId];
                int? directKm = b.DirectDistanceKm is > 0 ? b.DirectDistanceKm : null;
                if (directKm == null && !isAir)
                {
                    int computed = await DistanceAsync(Pickup(b), Dropoff(b));
                    directKm = computed > 0 ? computed : null;
                }
                decimal? detour = directKm is > 0 && !isAir ? Math.Round((decimal)routeKm / directKm.Value, 2) : null;

                var eta = new RouteBookingEtaModel
                {
                    BookingId = b.BookingId,
                    BookingCode = b.BookingCode,
                    PickupAt = pickupAt[b.BookingId],
                    EstimatedDeliveryAt = dropAt[b.BookingId],
                    DeliveryDeadline = b.DeliveryDeadline,
                    OnTime = dropAt[b.BookingId] <= b.DeliveryDeadline,
                    RouteDistanceKm = routeKm,
                    DirectDistanceKm = directKm,
                    DetourRatio = detour
                };
                result.Bookings.Add(eta);

                if (!eta.OnTime)
                {
                    result.Warnings.Add(Warning(PlanWarningCodes.DeadlineMissed, PlanWarningSeverities.Error, b.BookingCode,
                        $"Booking {b.BookingCode} is expected at {eta.EstimatedDeliveryAt:yyyy-MM-dd HH:mm zzz}, after its delivery deadline {b.DeliveryDeadline:yyyy-MM-dd HH:mm zzz}.",
                        ("eta", Iso(eta.EstimatedDeliveryAt)), ("deadline", Iso(b.DeliveryDeadline))));
                }

                if (eta.PickupAt < b.PickupWindowStart || eta.PickupAt >= b.PickupWindowEnd.AddDays(1))
                {
                    result.Warnings.Add(Warning(PlanWarningCodes.PickupOutsideWindow, PlanWarningSeverities.Error, b.BookingCode,
                        $"Booking {b.BookingCode} would be picked up at {eta.PickupAt:yyyy-MM-dd HH:mm zzz}, outside its pickup window {b.PickupWindowStart:yyyy-MM-dd} - {b.PickupWindowEnd:yyyy-MM-dd}.",
                        ("pickupAt", Iso(eta.PickupAt)), ("from", b.PickupWindowStart.ToString("yyyy-MM-dd")), ("to", b.PickupWindowEnd.ToString("yyyy-MM-dd"))));
                }

                if (detour is decimal ratio && ratio > _options.MaxDetourRatio)
                {
                    result.Warnings.Add(Warning(PlanWarningCodes.Detour, PlanWarningSeverities.Warning, b.BookingCode,
                        $"Booking {b.BookingCode} travels {routeKm} km on this route instead of {directKm} km direct ({ratio:0.##}x, limit {_options.MaxDetourRatio:0.##}x).",
                        ("routeKm", routeKm.ToString(CultureInfo.InvariantCulture)), ("directKm", directKm!.Value.ToString(CultureInfo.InvariantCulture)),
                        ("ratio", ratio.ToString("0.##", CultureInfo.InvariantCulture)), ("limit", _options.MaxDetourRatio.ToString("0.##", CultureInfo.InvariantCulture))));
                }
            }

            var countries = ordered.Select(b => b.PickupCountryCode).Distinct().ToList();
            if (countries.Count > 1)
            {
                result.Warnings.Add(Warning(PlanWarningCodes.DifferentPickupCountry, PlanWarningSeverities.Warning, null,
                    $"The bookings are picked up in different countries ({string.Join(", ", countries)}).",
                    ("countries", string.Join(", ", countries))));
            }

            if (!isAir)
            {
                foreach (var b in ordered.Where(b => Pickup(b) != Pickup(first)))
                {
                    int km = await DistanceAsync(Pickup(first), Pickup(b));
                    if (km > _options.MaxPickupSpreadKm)
                    {
                        result.Warnings.Add(Warning(PlanWarningCodes.PickupsFarApart, PlanWarningSeverities.Warning, b.BookingCode,
                            $"The pickup of booking {b.BookingCode} is {km} km from the origin (limit {_options.MaxPickupSpreadKm} km).",
                            ("km", km.ToString(CultureInfo.InvariantCulture)), ("limit", _options.MaxPickupSpreadKm.ToString(CultureInfo.InvariantCulture))));
                    }
                }
            }

            return result;
        }

        // ---------------------------------------------------------------------------------------------

        // Chặng bay: xe ra sân bay + có mặt trước giờ bay + bay + kiểm tra ở nơi đến + xe tới nơi nhận
        private DateTimeOffset Fly(int? flightDistanceKm, DateTimeOffset time)
        {
            var air = _estimator.Estimate(new TransitEstimateInputModel
            {
                TransportMode = TransportModes.Air,
                DistanceKm = flightDistanceKm,
                Stops = 0
            });
            return time.AddHours((double)air.TotalHours);
        }

        // Lái từ from tới to: giờ lái + nghỉ theo TransitTimeEstimator; chặng dài phải nghỉ đêm thì ghi chú để thêm mốc RestStop
        private DateTimeOffset Drive(RouteSuggestionModel result, RoutePlace from, RoutePlace to, int km, DateTimeOffset time, int driverCount)
        {
            if (km <= 0)
            {
                return time;
            }

            var estimate = _estimator.Estimate(new TransitEstimateInputModel
            {
                TransportMode = TransportModes.Ground,
                DistanceKm = km,
                BorderCrossings = 0,
                Stops = 0,
                DriverCount = Math.Max(1, driverCount)
            });

            if (estimate.OvernightStops > 0)
            {
                result.Warnings.Add(Warning(PlanWarningCodes.OvernightRest, PlanWarningSeverities.Info, null,
                    $"The leg {ShortName(from.Address)} -> {ShortName(to.Address)} ({km} km) needs {estimate.OvernightStops} overnight rest stop(s); add RestStop checkpoints for them.",
                    ("from", ShortName(from.Address)), ("to", ShortName(to.Address)), ("km", km.ToString(CultureInfo.InvariantCulture)),
                    ("count", estimate.OvernightStops.ToString(CultureInfo.InvariantCulture))));
            }

            return time.AddHours((double)estimate.TotalHours);
        }

        // Chèn mốc cửa khẩu khi đổi nước. Có đơn đích thì đi theo danh sách nước quá cảnh của đơn đó.
        private DateTimeOffset AddBorders(RouteSuggestionModel result, string fromCountry, string toCountry, RouteBookingInput? booking, DateTimeOffset time)
        {
            if (string.Equals(fromCountry, toCountry, StringComparison.OrdinalIgnoreCase))
            {
                return time;
            }

            var path = new List<string> { fromCountry, toCountry };
            if (booking != null)
            {
                var full = new List<string> { booking.PickupCountryCode };
                full.AddRange(booking.TransitCountryCodes);
                full.Add(booking.DropoffCountryCode);
                int index = full.FindIndex(c => string.Equals(c, fromCountry, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    path = full.Skip(index).ToList();
                }
            }

            for (int i = 1; i < path.Count; i++)
            {
                if (string.Equals(path[i - 1], path[i], StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                // Cửa khẩu đặt ở đầu chặng; Coordinator sửa lại tên / địa chỉ cửa khẩu thật
                time = time.AddMinutes(1);
                result.Checkpoints.Add(new SuggestedCheckpointModel
                {
                    CheckpointName = $"Border {path[i - 1]} - {path[i]}",
                    CheckpointType = CheckpointTypes.BorderGate,
                    Address = $"Border crossing {path[i - 1]} -> {path[i]}",
                    PlannedTime = time,
                    FromCountryCode = path[i - 1],
                    ToCountryCode = path[i]
                });
                time = time.AddHours((double)_options.BorderCrossingHours);
            }
            return time;
        }

        private async Task<(RouteBookingInput Booking, int Km)> NearestAsync(
            RoutePlace from, List<RouteBookingInput> candidates, Func<RouteBookingInput, RoutePlace> place)
        {
            RouteBookingInput? best = null;
            int bestKm = int.MaxValue;
            foreach (var candidate in candidates)
            {
                int km = await DistanceAsync(from, place(candidate));
                if (km < bestKm)
                {
                    best = candidate;
                    bestKm = km;
                }
            }
            return (best!, bestKm);
        }

        private async Task<int> DistanceAsync(RoutePlace from, RoutePlace to)
        {
            if (from == to)
            {
                return 0;
            }
            if (_distanceCache.TryGetValue((from, to), out int cached))
            {
                return cached;
            }
            int km = Math.Max(0, await _distance(from, to));
            _distanceCache[(from, to)] = km;
            _distanceCache[(to, from)] = km;
            return km;
        }

        private static RoutePlace Pickup(RouteBookingInput b) => new(Normalize(b.PickupAddress), b.PickupCountryCode.ToUpperInvariant());
        private static RoutePlace Dropoff(RouteBookingInput b) => new(Normalize(b.DropoffAddress), b.DropoffCountryCode.ToUpperInvariant());

        private static string Normalize(string address) => address.Trim();

        // Tên ngắn cho mốc: phần đầu của địa chỉ (trước dấu phẩy)
        private static string ShortName(string address)
        {
            string head = address.Split(',')[0].Trim();
            if (head.Length == 0)
            {
                head = address.Trim();
            }
            return head.Length > 60 ? head[..60] : head;
        }

        private static string Iso(DateTimeOffset value) => value.ToString("o", CultureInfo.InvariantCulture);

        private static PlanWarningModel Warning(string code, string severity, string? bookingCode, string message, params (string Key, string Value)[] parameters)
        {
            var warning = new PlanWarningModel { Code = code, Severity = severity, BookingCode = bookingCode, Message = message };
            foreach (var (key, value) in parameters)
            {
                warning.Params[key] = value;
            }
            if (bookingCode != null)
            {
                warning.Params["bookingCode"] = bookingCode;
            }
            return warning;
        }
    }
}
