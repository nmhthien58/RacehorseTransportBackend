namespace RacehorseTransport.Services.BusinessModels
{
    public class EmailMessage
    {
        public string ToAddress { get; set; } = string.Empty;
        public string? ToName { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
    }

    // Người dùng lấy ra từ ID token của Google (email đã được Google xác nhận)
    public class GoogleUserInfo
    {
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
    }

    // Tên các mẫu email = tên file trong RacehorseTransport.Services/EmailTemplates (không có đuôi .html)
    public static class EmailTemplateNames
    {
        public const string ResetPassword = "ResetPassword";   // {{FullName}}, {{Link}}, {{ExpiresInMinutes}}
        public const string Notification = "Notification";     // {{FullName}}, {{Title}}, {{Message}}, {{Link}}

        // Mẫu riêng cho từng sự kiện gửi khách hàng. {{FullName}} và {{Link}} do NotificationService điền.
        public const string BookingApproved = "BookingApproved";         // {{BookingCode}}
        public const string BookingRejected = "BookingRejected";         // {{BookingCode}}, {{RejectionReason}}
        public const string DocumentsRequested = "DocumentsRequested";   // {{BookingCode}}, {{HorseName}}, {{DocumentRequest}}
        public const string DossierCleared = "DossierCleared";           // {{BookingCode}}, {{HorseName}}, {{ClearanceNumber}}
        public const string TripIncident = "TripIncident";               // {{TripCode}}, {{BookingCode}}, {{IncidentCode}}, {{Severity}}, {{IncidentDescription}}
        public const string HorseArrived = "HorseArrived";               // {{BookingCode}}, {{HorseName}}, {{Destination}}
        public const string BalanceDue = "BalanceDue";                   // {{BookingCode}}, {{TripCode}}, {{DepartureDate}}, {{Amount}}, {{AmountVnd}}, {{DueDate}}
    }
}
