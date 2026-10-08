namespace RacehorseTransport.Repositories.Entities
{
    // common.system_notifications
    public class SystemNotification
    {
        public long NotificationId { get; set; }
        public int RecipientUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ReferenceType { get; set; } // Ví dụ: "Booking", "Trip", "Dossier", "Incident"
        public int? ReferenceId { get; set; }
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual User? Recipient { get; set; }
    }
}
