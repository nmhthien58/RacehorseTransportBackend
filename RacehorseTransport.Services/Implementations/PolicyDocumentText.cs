using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RacehorseTransport.Services.Implementations
{
    // R-17: văn bản Chính sách dịch vụ (docs/CHINH_SACH_DICH_VU.md nhúng vào assembly) chuẩn bị để in làm phụ lục hợp đồng:
    // bỏ khối ghi chú cho nhóm phát triển, ghi phiên bản hiện hành, thay các giá trị [Khóa = mặc định] bằng cấu hình hiện hành.
    public static class PolicyDocumentText
    {
        private const string ResourceName = "RacehorseTransport.Services.PolicyDocument.md";
        private static readonly CultureInfo VietnameseCulture = new("vi-VN");

        // `[Payment.DepositPercent = 30]` hoặc [Payment.UsdVndRate = 26.000 đ/USD]: khóa, số mặc định, phần chữ phía sau
        private static readonly Regex ConfigValue = new(
            @"`?\[(?<key>[A-Za-z]+\.[A-Za-z0-9]+) = (?<number>[0-9][0-9.,]*)(?<suffix>[^\]]*)\]`?",
            RegexOptions.Compiled);

        private static readonly Lazy<string> Source = new(() =>
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
                ?? throw new InvalidOperationException($"Embedded resource {ResourceName} is missing.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        });

        public static string Load() => Source.Value;

        // values: khóa cấu hình -> giá trị dạng invariant ("5000", "1.5"); khóa không có giữ số mặc định của văn bản
        public static string Prepare(string markdown, IReadOnlyDictionary<string, string> values, string policyVersion)
        {
            var lines = markdown.Replace("\r\n", "\n").Split('\n')
                .Where(l => !l.TrimStart().StartsWith('>'))
                .Select(l => l.StartsWith("Phiên bản:", StringComparison.Ordinal) ? $"Phiên bản: {policyVersion}" : l);
            string text = string.Join("\n", lines);

            text = ConfigValue.Replace(text, m =>
            {
                string number = values.TryGetValue(m.Groups["key"].Value, out var raw)
                    && decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                    ? value.ToString("#,##0.##", VietnameseCulture)
                    : m.Groups["number"].Value;
                return number + m.Groups["suffix"].Value;
            });

            // Gộp các dòng trống liền nhau còn lại sau khi bỏ khối ghi chú
            return Regex.Replace(text, @"\n{3,}", "\n\n").Trim() + "\n";
        }
    }
}
