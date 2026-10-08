namespace RacehorseTransport.Services.Constants
{
    // Tiền tố mã nghiệp vụ, dùng với ICodeGenerator.NextAsync
    public static class CodePrefixes
    {
        public const string Booking = "BKG";
        public const string Dossier = "DOS";
        public const string Trip = "TRP";
        public const string Incident = "INC";
    }

    // Giá trị cột reference_type của thông báo: cho FE biết bấm vào thông báo thì mở màn hình nào
    public static class NotificationReferenceTypes
    {
        public const string Booking = "Booking";
        public const string Dossier = "Dossier";
        public const string Trip = "Trip";
        public const string Incident = "Incident";
    }
}
