using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class CompartmentRulesTests
{
    private static TransportAsset Vehicle(int capacity = 9, int compartments = 3) => new()
    {
        AssetId = 1,
        AssetCode = "29H-112.45",
        AssetType = AssetTypes.HorseTruckAirSuspension,
        CapacityHorses = capacity,
        CompartmentCount = compartments
    };

    private static CompartmentPlacement Horse(string name, string stallClass, int compartment, int position) =>
        new(name, stallClass, compartment, position);

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, 0, 0, 1)]
    [InlineData(3, 0, 0, 1)]
    [InlineData(4, 0, 0, 2)]
    [InlineData(0, 1, 0, 1)]
    [InlineData(0, 3, 0, 2)]
    [InlineData(0, 0, 2, 2)]
    [InlineData(4, 3, 1, 5)]
    public void RequiredCompartments_IsCeilSharedOver3PlusCeilComfortOver2PlusPrivate(int shared, int comfort, int @private, int expected)
    {
        var classes = Enumerable.Repeat(StallClasses.Shared, shared)
            .Concat(Enumerable.Repeat(StallClasses.Comfort, comfort))
            .Concat(Enumerable.Repeat(StallClasses.Private, @private));

        Assert.Equal(expected, CompartmentRules.RequiredCompartments(classes));
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(6, 2)]
    [InlineData(8, 3)]
    [InlineData(9, 3)]
    [InlineData(12, 4)]
    public void DefaultCompartmentCount_IsCeilCapacityOver3(int capacity, int expected)
    {
        Assert.Equal(expected, CompartmentRules.DefaultCompartmentCount(capacity));
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(1, 3, 3)]
    [InlineData(2, 1, 4)]
    [InlineData(4, 3, 12)]
    public void StallSlot_ConvertsBothWays(int compartment, int position, int slot)
    {
        Assert.Equal(slot, CompartmentRules.ToStallSlot(compartment, position));
        Assert.Equal((compartment, position), CompartmentRules.FromStallSlot(slot));
    }

    [Fact]
    public void Validate_OneClassPerCompartmentWithinLimits_IsAccepted()
    {
        var horses = new List<CompartmentPlacement>
        {
            Horse("A", StallClasses.Shared, 1, 1),
            Horse("B", StallClasses.Shared, 1, 2),
            Horse("C", StallClasses.Shared, 1, 3),
            Horse("D", StallClasses.Comfort, 2, 1),
            Horse("E", StallClasses.Comfort, 2, 3),
            Horse("F", StallClasses.Private, 3, 2)
        };

        CompartmentRules.Validate(Vehicle(), horses);
    }

    [Fact]
    public void Validate_MixedClassesInOneCompartment_NamesTheHorse()
    {
        var horses = new List<CompartmentPlacement>
        {
            Horse("Sao Mai", StallClasses.Comfort, 2, 1),
            Horse("Kim Ngưu", StallClasses.Private, 2, 2)
        };

        var ex = Assert.Throws<BusinessException>(() => CompartmentRules.Validate(Vehicle(), horses));

        Assert.Equal("Compartment 2 holds Comfort horses; Kim Ngưu (Private) cannot share it.", ex.Message);
    }

    [Fact]
    public void Validate_TooManyHorsesForTheClass_IsRejected()
    {
        var horses = new List<CompartmentPlacement>
        {
            Horse("A", StallClasses.Comfort, 1, 1),
            Horse("B", StallClasses.Comfort, 1, 2),
            Horse("C", StallClasses.Comfort, 1, 3)
        };

        var ex = Assert.Throws<BusinessException>(() => CompartmentRules.Validate(Vehicle(), horses));

        Assert.Contains("Compartment 1 has 3 Comfort horses", ex.Message);
        Assert.Contains("at most 2", ex.Message);
    }

    [Fact]
    public void Validate_NotEnoughCompartments_IsRejected()
    {
        // 4 Private cần 4 khoang, xe có 3
        var horses = Enumerable.Range(1, 4)
            .Select(i => Horse($"H{i}", StallClasses.Private, Math.Min(i, 3), 1))
            .ToList();

        var ex = Assert.Throws<BusinessException>(() => CompartmentRules.Validate(Vehicle(), horses));

        Assert.StartsWith("Vehicle 29H-112.45 has 3 compartment(s) but these horses need 4", ex.Message);
    }

    [Fact]
    public void Validate_CompartmentBeyondVehicle_IsRejected()
    {
        var horses = new List<CompartmentPlacement> { Horse("A", StallClasses.Shared, 4, 1) };

        var ex = Assert.Throws<BusinessException>(() => CompartmentRules.Validate(Vehicle(), horses));

        Assert.Contains("compartments 1 to 3", ex.Message);
    }

    [Fact]
    public void Validate_SamePositionTwice_IsRejected()
    {
        var horses = new List<CompartmentPlacement>
        {
            Horse("A", StallClasses.Shared, 1, 2),
            Horse("B", StallClasses.Shared, 1, 2)
        };

        var ex = Assert.Throws<BusinessException>(() => CompartmentRules.Validate(Vehicle(), horses));

        Assert.Equal("A and B cannot share position 2 of compartment 1.", ex.Message);
    }

    [Fact]
    public void Validate_MoreHorsesThanCapacity_IsRejected()
    {
        // Xe 8 chỗ có 3 khoang (9 vị trí) nhưng chỉ chở tối đa 8 con
        var horses = Enumerable.Range(1, 9)
            .Select(i => Horse($"H{i}", StallClasses.Shared, (i - 1) / 3 + 1, (i - 1) % 3 + 1))
            .ToList();

        var ex = Assert.Throws<BusinessException>(() => CompartmentRules.Validate(Vehicle(capacity: 8), horses));

        Assert.Contains("carries at most 8 horse(s)", ex.Message);
    }

    [Fact]
    public void Pack_FillsEachClassIntoItsOwnCompartments()
    {
        var classes = new List<string>
        {
            StallClasses.Comfort, StallClasses.Shared, StallClasses.Comfort,
            StallClasses.Comfort, StallClasses.Private, StallClasses.Shared
        };

        var places = CompartmentRules.Pack(classes);

        // Comfort: khoang 1 (2 con) + khoang 2; Shared: khoang 3; Private: khoang 4
        Assert.Equal(new[] { (1, 1), (3, 1), (1, 2), (2, 1), (4, 1), (3, 2) }, places);
    }

    [Fact]
    public void Pack_StartsFromTheGivenCompartment()
    {
        var places = CompartmentRules.Pack(new List<string> { StallClasses.Private }, firstCompartment: 3);

        Assert.Equal(new[] { (3, 1) }, places);
    }
}
