using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RacehorseTransport.API.RequestModels
{
    public class DossierQueryRequest : QueryOptionsRequest
    {
        [FromQuery(Name = "status")]
        public string? Status { get; set; }

        [FromQuery(Name = "bookingId")]
        public int? BookingId { get; set; }
    }

    public class RequestDocumentsRequest
    {
        [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }
    }

    // File phải được upload trước qua POST /api/files (category "documents"); gửi "path" nhận được vào FilePath.
    public class UploadDocumentRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "DocTypeId is required.")]
        public int DocTypeId { get; set; }

        [MaxLength(100, ErrorMessage = "DocumentNumber cannot exceed 100 characters.")]
        public string? DocumentNumber { get; set; }

        [Required(ErrorMessage = "FilePath is required.")]
        [MaxLength(500, ErrorMessage = "FilePath cannot exceed 500 characters.")]
        public string FilePath { get; set; } = string.Empty;

        public DateOnly? ExpiryDate { get; set; }
    }

    public class RejectDocumentRequest
    {
        [Required(ErrorMessage = "CorrectionNote is required.")]
        [MaxLength(500, ErrorMessage = "CorrectionNote cannot exceed 500 characters.")]
        public string CorrectionNote { get; set; } = string.Empty;
    }

    public class ClearDossierRequest
    {
        [Required(ErrorMessage = "ClearanceNumber is required.")]
        [MaxLength(50, ErrorMessage = "ClearanceNumber cannot exceed 50 characters.")]
        public string ClearanceNumber { get; set; } = string.Empty;
    }

    public class DossierIssueRequest
    {
        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(2000, ErrorMessage = "Reason cannot exceed 2000 characters.")]
        public string Reason { get; set; } = string.Empty;
    }
}