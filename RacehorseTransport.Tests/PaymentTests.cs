using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Implementations;
using RacehorseTransport.Services.Options;
using Xunit;

namespace RacehorseTransport.Tests;

public class VnPayGatewayTests
{
    private static readonly VnPayOptions Options = new()
    {
        TmnCode = "TESTCODE",
        HashSecret = "TESTSECRETTESTSECRETTESTSECRET12",
        PaymentUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
    };

    private static VnPayGateway CreateGateway() =>
        new(new HttpClient(), Microsoft.Extensions.Options.Options.Create(Options), NullLogger<VnPayGateway>.Instance);

    [Fact]
    public void PaymentUrl_ContainsSortedParamsAndValidSignature()
    {
        var gateway = CreateGateway();
        var now = new DateTimeOffset(2026, 10, 18, 3, 0, 0, TimeSpan.Zero);

        string url = gateway.CreatePaymentUrl(new GatewayPaymentRequest
        {
            TxnRef = "PAY20260004",
            AmountVnd = 58500000,
            OrderInfo = "Thanh toan coc don BKG-2026-0002",
            ReturnUrl = "http://localhost:5080/api/payments/vnpay-return",
            ClientIp = "127.0.0.1",
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(15)
        });

        var query = ParseQuery(url);
        Assert.Equal("5850000000", query["vnp_Amount"]);      // VNPay nhận số tiền x100
        Assert.Equal("20261018100000", query["vnp_CreateDate"]); // giờ Việt Nam (GMT+7)
        Assert.Equal("TESTCODE", query["vnp_TmnCode"]);

        // Ký lại đúng cách VNPay kiểm tra: dữ liệu quay về (không có SecureHash) phải hợp lệ
        var callback = gateway.ParseCallback(query);
        Assert.True(callback.IsSignatureValid);
        Assert.Equal("PAY20260004", callback.TxnRef);
        Assert.Equal(58500000, callback.AmountVnd);
    }

    [Fact]
    public void Callback_WithTamperedAmount_IsRejected()
    {
        var gateway = CreateGateway();
        var now = DateTimeOffset.UtcNow;
        var query = ParseQuery(gateway.CreatePaymentUrl(new GatewayPaymentRequest
        {
            TxnRef = "PAY20260005",
            AmountVnd = 100000,
            OrderInfo = "Thanh toan",
            ReturnUrl = "http://localhost/r",
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(15)
        }));

        query["vnp_Amount"] = "100"; // đổi số tiền sau khi đã ký
        Assert.False(gateway.ParseCallback(query).IsSignatureValid);
    }

    [Fact]
    public void Callback_SuccessNeedsResponseAndTransactionStatus00()
    {
        Assert.True(new GatewayCallbackData { ResponseCode = "00", TransactionStatus = "00" }.IsSuccess);
        Assert.False(new GatewayCallbackData { ResponseCode = "24", TransactionStatus = "02" }.IsSuccess);
    }

    private static Dictionary<string, string> ParseQuery(string url)
    {
        string query = url[(url.IndexOf('?') + 1)..];
        return query.Split('&')
            .Select(p => p.Split('=', 2))
            .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => Uri.UnescapeDataString(p[1].Replace('+', ' ')));
    }
}

public class PaymentSummaryTests
{
    private static readonly PaymentSettingsModel Settings = new() { UsdVndRate = 26000, BalanceDueDaysBeforeDeparture = 2 };

    // BuildSummary chỉ tính toán trên đơn đã nạp, không dùng repository / cổng thanh toán
    private static PaymentService CreateService() => new(
        null!, null!, null!, null!, null!, null!, null!, null!,
        Microsoft.Extensions.Options.Options.Create(new PaymentOptions()),
        Microsoft.Extensions.Options.Options.Create(new VnPayOptions()));

    private static BookingRequest Booking(string status, params Payment[] payments) => new()
    {
        BookingId = 1,
        Status = status,
        EstimatedCost = 10000,
        CurrencyCode = "USD",
        DepositPercent = 30,
        DepositAmount = 3000,
        Payments = payments.ToList()
    };

    private static Payment Paid(string type, decimal amount) =>
        new() { PaymentType = type, Status = PaymentStatuses.Succeeded, Amount = amount };

    [Fact]
    public void AwaitingDeposit_NextIsDeposit()
    {
        var summary = CreateService().BuildSummary(Booking(BookingStatuses.AwaitingDeposit), Settings);

        Assert.Equal(PaymentTypes.Deposit, summary.NextPaymentType);
        Assert.Equal(3000, summary.NextPaymentAmount);
        Assert.Equal(78000000, summary.NextPaymentAmountVnd);
        Assert.Equal(7000, summary.BalanceAmount);
    }

    [Fact]
    public void Approved_AfterDeposit_NextIsBalance()
    {
        var summary = CreateService().BuildSummary(
            Booking(BookingStatuses.Approved, Paid(PaymentTypes.Deposit, 3000)), Settings);

        Assert.Equal(PaymentTypes.Balance, summary.NextPaymentType);
        Assert.Equal(7000, summary.NextPaymentAmount);
        Assert.Equal(7000, summary.Outstanding);
    }

    [Fact]
    public void AwaitingFinalPayment_NextIsExtra()
    {
        var booking = Booking(BookingStatuses.AwaitingFinalPayment,
            Paid(PaymentTypes.Deposit, 3000), Paid(PaymentTypes.Balance, 7000));
        booking.ExtraCharges = 400;
        booking.ExtraDueAt = DateTimeOffset.UtcNow.AddDays(-1);

        var summary = CreateService().BuildSummary(booking, Settings);

        Assert.Equal(PaymentTypes.Extra, summary.NextPaymentType);
        Assert.Equal(400, summary.NextPaymentAmount);
        Assert.True(summary.ExtraOverdue);
    }

    [Fact]
    public void CancelledBooking_HasNothingOutstanding()
    {
        var summary = CreateService().BuildSummary(
            Booking(BookingStatuses.Cancelled, Paid(PaymentTypes.Deposit, 3000), Paid(PaymentTypes.Refund, 1500)), Settings);

        Assert.Null(summary.NextPaymentType);
        Assert.Equal(0, summary.Outstanding);
        Assert.Equal(1500, summary.RefundedAmount);
    }

    [Fact]
    public void LegacyBooking_WithoutDepositPercent_RequiresNoPayment()
    {
        var booking = Booking(BookingStatuses.Approved);
        booking.DepositPercent = null;

        var summary = CreateService().BuildSummary(booking, Settings);

        Assert.False(summary.PaymentRequired);
        Assert.Null(summary.NextPaymentType);
        Assert.Equal(0, summary.TotalDue);
    }

    [Fact]
    public void BalanceDueDate_IsBeforeFirstDeparture()
    {
        var departure = new DateTimeOffset(2026, 12, 10, 0, 0, 0, TimeSpan.Zero);
        var booking = Booking(BookingStatuses.Approved, Paid(PaymentTypes.Deposit, 3000));
        booking.BookingHorses.Add(new BookingHorse
        {
            TripHorses = { new TripHorse { Trip = new Trip { PlannedStartDate = departure, OverallStatus = TripStatuses.Scheduled } } }
        });

        var summary = CreateService().BuildSummary(booking, Settings);

        Assert.Equal(departure.AddDays(-2), summary.BalanceDueAt);
    }
}
