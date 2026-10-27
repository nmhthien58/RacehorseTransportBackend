using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class HorseEligibilityRulesTests
{
    // Mặc định theo chính sách: 6 tháng, thai kỳ 340 ngày, 90% -> 34 ngày trước ngày dự sinh, 7 ngày sau khi đẻ
    private static readonly PolicySettingsModel Settings = new();

    private static readonly DateOnly WindowStart = new(2026, 11, 1);
    private static readonly DateOnly WindowEnd = new(2026, 11, 10);

    private static Horse Horse(DateOnly? born = null, string gender = HorseGenders.Mare) => new()
    {
        Name = "Sakura Wind",
        Gender = gender,
        DateOfBirth = born ?? new DateOnly(2020, 1, 1)
    };

    private static List<string> Problems(Horse horse) =>
        HorseEligibilityRules.Problems(horse, WindowStart, WindowEnd, Settings);

    [Fact]
    public void LatePregnancyDays_DefaultIs34()
    {
        Assert.Equal(34, HorseEligibilityRules.LatePregnancyDays(Settings));
    }

    [Fact]
    public void AdultHorse_IsEligible()
    {
        Assert.Empty(Problems(Horse()));
    }

    // ---------- Tuổi tối thiểu (tính tới ngày cuối khoảng đón: 2026-11-10) ----------

    [Fact]
    public void Age_ExactlySixMonthsOnLastPickupDay_IsEligible()
    {
        Assert.Empty(Problems(Horse(born: new DateOnly(2026, 5, 10))));
    }

    [Fact]
    public void Age_OneDayShortOfSixMonths_IsBlocked()
    {
        var problems = Problems(Horse(born: new DateOnly(2026, 5, 11)));

        Assert.Contains(problems, p => p.Contains("younger than 6 months"));
    }

    [Fact]
    public void Age_OlderThanSixMonths_IsEligible()
    {
        Assert.Empty(Problems(Horse(born: new DateOnly(2026, 5, 9))));
    }

    [Fact]
    public void Age_UsesConfiguredMinimum()
    {
        var settings = new PolicySettingsModel { HorseMinAgeMonths = 12 };

        var problems = HorseEligibilityRules.Problems(Horse(born: new DateOnly(2026, 1, 1)), WindowStart, WindowEnd, settings);

        Assert.Contains(problems, p => p.Contains("younger than 12 months"));
    }

    // ---------- Mang thai (ngày cuối khoảng đón phải trước ngày dự sinh - 34 ngày) ----------

    private static Horse Pregnant(DateOnly due)
    {
        var horse = Horse();
        horse.IsPregnant = true;
        horse.ExpectedFoalingDate = due;
        return horse;
    }

    [Fact]
    public void Pregnancy_LastPickupDayOneDayBeforeLimit_IsEligible()
    {
        // giới hạn = 2026-12-15 - 34 = 2026-11-11; ngày cuối khoảng đón 2026-11-10 < 2026-11-11
        Assert.Empty(Problems(Pregnant(new DateOnly(2026, 12, 15))));
    }

    [Fact]
    public void Pregnancy_LastPickupDayOnLimit_IsBlocked()
    {
        // giới hạn = 2026-12-14 - 34 = 2026-11-10 = ngày cuối khoảng đón
        var problems = Problems(Pregnant(new DateOnly(2026, 12, 14)));

        Assert.Contains(problems, p => p.Contains("due to foal on 2026-12-14"));
    }

    [Fact]
    public void Pregnancy_PastLimit_IsBlocked()
    {
        Assert.NotEmpty(Problems(Pregnant(new DateOnly(2026, 11, 20))));
    }

    [Fact]
    public void Pregnancy_UsesConfiguredPercent()
    {
        // 80% -> 68 ngày trước ngày dự sinh: 2026-12-15 - 68 = 2026-10-08, ngày cuối khoảng đón đã qua -> chặn
        var settings = new PolicySettingsModel { HorseLatePregnancyPercent = 80 };

        var problems = HorseEligibilityRules.Problems(Pregnant(new DateOnly(2026, 12, 15)), WindowStart, WindowEnd, settings);

        Assert.NotEmpty(problems);
    }

    [Fact]
    public void NotPregnant_FoalingDateIgnored()
    {
        var horse = Horse();
        horse.ExpectedFoalingDate = new DateOnly(2026, 11, 5);

        Assert.Empty(Problems(horse));
    }

    // ---------- Vừa đẻ (ngày đầu khoảng đón phải cách lần đẻ >= 7 ngày) ----------

    private static Horse Foaled(DateOnly foaled)
    {
        var horse = Horse();
        horse.LastFoalingDate = foaled;
        return horse;
    }

    [Fact]
    public void Foaling_ExactlySevenDaysBeforeWindowStart_IsEligible()
    {
        Assert.Empty(Problems(Foaled(new DateOnly(2026, 10, 25))));
    }

    [Fact]
    public void Foaling_SixDaysBeforeWindowStart_IsBlocked()
    {
        var problems = Problems(Foaled(new DateOnly(2026, 10, 26)));

        Assert.Contains(problems, p => p.Contains("can travel from 2026-11-02"));
    }

    [Fact]
    public void Foaling_LongAgo_IsEligible()
    {
        Assert.Empty(Problems(Foaled(new DateOnly(2025, 3, 1))));
    }

    // ---------- Thông báo gộp ----------

    [Fact]
    public void Describe_ListsEveryBlockedHorseWithReason()
    {
        var young = Horse(born: new DateOnly(2026, 8, 1));
        young.Name = "Little Star";
        var due = Pregnant(new DateOnly(2026, 11, 30));
        due.Name = "Midnight Crown";
        var ok = Horse();
        ok.Name = "Royal Thunder";

        string? message = HorseEligibilityRules.Describe(new[] { young, due, ok }, WindowStart, WindowEnd, Settings);

        Assert.NotNull(message);
        Assert.Contains("Little Star", message);
        Assert.Contains("Midnight Crown", message);
        Assert.DoesNotContain("Royal Thunder", message);
    }

    [Fact]
    public void Describe_AllEligible_ReturnsNull()
    {
        Assert.Null(HorseEligibilityRules.Describe(new[] { Horse() }, WindowStart, WindowEnd, Settings));
    }
}
