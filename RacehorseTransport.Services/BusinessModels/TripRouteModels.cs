namespace RacehorseTransport.Services.BusinessModels
{
    // ---------- Gợi ý lộ trình khi ghép nhiều đơn vào một chuyến (R-04) ----------

    public class RouteSuggestionRequestModel
    {
        public List<int> BookingHorseIds { get; set; } = new();
        public DateTimeOffset PlannedStartDate { get; set; }
        public int DriverCount { get; set; } = 1; // từ 2 tài xế thay ca được lái nhiều giờ hơn mỗi ngày
    }

    // Một đơn cần đón / trả, đầu vào của RouteSuggestionBuilder
    public class RouteBookingInput
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string PickupAddress { get; set; } = string.Empty;
        public string PickupCountryCode { get; set; } = string.Empty;
        public string DropoffAddress { get; set; } = string.Empty;
        public string DropoffCountryCode { get; set; } = string.Empty;
        public List<string> TransitCountryCodes { get; set; } = new();
        public DateTimeOffset PickupWindowStart { get; set; }
        public DateTimeOffset PickupWindowEnd { get; set; }
        public DateTimeOffset DeliveryDeadline { get; set; }
        public int? DirectDistanceKm { get; set; } // quãng đường đi thẳng lúc báo giá
    }

    // Một địa điểm để tính quãng đường
    public record RoutePlace(string Address, string CountryCode);

    public class RouteSuggestionModel
    {
        public string TransportMode { get; set; } = string.Empty;
        public DateTimeOffset PlannedStartDate { get; set; }
        public DateTimeOffset SuggestedEndDate { get; set; }   // tới mốc cuối + dỡ ngựa
        public int TotalDistanceKm { get; set; }
        public decimal TotalHours { get; set; }
        public List<SuggestedCheckpointModel> Checkpoints { get; set; } = new();
        public List<RouteBookingEtaModel> Bookings { get; set; } = new();
        public List<PlanWarningModel> Warnings { get; set; } = new();
    }

    public class SuggestedCheckpointModel
    {
        public string CheckpointName { get; set; } = string.Empty;
        public string CheckpointType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTimeOffset PlannedTime { get; set; }
        public int MandatoryRestMinutes { get; set; }
        public int? BookingId { get; set; }
        public string? BookingCode { get; set; }
        public int LegDistanceKm { get; set; } // quãng đường từ mốc trước tới mốc này (0 với cửa khẩu: tính trong chặng)
    }

    // Giờ đón / giờ giao dự kiến của từng đơn trên tuyến ghép
    public class RouteBookingEtaModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public DateTimeOffset PickupAt { get; set; }
        public DateTimeOffset EstimatedDeliveryAt { get; set; }
        public DateTimeOffset DeliveryDeadline { get; set; }
        public bool OnTime { get; set; }
        public int RouteDistanceKm { get; set; }    // quãng đường ngựa của đơn thực đi trên tuyến ghép
        public int? DirectDistanceKm { get; set; }  // nếu đi thẳng
        public decimal? DetourRatio { get; set; }   // RouteDistanceKm / DirectDistanceKm
    }

    // Cảnh báo khi ghép đơn. Severity = Error: trình duyệt sẽ bị chặn; Warning: nên xem lại; Info: gợi ý.
    public class PlanWarningModel
    {
        public string Code { get; set; } = string.Empty;
        public string Severity { get; set; } = PlanWarningSeverities.Warning;
        public string Message { get; set; } = string.Empty;
        public string? BookingCode { get; set; }
        public Dictionary<string, string> Params { get; set; } = new();
    }

    public static class PlanWarningSeverities
    {
        public const string Error = "Error";
        public const string Warning = "Warning";
        public const string Info = "Info";
    }

    public static class PlanWarningCodes
    {
        public const string DeadlineMissed = "DeadlineMissed";             // giờ giao dự kiến trễ hơn hạn giao của đơn
        public const string PickupOutsideWindow = "PickupOutsideWindow";   // giờ đón nằm ngoài khoảng pickup khách chọn
        public const string DifferentPickupCountry = "DifferentPickupCountry";
        public const string PickupsFarApart = "PickupsFarApart";           // điểm đón cách điểm xuất phát quá xa
        public const string Detour = "Detour";                             // đơn phải đi vòng quá nhiều so với đi thẳng
        public const string DossierNotCleared = "DossierNotCleared";       // hồ sơ chưa thông quan: xe sẽ không xuất phát được
        public const string OvernightRest = "OvernightRest";               // chặng dài cần nghỉ qua đêm: nên thêm mốc RestStop
    }

    // ---------- Kiểm tra lộ trình khi trình / duyệt ----------

    // Thông tin đơn cần cho các quy tắc lộ trình (TripRouteRules)
    public class RouteBookingRule
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public DateTimeOffset PickupWindowStart { get; set; }
        public DateTimeOffset PickupWindowEnd { get; set; }
        public DateTimeOffset DeliveryDeadline { get; set; }
    }
}
