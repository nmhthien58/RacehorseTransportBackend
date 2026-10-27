using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Implementations
{
    // R-10: kiểm tra tuyến dự kiến. Hàm thuần để dễ viết unit test.
    //   - Mỗi lần qua biên giới phải có một mốc BorderGate (cửa khẩu nằm trên biên giới, tra nước có thể ra nước bên này
    //     hoặc bên kia). Một chặng được đi qua tối đa 1 + (số đầu chặng là BorderGate) nước: chặng thường 1 nước,
    //     chặng có một đầu là cửa khẩu 2 nước, chặng nối hai cửa khẩu 3 nước.
    //   - Nước quá cảnh = các nước tuyến đi qua, bỏ nước đi và nước đến, giữ thứ tự, không trùng.
    public static class RoutePlanRules
    {
        public static List<RoutePlanWarningModel> BorderWarnings(
            IReadOnlyList<string> stopTypes,
            IReadOnlyList<string> stopNames,
            IReadOnlyList<IReadOnlyList<string>> legCountries)
        {
            var warnings = new List<RoutePlanWarningModel>();
            for (int i = 0; i < legCountries.Count; i++)
            {
                var countries = legCountries[i];
                if (countries.Count <= 1)
                {
                    continue;
                }

                int gates = (stopTypes[i] == RouteStopTypes.BorderGate ? 1 : 0) + (stopTypes[i + 1] == RouteStopTypes.BorderGate ? 1 : 0);
                if (countries.Count <= 1 + gates)
                {
                    continue;
                }

                warnings.Add(new RoutePlanWarningModel
                {
                    FromIndex = i,
                    ToIndex = i + 1,
                    CountryCodes = countries.ToList(),
                    Message = $"The leg {stopNames[i]} → {stopNames[i + 1]} crosses {string.Join(" → ", countries)} without a BorderGate stop at each border; add the border gate(s) the trip will use."
                });
            }
            return warnings;
        }

        public static List<string> TransitCountries(IEnumerable<string> countriesAlong, string originCountry, string destinationCountry)
        {
            return countriesAlong
                .Where(c => !string.IsNullOrEmpty(c) && c != originCountry && c != destinationCountry)
                .Distinct()
                .ToList();
        }

        // Số lần qua biên giới = số lần đổi nước liên tiếp dọc tuyến
        public static int BorderCrossings(IReadOnlyList<string> countriesAlong) =>
            Math.Max(0, countriesAlong.Count - 1);
    }
}
