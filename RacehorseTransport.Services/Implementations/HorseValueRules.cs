using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Services.Implementations
{
    // R-12 / R-13: giá trị khai báo từng con ngựa khi tạo đơn. Hàm thuần để dễ viết unit test.
    //   - Khai theo từng con thì phải khai cho mọi con (tổng của đơn = tổng các con).
    //   - Giá trị từ Insurance.ProofThresholdUsd trở lên bắt buộc kèm chứng từ (hóa đơn mua, giấy định giá, kết quả đấu giá).
    //   - Gói bảo hiểm: None (mặc định) hoặc Full.
    public static class HorseValueRules
    {
        public sealed record HorseValue(string HorseName, decimal? DeclaredValue, string? ValueProofPath, string? InsurancePlan);

        // Danh sách lỗi; rỗng = hợp lệ. isValidProofPath kiểm tra đường dẫn do POST /api/files trả về.
        public static List<string> Validate(IReadOnlyList<HorseValue> horses, decimal proofThresholdUsd, Func<string, bool> isValidProofPath)
        {
            var errors = new List<string>();

            int declared = horses.Count(h => h.DeclaredValue.HasValue);
            if (declared > 0 && declared < horses.Count)
            {
                var missing = horses.Where(h => !h.DeclaredValue.HasValue).Select(h => h.HorseName);
                errors.Add($"Declare the value of every horse or of none. Missing: {string.Join(", ", missing)}.");
            }

            foreach (var horse in horses)
            {
                if (horse.DeclaredValue < 0)
                {
                    errors.Add($"{horse.HorseName}: DeclaredValue cannot be negative.");
                }
                else if (horse.DeclaredValue >= proofThresholdUsd && string.IsNullOrWhiteSpace(horse.ValueProofPath))
                {
                    errors.Add($"{horse.HorseName}: a value from {proofThresholdUsd:N0} USD needs a proof document (purchase invoice, valuation or auction result). Upload it with POST /api/files (category 'documents') and send its path as ValueProofPath.");
                }

                if (!string.IsNullOrWhiteSpace(horse.ValueProofPath) && !isValidProofPath(horse.ValueProofPath.Trim()))
                {
                    errors.Add($"{horse.HorseName}: ValueProofPath is not a valid uploaded file path.");
                }

                if (!string.IsNullOrWhiteSpace(horse.InsurancePlan) && !InsurancePlans.All.Contains(horse.InsurancePlan))
                {
                    errors.Add($"{horse.HorseName}: InsurancePlan '{horse.InsurancePlan}' is invalid. Allowed values: {string.Join(", ", InsurancePlans.All)}.");
                }
            }

            return errors;
        }

        // Giá trị dùng cho bảo hiểm / bồi thường: giá trị thỏa thuận, chưa chốt thì lấy giá trị khai báo
        public static decimal? EffectiveValue(decimal? agreedValue, decimal? declaredValue) => agreedValue ?? declaredValue;
    }
}
