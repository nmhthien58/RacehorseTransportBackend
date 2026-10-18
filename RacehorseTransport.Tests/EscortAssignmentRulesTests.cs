using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class EscortAssignmentRulesTests
{
    private static TripCrewInputModel Escort(int userId, bool lead = false) =>
        new() { UserId = userId, CrewRole = CrewRoles.Escort, IsLead = lead };

    private static TripCrewInputModel Driver(int userId) =>
        new() { UserId = userId, CrewRole = CrewRoles.Driver, IsLead = true };

    private static List<TripHorseInputModel> Horses(params int[] slots) =>
        slots.Select((slot, i) => new TripHorseInputModel { BookingHorseId = i + 1, StallSlotNumber = slot }).ToList();

    [Fact]
    public void Apply_FillsStallsOneToThreeForFirstEscortThenNext()
    {
        var horses = Horses(5, 1, 4, 2, 3);
        var crew = new List<TripCrewInputModel> { Driver(1), Escort(20), Escort(10, lead: true) };

        EscortAssignmentRules.Apply(horses, crew);

        // Trưởng nhóm escort (10) nhận ô 1-3, escort còn lại nhận ô 4-5
        Assert.Equal(10, horses.Single(h => h.StallSlotNumber == 1).EscortUserId);
        Assert.Equal(10, horses.Single(h => h.StallSlotNumber == 3).EscortUserId);
        Assert.Equal(20, horses.Single(h => h.StallSlotNumber == 4).EscortUserId);
        Assert.Equal(20, horses.Single(h => h.StallSlotNumber == 5).EscortUserId);
    }

    [Fact]
    public void Apply_KeepsManualChoiceAndFillsTheRest()
    {
        var horses = Horses(1, 2, 3, 4);
        horses[0].EscortUserId = 20;
        var crew = new List<TripCrewInputModel> { Escort(10, lead: true), Escort(20) };

        EscortAssignmentRules.Apply(horses, crew);

        Assert.Equal(20, horses[0].EscortUserId);
        Assert.Equal(new int?[] { 10, 10, 10 }, horses.Skip(1).Select(h => h.EscortUserId).ToArray());
    }

    [Fact]
    public void Apply_RejectsEscortNotOnCrew()
    {
        var horses = Horses(1);
        horses[0].EscortUserId = 1; // là Driver, không phải Escort
        var crew = new List<TripCrewInputModel> { Driver(1), Escort(10) };

        Assert.Throws<BusinessException>(() => EscortAssignmentRules.Apply(horses, crew));
    }

    [Fact]
    public void Apply_RejectsMoreThanThreeHorsesPerEscort()
    {
        var horses = Horses(1, 2, 3, 4);
        horses.ForEach(h => h.EscortUserId = 10);
        var crew = new List<TripCrewInputModel> { Escort(10), Escort(20) };

        Assert.Throws<BusinessException>(() => EscortAssignmentRules.Apply(horses, crew));
    }

    [Fact]
    public void Apply_RejectsWhenNotEnoughEscortsLeft()
    {
        var horses = Horses(1, 2, 3, 4);
        var crew = new List<TripCrewInputModel> { Escort(10) };

        Assert.Throws<BusinessException>(() => EscortAssignmentRules.Apply(horses, crew));
    }

    [Fact]
    public void Apply_SameUserCanDriveAndEscort()
    {
        var horses = Horses(1, 2);
        var crew = new List<TripCrewInputModel> { Driver(4), Escort(4, lead: true) };

        EscortAssignmentRules.Apply(horses, crew);

        Assert.All(horses, h => Assert.Equal(4, h.EscortUserId));
    }
}
