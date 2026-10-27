using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // Cấu hình hệ thống Admin / Manager sửa được (hiện có nhóm cấu hình thanh toán)
    public interface ISystemSettingService
    {
        Task<PaymentSettingsModel> GetPaymentSettingsAsync();
        Task<PaymentSettingsModel> UpdatePaymentSettingsAsync(PaymentSettingsModel model, CurrentUser currentUser);

        // BE-17: cấu hình dùng trong Chính sách dịch vụ (báo giá, giao trễ, bảo hiểm, ngựa, giao nhận, phiên bản)
        Task<PolicySettingsModel> GetPolicySettingsAsync();
        Task<PolicySettingsModel> UpdatePolicySettingsAsync(PolicySettingsModel model, CurrentUser currentUser);

        // Trang Chính sách công khai: phiên bản + mọi giá trị cấu hình chính sách nhắc tới (kể cả Payment.*)
        Task<PublicPolicyModel> GetPublicPolicyAsync();
    }

    // Một cổng thanh toán (VNPay thật hoặc cổng giả lập)
    public interface IPaymentGateway
    {
        string ProviderName { get; }

        // Link chuyển khách sang trang thanh toán
        string CreatePaymentUrl(GatewayPaymentRequest request);

        // Đọc và kiểm tra chữ ký dữ liệu cổng gửi về (ReturnUrl / IPN)
        GatewayCallbackData ParseCallback(IReadOnlyDictionary<string, string> query);

        Task<GatewayRefundResult> RefundAsync(GatewayRefundRequest request);
    }

    // Chọn cổng thanh toán theo tên provider (VnPay / Mock)
    public interface IPaymentGatewayResolver
    {
        IPaymentGateway Current { get; }   // cổng dùng cho giao dịch mới
        IPaymentGateway Get(string provider);
    }

    // Đơn chuyển sang Approved sau khi đã cọc: mở hồ sơ kiểm dịch cho từng con ngựa, báo khách và Specialist
    public interface IBookingActivator
    {
        Task ActivateAsync(BookingRequest booking);
    }

    public interface IPaymentService
    {
        Task<BookingPaymentSummaryModel> GetBookingPaymentsAsync(int bookingId, CurrentUser currentUser);

        // Trang Thanh toán của khách: mọi đơn có thanh toán + mọi giao dịch của khách
        Task<CustomerBillingModel> GetMyBillingAsync(CurrentUser currentUser);
        CustomerBillingModel BuildCustomerBilling(IEnumerable<BookingRequest> bookings, PaymentSettingsModel settings);

        // Tính tình hình tiền từ đơn đã nạp kèm Payments (và chuyến của ngựa để biết hạn trả phần còn lại)
        BookingPaymentSummaryModel BuildSummary(BookingRequest booking, PaymentSettingsModel settings);

        Task<CreatePaymentResultModel> CreatePaymentAsync(int bookingId, string paymentType, string clientIp, CurrentUser currentUser);

        // VNPay gọi lại (IPN) hoặc khách quay về (ReturnUrl): cả hai đều kiểm tra chữ ký rồi cập nhật giao dịch
        Task<PaymentCallbackResultModel> HandleGatewayCallbackAsync(IReadOnlyDictionary<string, string> query);

        // Cổng giả lập: khách bấm "Thanh toán thành công / thất bại"
        Task<PaymentCallbackResultModel> CompleteMockPaymentAsync(string paymentCode, bool success, CurrentUser currentUser);

        Task<PaymentModel> RecordManualPaymentAsync(int bookingId, ManualPaymentModel model, CurrentUser currentUser);

        // Khách hủy đơn: hoàn % tiền cọc theo cấu hình + toàn bộ phần còn lại đã trả
        // fullRefund: hoàn 100% cả tiền cọc (R-10: khách hủy vì công ty đổi tuyến thêm nước mới)
        Task<List<PaymentModel>> RefundForCancellationAsync(BookingRequest booking, CurrentUser currentUser, bool fullRefund = false);

        Task<PaymentModel> RetryRefundAsync(int refundPaymentId, CurrentUser currentUser);
        Task<PaymentModel> MarkRefundedManuallyAsync(int refundPaymentId, string reference, string? note, CurrentUser currentUser);

        // Nhắc nợ chi phí phát sinh quá hạn (ngày đến hạn, +3, +7); trả về số đơn đã nhắc
        Task<int> SendOverdueRemindersAsync();

        // Khách có khoản phát sinh quá hạn chưa trả thì không được tạo đơn mới
        Task<bool> HasOverdueDebtAsync(int customerUserId);
    }
}
