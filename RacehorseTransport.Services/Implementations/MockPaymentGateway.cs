using Microsoft.Extensions.Options;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    // Cổng thanh toán giả lập để chạy local / demo khi chưa có key VNPay.
    // Link thanh toán mở trang giả lập của frontend; trang đó gọi POST /api/payments/mock/{txnRef}/complete.
    // Hoàn tiền luôn thành công.
    public class MockPaymentGateway : IPaymentGateway
    {
        private readonly EmailOptions _emailOptions;

        public MockPaymentGateway(IOptions<EmailOptions> emailOptions)
        {
            _emailOptions = emailOptions.Value;
        }

        public string ProviderName => PaymentProviders.Mock;

        public string CreatePaymentUrl(GatewayPaymentRequest request)
        {
            return $"{_emailOptions.FrontendBaseUrl.TrimEnd('/')}/customer/payments/mock?txnRef={Uri.EscapeDataString(request.TxnRef)}&amountVnd={request.AmountVnd}";
        }

        public GatewayCallbackData ParseCallback(IReadOnlyDictionary<string, string> query)
        {
            // Cổng giả lập không gọi lại qua ReturnUrl / IPN
            return new GatewayCallbackData { IsSignatureValid = false };
        }

        public Task<GatewayRefundResult> RefundAsync(GatewayRefundRequest request)
        {
            return Task.FromResult(new GatewayRefundResult
            {
                Success = true,
                ResponseCode = "00",
                Message = "Mock refund succeeded",
                TransactionNo = $"MOCK-{request.RefundTxnRef}"
            });
        }
    }

    public class PaymentGatewayResolver : IPaymentGatewayResolver
    {
        private readonly IEnumerable<IPaymentGateway> _gateways;
        private readonly PaymentOptions _paymentOptions;
        private readonly VnPayOptions _vnPayOptions;

        public PaymentGatewayResolver(
            IEnumerable<IPaymentGateway> gateways,
            IOptions<PaymentOptions> paymentOptions,
            IOptions<VnPayOptions> vnPayOptions)
        {
            _gateways = gateways;
            _paymentOptions = paymentOptions.Value;
            _vnPayOptions = vnPayOptions.Value;
        }

        // Payment:Provider ghi rõ thì theo đó; để trống thì có key VNPay dùng VNPay, không thì dùng giả lập
        public IPaymentGateway Current
        {
            get
            {
                string provider = _paymentOptions.Provider;
                if (string.IsNullOrWhiteSpace(provider))
                {
                    provider = _vnPayOptions.IsConfigured ? PaymentProviders.VnPay : PaymentProviders.Mock;
                }
                return Get(provider);
            }
        }

        public IPaymentGateway Get(string provider)
        {
            return _gateways.FirstOrDefault(g => string.Equals(g.ProviderName, provider, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Payment provider '{provider}' is not registered.");
        }
    }
}
