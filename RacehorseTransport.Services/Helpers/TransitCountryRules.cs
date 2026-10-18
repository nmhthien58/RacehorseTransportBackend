using System.Text.RegularExpressions;
using RacehorseTransport.Services.Exceptions;

namespace RacehorseTransport.Services.Helpers
{
    // Chuẩn hóa và kiểm tra danh sách nước quá cảnh của một đơn (R-03)
    public static class TransitCountryRules
    {
        public const int MaxTransitCountries = 5;

        private static readonly Regex CountryCodePattern = new("^[A-Z]{2}$", RegexOptions.Compiled);

        // Viết hoa, bỏ khoảng trắng, giữ thứ tự; báo lỗi khi sai mã, trùng nhau hoặc trùng nước đi / nước đến
        public static List<string> Normalize(IEnumerable<string>? codes, string originCountry, string destinationCountry)
        {
            var result = (codes ?? Enumerable.Empty<string>())
                .Select(c => (c ?? string.Empty).Trim().ToUpperInvariant())
                .ToList();

            var invalid = result.FirstOrDefault(c => !CountryCodePattern.IsMatch(c));
            if (invalid != null)
            {
                throw new BusinessException($"Transit country code '{invalid}' must be a 2-letter country code.");
            }
            if (result.Count != result.Distinct().Count())
            {
                throw new BusinessException("The same transit country is listed more than once.");
            }

            string origin = (originCountry ?? string.Empty).Trim().ToUpperInvariant();
            string destination = (destinationCountry ?? string.Empty).Trim().ToUpperInvariant();
            if (result.Contains(origin) || result.Contains(destination))
            {
                throw new BusinessException("Transit countries cannot include the origin or the destination country.");
            }
            if (result.Count > MaxTransitCountries)
            {
                throw new BusinessException($"A route can pass through at most {MaxTransitCountries} transit countries.");
            }

            return result;
        }
    }
}
