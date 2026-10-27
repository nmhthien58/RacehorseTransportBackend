using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RacehorseTransport.API.RequestModels
{
    public class GeoSearchRequest
    {
        [FromQuery(Name = "q")]
        [Required(ErrorMessage = "q is required.")]
        [MinLength(2, ErrorMessage = "q must have at least 2 characters.")]
        [MaxLength(200, ErrorMessage = "q cannot exceed 200 characters.")]
        public string Q { get; set; } = string.Empty;

        // Mã nước ISO 2 chữ (VN, CN...). Bỏ trống = tìm khắp nơi
        [FromQuery(Name = "country")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "country must be a 2-letter ISO code such as VN.")]
        public string? Country { get; set; }
    }

    public class GeoReverseRequest
    {
        [FromQuery(Name = "lat")]
        [Required(ErrorMessage = "lat is required.")]
        [Range(-90, 90, ErrorMessage = "lat must be between -90 and 90.")]
        public double? Lat { get; set; }

        [FromQuery(Name = "lng")]
        [Required(ErrorMessage = "lng is required.")]
        [Range(-180, 180, ErrorMessage = "lng must be between -180 and 180.")]
        public double? Lng { get; set; }
    }

    public class GeoPointRequest
    {
        [Range(-90, 90, ErrorMessage = "lat must be between -90 and 90.")]
        public double Lat { get; set; }

        [Range(-180, 180, ErrorMessage = "lng must be between -180 and 180.")]
        public double Lng { get; set; }

        // Biết trước nước của điểm (từ kết quả tìm địa chỉ) thì gửi kèm để đỡ phải tra
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "countryCode must be a 2-letter ISO code such as VN.")]
        public string? CountryCode { get; set; }
    }

    public class GeoRouteRequest
    {
        [Required(ErrorMessage = "points is required.")]
        [MinLength(2, ErrorMessage = "points must have at least 2 points.")]
        [MaxLength(25, ErrorMessage = "points cannot have more than 25 points.")]
        public List<GeoPointRequest> Points { get; set; } = new();

        // true = trả thêm các nước tuyến đi qua (chậm hơn ở lần đầu vì phải tra từng điểm)
        public bool IncludeCountries { get; set; }
    }
}
