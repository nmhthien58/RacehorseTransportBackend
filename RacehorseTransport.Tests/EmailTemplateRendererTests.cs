using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Implementations;
using System.Reflection;
using Xunit;

namespace RacehorseTransport.Tests;

public class EmailTemplateRendererTests
{
    private static readonly Dictionary<string, string> SampleValues = new()
    {
        ["FullName"] = "Nguyễn Văn A",
        ["Link"] = "https://racehorse.test/sample",
        ["ExpiresInMinutes"] = "60",
        ["Title"] = "Tiêu đề thông báo",
        ["Message"] = "Nội dung thông báo mẫu",
        ["BookingCode"] = "BKG-2026-0001",
        ["RejectionReason"] = "Lý do từ chối mẫu",
        ["HorseName"] = "Thunderbolt",
        ["DocumentRequest"] = "Giấy chứng nhận tiêm phòng",
        ["ClearanceNumber"] = "CLR-2026-9999",
        ["TripCode"] = "TRP-2026-0001",
        ["IncidentCode"] = "INC-2026-0001",
        ["Severity"] = "High",
        ["IncidentDescription"] = "Xe gặp sự cố kỹ thuật nhẹ",
        ["Destination"] = "Trang trại Hà Nội",
        ["DepartureDate"] = "2026-11-01",
        ["Amount"] = "1,500.00 USD",
        ["AmountVnd"] = "39.000.000 đ",
        ["DueDate"] = "2026-10-30",
        ["PriceUsd"] = "5,000.00",
        ["PriceVnd"] = "130.000.000",
        ["ExpiresAt"] = "2026-10-29 18:00",
        ["DocumentList"] = "Hộ chiếu ngựa, Xét nghiệm máu",
        ["Deadline"] = "2026-11-05 17:00",
        ["ReminderNumber"] = "1",
        ["RefundAmount"] = "750.00 USD",
        ["RecipientName"] = "Trần Thị B",
        ["HorseNames"] = "Thunderbolt, Starlight",
        ["ExpectedDate"] = "2026-11-02 10:00",
        ["ContactPhone"] = "0901234567",
        ["Eta"] = "2026-11-02 09:30",
        ["EtaWindow"] = "09:00 - 10:00 02/11/2026",
        ["DriverPhone"] = "0912345678",
        ["SignedAt"] = "2026-11-02 09:45",
        ["Condition"] = "Sức khỏe tốt, không thương tích",
        ["Reason"] = "Người nhận không có mặt tại điểm giao",
        ["DecisionDeadline"] = "2026-11-03 09:45",
        ["DelayHours"] = "14",
        ["ClaimCode"] = "CLM-2026-0001",
        ["Decision"] = "Chấp thuận bồi thường",
    };

    public static IEnumerable<object[]> GetAllTemplateNames()
    {
        var fields = typeof(EmailTemplateNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

        foreach (var field in fields)
        {
            var value = field.GetValue(null)?.ToString();
            if (!string.IsNullOrEmpty(value))
            {
                yield return new object[] { value };
            }
        }
    }

    [Theory]
    [MemberData(nameof(GetAllTemplateNames))]
    public void AllTemplates_RenderWithoutUnresolvedPlaceholders(string templateName)
    {
        var (subject, body) = EmailTemplateRenderer.Render(templateName, SampleValues);

        Assert.False(string.IsNullOrWhiteSpace(subject), $"Subject for template '{templateName}' was empty.");
        Assert.False(string.IsNullOrWhiteSpace(body), $"Body for template '{templateName}' was empty.");

        Assert.DoesNotContain("{{", subject);
        Assert.DoesNotContain("}}", subject);

        Assert.DoesNotContain("{{", body);
        Assert.DoesNotContain("}}", body);
    }
}
