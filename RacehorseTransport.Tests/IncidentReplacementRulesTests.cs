using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class IncidentReplacementRulesTests
{
    private static TransportAsset Vehicle(int id, int capacity = 4, bool climate = true, string status = AssetStatuses.Available, int compartments = 2) => new()
    {
        AssetId = id,
        AssetCode = $"TRK-{id}",
        AssetType = AssetTypes.HorseTruckAirSuspension,
        CapacityHorses = capacity,
        CompartmentCount = compartments,
        HasClimateControl = climate,
        Status = status
    };

    private static List<string> Shared(int count) => Enumerable.Repeat(StallClasses.Shared, count).ToList();

    [Fact]
    public void Vehicle_AvailableWithEnoughCapacity_IsAccepted()
    {
        IncidentReplacementRules.ValidateVehicle(Vehicle(1, status: AssetStatuses.InTransit), Vehicle(2, capacity: 3), Shared(3), true);
    }

    [Fact]
    public void Vehicle_WithInsufficientCapacity_HasClearError()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            IncidentReplacementRules.ValidateVehicle(Vehicle(1, status: AssetStatuses.InTransit), Vehicle(2, capacity: 2), Shared(3), false));

        Assert.Contains("only 2 stall(s)", ex.Message);
        Assert.Contains("3 horse(s)", ex.Message);
    }

    [Fact]
    public void Vehicle_WithTooFewCompartments_HasClearError()
    {
        // 2 Private + 1 Comfort cần 3 khoang, xe thay thế chỉ có 2
        var classes = new List<string> { StallClasses.Private, StallClasses.Private, StallClasses.Comfort };

        var ex = Assert.Throws<BusinessException>(() =>
            IncidentReplacementRules.ValidateVehicle(Vehicle(1, status: AssetStatuses.InTransit), Vehicle(2, capacity: 6, compartments: 2), classes, false));

        Assert.Contains("has 2 compartment(s)", ex.Message);
        Assert.Contains("need 3", ex.Message);
    }

    [Fact]
    public void Vehicle_WithoutRequiredClimateControl_IsRejected()
    {
        var ex = Assert.Throws<BusinessException>(() =>
            IncidentReplacementRules.ValidateVehicle(Vehicle(1, status: AssetStatuses.InTransit), Vehicle(2, climate: false), Shared(2), true));

        Assert.Contains("climate control", ex.Message);
    }

    [Fact]
    public void Vehicle_NotAvailable_IsRejected()
    {
        Assert.Throws<BusinessException>(() =>
            IncidentReplacementRules.ValidateVehicle(Vehicle(1, status: AssetStatuses.InTransit), Vehicle(2, status: AssetStatuses.Maintenance), Shared(2), false));
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
