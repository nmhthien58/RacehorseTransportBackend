using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class IncidentReplacementRulesTests
{
    private static TransportAsset Vehicle(int id, int capacity = 4, bool climate = true, string status = AssetStatuses.Available) => new()
    {
        AssetId = id,
        AssetCode = $"TRK-{id}",
        AssetType = AssetTypes.HorseTruckAirSuspension,
        CapacityHorses = capacity,
        HasClimateControl = climate,
        Status = status
    };

    [Fact]
    public void Vehicle_AvailableWithEnoughCapacity_IsAccepted()
    {
        IncidentReplacementRules.ValidateVehicle(Vehicle(1, status: AssetStatuses.InTransit), Vehicle(2, capacity: 3), 3, true);
    }

    [Fact]
    public void Vehicle_WithInsufficientCapacity_HasClearError()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            IncidentReplacementRules.ValidateVehicle(Vehicle(1, status: AssetStatuses.InTransit), Vehicle(2, capacity: 2), 3, false));

        Assert.Contains("only 2 stall(s)", ex.Message);
        Assert.Contains("3 horse(s)", ex.Message);
    }

    [Fact]
    public void Vehicle_WithoutRequiredClimateControl_IsRejected()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            IncidentReplacementRules.ValidateVehicle(Vehicle(1, status: AssetStatuses.InTransit), Vehicle(2, climate: false), 2, true));

        Assert.Contains("climate control", ex.Message);
    }

    [Fact]
    public void Vehicle_NotAvailable_IsRejected()
    {
        Assert.Throws<BusinessException>(() =>
            IncidentReplacementRules.ValidateVehicle(Vehicle(1, status: AssetStatuses.InTransit), Vehicle(2, status: AssetStatuses.Maintenance), 2, false));
    }

    [Fact]
    public void Crew_ActiveDriverEscortWithoutConflict_IsAccepted()
    {
        var crew = new[] { new TripCrew { UserId = 10, CrewRole = CrewRoles.Escort, IsLead = true } };
        var replacement = new User { UserId = 20, FullName = "New Escort", Role = UserRoles.DriverEscort, IsActive = true };

        var removed = IncidentReplacementRules.ValidateCrew(crew, 10, replacement, CrewRoles.Escort, null);

        Assert.Equal(10, removed.UserId);
    }

    [Fact]
    public void Crew_WithOverlappingTrip_IsRejected()
    {
        var crew = new[] { new TripCrew { UserId = 10, CrewRole = CrewRoles.Driver } };
        var replacement = new User { UserId = 20, FullName = "Busy Driver", Role = UserRoles.DriverEscort, IsActive = true };

        var ex = Assert.Throws<BusinessException>(() =>
            IncidentReplacementRules.ValidateCrew(crew, 10, replacement, CrewRoles.Driver, "TRIP-OTHER"));

        Assert.Contains("TRIP-OTHER", ex.Message);
    }

    [Theory]
    [InlineData(false, UserRoles.DriverEscort)]
    [InlineData(true, UserRoles.Customer)]
    public void Crew_InactiveOrWrongRole_IsRejected(bool active, string role)
    {
        var crew = new[] { new TripCrew { UserId = 10, CrewRole = CrewRoles.Driver } };
        var replacement = new User { UserId = 20, FullName = "Invalid", Role = role, IsActive = active };

        Assert.Throws<BusinessException>(() =>
            IncidentReplacementRules.ValidateCrew(crew, 10, replacement, CrewRoles.Driver, null));
    }
}
