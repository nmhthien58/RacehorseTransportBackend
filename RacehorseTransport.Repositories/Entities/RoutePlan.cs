using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Repositories.Entities
{
    // booking.route_plans: tuyến dự kiến của một đơn (R-10). Một đơn có nhiều phiên bản; chỉ một bản Approved hiệu lực.
    public class RoutePlan
    {
        public int RoutePlanId { get; set; }
        public int BookingId { get; set; }
        public int Version { get; set; }
        public string Status { get; set; } = RoutePlanStatuses.Draft;
        public string TransitCountryCodes { get; set; } = "[]";   // jsonb: mã nước quá cảnh theo thứ tự
        public decimal TotalDistanceKm { get; set; }
        public decimal EstimatedHours { get; set; }               // thời gian vận chuyển dự kiến (đã gồm nghỉ, cửa khẩu)
        public bool IsEstimate { get; set; }                      // có chặng tính theo đường chim bay (bản đồ lỗi)
        public string? Polyline { get; set; }                     // jsonb: [[lat, lng], ...] đã rút gọn để vẽ
        public string? Notes { get; set; }
        public int PlannedByUserId { get; set; }
        public int? ReviewedByUserId { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public virtual BookingRequest? Booking { get; set; }
        public virtual User? PlannedBy { get; set; }
        public virtual User? ReviewedBy { get; set; }
        public virtual ICollection<RoutePlanStop> Stops { get; set; } = new List<RoutePlanStop>();
    }
}
