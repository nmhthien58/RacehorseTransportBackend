using System.ComponentModel.DataAnnotations;

namespace RacehorseTransport.API.RequestModels
{
    public class CreateDocumentTypeRequest
    {
        [Required(ErrorMessage = "Code is required.")]
        [MaxLength(30, ErrorMessage = "Code cannot exceed 30 characters.")]
        [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "Code may only contain letters, digits and underscores.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        public string? Description { get; set; }

        public bool IsMandatory { get; set; } = true;

        /// <summary>Ai lo giấy này: Customer (khách nộp) hoặc Company (chuyên viên công ty xin).</summary>
        [RegularExpression("^(Customer|Company)$", ErrorMessage = "ProvidedBy must be Customer or Company.")]
        public string ProvidedBy { get; set; } = "Customer";
    }

    public class UpdateDocumentTypeRequest
    {
        [Required(ErrorMessage = "Code is required.")]
        [MaxLength(30, ErrorMessage = "Code cannot exceed 30 characters.")]
        [RegularExpression("^[A-Za-z0-9_]+$", ErrorMessage = "Code may only contain letters, digits and underscores.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        public string? Description { get; set; }

        public bool IsMandatory { get; set; } = true;

        /// <summary>Ai lo giấy này: Customer (khách nộp) hoặc Company (chuyên viên công ty xin).</summary>
        [RegularExpression("^(Customer|Company)$", ErrorMessage = "ProvidedBy must be Customer or Company.")]
        public string ProvidedBy { get; set; } = "Customer";
    }
}
