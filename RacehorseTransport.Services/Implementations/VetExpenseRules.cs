using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.Services.Implementations
{
    // Ai chịu viện phí khi ngựa ở lại phòng khám (Chính sách 9.6). Hàm thuần để dễ viết unit test.
    public static class VetExpenseRules
    {
        // Lỗi công ty: công ty chịu. Ngựa tự phát bệnh / bất khả kháng / lỗi khách: chủ ngựa chịu.
        // Chủ ngựa từ chối khuyến nghị an tử: chủ ngựa chịu chi phí điều trị tiếp, kể cả khi sự cố do lỗi công ty.
        public static string Bearer(string faultParty, bool euthanasiaDeclined)
        {
            if (euthanasiaDeclined)
            {
                return VetExpenseBearers.Customer;
            }
            return faultParty == FaultParties.Company ? VetExpenseBearers.Company : VetExpenseBearers.Customer;
        }
    }
}
