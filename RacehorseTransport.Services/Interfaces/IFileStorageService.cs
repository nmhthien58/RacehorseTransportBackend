using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // Lưu file lên cloud storage. Nghiệp vụ chỉ biết interface này, nên đổi nhà cung cấp không phải sửa các service khác.
    public interface IFileStorageService
    {
        // Kiểm tra loại và dung lượng file, lưu vào thư mục của category, trả về đường dẫn và link xem tạm
        Task<StoredFileModel> UploadAsync(Stream content, string fileName, long sizeBytes, string category);

        // Cấp link xem có thời hạn cho một đường dẫn đã lưu
        Task<SignedFileUrlModel> CreateSignedUrlAsync(string path);

        // Đổi nhiều đường dẫn đã lưu thành link xem có thời hạn, để trả kèm trong response (một lần gọi storage, có cache).
        // Kết quả: đường dẫn -> link. Giá trị đã là http(s) được giữ nguyên; đường dẫn sai hoặc file không còn thì không có trong kết quả.
        // Storage lỗi không làm hỏng request: các đường dẫn chưa đổi được chỉ đơn giản là vắng mặt.
        Task<IReadOnlyDictionary<string, string>> GetViewUrlsAsync(IEnumerable<string?> paths);

        // true nếu chuỗi đúng dạng đường dẫn do UploadAsync sinh ra (dùng để kiểm tra dữ liệu client gửi lên)
        bool IsValidPath(string? path, string? category = null);
    }
}
