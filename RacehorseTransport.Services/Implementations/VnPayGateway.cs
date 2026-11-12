using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    // Cổng VNPay (phiên bản API 2.1.0), theo tài liệu sandbox.vnpayment.vn/apis/docs:
    //   - Tạo link: các tham số vnp_* sắp theo tên, mã hóa URL, ký HMAC-SHA512 bằng HashSecret, gắn vnp_SecureHash.
    //   - ReturnUrl / IPN: bỏ vnp_SecureHash(+Type), ký lại phần còn lại rồi so chữ ký.
    //   - Hoàn tiền: POST JSON tới merchant_webapi, chữ ký là HMAC-SHA512 của các trường nối bằng dấu '|'.
    // Thời gian gửi VNPay dùng giờ Việt Nam (GMT+7), dạng yyyyMMddHHmmss.
    public class VnPayGateway : IPaymentGateway
    {
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

        private readonly HttpClient _httpClient;
        private readonly VnPayOptions _options;
        private readonly ILogger<VnPayGateway> _logger;

        public VnPayGateway(HttpClient httpClient, IOptions<VnPayOptions> options, ILogger<VnPayGateway> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public string ProviderName => PaymentProviders.VnPay;

        public string CreatePaymentUrl(GatewayPaymentRequest request)
        {
            var data = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Version"] = _options.Version,
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = _options.TmnCode,
                ["vnp_Amount"] = (request.AmountVnd * 100).ToString(CultureInfo.InvariantCulture), // VNPay nhận số tiền x100
                ["vnp_CurrCode"] = "VND",
                ["vnp_TxnRef"] = request.TxnRef,
                ["vnp_OrderInfo"] = request.OrderInfo,
                ["vnp_OrderType"] = "other",
                ["vnp_Locale"] = _options.Locale,
                ["vnp_ReturnUrl"] = request.ReturnUrl,
                ["vnp_IpAddr"] = request.ClientIp,
                ["vnp_CreateDate"] = ToVnTime(request.CreatedAt),
                ["vnp_ExpireDate"] = ToVnTime(request.ExpiresAt)
            };

            string query = BuildQuery(data);
            string secureHash = HmacSha512(_options.HashSecret, query);
            return $"{_options.PaymentUrl}?{query}&vnp_SecureHash={secureHash}";
        }

        public GatewayCallbackData ParseCallback(IReadOnlyDictionary<string, string> query)
        {
            var data = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in query)
            {
                if (key.StartsWith("vnp_", StringComparison.Ordinal)
                    && key != "vnp_SecureHash" && key != "vnp_SecureHashType"
                    && !string.IsNullOrEmpty(value))
                {
                    data[key] = value;
                }
            }

            query.TryGetValue("vnp_SecureHash", out var receivedHash);
            string expectedHash = HmacSha512(_options.HashSecret, BuildQuery(data));

            long.TryParse(Get(data, "vnp_Amount"), NumberStyles.Integer, CultureInfo.InvariantCulture, out long amountX100);

            return new GatewayCallbackData
            {
                IsSignatureValid = !string.IsNullOrEmpty(receivedHash)
                    && string.Equals(receivedHash, expectedHash, StringComparison.OrdinalIgnoreCase),
                TxnRef = Get(data, "vnp_TxnRef") ?? string.Empty,
                AmountVnd = amountX100 / 100,
                ResponseCode = Get(data, "vnp_ResponseCode") ?? string.Empty,
                TransactionStatus = Get(data, "vnp_TransactionStatus") ?? string.Empty,
                TransactionNo = Get(data, "vnp_TransactionNo"),
                PayDate = Get(data, "vnp_PayDate"),
                BankCode = Get(data, "vnp_BankCode")
            };
        }

        public async Task<GatewayRefundResult> RefundAsync(GatewayRefundRequest request)
        {
            // vnp_RequestId không được trùng: mã thanh toán có thể lặp lại sau khi dựng lại dữ liệu demo, nên thêm hậu tố thời gian
            string requestId = $"{request.RefundTxnRef}{DateTimeOffset.UtcNow:HHmmssfff}";
            string createDate = ToVnTime(DateTimeOffset.UtcNow);
            string transactionType = request.IsFullRefund ? "02" : "03"; // 02 hoàn toàn phần, 03 hoàn một phần
            string amount = (request.AmountVnd * 100).ToString(CultureInfo.InvariantCulture);
            string transactionNo = request.OriginalTransactionNo ?? string.Empty;
            string transactionDate = request.OriginalPayDate ?? createDate;

            string signData = string.Join("|",
                requestId, _options.Version, "refund", _options.TmnCode, transactionType, request.OriginalTxnRef,
                amount, transactionNo, transactionDate, request.CreatedBy, createDate, request.ClientIp, request.OrderInfo);

            var body = new Dictionary<string, string>
            {
                ["vnp_RequestId"] = requestId,
                ["vnp_Version"] = _options.Version,
                ["vnp_Command"] = "refund",
                ["vnp_TmnCode"] = _options.TmnCode,
                ["vnp_TransactionType"] = transactionType,
                ["vnp_TxnRef"] = request.OriginalTxnRef,
                ["vnp_Amount"] = amount,
                ["vnp_OrderInfo"] = request.OrderInfo,
                ["vnp_TransactionNo"] = transactionNo,
                ["vnp_TransactionDate"] = transactionDate,
                ["vnp_CreateBy"] = request.CreatedBy,
                ["vnp_CreateDate"] = createDate,
                ["vnp_IpAddr"] = request.ClientIp,
                ["vnp_SecureHash"] = HmacSha512(_options.HashSecret, signData)
            };

            try
            {
                // Gửi body có Content-Length: VNPay (openresty) trả 403 trang HTML cho body dạng chunked,
                // mà PostAsJsonAsync lại gửi chunked
                using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(_options.ApiUrl, content);
                string raw = await response.Content.ReadAsStringAsync();
                if (!raw.TrimStart().StartsWith('{'))
                {
                    _logger.LogWarning("VNPay refund for {TxnRef} returned HTTP {Status} without JSON.", request.OriginalTxnRef, (int)response.StatusCode);
                    return new GatewayRefundResult
                    {
                        Success = false,
                        ResponseCode = "EX",
                        Message = $"VNPay answered HTTP {(int)response.StatusCode} without a JSON body"
                    };
                }

                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
                var root = doc.RootElement;
                string code = root.TryGetProperty("vnp_ResponseCode", out var c) ? c.GetString() ?? string.Empty : string.Empty;
                string message = root.TryGetProperty("vnp_Message", out var m) ? m.GetString() ?? string.Empty : string.Empty;
                string? txnNo = root.TryGetProperty("vnp_TransactionNo", out var t) ? t.ToString() : null;

                return new GatewayRefundResult
                {
                    Success = code == "00",
                    ResponseCode = code,
                    Message = string.IsNullOrEmpty(message) ? $"HTTP {(int)response.StatusCode}" : message,
                    TransactionNo = txnNo,
                    RawResponse = raw
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "VNPay refund for {TxnRef} timed out.", request.OriginalTxnRef);
                return new GatewayRefundResult
                {
                    Success = false,
                    ResponseCode = GatewayRefundResult.Unreachable,
                    Message = "VNPay did not answer in time (the refund API may not accept calls from this server's IP)"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "VNPay refund for {TxnRef} could not reach VNPay.", request.OriginalTxnRef);
                return new GatewayRefundResult { Success = false, ResponseCode = GatewayRefundResult.Unreachable, Message = ex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "VNPay refund for {TxnRef} failed.", request.OriginalTxnRef);
                return new GatewayRefundResult { Success = false, ResponseCode = "EX", Message = ex.Message };
            }
        }

        private static string BuildQuery(SortedDictionary<string, string> data)
        {
            return string.Join("&", data
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));
        }

        public static string HmacSha512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string ToVnTime(DateTimeOffset time) =>
            time.ToOffset(VietnamOffset).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        private static string? Get(IDictionary<string, string> data, string key) =>
            data.TryGetValue(key, out var value) ? value : null;
    }
}
