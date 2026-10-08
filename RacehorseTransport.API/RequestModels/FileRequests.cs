using System.ComponentModel.DataAnnotations;

namespace RacehorseTransport.API.RequestModels
{
    // multipart/form-data
    public class FileUploadRequest
    {
        [Required(ErrorMessage = "File is required.")]
        public IFormFile? File { get; set; }

        // documents | horses | welfare | signatures
        [Required(ErrorMessage = "Category is required.")]
        public string Category { get; set; } = string.Empty;
    }
}
