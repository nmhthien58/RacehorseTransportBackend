using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Repositories.Entities
{
    // booking.booking_contracts (R-17): hợp đồng vận chuyển dạng PDF, mỗi lần đổi điều khoản là một phiên bản mới
    public class BookingContract
    {
        public int ContractId { get; set; }
        public string ContractCode { get; set; } = string.Empty;   // HD-2026-0001 (mỗi phiên bản một mã)
        public int BookingId { get; set; }
        public int Version { get; set; }
        public string Status { get; set; } = ContractStatuses.Draft;
        public string Reason { get; set; } = ContractReasons.Initial;
        public string FilePath { get; set; } = string.Empty;       // đường dẫn trên storage (category documents)
        public string FileSha256 { get; set; } = string.Empty;     // mã băm file PDF (hex thường)
        public string PolicyVersion { get; set; } = string.Empty;  // phiên bản chính sách in trong phụ lục

        // Bằng chứng giao kết (chỉ có khi Accepted)
        public int? AcceptedByUserId { get; set; }
        public DateTimeOffset? AcceptedAt { get; set; }
        public string? AcceptedIp { get; set; }
        public string? AcceptedUserAgent { get; set; }
        public string? AcceptedPolicyVersion { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public virtual BookingRequest? Booking { get; set; }
        public virtual User? AcceptedBy { get; set; }
    }
}
