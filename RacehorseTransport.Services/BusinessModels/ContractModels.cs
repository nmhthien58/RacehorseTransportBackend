namespace RacehorseTransport.Services.BusinessModels
{
    // R-17: dữ liệu in lên hợp đồng PDF. Chỉ chứa dữ liệu nghiệp vụ (không có thời điểm sinh file) để sinh lại ra cùng mã băm.
    public class ContractDocumentModel
    {
        public string ContractCode { get; set; } = string.Empty;
        public int Version { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTimeOffset IssuedAt { get; set; }               // lúc gửi báo giá chốt (hoặc lúc phát sinh thay đổi)
        public string PolicyVersion { get; set; } = string.Empty;

        public string BookingCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string? RecipientInfo { get; set; }                 // TODO(BE-15): người nhận trên đơn

        public string TransportMode { get; set; } = string.Empty;
        public string PickupAddress { get; set; } = string.Empty;
        public string PickupCountryCode { get; set; } = string.Empty;
        public string DropoffAddress { get; set; } = string.Empty;
        public string DropoffCountryCode { get; set; } = string.Empty;
        public List<string> TransitCountryCodes { get; set; } = new();
        public decimal TotalDistanceKm { get; set; }
        public List<ContractStopModel> Stops { get; set; } = new();
        public DateTimeOffset PickupWindowStart { get; set; }
        public DateTimeOffset PickupWindowEnd { get; set; }
        public DateTimeOffset DeliveryDeadline { get; set; }

        public List<ContractHorseModel> Horses { get; set; } = new();

        public string CurrencyCode { get; set; } = "USD";
        public List<QuoteLineModel> Lines { get; set; } = new();
        public decimal Total { get; set; }
        public decimal DepositPercent { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public DateTimeOffset? DepositDueAt { get; set; }
        public int BalanceDueDaysBeforeDeparture { get; set; }
        public int ExtraPaymentDueDays { get; set; }

        public string? ApprovedByName { get; set; }
        public DateTimeOffset? ApprovedAt { get; set; }
    }

    public class ContractStopModel
    {
        public string Name { get; set; } = string.Empty;
        public string StopType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? CountryCode { get; set; }
    }

    public class ContractHorseModel
    {
        public string Name { get; set; } = string.Empty;
        public string MicrochipNumber { get; set; } = string.Empty;
        public string StallClass { get; set; } = string.Empty;
        public decimal? AgreedValue { get; set; }
        public string InsurancePlan { get; set; } = string.Empty;
    }

    // Thông tin một phiên bản hợp đồng trả cho API
    public class BookingContractModel
    {
        public int ContractId { get; set; }
        public string ContractCode { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public int Version { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string FileSha256 { get; set; } = string.Empty;
        public string PolicyVersion { get; set; } = string.Empty;
        public string? FileUrl { get; set; }                       // link tải có hạn (chỉ có ở API xem bản hiện hành)
        public DateTimeOffset? FileUrlExpiresAt { get; set; }
        public int? AcceptedByUserId { get; set; }
        public string? AcceptedByName { get; set; }
        public DateTimeOffset? AcceptedAt { get; set; }
        public string? AcceptedIp { get; set; }
        public string? AcceptedUserAgent { get; set; }
        public string? AcceptedPolicyVersion { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    // Bằng chứng giao kết lấy từ request (controller điền)
    public class ContractAcceptanceModel
    {
        public int ContractId { get; set; }
        public string ContractSha256 { get; set; } = string.Empty;
        public bool AcceptPolicy { get; set; }
        public string PolicyVersion { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    // R-17: vận đơn hàng không của chuyến
    public class AirWaybillModel
    {
        public int TripId { get; set; }
        public string TripCode { get; set; } = string.Empty;
        public string? AirWaybillNumber { get; set; }
        public string? AirWaybillFilePath { get; set; }
        public string? AirWaybillFileUrl { get; set; }
    }
}
