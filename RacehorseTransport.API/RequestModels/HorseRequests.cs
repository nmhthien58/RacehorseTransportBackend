using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RacehorseTransport.API.RequestModels
{
    public class HorseQueryRequest : QueryOptionsRequest
    {
        // Chỉ có tác dụng với nhân sự; khách hàng luôn chỉ thấy ngựa của mình
        [FromQuery(Name = "ownerUserId")]
        public int? OwnerUserId { get; set; }

        [FromQuery(Name = "includeInactive")]
        public bool IncludeInactive { get; set; }
    }

    // multipart/form-data
    public class CreateHorseRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        // Chuẩn ISO 11784/11785: đúng 15 chữ số (3 số mã quốc gia/nhà sản xuất + 12 số định danh)
        [Required(ErrorMessage = "MicrochipNumber is required.")]
        [RegularExpression(@"^\s*[0-9]{15}\s*$", ErrorMessage = "MicrochipNumber must be exactly 15 digits (ISO 11784/11785).")]
        public string MicrochipNumber { get; set; } = string.Empty;

        // Số UELN: 15 ký tự = 3 số mã quốc gia + 3 số mã cơ quan cấp + 9 chữ/số định danh
        [Required(ErrorMessage = "PassportNumber is required.")]
        [RegularExpression(@"^\s*[0-9]{6}[A-Za-z0-9]{9}\s*$", ErrorMessage = "PassportNumber must be a 15-character UELN: 6 digits followed by 9 letters or digits.")]
        public string PassportNumber { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "Breed cannot exceed 50 characters.")]
        public string? Breed { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "DateOfBirth is required.")]
        public DateOnly? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Color is required.")]
        [MaxLength(30, ErrorMessage = "Color cannot exceed 30 characters.")]
        public string Color { get; set; } = string.Empty;

        public string? SpecialCareRequirements { get; set; }

        // Ảnh ngựa (jpg, png), không bắt buộc
        public IFormFile? Photo { get; set; }
    }

    // Không gửi Photo thì giữ ảnh đang có
    public class UpdateHorseRequest : CreateHorseRequest
    {
        // true: xóa ảnh đang có (bỏ qua nếu gửi kèm Photo mới)
        public bool RemovePhoto { get; set; }
    }

    // multipart/form-data: file hồ sơ gửi kèm trong trường "file"
    public class CreateHorseDocumentRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "DocTypeId is required.")]
        public int DocTypeId { get; set; }

        [MaxLength(100, ErrorMessage = "DocumentNumber cannot exceed 100 characters.")]
        public string? DocumentNumber { get; set; }

        [MaxLength(200, ErrorMessage = "IssuedBy cannot exceed 200 characters.")]
        public string? IssuedBy { get; set; }

        public DateOnly? IssueDate { get; set; }

        public DateOnly? ExpiryDate { get; set; }

        [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        public string? Notes { get; set; }

        // pdf, jpg, png; tối đa 10 MB
        [Required(ErrorMessage = "File is required.")]
        public IFormFile? File { get; set; }
    }
}
