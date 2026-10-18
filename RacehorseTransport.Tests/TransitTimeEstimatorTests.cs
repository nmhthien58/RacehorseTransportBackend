using Microsoft.Extensions.Options;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using RacehorseTransport.Services.Options;
using Xunit;

namespace RacehorseTransport.Tests;

// Kiểm tra công thức ước tính thời gian với tham số mặc định:
// xe 55 km/h, lái 4,5 giờ nghỉ 45 phút, 1 tài xế lái tối đa 9 giờ/ngày, nghỉ đêm 10 giờ,
// qua cửa khẩu 6 giờ, xếp / dỡ 1 giờ mỗi điểm, biên an toàn 15%.
public class TransitTimeEstimatorTests
{
    private static readonly TransitTimeEstimator Estimator = new(Options.Create(new TransitOptions()));

    [Fact]
    public void Ground_ShortTrip_NoBreak()
    {
        // 220 km = 4 giờ lái, chưa tới 4,5 giờ nên không nghỉ
        var result = Estimator.Estimate(Ground(220));

        Assert.Equal(4m, result.DrivingHours);
        Assert.Equal(0m, result.RestHours);
        Assert.Equal(0, result.OvernightStops);
        Assert.Equal(2m, result.HandlingHours);
        Assert.Equal(6m, result.TotalHours);
        Assert.Equal(6.9m, result.MaxHours);
        Assert.Equal(1, result.TransitDays);
    }

    [Fact]
    public void Ground_OneDay_TakesOneBreak()
    {
        // 495 km = 9 giờ lái: lái 4,5 -> nghỉ 45 phút -> lái 4,5 là hết
        var result = Estimator.Estimate(Ground(495));

        Assert.Equal(9m, result.DrivingHours);
        Assert.Equal(0.8m, result.RestHours); // 0,75 làm tròn 1 chữ số
        Assert.Equal(0, result.OvernightStops);
    }

    [Fact]
    public void Ground_LongTrip_SingleDriverRestsOvernight()
    {
        // 1100 km = 20 giờ lái: ngày 1 lái 9 (1 lần nghỉ), nghỉ đêm; ngày 2 lái 9 (1 lần nghỉ), nghỉ đêm; ngày 3 lái 2
        var result = Estimator.Estimate(Ground(1100, borders: 1));

        Assert.Equal(20m, result.DrivingHours);
        Assert.Equal(2, result.OvernightStops);
        Assert.Equal(21.5m, result.RestHours); // 2 x 10 + 2 x 0,75
        Assert.Equal(6m, result.BorderHours);
        Assert.Equal(49.5m, result.TotalHours); // 20 + 21,5 + 6 + 2
        Assert.Equal(3, result.TransitDays);
    }

    [Fact]
    public void Ground_TwoDrivers_AreFaster()
    {
        var single = Estimator.Estimate(Ground(1100));
        var team = Estimator.Estimate(Ground(1100, drivers: 2));

        Assert.True(team.TotalHours < single.TotalHours);
        Assert.Equal(1, team.OvernightStops); // 18 giờ lái/ngày
    }

    [Fact]
    public void Ground_WithoutDistance_Throws()
    {
        Assert.Throws<BusinessException>(() => Estimator.Estimate(new TransitEstimateInputModel
        {
            TransportMode = TransportModes.Ground
        }));
    }

    [Fact]
    public void Air_UsesDefaultFlightHoursWhenDistanceUnknown()
    {
        var result = Estimator.Estimate(new TransitEstimateInputModel { TransportMode = TransportModes.Air });

        Assert.Equal(8m, result.DrivingHours);  // 2 chặng xe ra / vào sân bay
        Assert.Equal(18m, result.FlightHours);  // có mặt trước 6 + bay 8 + kiểm tra 4
        Assert.Equal(28m, result.TotalHours);   // + xếp / dỡ 2
        Assert.Equal(2, result.TransitDays);
    }

    private static TransitEstimateInputModel Ground(int km, int borders = 0, int drivers = 1)
    {
        return new TransitEstimateInputModel
        {
            TransportMode = TransportModes.Ground,
            DistanceKm = km,
            BorderCrossings = borders,
            DriverCount = drivers
        };
    }
}
