namespace RacehorseTransport.Services.Options
{
    // Section "Payment": chọn cổng thanh toán và địa chỉ công khai của API để VNPay gọi lại
    public class PaymentOptions
    {
        public const string SectionName = "Payment";

        // "VnPay" | "Mock". Để trống: có đủ key VNPay thì dùng VnPay, không thì dùng cổng giả lập (Mock)
        public string Provider { get; set; } = string.Empty;

        // Địa chỉ công khai của backend, dùng tạo vnp_ReturnUrl (vd https://...railway.app). Local: http://localhost:5080
        public string PublicApiBaseUrl { get; set; } = "http://localhost:5080";

        // Link thanh toán hết hạn sau bấy nhiêu phút (vnp_ExpireDate)
        public int PaymentExpireMinutes { get; set; } = 15;
    }

    // Section "VnPay": thông tin VNPay gửi qua email khi đăng ký merchant. HashSecret là bí mật, không commit.
    public class VnPayOptions
    {
        public const string SectionName = "VnPay";

        public string TmnCode { get; set; } = string.Empty;
        public string HashSecret { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

        // API truy vấn / hoàn tiền (merchant_webapi)
        public string ApiUrl { get; set; } = "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";
        public string Version { get; set; } = "2.1.0";
        public string Locale { get; set; } = "vn";

        public bool IsConfigured => !string.IsNullOrWhiteSpace(TmnCode) && !string.IsNullOrWhiteSpace(HashSecret);
    }
}
