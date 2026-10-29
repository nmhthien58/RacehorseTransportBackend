namespace RacehorseTransport.Services.BusinessModels
{
    // BE-10: kết quả kiểm tra trước khi xuất phát. Error chặn POST /api/trips/{id}/start, Warning chỉ nhắc.
    public class DepartureCheckModel
    {
        public bool IsReady { get; set; }
        public DateTimeOffset DepartureAt { get; set; } // giờ dùng để tính hạn giấy
        public List<DepartureIssueModel> Issues { get; set; } = new();
    }

    public class DepartureIssueModel
    {
        public string Code { get; set; } = string.Empty;          // DepartureIssueCodes
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;      // DepartureIssueSeverities
        public string Category { get; set; } = string.Empty;      // DepartureIssueCategories
        public string? HorseName { get; set; }
        public string? BookingCode { get; set; }
    }

    public static class DepartureIssueSeverities
    {
        public const string Error = "Error";
        public const string Warning = "Warning";
    }

    public static class DepartureIssueCategories
    {
        public const string Trip = "Trip";
        public const string Dossier = "Dossier";
        public const string Documents = "Documents";
        public const string Payment = "Payment";
        public const string Crew = "Crew";
    }

    public static class DepartureIssueCodes
    {
        public const string TripNotScheduled = "TripNotScheduled";
        public const string DossierNotCleared = "DossierNotCleared";
        public const string DocumentExpired = "DocumentExpired";
        public const string DocumentExpiresBeforeArrival = "DocumentExpiresBeforeArrival";
        public const string FitExamMissing = "FitExamMissing";
        public const string FitExamTypeNotConfigured = "FitExamTypeNotConfigured";
        public const string BalanceUnpaid = "BalanceUnpaid";
        public const string NotEnoughDrivers = "NotEnoughDrivers";
    }

    // Dữ liệu đầu vào của DepartureCheckRules (đã đọc sẵn từ DB, để quy tắc thuần và test được)
    public class DepartureCheckInput
    {
        public DateTimeOffset DepartureAt { get; set; }
        public DateTimeOffset PlannedArrivalAt { get; set; } // giờ tới dự kiến, đã dời theo giờ xuất phát thực tế
        public string TripStatus { get; set; } = string.Empty;
        public int FitExamValidHours { get; set; }

        // false: DB chưa có loại giấy FIT_EXAM (chưa chạy 03_update_fit_exam.sql) -> chỉ cảnh báo, không chặn
        public bool FitExamTypeConfigured { get; set; }

        public List<DepartureHorseInput> Horses { get; set; } = new();
        public List<DepartureBookingInput> Bookings { get; set; } = new();

        // Quy tắc số tài xế (BE-07) chỉ áp cho đường bộ; DrivingHours null = không ước tính được
        public bool IsGround { get; set; }
        public decimal? DrivingHours { get; set; }
        public int DriverCount { get; set; }
        public decimal SingleDriverLimitHours { get; set; }
    }

    public class DepartureHorseInput
    {
        public string HorseName { get; set; } = string.Empty;
        public string BookingCode { get; set; } = string.Empty;
        public string? DossierStatus { get; set; } // null = chưa có hồ sơ

        // Giấy đã duyệt trong hồ sơ của đơn
        public List<DepartureDocumentInput> Documents { get; set; } = new();

        // Ngày cấp các giấy khám FIT_EXAM của con ngựa (hồ sơ thú y của ngựa + hồ sơ của đơn)
        public List<DateOnly> FitExamIssueDates { get; set; } = new();
    }

    public class DepartureDocumentInput
    {
        public string DocTypeName { get; set; } = string.Empty;
        public DateOnly? ExpiryDate { get; set; }
    }

    public class DepartureBookingInput
    {
        public string BookingCode { get; set; } = string.Empty;
        public decimal EstimatedCost { get; set; }
        public decimal Paid { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
    }
}
