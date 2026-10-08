using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    // Kiểm tra ID token của Google ngay tại server: chữ ký (bằng khóa công khai Google công bố), nơi phát hành,
    // ứng dụng nhận (ClientId của mình) và hạn dùng. Không gọi Google cho từng lần đăng nhập: khóa công khai được cache.
    public class GoogleIdTokenValidator : IGoogleTokenValidator
    {
        private static readonly string[] ValidIssuers = { "accounts.google.com", "https://accounts.google.com" };
        private static readonly TimeSpan KeyCacheTtl = TimeSpan.FromHours(6);

        private readonly HttpClient _http;
        private readonly ICacheService _cache;
        private readonly GoogleOptions _options;
        private readonly ILogger<GoogleIdTokenValidator> _logger;

        public GoogleIdTokenValidator(HttpClient http, ICacheService cache, IOptions<GoogleOptions> options, ILogger<GoogleIdTokenValidator> logger)
        {
            _http = http;
            _cache = cache;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<GoogleUserInfo> ValidateAsync(string idToken)
        {
            if (string.IsNullOrWhiteSpace(_options.ClientId))
            {
                throw new BusinessException("Google sign-in is not configured on this server.");
            }

            var result = await ValidateWithKeysAsync(idToken, await GetSigningKeysAsync(forceRefresh: false));

            // Google đổi khóa định kỳ: token ký bằng khóa mới hơn bản đang cache thì tải lại khóa rồi thử thêm một lần
            if (!result.IsValid && result.Exception is SecurityTokenSignatureKeyNotFoundException)
            {
                result = await ValidateWithKeysAsync(idToken, await GetSigningKeysAsync(forceRefresh: true));
            }

            if (!result.IsValid)
            {
                _logger.LogInformation("Rejected a Google ID token: {Reason}", result.Exception?.GetType().Name);
                throw new UnauthorizedException("Google sign-in failed: the Google token is invalid or has expired.");
            }

            string? email = result.Claims.TryGetValue("email", out object? emailClaim) ? emailClaim?.ToString() : null;
            bool emailVerified = result.Claims.TryGetValue("email_verified", out object? verifiedClaim)
                && string.Equals(verifiedClaim?.ToString(), "true", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(email) || !emailVerified)
            {
                throw new UnauthorizedException("Google sign-in failed: this Google account has no verified email address.");
            }

            return new GoogleUserInfo
            {
                Email = email.Trim().ToLowerInvariant(),
                FullName = result.Claims.TryGetValue("name", out object? nameClaim) ? nameClaim?.ToString() : null
            };
        }

        private async Task<TokenValidationResult> ValidateWithKeysAsync(string idToken, IEnumerable<SecurityKey> keys)
        {
            var parameters = new TokenValidationParameters
            {
                ValidIssuers = ValidIssuers,
                ValidAudience = _options.ClientId,
                IssuerSigningKeys = keys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            return await new JsonWebTokenHandler().ValidateTokenAsync(idToken, parameters);
        }

        private async Task<IList<SecurityKey>> GetSigningKeysAsync(bool forceRefresh)
        {
            string? json = forceRefresh ? null : await _cache.GetAsync<string>(CacheKeys.GoogleSigningKeys);
            if (json == null)
            {
                try
                {
                    json = await _http.GetStringAsync(_options.CertsUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not download Google's signing keys.");
                    throw new InvalidOperationException("Google sign-in is temporarily unavailable.");
                }
                await _cache.SetAsync(CacheKeys.GoogleSigningKeys, json, KeyCacheTtl);
            }

            return new JsonWebKeySet(json).GetSigningKeys();
        }
    }

    // Gửi mail bằng Gmail API qua HTTPS, thay cho SMTP (nhiều nền tảng chặn cổng SMTP).
    // Dùng refresh token của tài khoản Gmail gửi đi để xin access token ngắn hạn, rồi gọi messages.send.
    public class GmailApiEmailSender : IEmailSender
    {
        private readonly HttpClient _http;
        private readonly ICacheService _cache;
        private readonly EmailOptions _emailOptions;
        private readonly GoogleOptions _googleOptions;

        public GmailApiEmailSender(HttpClient http, ICacheService cache, IOptions<EmailOptions> emailOptions, IOptions<GoogleOptions> googleOptions)
        {
            _http = http;
            _cache = cache;
            _emailOptions = emailOptions.Value;
            _googleOptions = googleOptions.Value;
        }

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            string raw = Base64UrlEncoder.Encode(BuildMime(message));

            using var response = await PostAsync(raw, await GetAccessTokenAsync(forceRefresh: false, cancellationToken), cancellationToken);
            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                await EnsureSuccessAsync(response, "send", cancellationToken);
                return;
            }

            // Access token trong cache vừa hết hạn hoặc bị thu hồi: xin cái mới rồi gửi lại một lần
            using var retry = await PostAsync(raw, await GetAccessTokenAsync(forceRefresh: true, cancellationToken), cancellationToken);
            await EnsureSuccessAsync(retry, "send", cancellationToken);
        }

        private async Task<HttpResponseMessage> PostAsync(string raw, string accessToken, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _googleOptions.GmailSendUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(new { raw });
            return await _http.SendAsync(request, cancellationToken);
        }

        private async Task<string> GetAccessTokenAsync(bool forceRefresh, CancellationToken cancellationToken)
        {
            if (!forceRefresh)
            {
                string? cached = await _cache.GetAsync<string>(CacheKeys.GmailAccessToken);
                if (cached != null)
                {
                    return cached;
                }
            }

            if (string.IsNullOrWhiteSpace(_googleOptions.ClientId)
                || string.IsNullOrWhiteSpace(_googleOptions.ClientSecret)
                || string.IsNullOrWhiteSpace(_googleOptions.GmailRefreshToken))
            {
                throw new InvalidOperationException("Gmail API is not configured. Set Google:ClientId, Google:ClientSecret and Google:GmailRefreshToken.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, _googleOptions.TokenUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["client_id"] = _googleOptions.ClientId,
                    ["client_secret"] = _googleOptions.ClientSecret,
                    ["refresh_token"] = _googleOptions.GmailRefreshToken
                })
            };

            using var response = await _http.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, "token", cancellationToken);

            var body = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
            if (string.IsNullOrEmpty(body?.AccessToken))
            {
                throw new InvalidOperationException("Google did not return an access token.");
            }

            // Giữ trong cache ngắn hơn hạn thật một phút
            await _cache.SetAsync(CacheKeys.GmailAccessToken, body.AccessToken, TimeSpan.FromSeconds(Math.Max(60, body.ExpiresIn - 60)));
            return body.AccessToken;
        }

        // Lỗi "invalid_grant" ở bước token nghĩa là refresh token đã hết hạn hoặc bị thu hồi: phải lấy lại (docs/DEPLOY.md)
        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string step, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            string detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Gmail API {step} failed with {(int)response.StatusCode}: {detail}");
        }

        // Thư dạng MIME: tiêu đề và tên người gửi mã hóa theo RFC 2047 để giữ được tiếng Việt, thân là HTML UTF-8
        private byte[] BuildMime(EmailMessage message)
        {
            var mime = new StringBuilder();
            mime.Append("From: ").Append(Address(_emailOptions.FromName, _emailOptions.FromAddress)).Append("\r\n");
            mime.Append("To: ").Append(Address(message.ToName, message.ToAddress)).Append("\r\n");
            mime.Append("Subject: ").Append(EncodeWord(message.Subject)).Append("\r\n");
            mime.Append("MIME-Version: 1.0\r\n");
            mime.Append("Content-Type: text/html; charset=utf-8\r\n");
            mime.Append("Content-Transfer-Encoding: base64\r\n\r\n");
            mime.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(message.HtmlBody), Base64FormattingOptions.InsertLineBreaks));
            return Encoding.ASCII.GetBytes(mime.ToString());
        }

        private static string Address(string? name, string address)
        {
            return string.IsNullOrWhiteSpace(name) ? $"<{address}>" : $"{EncodeWord(name)} <{address}>";
        }

        private static string EncodeWord(string text)
        {
            return $"=?UTF-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes(text))}?=";
        }

        private class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
        }
    }
}
