namespace RacehorseTransport.Services.Options
{
    // Đọc từ section "Supabase" trong appsettings (điền ở appsettings.Local.json, không commit khóa)
    public class SupabaseOptions
    {
        public const string SectionName = "Supabase";

        public string Url { get; set; } = string.Empty;        // https://<mã project>.supabase.co
        public string ServiceKey { get; set; } = string.Empty; // khóa service_role / secret: chỉ dùng ở backend
        public string Bucket { get; set; } = string.Empty;     // bucket riêng tư

        // Thời hạn của link xem file cấp cho client
        public int SignedUrlMinutes { get; set; } = 60;
    }
}
