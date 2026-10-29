namespace RacehorseTransport.Services.Implementations
{
    // Chính sách mục 7: hạn giao sớm nhất khách được chọn. Ước tính thời gian chạy không chính xác tới phút,
    // nên làm tròn lên hết ngày rồi cộng thêm Delivery.DeadlineBufferDays ngày dự phòng; so theo ngày.
    // Hàm thuần để dễ viết unit test.
    public static class DeliveryDeadlineRules
    {
        // latestPickupEnd = hết ngày cuối của khoảng đón (theo múi giờ khách gửi lên).
        // Tới đúng 00:00 ngày D nghĩa là xong trong ngày D-1.
        public static DateOnly EarliestDeliveryDate(DateTimeOffset latestPickupEnd, decimal maxTransitHours, int bufferDays)
        {
            var arrival = latestPickupEnd + TimeSpan.FromHours((double)maxTransitHours);
            var day = DateOnly.FromDateTime(arrival.DateTime);
            if (arrival.TimeOfDay == TimeSpan.Zero)
            {
                day = day.AddDays(-1);
            }
            return day.AddDays(Math.Max(0, bufferDays));
        }

        // Ngày của hạn giao lấy theo múi giờ của chính giá trị khách gửi
        public static bool IsDeadlineAllowed(DateTimeOffset deliveryDeadline, DateOnly earliestDeliveryDate) =>
            DateOnly.FromDateTime(deliveryDeadline.DateTime) >= earliestDeliveryDate;
    }
}
