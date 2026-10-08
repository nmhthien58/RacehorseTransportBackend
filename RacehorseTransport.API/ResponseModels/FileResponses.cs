namespace RacehorseTransport.API.ResponseModels
{
    public class StoredFileResponse
    {
        // Giá trị cần gửi lại trong các request khác (fileUrl của giấy tờ, photoUrl, chữ ký...)
        public string Path { get; set; } = string.Empty;

        // Link xem tạm thời; hết hạn thì gọi GET /api/files/url để lấy link mới
        public string Url { get; set; } = string.Empty;
        public DateTimeOffset UrlExpiresAt { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }

    public class SignedFileUrlResponse
    {
        public string Path { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTimeOffset UrlExpiresAt { get; set; }
    }
}
