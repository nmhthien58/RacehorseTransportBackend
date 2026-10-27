namespace RacehorseTransport.Services.BusinessModels
{
    public class GeoPointModel
    {
        public double Lat { get; set; }
        public double Lng { get; set; }

        // Biết trước nước của điểm (ví dụ lấy từ kết quả tìm địa chỉ) thì khỏi tra lại
        public string? CountryCode { get; set; }

        public GeoPointModel() { }

        public GeoPointModel(double lat, double lng, string? countryCode = null)
        {
            Lat = lat;
            Lng = lng;
            CountryCode = countryCode;
        }
    }

    // Một địa điểm (gợi ý khi tìm, hoặc kết quả tọa độ -> địa chỉ)
    public class GeoPlaceModel
    {
        public string DisplayName { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string? CountryCode { get; set; } // mã ISO 2 chữ in hoa, ví dụ "VN"
        public string Provider { get; set; } = string.Empty;
    }

    // Một chặng giữa hai điểm liên tiếp
    public class GeoRouteLegModel
    {
        public int FromIndex { get; set; }
        public int ToIndex { get; set; }
        public decimal DistanceKm { get; set; }
        public decimal DurationHours { get; set; }
        public string Provider { get; set; } = string.Empty; // Goong | Osrm | Estimate
        public bool IsEstimate { get; set; }                 // true = nhà cung cấp lỗi, tính theo đường chim bay
        public List<GeoPointModel> Polyline { get; set; } = new();
    }

    public class GeoRouteModel
    {
        public List<GeoRouteLegModel> Legs { get; set; } = new();
        public decimal TotalDistanceKm { get; set; }
        public decimal TotalDurationHours { get; set; }
        public bool IsEstimate { get; set; }                 // có ít nhất một chặng là ước tính
        public List<GeoPointModel> Polyline { get; set; } = new(); // cả tuyến, đã rút gọn để vẽ
        public List<string>? CountryCodes { get; set; }      // các nước đi qua theo thứ tự (chỉ khi yêu cầu)
    }
}
