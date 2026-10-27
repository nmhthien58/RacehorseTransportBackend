namespace RacehorseTransport.API.ResponseModels
{
    public class GeoPlaceResponse
    {
        public string DisplayName { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string? CountryCode { get; set; }
        public string Provider { get; set; } = string.Empty;
    }

    public class GeoRouteLegResponse
    {
        public int FromIndex { get; set; }
        public int ToIndex { get; set; }
        public decimal DistanceKm { get; set; }
        public decimal DurationHours { get; set; }
        public string Provider { get; set; } = string.Empty;
        public bool IsEstimate { get; set; }

        // Đường vẽ của chặng: mảng [vĩ độ, kinh độ], dùng thẳng cho Leaflet Polyline
        public List<double[]> Polyline { get; set; } = new();
    }

    public class GeoRouteResponse
    {
        public List<GeoRouteLegResponse> Legs { get; set; } = new();
        public decimal TotalDistanceKm { get; set; }
        public decimal TotalDurationHours { get; set; }
        public bool IsEstimate { get; set; }
        public List<double[]> Polyline { get; set; } = new();
        public List<string>? CountryCodes { get; set; }
    }
}
