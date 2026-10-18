using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RacehorseTransport.API.Helpers;
using RacehorseTransport.API.Mappers;
using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.API.Controllers
{
    // R-02 Thanh toán: đặt cọc, phần còn lại, chi phí phát sinh, hoàn tiền; cấu hình % cọc / tỉ giá
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private const string ManagerRoles = UserRoles.LogisticsManager + "," + UserRoles.Admin;

        private readonly IPaymentService _paymentService;
        private readonly ISystemSettingService _settingService;
        private readonly EmailOptions _emailOptions;

        public PaymentsController(IPaymentService paymentService, ISystemSettingService settingService, IOptions<EmailOptions> emailOptions)
        {
            _paymentService = paymentService;
            _settingService = settingService;
            _emailOptions = emailOptions.Value;
        }

        /// <summary>
        /// Payment status of a booking: amounts due / paid, the next payment the customer can make, and the payment history.
        /// </summary>
        [HttpGet("bookings/{id:int}/payments")]
        [ProducesResponseType(typeof(ApiResponse<BookingPaymentSummaryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBookingPayments(int id)
        {
            var summary = await _paymentService.GetBookingPaymentsAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<BookingPaymentSummaryResponse>.SuccessResult(summary.ToResponse(), "Payments retrieved successfully"));
        }

        /// <summary>
        /// Customer billing page: payment status of every booking of the current customer, totals, and all transactions.
        /// </summary>
        [HttpGet("payments/my")]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<CustomerBillingResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyBilling()
        {
            var billing = await _paymentService.GetMyBillingAsync(User.ToCurrentUser());
            return Ok(ApiResponse<CustomerBillingResponse>.SuccessResult(billing.ToResponse(), "Billing retrieved successfully"));
        }

        /// <summary>
        /// Customer starts a payment (Deposit / Balance / Extra) and gets the VNPay (or mock) payment URL to open.
        /// </summary>
        [HttpPost("bookings/{id:int}/payments")]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<CreatePaymentResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreatePayment(int id, [FromBody] CreatePaymentRequest request)
        {
            var result = await _paymentService.CreatePaymentAsync(id, request.PaymentType.Trim(), GetClientIp(), User.ToCurrentUser());
            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<CreatePaymentResponse>.SuccessResult(result.ToResponse(), "Payment created; open paymentUrl to pay"));
        }

        /// <summary>
        /// Manager records money received outside the system (bank transfer...).
        /// </summary>
        [HttpPost("bookings/{id:int}/payments/manual")]
        [Authorize(Roles = ManagerRoles)]
        [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RecordManualPayment(int id, [FromBody] ManualPaymentRequest request)
        {
            var payment = await _paymentService.RecordManualPaymentAsync(id, request.ToBusinessModel(), User.ToCurrentUser());
            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<PaymentResponse>.SuccessResult(payment.ToResponse(), "Payment recorded successfully"));
        }

        /// <summary>
        /// VNPay redirects the customer's browser here after paying. The result is verified and saved,
        /// then the browser is redirected to the frontend result page.
        /// </summary>
        [HttpGet("payments/vnpay-return")]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> VnPayReturn()
        {
            var result = await _paymentService.HandleGatewayCallbackAsync(ReadQuery());

            string status = result.RspCode switch
            {
                "00" or "02" => result.Success ? "success" : "failed",
                "97" => "invalid",
                _ => "failed"
            };

            string url = $"{_emailOptions.FrontendBaseUrl.TrimEnd('/')}/customer/payments/result"
                + $"?status={status}"
                + $"&code={Uri.EscapeDataString(result.Payment?.PaymentCode ?? string.Empty)}"
                + $"&bookingId={result.Payment?.BookingId}";
            return Redirect(url);
        }

        /// <summary>
        /// VNPay server-to-server notification (IPN). Answers in the format VNPay expects: { RspCode, Message }.
        /// </summary>
        [HttpGet("payments/vnpay-ipn")]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> VnPayIpn()
        {
            var result = await _paymentService.HandleGatewayCallbackAsync(ReadQuery());
            return Ok(new VnPayIpnResponse { RspCode = result.RspCode, Message = result.Message });
        }

        /// <summary>
        /// Mock gateway only (local / demo without VNPay keys): the customer confirms or fails the payment.
        /// </summary>
        [HttpPost("payments/mock/{txnRef}/complete")]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<PaymentCallbackResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CompleteMockPayment(string txnRef, [FromBody] MockPaymentCompleteRequest request)
        {
            var result = await _paymentService.CompleteMockPaymentAsync(txnRef, request.Success, User.ToCurrentUser());
            return Ok(ApiResponse<PaymentCallbackResponse>.SuccessResult(result.ToResponse(), result.Message));
        }

        /// <summary>
        /// Retry a refund that failed at the payment gateway.
        /// </summary>
        [HttpPost("payments/{id:int}/retry-refund")]
        [Authorize(Roles = ManagerRoles)]
        [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RetryRefund(int id)
        {
            var refund = await _paymentService.RetryRefundAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<PaymentResponse>.SuccessResult(refund.ToResponse(),
                refund.Status == PaymentStatuses.Succeeded ? "Refund succeeded" : "Refund failed again"));
        }

        /// <summary>
        /// Mark a refund as done manually (money returned outside the gateway).
        /// </summary>
        [HttpPost("payments/{id:int}/mark-refunded")]
        [Authorize(Roles = ManagerRoles)]
        [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkRefunded(int id, [FromBody] MarkRefundedRequest request)
        {
            var refund = await _paymentService.MarkRefundedManuallyAsync(id, request.Reference, request.Note, User.ToCurrentUser());
            return Ok(ApiResponse<PaymentResponse>.SuccessResult(refund.ToResponse(), "Refund marked as completed"));
        }

        /// <summary>
        /// Payment settings: deposit %, refund % on cancellation, USD -> VND rate, due days.
        /// </summary>
        [HttpGet("settings/payment")]
        [ProducesResponseType(typeof(ApiResponse<PaymentSettingsResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaymentSettings()
        {
            var settings = await _settingService.GetPaymentSettingsAsync();
            return Ok(ApiResponse<PaymentSettingsResponse>.SuccessResult(settings.ToResponse()));
        }

        /// <summary>
        /// Change the payment settings. New values apply to bookings approved from now on (approved bookings keep their deposit).
        /// </summary>
        [HttpPut("settings/payment")]
        [Authorize(Roles = ManagerRoles)]
        [ProducesResponseType(typeof(ApiResponse<PaymentSettingsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePaymentSettings([FromBody] UpdatePaymentSettingsRequest request)
        {
            var settings = await _settingService.UpdatePaymentSettingsAsync(request.ToBusinessModel(), User.ToCurrentUser());
            return Ok(ApiResponse<PaymentSettingsResponse>.SuccessResult(settings.ToResponse(), "Payment settings updated successfully"));
        }

        private Dictionary<string, string> ReadQuery()
        {
            return Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
        }

        // Railway đứng sau proxy: IP thật của khách nằm ở X-Forwarded-For
        private string GetClientIp()
        {
            string? forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                return forwarded.Split(',')[0].Trim();
            }
            var ip = HttpContext.Connection.RemoteIpAddress;
            if (ip == null)
            {
                return "127.0.0.1";
            }
            return ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4().ToString() : ip.ToString() == "::1" ? "127.0.0.1" : ip.ToString();
        }
    }
}
