namespace RacehorseTransport.Services.BusinessModels
{
    public class EmailMessage
    {
        public string ToAddress { get; set; } = string.Empty;
        public string? ToName { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
    }

    // Tên các mẫu email = tên file trong RacehorseTransport.Services/EmailTemplates (không có đuôi .html)
    public static class EmailTemplateNames
    {
        public const string VerifyEmail = "VerifyEmail";       // {{FullName}}, {{Link}}, {{ExpiresInHours}}
        public const string ResetPassword = "ResetPassword";   // {{FullName}}, {{Link}}, {{ExpiresInMinutes}}
        public const string Notification = "Notification";     // {{FullName}}, {{Title}}, {{Message}}, {{Link}}
    }
}
