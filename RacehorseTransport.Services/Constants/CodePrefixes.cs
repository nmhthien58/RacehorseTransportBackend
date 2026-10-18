namespace RacehorseTransport.Services.Constants
{
    // Tiền tố mã nghiệp vụ, dùng với ICodeGenerator.NextAsync
    public static class CodePrefixes
    {
        public const string Booking = "BKG";
        public const string Dossier = "DOS";
        public const string Trip = "TRP";
        public const string Incident = "INC";
        public const string Payment = "PAY";
    }

    // Giá trị cột reference_type của thông báo: cho FE biết bấm vào thông báo thì mở màn hình nào
    public static class NotificationReferenceTypes
    {
        public const string Booking = "Booking";
        public const string Dossier = "Dossier";
        public const string Trip = "Trip";
        public const string Incident = "Incident";
    }

    // Đường dẫn trang của khách hàng trên frontend (src/routes/routes.js), dùng làm {{Link}} trong email
    public static class CustomerPages
    {
        public static string Booking(int bookingId) => $"/customer/bookings/{bookingId}";
        public static string Trip(int tripId) => $"/customer/trips/{tripId}";
    }
}
