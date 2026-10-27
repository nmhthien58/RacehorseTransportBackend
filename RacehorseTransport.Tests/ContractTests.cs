using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class ContractTests
{
    private static ContractDocumentModel SampleContract() => new()
    {
        ContractCode = "HD-2026-0001",
        Version = 1,
        Reason = ContractReasons.Initial,
        IssuedAt = new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero),
        PolicyVersion = "1.0",
        BookingCode = "BKG-2026-0002",
        CustomerName = "Hoàng Minh Châu",
        CustomerEmail = "minhchau.hoang@horseowner.com",
        TransportMode = TransportModes.Ground,
        PickupAddress = "Trang trại Yên Bài, Ba Vì, Hà Nội",
        PickupCountryCode = "VN",
        DropoffAddress = "Trường đua Tùng Hóa, Quảng Châu",
        DropoffCountryCode = "CN",
        TotalDistanceKm = 912.4m,
        Stops = new()
        {
            new() { Name = "Điểm đón", StopType = RouteStopTypes.Origin, Address = "Ba Vì", CountryCode = "VN" },
            new() { Name = "Cửa khẩu Hữu Nghị", StopType = RouteStopTypes.BorderGate, Address = "Đồng Đăng, Lạng Sơn" },
            new() { Name = "Điểm giao", StopType = RouteStopTypes.Destination, Address = "Quảng Châu", CountryCode = "CN" }
        },
        PickupWindowStart = new DateTimeOffset(2026, 11, 25, 0, 0, 0, TimeSpan.FromHours(7)),
        PickupWindowEnd = new DateTimeOffset(2026, 11, 30, 0, 0, 0, TimeSpan.FromHours(7)),
        DeliveryDeadline = new DateTimeOffset(2026, 12, 6, 0, 0, 0, TimeSpan.FromHours(7)),
        Horses = new() { new() { Name = "Royal Thunder", MicrochipNumber = "982000412345671", StallClass = "Shared", AgreedValue = 40000, InsurancePlan = InsurancePlans.Full } },
        Lines = new()
        {
            new() { Code = "FREIGHT_SHARED", Name = "Cước vận chuyển", Quantity = 1, UnitPrice = 900, Amount = 900 },
            new() { Code = "INS_FULL", Name = "Bảo hiểm toàn giá trị (1%) - Royal Thunder", Quantity = 1, UnitPrice = 400, Amount = 400 }
        },
        Total = 1300,
        DepositPercent = 30,
        DepositAmount = 390,
        BalanceAmount = 910,
        DepositDueAt = new DateTimeOffset(2026, 11, 4, 8, 0, 0, TimeSpan.Zero),
        BalanceDueDaysBeforeDeparture = 2,
        ExtraPaymentDueDays = 7,
        ApprovedByName = "Logistics Manager",
        ApprovedAt = new DateTimeOffset(2026, 11, 1, 7, 0, 0, TimeSpan.Zero)
    };

    private static string Policy() => PolicyDocumentText.Prepare(
        PolicyDocumentText.Load(), new Dictionary<string, string>(), "1.0");

    [Fact]
    public void Pdf_SameData_SameHash()
    {
        var first = ContractPdfRenderer.Render(SampleContract(), Policy());
        var second = ContractPdfRenderer.Render(SampleContract(), Policy());

        Assert.True(first.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(first, 0, 4));
        Assert.Equal(ContractPdfRenderer.Sha256(first), ContractPdfRenderer.Sha256(second));
        Assert.Equal(64, ContractPdfRenderer.Sha256(first).Length);
    }

    [Fact]
    public void Pdf_DifferentData_DifferentHash()
    {
        var changed = SampleContract();
        changed.Total = 1301;
        Assert.NotEqual(
            ContractPdfRenderer.Sha256(ContractPdfRenderer.Render(SampleContract(), Policy())),
            ContractPdfRenderer.Sha256(ContractPdfRenderer.Render(changed, Policy())));
    }

    [Fact]
    public void Policy_EmbeddedDocument_Loads()
    {
        string text = PolicyDocumentText.Load();
        Assert.Contains("## 12. Hợp đồng vận chuyển", text);
    }

    [Fact]
    public void Policy_Prepare_RemovesDevNotes_SetsVersion_ReplacesValues()
    {
        string source = "# CHÍNH SÁCH\n\nPhiên bản: 1.0 (bản nháp, 26/10/2026)\n\n> Ghi chú cho nhóm phát triển:\n> - bí mật\n\n"
            + "Cọc `[Payment.DepositPercent = 30]`% trong `[Booking.QuoteAcceptDays = 3]` ngày, tỉ giá `[Payment.UsdVndRate = 26.000 đ/USD]`, "
            + "phí `[Insurance.PremiumPercent = 1]`%, mức `[Unknown.Key = 7]`.\n";
        var values = new Dictionary<string, string>
        {
            ["Payment.DepositPercent"] = "40",
            ["Booking.QuoteAcceptDays"] = "5",
            ["Payment.UsdVndRate"] = "25500",
            ["Insurance.PremiumPercent"] = "1.5"
        };

        string text = PolicyDocumentText.Prepare(source, values, "1.2");

        Assert.DoesNotContain("Ghi chú cho nhóm phát triển", text);
        Assert.DoesNotContain("bí mật", text);
        Assert.Contains("Phiên bản: 1.2\n", text);
        Assert.Contains("Cọc 40% trong 5 ngày, tỉ giá 25.500 đ/USD, phí 1,5%, mức 7.", text);
        Assert.DoesNotContain("[", text);
    }

    [Fact]
    public void Policy_RealDocument_HasNoPlaceholdersLeft()
    {
        var values = SystemSettingService.ConfigValues(new PaymentSettingsModel(), new PolicySettingsModel());
        string text = PolicyDocumentText.Prepare(PolicyDocumentText.Load(), values, "1.0");
        Assert.DoesNotContain("`[", text);
        Assert.DoesNotContain("> ", text);
        Assert.Contains("26.000 đ/USD", text);
    }

    [Fact]
    public void Accept_CurrentDraftWithMatchingHash_Passes()
    {
        ContractRules.EnsureAcceptable(ContractStatuses.Draft, true, "abc123", "ABC123 ");
    }

    [Theory]
    [InlineData(ContractStatuses.Draft, true, "old-hash")]          // khách đang xem bản cũ
    [InlineData(ContractStatuses.Draft, true, null)]                 // không gửi mã băm
    [InlineData(ContractStatuses.Superseded, false, "abc123")]       // bản nháp đã bị thay
    [InlineData(ContractStatuses.Accepted, false, "abc123")]         // đã chấp nhận rồi
    public void Accept_StaleOrWrongHash_Blocked(string status, bool isCurrentDraft, string? sentHash)
    {
        var ex = Assert.Throws<BusinessException>(() => ContractRules.EnsureAcceptable(status, isCurrentDraft, "abc123", sentHash));
        Assert.Equal(ContractRules.ChangedMessage, ex.Message);
    }

    [Fact]
    public void NextVersion_AlwaysIncreases()
    {
        Assert.Equal(1, ContractRules.NextVersion(Array.Empty<int>()));
        Assert.Equal(4, ContractRules.NextVersion(new[] { 1, 3, 2 }));
    }
}
