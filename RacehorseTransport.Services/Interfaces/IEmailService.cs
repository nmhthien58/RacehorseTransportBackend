using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // Các service nghiệp vụ chỉ gọi interface này. Mail được xếp vào hàng đợi và gửi ở nền:
    // lời gọi trả về ngay và không bao giờ ném lỗi, nên gửi mail hỏng không làm hỏng request.
    public interface IEmailService
    {
        // templateName: tên file trong EmailTemplates (không có đuôi), ví dụ EmailTemplateNames.ResetPassword.
        // values: giá trị cho các chỗ {{Tên}} trong mẫu.
        void Queue(string toAddress, string? toName, string templateName, IDictionary<string, string> values);
    }

    // Hàng đợi trong bộ nhớ giữa request và job gửi mail chạy nền
    public interface IEmailQueue
    {
        bool TryEnqueue(EmailMessage message);
        IAsyncEnumerable<EmailMessage> ReadAllAsync(CancellationToken cancellationToken);
    }

    // Thực sự gửi một email đi (SMTP hoặc API của nhà cung cấp). Đổi nhà cung cấp chỉ cần thêm một lớp cài interface này.
    public interface IEmailSender
    {
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
    }
}
