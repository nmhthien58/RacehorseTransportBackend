using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Implementations;
using Xunit;

namespace RacehorseTransport.Tests;

public class PaymentListRulesTests
{
    private static Payment Pay(int id, string type, string status, decimal amount, DateTimeOffset createdAt,
        string bookingCode = "BKG-2026-0001", string customer = "Nguyen Van A") => new()
    {
        PaymentId = id,
        PaymentType = type,
        Status = status,
        Amount = amount,
        CreatedAt = createdAt,
        Booking = new BookingRequest { BookingCode = bookingCode, Customer = new User { FullName = customer } }
    };

    private static readonly DateTimeOffset Base = new(2026, 10, 10, 5, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreatedRange_UsesVietnamDaysInclusive()
    {
        var (from, to) = PaymentListRules.CreatedRange(new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 10));
        // 00:00 giờ VN ngày 10 = 17:00 UTC ngày 9; hết ngày 10 giờ VN = 17:00 UTC ngày 10
        Assert.Equal(new DateTimeOffset(2026, 10, 9, 17, 0, 0, TimeSpan.Zero), from);
        Assert.Equal(new DateTimeOffset(2026, 10, 10, 17, 0, 0, TimeSpan.Zero), to);
    }

    [Fact]
    public void CreatedRange_FromAfterTo_Throws()
    {
        Assert.Throws<BusinessException>(() => PaymentListRules.CreatedRange(new DateOnly(2026, 10, 11), new DateOnly(2026, 10, 10)));
    }

    [Fact]
    public void Filter_ByDate_IncludesWholeVietnamDay()
    {
        var payments = new[]
        {
            Pay(1, PaymentTypes.Deposit, PaymentStatuses.Succeeded, 100, new DateTimeOffset(2026, 10, 9, 16, 59, 0, TimeSpan.Zero)), // 23:59 VN ngày 9
            Pay(2, PaymentTypes.Deposit, PaymentStatuses.Succeeded, 100, new DateTimeOffset(2026, 10, 9, 17, 0, 0, TimeSpan.Zero)),  // 00:00 VN ngày 10
            Pay(3, PaymentTypes.Deposit, PaymentStatuses.Succeeded, 100, new DateTimeOffset(2026, 10, 10, 16, 59, 0, TimeSpan.Zero)), // 23:59 VN ngày 10
            Pay(4, PaymentTypes.Deposit, PaymentStatuses.Succeeded, 100, new DateTimeOffset(2026, 10, 10, 17, 0, 0, TimeSpan.Zero))  // 00:00 VN ngày 11
        }.AsQueryable();

        var ids = PaymentListRules.ApplyFilter(payments, new PaymentFilterOptions { From = new DateOnly(2026, 10, 10), To = new DateOnly(2026, 10, 10) })
            .Select(p => p.PaymentId).ToList();

        Assert.Equal(new[] { 2, 3 }, ids);
    }

    [Fact]
    public void Filter_ByTypeStatusAndSearch()
    {
        var payments = new[]
        {
            Pay(1, PaymentTypes.Refund, PaymentStatuses.Failed, 50, Base, "BKG-2026-0007", "Tran Thi B"),
            Pay(2, PaymentTypes.Refund, PaymentStatuses.Succeeded, 50, Base, "BKG-2026-0007", "Tran Thi B"),
            Pay(3, PaymentTypes.Deposit, PaymentStatuses.Failed, 50, Base, "BKG-2026-0008", "Le Van C"),
            Pay(4, PaymentTypes.Refund, PaymentStatuses.Failed, 50, Base, "BKG-2026-0009", "Pham D")
        }.AsQueryable();

        var byType = PaymentListRules.ApplyFilter(payments, new PaymentFilterOptions { Type = PaymentTypes.Refund, Status = PaymentStatuses.Failed });
        Assert.Equal(new[] { 1, 4 }, byType.Select(p => p.PaymentId).ToArray());

        var byName = PaymentListRules.ApplyFilter(payments, new PaymentFilterOptions { Search = " tran thi " });
        Assert.Equal(new[] { 1, 2 }, byName.Select(p => p.PaymentId).ToArray());

        var byCode = PaymentListRules.ApplyFilter(payments, new PaymentFilterOptions { Search = "bkg-2026-0008" });
        Assert.Equal(3, Assert.Single(byCode).PaymentId);
    }

    [Theory]
    [InlineData("Cash", null)]
    [InlineData(null, "Done")]
    public void Filter_InvalidTypeOrStatus_Throws(string? type, string? status)
    {
        Assert.Throws<BusinessException>(() =>
            PaymentListRules.ApplyFilter(Array.Empty<Payment>().AsQueryable(), new PaymentFilterOptions { Type = type, Status = status }));
    }

    [Fact]
    public void Summarize_SeparatesChargesFromRefunds()
    {
        var summary = PaymentListRules.Summarize(new[]
        {
            Pay(1, PaymentTypes.Deposit, PaymentStatuses.Succeeded, 300, Base),
            Pay(2, PaymentTypes.Balance, PaymentStatuses.Succeeded, 700, Base),
            Pay(3, PaymentTypes.Extra, PaymentStatuses.Pending, 120, Base),
            Pay(4, PaymentTypes.Deposit, PaymentStatuses.Failed, 300, Base),
            Pay(5, PaymentTypes.Balance, PaymentStatuses.Expired, 700, Base),
            Pay(6, PaymentTypes.Refund, PaymentStatuses.Succeeded, 150, Base),
            Pay(7, PaymentTypes.Refund, PaymentStatuses.Failed, 80, Base),
            Pay(8, PaymentTypes.Refund, PaymentStatuses.Pending, 20, Base),
            Pay(9, PaymentTypes.Compensation, PaymentStatuses.Succeeded, 500, Base)
        });

        Assert.Equal(2, summary.SucceededCount);
        Assert.Equal(1000m, summary.SucceededAmount);
        Assert.Equal(1, summary.PendingCount);
        Assert.Equal(120m, summary.PendingAmount);
        Assert.Equal(2, summary.FailedCount);   // cọc thất bại + dòng hoàn thất bại
        Assert.Equal(1, summary.ExpiredCount);
        Assert.Equal(150m, summary.RefundedAmount);
        Assert.Equal(2, summary.RefundsNeedingActionCount);
        Assert.Equal("USD", summary.CurrencyCode);
    }

    [Fact]
    public void Summarize_Empty_ReturnsZeros()
    {
        var summary = PaymentListRules.Summarize(Array.Empty<Payment>());
        Assert.Equal(0, summary.SucceededCount);
        Assert.Equal(0m, summary.RefundedAmount);
        Assert.Equal(0, summary.RefundsNeedingActionCount);
    }
}
