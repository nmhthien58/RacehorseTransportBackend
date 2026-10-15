namespace RacehorseTransport.Services.BusinessModels
{
    public class NotificationModel
    {
        public long NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class NotificationQueryOptions : BusinessQueryOptions
    {
        public bool? IsRead { get; set; }
    }
}
