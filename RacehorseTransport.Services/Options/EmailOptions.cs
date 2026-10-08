namespace RacehorseTransport.Services.Options
{
    // Đọc từ section "Email" trong appsettings (điền ở appsettings.Local.json, không commit mật khẩu / khóa)
    public class EmailOptions
    {
        public const string SectionName = "Email";

        // Cách gửi:
        //   "GmailApi": gọi Gmail API qua HTTPS bằng thông tin trong section "Google". Dùng được ở mọi nơi, kể cả
        //               nền tảng chặn cổng SMTP như Railway.
        //   "Smtp":     SMTP (ví dụ Gmail + app password). Chỉ dùng được ở nơi không chặn cổng 587.
        //   bỏ trống:   không gửi, chỉ ghi log (kèm link đặt lại mật khẩu).
        public string Provider { get; set; } = string.Empty;

        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "Racehorse Transport";

        // Địa chỉ frontend, dùng để tạo link trong email (đặt lại mật khẩu, nút mở ứng dụng)
        public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

        public int PasswordResetTokenMinutes { get; set; } = 60;

        public SmtpOptions Smtp { get; set; } = new();
    }

    public class SmtpOptions
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // Gmail: app password 16 ký tự, không phải mật khẩu đăng nhập
    }

    public static class EmailProviders
    {
        public const string GmailApi = "GmailApi";
        public const string Smtp = "Smtp";
    }

    // Đọc từ section "Google": một OAuth client (loại Web application) trên Google Cloud dùng cho cả hai việc:
    // đăng nhập bằng Google và gửi mail qua Gmail API. Cách tạo: docs/DEPLOY.md, mục Google.
    public class GoogleOptions
    {
        public const string SectionName = "Google";

        // Dạng xxx.apps.googleusercontent.com. Không phải bí mật: frontend cũng dùng giá trị này.
        public string ClientId { get; set; } = string.Empty;

        // Hai giá trị dưới chỉ cần cho việc gửi mail (Email:Provider = GmailApi). BÍ MẬT, không commit.
        public string ClientSecret { get; set; } = string.Empty;
        public string GmailRefreshToken { get; set; } = string.Empty; // cấp cho tài khoản Gmail của đồ án, quyền gmail.send

        // Địa chỉ của Google; chỉ đổi khi test
        public string TokenUrl { get; set; } = "https://oauth2.googleapis.com/token";
        public string CertsUrl { get; set; } = "https://www.googleapis.com/oauth2/v3/certs";
        public string GmailSendUrl { get; set; } = "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";
    }
}
