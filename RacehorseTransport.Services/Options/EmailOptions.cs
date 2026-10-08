namespace RacehorseTransport.Services.Options
{
    // Đọc từ section "Email" trong appsettings (điền ở appsettings.Local.json, không commit mật khẩu / khóa)
    public class EmailOptions
    {
        public const string SectionName = "Email";

        // Cách gửi: "Smtp" (ví dụ Gmail; dùng khi chạy ở máy), "Brevo" (gọi API qua HTTPS; dùng cho bản deploy
        // vì nhiều nền tảng chặn cổng SMTP). Bỏ trống = không gửi, chỉ ghi log.
        public string Provider { get; set; } = string.Empty;

        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "Racehorse Transport";

        // Địa chỉ frontend, dùng để tạo link trong email (xác thực, đặt lại mật khẩu)
        public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

        public int VerificationTokenHours { get; set; } = 24;
        public int PasswordResetTokenMinutes { get; set; } = 60;

        public SmtpOptions Smtp { get; set; } = new();
        public BrevoOptions Brevo { get; set; } = new();
    }

    public class SmtpOptions
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // Gmail: app password 16 ký tự, không phải mật khẩu đăng nhập
    }

    public class BrevoOptions
    {
        public string ApiUrl { get; set; } = "https://api.brevo.com/v3/smtp/email";
        public string ApiKey { get; set; } = string.Empty;
    }

    public static class EmailProviders
    {
        public const string Smtp = "Smtp";
        public const string Brevo = "Brevo";
    }
}
