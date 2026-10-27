using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class RecipientRulesTests
{
    private static readonly DateTimeOffset Eta = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Window_IsEtaPlusMinusOneHour()
    {
        var (start, end) = RecipientRules.NotifiedWindow(Eta);
        Assert.Equal(Eta.AddHours(-1), start);
        Assert.Equal(Eta.AddHours(1), end);
    }

    [Theory]
    [InlineData(-25, false)] // còn hơn 24 giờ
    [InlineData(-24, true)]
    [InlineData(-2, true)]
    [InlineData(0, false)]   // đã tới giờ dự kiến
    public void ConfirmRequest_Within24Hours(int hoursFromEta, bool expected)
    {
        Assert.Equal(expected, RecipientRules.NeedsConfirmRequest(Eta, null, null, Eta.AddHours(hoursFromEta)));
    }

    [Fact]
    public void ConfirmRequest_OnlyOnce_AndNotAfterConfirmation()
    {
        var now = Eta.AddHours(-5);
        Assert.False(RecipientRules.NeedsConfirmRequest(Eta, now.AddHours(-1), null, now));
        Assert.False(RecipientRules.NeedsConfirmRequest(Eta, null, now.AddHours(-1), now));
    }

    [Theory]
    [InlineData("Trần Thị B", "tran  thi b")]
    [InlineData("Đặng Văn Đức", "DANG VAN DUC")]
    [InlineData("  Nguyễn Ánh ", "nguyen anh")]
    public void Signer_MatchesRecipient_IgnoringCaseAccentsAndSpaces(string recipient, string signer)
    {
        Assert.True(RecipientRules.IsAllowedSigner(signer, recipient, null));
    }

    [Fact]
    public void Signer_OtherPerson_NeedsApprovedAlternate()
    {
        Assert.False(RecipientRules.IsAllowedSigner("Lê Văn C", "Trần Thị B", null));
        Assert.True(RecipientRules.IsAllowedSigner("Le Van C", "Trần Thị B", "Lê Văn C"));
    }

    [Fact]
    public void Signer_NoRecipientOnOldBooking_IsAllowed()
    {
        Assert.True(RecipientRules.IsAllowedSigner("Anyone", null, null));
    }
}
