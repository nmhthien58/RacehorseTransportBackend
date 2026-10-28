using RacehorseTransport.Services.Exceptions;

namespace RacehorseTransport.Services.Implementations
{
    // BE-07: các điều kiện phải đúng ở cả lúc trình và lúc duyệt chuyến.
    public static class TripAssignmentRules
    {
        public static void EnsureDriverCount(decimal drivingHours, int driverCount, decimal singleDriverLimitHours)
        {
            if (drivingHours > singleDriverLimitHours && driverCount < 2)
            {
                throw new BusinessException(
                    $"A trip with {drivingHours:0.#} estimated driving hours needs at least 2 drivers; this trip has {driverCount}.");
            }
        }

        public static void EnsureCountriesAreApproved(
            string bookingCode,
            IEnumerable<string> actualCountryCodes,
            IEnumerable<string> approvedCountryCodes)
        {
            var approved = approvedCountryCodes
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(Normalize)
                .ToHashSet();
            var outside = actualCountryCodes
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(Normalize)
                .Where(code => !approved.Contains(code))
                .Distinct()
                .ToList();

            if (outside.Count > 0)
            {
                throw new BusinessException(
                    $"Trip route for booking {bookingCode} passes through country {string.Join(", ", outside)}, which is outside its approved route. Create and approve a new route plan first.");
            }
        }

        private static string Normalize(string code) => code.Trim().ToUpperInvariant();
    }
}
