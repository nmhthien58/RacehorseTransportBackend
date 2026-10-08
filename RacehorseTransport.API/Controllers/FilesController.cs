using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.Implementations;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Controllers
{
    // Upload riêng một file và cấp link xem. Ảnh ngựa, giấy tờ hồ sơ và ảnh nhật ký được gửi kèm ngay trong
    // request nghiệp vụ (multipart) nên không cần upload ở đây trước.
    [ApiController]
    [Route("api/files")]
    [Produces("application/json")]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IFileStorageService _fileStorage;

        public FilesController(IFileStorageService fileStorage)
        {
            _fileStorage = fileStorage;
        }

        /// <summary>
        /// Upload a file (pdf, jpg, png; up to 10 MB). Returns the stored path and a temporary view URL.
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(SupabaseFileStorageService.MaxFileSizeBytes + 1024 * 1024)]
        [ProducesResponseType(typeof(ApiResponse<StoredFileResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Upload([FromForm] FileUploadRequest request)
        {
            var file = request.File!;

            await using var stream = file.OpenReadStream();
            var stored = await _fileStorage.UploadAsync(stream, file.FileName, file.Length, request.Category.Trim().ToLowerInvariant());

            var response = new StoredFileResponse
            {
                Path = stored.Path,
                Url = stored.Url,
                UrlExpiresAt = stored.UrlExpiresAt,
                FileName = stored.FileName,
                ContentType = stored.ContentType,
                SizeBytes = stored.SizeBytes
            };

            return StatusCode(
                StatusCodes.Status201Created,
                ApiResponse<StoredFileResponse>.SuccessResult(response, "File uploaded successfully"));
        }

        /// <summary>
        /// Get a fresh temporary view URL for a stored file path.
        /// </summary>
        [HttpGet("url")]
        [ProducesResponseType(typeof(ApiResponse<SignedFileUrlResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUrl([FromQuery] string path)
        {
            var signed = await _fileStorage.CreateSignedUrlAsync(path);

            var response = new SignedFileUrlResponse
            {
                Path = signed.Path,
                Url = signed.Url,
                UrlExpiresAt = signed.UrlExpiresAt
            };

            return Ok(ApiResponse<SignedFileUrlResponse>.SuccessResult(response));
        }
    }
}
