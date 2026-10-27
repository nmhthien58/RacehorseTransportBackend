using RacehorseTransport.Services.Implementations;
using Xunit;
using A = RacehorseTransport.Services.Implementations.DocumentDeadlineRules.Action;

namespace RacehorseTransport.Tests;

public class DocumentDeadlineRulesTests
{
    private static readonly DateTimeOffset Due = new(2026, 11, 10, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DueAt_SubmitDaysFromDeposit()
    {
        var paid = new DateTimeOffset(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);
        Assert.Equal(Due, DocumentDeadlineRules.DueAt(paid, 7));
    }

    [Theory]
    [InlineData(-96, 0, A.None)]            // còn 4 ngày
    [InlineData(-72, 0, A.FirstReminder)]   // còn đúng 3 ngày
    [InlineData(-50, 1, A.None)]            // đã nhắc lần 1, còn hơn 1 ngày
    [InlineData(-24, 1, A.SecondReminder)]  // còn 1 ngày
    [InlineData(-20, 0, A.SecondReminder)]  // job dừng lâu: bỏ qua lần 1, nhắc lần 2 luôn
    [InlineData(-1, 2, A.None)]             // đã nhắc đủ 2 lần
    [InlineData(0, 2, A.Expire)]            // đúng hạn
    [InlineData(5, 0, A.Expire)]            // quá hạn
    [InlineData(5, 3, A.None)]              // quá hạn nhưng đã báo Manager (ngựa đã lên chuyến)
    public void Decide(int hoursFromDue, int remindersSent, A expected)
    {
        Assert.Equal(expected, DocumentDeadlineRules.Decide(Due, remindersSent, Due.AddHours(hoursFromDue)));
    }

    [Fact]
    public void RouteChange_ExtendsButNeverShortens()
    {
        var now = new DateTimeOffset(2026, 11, 8, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(now.AddDays(7), DocumentDeadlineRules.ExtendedDueAt(Due, now, 7));
        var later = now.AddDays(30);
        Assert.Equal(later, DocumentDeadlineRules.ExtendedDueAt(later, now, 7));
        Assert.Equal(now.AddDays(7), DocumentDeadlineRules.ExtendedDueAt(null, now, 7));
    }

    [Fact]
    public void NewCountries_OnlyAddedOnes_InNewOrder()
    {
        Assert.Equal(new[] { "LA", "CN" }, DocumentDeadlineRules.NewCountries(new[] { "KH" }, new[] { "LA", "KH", "CN" }));
        Assert.Empty(DocumentDeadlineRules.NewCountries(new[] { "LA", "KH" }, new[] { "KH" }));
        Assert.Empty(DocumentDeadlineRules.NewCountries(Array.Empty<string>(), Array.Empty<string>()));
    }
}
