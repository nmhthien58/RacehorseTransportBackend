using Microsoft.Extensions.Options;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    // Ước tính thời gian vận chuyển (R-08). Chỉ tính toán, không gọi DB hay API ngoài, nên dễ kiểm thử.
    //   Đường bộ: giờ lái = km / tốc độ xe tải; cứ MaxDrivingHoursBeforeBreak giờ nghỉ BreakMinutes phút;
    //             hết giờ lái trong ngày thì nghỉ qua đêm; + giờ qua cửa khẩu + giờ xếp / dỡ ngựa.
    //   Đường bay: xe ra sân bay + có mặt trước giờ bay + giờ bay + kiểm tra ở nơi đến + xe tới nơi nhận.
    // Tham số nằm ở TransitOptions (section "Transit").
    public class TransitTimeEstimator : ITransitTimeEstimator
    {
        private const decimal Epsilon = 0.0001m;

        private readonly TransitOptions _options;

        public TransitTimeEstimator(IOptions<TransitOptions> options)
        {
            _options = options.Value;
        }

        public TransitEstimateModel Estimate(TransitEstimateInputModel input)
        {
            var result = input.TransportMode switch
            {
                TransportModes.Ground => EstimateGround(input),
                TransportModes.Air => EstimateAir(input),
                _ => throw new BusinessException($"TransportMode '{input.TransportMode}' is invalid. Allowed values: {string.Join(", ", TransportModes.All)}.")
            };

            result.TotalHours = Round(result.DrivingHours + result.RestHours + result.BorderHours + result.HandlingHours + result.FlightHours);
            result.MaxHours = Round(result.TotalHours * (1 + _options.BufferPercent / 100));
            result.TransitDays = Math.Max(1, (int)Math.Ceiling(result.TotalHours / 24));
            return result;
        }

        private TransitEstimateModel EstimateGround(TransitEstimateInputModel input)
        {
            if (input.DistanceKm is null or <= 0)
            {
                throw new BusinessException("DistanceKm is required to estimate ground transit time.");
            }

            decimal drivingHours = input.DistanceKm.Value / _options.TruckAverageSpeedKmh;
            decimal dailyLimit = input.DriverCount >= 2
                ? _options.MaxDrivingHoursPerDayTeam
                : _options.MaxDrivingHoursPerDaySingleDriver;
            decimal breakHours = _options.BreakMinutes / 60m;

            // Đi từng đoạn lái: mỗi đoạn dừng lại khi tới giờ nghỉ giữa chặng hoặc hết giờ lái trong ngày
            decimal remaining = drivingHours;
            decimal leftToday = dailyLimit;
            decimal sinceBreak = 0;
            decimal restHours = 0;
            int overnightStops = 0;

            while (remaining > Epsilon)
            {
                decimal chunk = Math.Min(remaining, Math.Min(_options.MaxDrivingHoursBeforeBreak - sinceBreak, leftToday));
                remaining -= chunk;
                leftToday -= chunk;
                sinceBreak += chunk;

                if (remaining <= Epsilon)
                {
                    break;
                }

                if (leftToday <= Epsilon)
                {
                    restHours += _options.OvernightRestHours;
                    overnightStops++;
                    leftToday = dailyLimit;
                    sinceBreak = 0;
                }
                else if (sinceBreak >= _options.MaxDrivingHoursBeforeBreak - Epsilon)
                {
                    restHours += breakHours;
                    sinceBreak = 0;
                }
            }

            return new TransitEstimateModel
            {
                DrivingHours = Round(drivingHours),
                RestHours = Round(restHours),
                OvernightStops = overnightStops,
                BorderHours = Round(Math.Max(0, input.BorderCrossings) * _options.BorderCrossingHours),
                HandlingHours = Round(Math.Max(0, input.Stops) * _options.LoadingHoursPerStop)
            };
        }

        private TransitEstimateModel EstimateAir(TransitEstimateInputModel input)
        {
            decimal flightHours = input.DistanceKm is > 0
                ? input.DistanceKm.Value / _options.AirCruiseSpeedKmh
                : _options.AirDefaultFlightHours;

            return new TransitEstimateModel
            {
                // Hai chặng xe: trang trại -> sân bay đi, sân bay đến -> nơi nhận
                DrivingHours = Round(2 * _options.AirGroundTransferHours),
                HandlingHours = Round(Math.Max(0, input.Stops) * _options.LoadingHoursPerStop),
                FlightHours = Round(_options.AirCheckInHours + flightHours + _options.AirArrivalInspectionHours)
            };
        }

        private static decimal Round(decimal value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);
    }
}
