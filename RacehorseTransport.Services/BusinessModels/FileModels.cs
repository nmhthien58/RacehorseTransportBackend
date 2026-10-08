namespace RacehorseTransport.Services.BusinessModels
{
    // File đã lưu. "Path" là thứ được ghi vào DB (file_url, photo_url, digital_signature_url);
    // "Url" là link xem có thời hạn, cấp lại bất cứ lúc nào bằng IFileStorageService.CreateSignedUrlAsync.
    public class StoredFileModel
    {
        public string Path { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTimeOffset UrlExpiresAt { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }

    public class SignedFileUrlModel
    {
        public string Path { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTimeOffset UrlExpiresAt { get; set; }
    }

    // Nhóm file, cũng là thư mục gốc trong bucket
    public static class FileCategories
    {
        public const string Documents = "documents";   // giấy tờ hồ sơ kiểm dịch
        public const string Horses = "horses";         // ảnh hồ sơ ngựa
        public const string Welfare = "welfare";       // ảnh nhật ký thể trạng
        public const string Signatures = "signatures"; // chữ ký bàn giao e-POD

        public static readonly string[] All = { Documents, Horses, Welfare, Signatures };
    }
}
