using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    // Dựng email từ mẫu rồi xếp vào hàng đợi. Job EmailDispatchJob (API/BackgroundJobs) lấy ra và gửi.
    public class EmailService : IEmailService
    {
        private readonly IEmailQueue _queue;
        private readonly EmailOptions _options;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IEmailQueue queue, IOptions<EmailOptions> options, ILogger<EmailService> logger)
        {
            _queue = queue;
            _options = options.Value;
            _logger = logger;
        }

        public void Queue(string toAddress, string? toName, string templateName, IDictionary<string, string> values)
        {
            try
            {
                var (subject, body) = EmailTemplateRenderer.Render(templateName, values);

                if (string.IsNullOrWhiteSpace(_options.Provider))
                {
                    // Chưa cấu hình gửi mail (thường là máy dev): ghi ra log để vẫn lấy được link xác thực / đặt lại mật khẩu
                    values.TryGetValue("Link", out string? link);
                    _logger.LogInformation("Email is not configured; skipped '{Subject}' to {To}. Link: {Link}", subject, toAddress, link ?? "(none)");
                    return;
                }

                bool queued = _queue.TryEnqueue(new EmailMessage { ToAddress = toAddress, ToName = toName, Subject = subject, HtmlBody = body });
                if (!queued)
                {
                    _logger.LogWarning("Email queue is full; dropped '{Subject}' to {To}.", subject, toAddress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not prepare email '{Template}' to {To}.", templateName, toAddress);
            }
        }
    }

    // Mẫu email là file .html nhúng trong assembly (thư mục EmailTemplates). Dòng đầu là tiêu đề: "Subject: ...".
    // Chỗ cần điền viết dạng {{Tên}}. Giá trị điền vào thân mail được mã hóa HTML.
    public static class EmailTemplateRenderer
    {
        private static readonly Regex Placeholder = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

        public static (string Subject, string HtmlBody) Render(string templateName, IDictionary<string, string> values)
        {
            string template = Load(templateName);

            int lineEnd = template.IndexOf('\n');
            string firstLine = (lineEnd < 0 ? template : template[..lineEnd]).Trim();
            if (!firstLine.StartsWith("Subject:", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Email template '{templateName}' must start with a 'Subject:' line.");
            }

            string subject = Fill(firstLine["Subject:".Length..].Trim(), values, encode: false);
            string body = Fill(lineEnd < 0 ? string.Empty : template[(lineEnd + 1)..].Trim(), values, encode: true);
            return (subject, body);
        }

        private static string Fill(string text, IDictionary<string, string> values, bool encode)
        {
            return Placeholder.Replace(text, match =>
            {
                string value = values.TryGetValue(match.Groups[1].Value, out string? found) ? found : string.Empty;
                return encode ? EscapeHtml(value) : value;
            });
        }

        // Chỉ thoát các ký tự có nghĩa trong HTML; chữ tiếng Việt giữ nguyên (mail gửi bằng UTF-8)
        private static string EscapeHtml(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        private static string Load(string templateName)
        {
            var assembly = typeof(EmailTemplateRenderer).Assembly;
            string resource = $"{assembly.GetName().Name}.EmailTemplates.{templateName}.html";

            using Stream? stream = assembly.GetManifestResourceStream(resource);
            if (stream == null)
            {
                throw new InvalidOperationException($"Email template '{templateName}' was not found (expected EmailTemplates/{templateName}.html).");
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    // Hàng đợi có giới hạn: đầy thì bỏ mail mới thay vì làm chậm request
    public class EmailQueue : IEmailQueue
    {
        private readonly Channel<EmailMessage> _channel = Channel.CreateBounded<EmailMessage>(
            new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });

        public bool TryEnqueue(EmailMessage message) => _channel.Writer.TryWrite(message);

        public IAsyncEnumerable<EmailMessage> ReadAllAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAllAsync(cancellationToken);
    }

    // Gửi qua SMTP (Gmail: smtp.gmail.com, cổng 587, tài khoản + app password)
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailOptions _options;

        public SmtpEmailSender(IOptions<EmailOptions> options)
        {
            _options = options.Value;
        }

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            var smtp = _options.Smtp;

            using var client = new SmtpClient(smtp.Host, smtp.Port) { EnableSsl = smtp.EnableSsl, Timeout = 30000 };
            if (!string.IsNullOrWhiteSpace(smtp.Username))
            {
                client.Credentials = new NetworkCredential(smtp.Username, smtp.Password);
            }

            using var mail = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = message.Subject,
                Body = message.HtmlBody,
                IsBodyHtml = true
            };
            mail.To.Add(new MailAddress(message.ToAddress, message.ToName));

            await client.SendMailAsync(mail, cancellationToken);
        }
    }

    // Dùng khi Email:Provider bỏ trống. EmailService đã không xếp mail vào hàng đợi trong trường hợp này.
    public class DisabledEmailSender : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
