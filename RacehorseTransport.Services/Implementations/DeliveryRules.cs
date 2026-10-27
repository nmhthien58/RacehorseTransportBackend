namespace RacehorseTransport.Services.Implementations
{
    // R-16 (BE-16, Chính sách 10.4): phí chờ tại điểm giao. Hàm thuần để dễ viết unit test.
    public static class DeliveryRules
    {
        // Giờ chờ tính phí = (lúc ký e-POD / ghi không giao được) - lúc xe check-in điểm giao - giờ miễn phí, làm tròn lên, tối đa maxHours.
        // Chỉ tính khi xe tới đúng hoặc sau đầu khung giờ đã báo người nhận (tới sớm hơn, hoặc chưa báo khung giờ nào, thì không tính).
        public static int WaitingHours(DateTimeOffset arrivedAt, DateTimeOffset endAt, DateTimeOffset? notifiedWindowStart, int freeHours, int maxHours)
        {
            if (notifiedWindowStart == null || arrivedAt < notifiedWindowStart.Value)
            {
                return 0;
            }
            // Tính theo phút đầy đủ (bỏ phần giây lẻ), phần giờ chưa đủ tính tròn lên
            long billableMinutes = (long)Math.Floor((endAt - arrivedAt).TotalMinutes) - freeHours * 60L;
            if (billableMinutes <= 0)
            {
                return 0;
            }
            return (int)Math.Min((billableMinutes + 59) / 60, Math.Max(0, maxHours));
        }

        public static decimal WaitingFee(int hours, decimal feePerHour) => hours * feePerHour;

        // Giao cho người nhận / địa chỉ khác: số km thêm (làm tròn lên) × phí mỗi km
        public static decimal ExtraDistanceCost(decimal extraKm, decimal feePerKm) =>
            extraKm <= 0 ? 0 : Math.Ceiling(extraKm) * feePerKm;
    }
}
