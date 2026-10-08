using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    // Supabase Storage qua REST API. Bucket để riêng tư: file chỉ xem được bằng link có chữ ký, hết hạn sau SignedUrlMinutes.
    public class SupabaseFileStorageService : IFileStorageService
    {
        public const long MaxFileSizeBytes = 10 * 1024 * 1024;

        // Đuôi file được nhận và content type tương ứng (không tin content type client gửi lên)
        private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

        // <category>/<năm>/<tháng>/<guid>.<đuôi>
        private static readonly Regex PathPattern = new(
            @"^(?<category>documents|horses|welfare|signatures)/\d{4}/\d{2}/[0-9a-f]{32}\.(pdf|jpg|jpeg|png)$",
            RegexOptions.Compiled);

        private readonly HttpClient _http;
        private readonly SupabaseOptions _options;
        private readonly ILogger<SupabaseFileStorageService> _logger;

        public SupabaseFileStorageService(HttpClient http, IOptions<SupabaseOptions> options, ILogger<SupabaseFileStorageService> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<StoredFileModel> UploadAsync(Stream content, string fileName, long sizeBytes, string category)
        {
            if (!FileCategories.All.Contains(category))
            {
                throw new BusinessException($"Category '{category}' is invalid. Allowed values: {string.Join(", ", FileCategories.All)}.");
            }

            string extension = Path.GetExtension(fileName);
            if (!AllowedTypes.TryGetValue(extension, out string? contentType))
            {
                throw new BusinessException($"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", AllowedTypes.Keys)}.");
            }

            // Chỉ giấy tờ hồ sơ mới nhận PDF; ảnh ngựa, ảnh nhật ký và chữ ký phải là ảnh
            if (category != FileCategories.Documents && contentType == "application/pdf")
            {
                throw new BusinessException($"Files in category '{category}' must be images (.jpg, .jpeg, .png).");
            }

            if (sizeBytes <= 0)
            {
                throw new BusinessException("The file is empty.");
            }
            if (sizeBytes > MaxFileSizeBytes)
            {
                throw new BusinessException($"The file is too large. Maximum size is {MaxFileSizeBytes / (1024 * 1024)} MB.");
            }

            // Đọc cả file vào bộ nhớ (tối đa 10 MB) để kiểm tra nội dung thật trước khi gửi đi
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer);
            byte[] bytes = buffer.ToArray();

            if (!MatchesSignature(bytes, contentType))
            {
                throw new BusinessException($"The file content does not match its extension '{extension}'.");
            }

            EnsureConfigured();

            var now = DateTimeOffset.UtcNow;
            string path = $"{category}/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

            using var request = new HttpRequestMessage(HttpMethod.Post, ObjectUrl("object", path));
            Authorize(request);
            request.Content = new ByteArrayContent(bytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            using var response = await _http.SendAsync(request);
            await EnsureSuccessAsync(response, "upload");

            var signed = await CreateSignedUrlAsync(path);
            return new StoredFileModel
            {
                Path = path,
                Url = signed.Url,
                UrlExpiresAt = signed.UrlExpiresAt,
                FileName = Path.GetFileName(fileName),
                ContentType = contentType,
                SizeBytes = bytes.LongLength
            };
        }

        public async Task<SignedFileUrlModel> CreateSignedUrlAsync(string path)
        {
            if (!IsValidPath(path))
            {
                throw new BusinessException("The file path is invalid.");
            }

            EnsureConfigured();

            int expiresInSeconds = Math.Max(1, _options.SignedUrlMinutes) * 60;

            using var request = new HttpRequestMessage(HttpMethod.Post, ObjectUrl("object/sign", path));
            Authorize(request);
            request.Content = JsonContent.Create(new { expiresIn = expiresInSeconds });

            using var response = await _http.SendAsync(request);
            await EnsureSuccessAsync(response, "sign");

            var body = await response.Content.ReadFromJsonAsync<SignResponse>();
            if (string.IsNullOrEmpty(body?.SignedUrl))
            {
                throw new InvalidOperationException("File storage did not return a signed URL.");
            }

            // Supabase trả về đường dẫn tương đối: /object/sign/<bucket>/<path>?token=...
            string relative = body.SignedUrl.StartsWith('/') ? body.SignedUrl : "/" + body.SignedUrl;
            return new SignedFileUrlModel
            {
                Path = path,
                Url = $"{_options.Url.TrimEnd('/')}/storage/v1{relative}",
                UrlExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds)
            };
        }

        public bool IsValidPath(string? path, string? category = null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var match = PathPattern.Match(path);
            return match.Success && (category == null || match.Groups["category"].Value == category);
        }

        private string ObjectUrl(string operation, string path)
        {
            return $"{_options.Url.TrimEnd('/')}/storage/v1/{operation}/{_options.Bucket}/{path}";
        }

        private void Authorize(HttpRequestMessage request)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceKey);
            request.Headers.Add("apikey", _options.ServiceKey);
        }

        private void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_options.Url)
                || string.IsNullOrWhiteSpace(_options.Bucket)
                || string.IsNullOrWhiteSpace(_options.ServiceKey)
                || _options.ServiceKey.StartsWith('<'))
            {
                throw new InvalidOperationException(
                    "File storage is not configured. Set Supabase:Url, Supabase:ServiceKey and Supabase:Bucket in appsettings.Local.json.");
            }
        }

        private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            // Ghi chi tiết vào log của server; không trả nội dung lỗi của nhà cung cấp ra cho client
            string detail = await response.Content.ReadAsStringAsync();

            // File không tồn tại: Supabase trả 404, hoặc 400 kèm "not_found" trong nội dung
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound
                || detail.Contains("not_found", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("Object not found", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotFoundException("The file was not found in storage.");
            }

            _logger.LogError("File storage {Operation} failed with {StatusCode}: {Detail}", operation, (int)response.StatusCode, detail);
            throw new InvalidOperationException($"File storage {operation} failed (status {(int)response.StatusCode}).");
        }

        // So vài byte đầu của file với chữ ký của định dạng, để chặn file đổi đuôi
        private static bool MatchesSignature(byte[] bytes, string contentType)
        {
            return contentType switch
            {
                "application/pdf" => StartsWith(bytes, 0x25, 0x50, 0x44, 0x46),       // %PDF
                "image/png" => StartsWith(bytes, 0x89, 0x50, 0x4E, 0x47),            // .PNG
                "image/jpeg" => StartsWith(bytes, 0xFF, 0xD8, 0xFF),
                _ => false
            };
        }

        private static bool StartsWith(byte[] bytes, params byte[] signature)
        {
            return bytes.Length >= signature.Length && bytes.AsSpan(0, signature.Length).SequenceEqual(signature);
        }

        private class SignResponse
        {
            [JsonPropertyName("signedURL")]
            public string? SignedUrl { get; set; }
        }
    }
}
