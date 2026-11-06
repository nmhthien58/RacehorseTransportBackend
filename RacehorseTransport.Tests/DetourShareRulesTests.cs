using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class DetourShareRulesTests
{
    [Theory]
    [InlineData(180, 2, 3, 120)]
    [InlineData(180, 1, 3, 60)]
    [InlineData(100, 1, 3, 33.33)]
    [InlineData(180, 3, 3, 180)]
    public void Share_IsProportionalToHorsesOnBoard(decimal total, int mine, int onBoard, decimal expected)
    {
        Assert.Equal(expected, DetourShareRules.Share(total, mine, onBoard));
    }

    [Theory]
    [InlineData(0, 1, 3)]
    [InlineData(180, 0, 3)]
    [InlineData(180, 1, 0)]
    public void Share_IsZero_WhenNothingToShare(decimal total, int mine, int onBoard)
    {
        Assert.Equal(0m, DetourShareRules.Share(total, mine, onBoard));
    }
}
