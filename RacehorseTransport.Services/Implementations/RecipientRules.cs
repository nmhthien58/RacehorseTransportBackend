using System.Globalization;
using System.Text;

namespace RacehorseTransport.Services.Implementations
{
    // R-16 (BE-15, Chính sách mục 10): người nhận ngựa. Hàm thuần để dễ viết unit test.
    public static class RecipientRules
    {
        // Khung giờ báo người nhận: ETA ± 1 giờ
        public static readonly TimeSpan WindowHalfWidth = TimeSpan.FromHours(1);

        // Nhắc khách xác nhận người nhận có mặt khi còn từ 24 giờ trở xuống tới ETA
        public static readonly TimeSpan ConfirmLeadTime = TimeSpan.FromHours(24);

        public static (DateTimeOffset Start, DateTimeOffset End) NotifiedWindow(DateTimeOffset eta) =>
            (eta - WindowHalfWidth, eta + WindowHalfWidth);

        public static bool NeedsConfirmRequest(DateTimeOffset eta, DateTimeOffset? requestedAt, DateTimeOffset? confirmedAt, DateTimeOffset now) =>
            requestedAt == null && confirmedAt == null && now < eta && eta - now <= ConfirmLeadTime;

        // So tên không phân biệt hoa thường, dấu tiếng Việt và khoảng trắng thừa ("Trần  Thị B" = "tran thi b")
        public static string NormalizeName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }
            string decomposed = name.Trim().Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
            }
            return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        // Người ký e-POD hợp lệ: đúng người nhận trong đơn, hoặc người nhận thay khách đã xác nhận.
        // Đơn chưa có người nhận (đơn cũ) thì không kiểm tra.
        public static bool IsAllowedSigner(string? signerName, string? recipientName, string? approvedAlternateName)
        {
            if (string.IsNullOrWhiteSpace(recipientName))
            {
                return true;
            }
            string signer = NormalizeName(signerName);
            return signer == NormalizeName(recipientName)
                || (!string.IsNullOrWhiteSpace(approvedAlternateName) && signer == NormalizeName(approvedAlternateName));
        }
    }
}
