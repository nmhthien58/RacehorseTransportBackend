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

        // BE-19: Các mẫu email cho các sự kiện mới (R-09 -> R-16)
        public const string QuoteReady = "QuoteReady";                                   // {{BookingCode}}, {{PriceUsd}}, {{PriceVnd}}, {{ExpiresAt}}
        public const string QuoteExpiring = "QuoteExpiring";                             // {{BookingCode}}, {{ExpiresAt}}
        public const string QuoteExpired = "QuoteExpired";                               // {{BookingCode}}
        public const string DocumentsReminder = "DocumentsReminder";                     // {{BookingCode}}, {{HorseName}}, {{DocumentList}}, {{Deadline}}, {{ReminderNumber}}
        public const string BookingCancelledMissingDocs = "BookingCancelledMissingDocs"; // {{BookingCode}}, {{RefundAmount}}
        public const string RecipientScheduled = "RecipientScheduled";                   // {{RecipientName}}, {{BookingCode}}, {{HorseNames}}, {{ExpectedDate}}, {{ContactPhone}}
        public const string RecipientDeparted = "RecipientDeparted";                     // {{RecipientName}}, {{BookingCode}}, {{Eta}}
        public const string RecipientArrivingSoon = "RecipientArrivingSoon";             // {{RecipientName}}, {{BookingCode}}, {{EtaWindow}}, {{DriverPhone}}
        public const string ConfirmRecipient = "ConfirmRecipient";                       // {{BookingCode}}, {{RecipientName}}, {{Eta}}
        public const string HandoverCompleted = "HandoverCompleted";                     // {{BookingCode}}, {{RecipientName}}, {{SignedAt}}, {{Condition}}
        public const string DeliveryFailed = "DeliveryFailed";                           // {{BookingCode}}, {{Reason}}, {{DecisionDeadline}}
        public const string DelayCompensation = "DelayCompensation";                     // {{BookingCode}}, {{DelayHours}}, {{Amount}}
        public const string ClaimDecision = "ClaimDecision";                             // {{ClaimCode}}, {{BookingCode}}, {{HorseName}}, {{Decision}}, {{Amount}}, {{Reason}}
    }
}
