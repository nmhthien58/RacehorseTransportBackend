using System.ComponentModel.DataAnnotations;

namespace RacehorseTransport.API.RequestModels
{
    // R-18: cấu hình phạm vi dịch vụ theo quốc gia

    public class CreateCountryRequest
    {
        /// <summary>Mã nước ISO 2 chữ, ví dụ VN, SG.</summary>
        [Required(ErrorMessage = "CountryCode is required.")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "CountryCode must contain exactly two letters.")]
        public string CountryCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "NameVi is required.")]
        [MaxLength(100, ErrorMessage = "NameVi cannot exceed 100 characters.")]
        public string NameVi { get; set; } = string.Empty;

        [Required(ErrorMessage = "NameEn is required.")]
        [MaxLength(100, ErrorMessage = "NameEn cannot exceed 100 characters.")]
        public string NameEn { get; set; } = string.Empty;

        /// <summary>Thứ tự hiển thị trong danh sách chọn (nhỏ lên trước).</summary>
        [Range(0, 9999, ErrorMessage = "SortOrder must be between 0 and 9999.")]
        public int SortOrder { get; set; }
    }

    public class UpdateCountryRequest
    {
        [Required(ErrorMessage = "NameVi is required.")]
        [MaxLength(100, ErrorMessage = "NameVi cannot exceed 100 characters.")]
        public string NameVi { get; set; } = string.Empty;

        [Required(ErrorMessage = "NameEn is required.")]
        [MaxLength(100, ErrorMessage = "NameEn cannot exceed 100 characters.")]
        public string NameEn { get; set; } = string.Empty;

        [Range(0, 9999, ErrorMessage = "SortOrder must be between 0 and 9999.")]
        public int SortOrder { get; set; }
    }

    public class SetCountryActiveRequest
    {
        [Required(ErrorMessage = "IsActive is required.")]
        public bool? IsActive { get; set; }
    }

    public class SetFreightRequest
    {
        /// <summary>Đường bộ: USD / km / con. Đường bay: USD / con.</summary>
        [Required(ErrorMessage = "Value is required.")]
        [Range(typeof(decimal), "0.0001", "99999999999999", ErrorMessage = "Value must be greater than zero.")]
        public decimal? Value { get; set; }

        /// <summary>true = khách chọn được phương thức này cho nước đến này.</summary>
        [Required(ErrorMessage = "IsActive is required.")]
        public bool? IsActive { get; set; }
    }

    public class SaveAirportRequest
    {
        /// <summary>Mã IATA 3 chữ, ví dụ SGN.</summary>
        [Required(ErrorMessage = "IataCode is required.")]
        [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "IataCode must contain exactly three letters.")]
        public string IataCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string? City { get; set; }
    }
}
