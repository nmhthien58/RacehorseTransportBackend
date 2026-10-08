using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Helpers
{
    public static class FormFileExtensions
    {
        // File trong form multipart -> model cho tầng Service. Trả null khi client không gửi file.
        public static FileUploadModel? ToUploadModel(this IFormFile? file)
        {
            if (file == null)
            {
                return null;
            }

            return new FileUploadModel
            {
                Content = file.OpenReadStream(),
                FileName = file.FileName,
                SizeBytes = file.Length
            };
        }
    }
}
