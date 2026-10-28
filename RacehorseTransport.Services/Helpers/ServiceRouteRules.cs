using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Services.Helpers
{
    // R-18: một tuyến A -> D đặt được bằng một phương thức khi nào.
    //   - Cả nước đi và nước đến đều nằm trong phạm vi dịch vụ (đang bật).
    //   - Nước đến có dòng cước FREIGHT đang bật cho phương thức đó (công ty đã có giá cho tuyến).
    //   - Đường bay: nước đi và nước đến đều có ít nhất một sân bay đang mở.
    // Đường bộ giữa hai nước bất kỳ có đi được không (vd Nhật -> Hàn) thì hệ thống không tự biết: Manager xét khi duyệt đơn.
    public static class ServiceRouteRules
    {
        public sealed record CountryState(string Code, bool IsActive, bool GroundFreightActive, bool AirFreightActive, int ActiveAirports);

        public static bool GroundAvailable(CountryState destination) =>
            destination.IsActive && destination.GroundFreightActive;

        public static bool AirAvailable(CountryState destination) =>
            destination.IsActive && destination.AirFreightActive && destination.ActiveAirports > 0;

        // Trả về lý do không đặt được (tiếng Anh, gửi thẳng cho khách), hoặc null nếu đặt được
        public static string? Problem(string transportMode, string originCode, CountryState? origin, string destinationCode, CountryState? destination)
        {
            if (origin == null || !origin.IsActive)
            {
                return $"We do not currently pick up horses in country '{originCode}'.";
            }
            if (destination == null || !destination.IsActive)
            {
                return $"We do not currently deliver horses to country '{destinationCode}'.";
            }

            if (transportMode == TransportModes.Ground && !GroundAvailable(destination))
            {
                return $"Ground transport to '{destinationCode}' is not offered at the moment. Please choose air transport.";
            }

            if (transportMode == TransportModes.Air)
            {
                if (!AirAvailable(destination))
                {
                    return $"Air transport to '{destinationCode}' is not offered at the moment. Please choose ground transport.";
                }
                if (origin.ActiveAirports == 0)
                {
                    return $"There is no airport we can fly horses from in '{originCode}'. Please choose ground transport.";
                }
            }

            return null;
        }
    }
}
