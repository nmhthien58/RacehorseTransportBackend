namespace RacehorseTransport.Repositories.Entities
{
    // acceptance.delivery_failures (R-16, BE-16): xe tới điểm giao nhưng không giao được ngựa của một đơn
    public class DeliveryFailure
    {
        public int FailureId { get; set; }
        public int TripId { get; set; }
        public int BookingId { get; set; }
        public int? CheckpointId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string PhotoPaths { get; set; } = "[]";          // jsonb: đường dẫn ảnh đã tải lên
        public int RecordedByUserId { get; set; }
        public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
        public int? IncidentId { get; set; }                    // sự cố tạo kèm để điều phối xử lý

        // Phí chờ tới lúc ghi không giao được (lỗi khách mới thu)
        public int WaitingHours { get; set; }
        public decimal WaitingFee { get; set; }
        public bool CustomerPays { get; set; }

        // Khách chọn cách xử lý (lỗi khách); quá hạn hệ thống chọn HoldAtStable
        public DateTimeOffset? DecisionDueAt { get; set; }
        public string? Decision { get; set; }
        public DateTimeOffset? DecidedAt { get; set; }
        public bool DecidedBySystem { get; set; }
        public decimal DecisionCost { get; set; }               // giao chỗ khác / chở về: tính lúc chọn; chuồng tạm: tính khi đóng chuyến
        public string? AlternateRecipientName { get; set; }
        public string? AlternateRecipientPhone { get; set; }
        public string? AlternateAddress { get; set; }
        public decimal? AlternateLat { get; set; }
        public decimal? AlternateLng { get; set; }
        public decimal? ExtraDistanceKm { get; set; }

        public virtual Trip? Trip { get; set; }
        public virtual BookingRequest? Booking { get; set; }
    }
}
