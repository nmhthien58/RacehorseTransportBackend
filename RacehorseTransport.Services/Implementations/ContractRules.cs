using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.Exceptions;

namespace RacehorseTransport.Services.Implementations
{
    // R-17: kiểm tra trước khi khách chấp nhận một bản hợp đồng. Hàm thuần để dễ viết unit test.
    public static class ContractRules
    {
        public const string ChangedMessage = "The contract has changed since you opened it. Please review the latest version.";

        // contractStatus: trạng thái bản khách gửi lên; storedSha256: mã băm đã lưu; sentSha256: mã băm khách đang xem
        public static void EnsureAcceptable(string contractStatus, bool isCurrentDraft, string storedSha256, string? sentSha256)
        {
            if (contractStatus != ContractStatuses.Draft || !isCurrentDraft)
            {
                throw new BusinessException(ChangedMessage);
            }
            if (string.IsNullOrWhiteSpace(sentSha256)
                || !string.Equals(storedSha256, sentSha256.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException(ChangedMessage);
            }
        }

        // Bản mới luôn có số phiên bản lớn hơn mọi bản trước
        public static int NextVersion(IEnumerable<int> existingVersions) =>
            existingVersions.DefaultIfEmpty(0).Max() + 1;

        // IP từ X-Forwarded-For (Railway chạy sau proxy) cắt gọn cho vừa cột
        public static string? Truncate(string? value, int max) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length <= max ? value.Trim() : value.Trim()[..max];
    }
}
