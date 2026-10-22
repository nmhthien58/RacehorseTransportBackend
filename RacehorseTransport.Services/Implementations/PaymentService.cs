using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;

namespace RacehorseTransport.Services.Implementations
{
    // R-02 Thanh toán. Quy trình đã chốt (docs/TASKS.md, "Quyết định đã chốt"):
    //   1. Manager duyệt -> đơn AwaitingDeposit, khách trả cọc (% cấu hình) -> đơn Approved (BookingActivator).
    //   2. Phần còn lại (giá báo - cọc) trả trước ngày xuất phát; chưa trả đủ thì chuyến không xuất phát được.
    //   3. Đóng chuyến -> cộng chi phí phát sinh vào đơn; còn nợ thì đơn AwaitingFinalPayment, trả xong -> Completed.
    //   4. Khách hủy trước khi xuất phát -> hoàn % tiền cọc theo cấu hình + toàn bộ phần còn lại đã trả.
    // Số tiền gốc tính USD như báo giá; khi trả qua VNPay quy đổi sang VND theo tỉ giá cấu hình, lưu tỉ giá vào giao dịch.
    public class PaymentService : IPaymentService
    {
        private static readonly string[] ClosedBookingStatuses =
        {
            BookingStatuses.Rejected, BookingStatuses.Expired, BookingStatuses.Cancelled
        };

        // Nhắc nợ chi phí phát sinh vào ngày đến hạn, sau 3 ngày và sau 7 ngày
        private static readonly int[] ReminderDaysAfterDue = { 0, 3, 7 };

        private readonly IRepository<Payment> _paymentRepo;
        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly ISystemSettingService _settingService;
        private readonly IPaymentGatewayResolver _gateways;
        private readonly IBookingActivator _activator;
        private readonly ICodeGenerator _codeGenerator;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly PaymentOptions _paymentOptions;
        private readonly VnPayOptions _vnPayOptions;

        public PaymentService(
            IRepository<Payment> paymentRepo,
            IRepository<BookingRequest> bookingRepo,
            ISystemSettingService settingService,
            IPaymentGatewayResolver gateways,
            IBookingActivator activator,
            ICodeGenerator codeGenerator,
            INotificationService notificationService,
            IUnitOfWork unitOfWork,
            IOptions<PaymentOptions> paymentOptions,
            IOptions<VnPayOptions> vnPayOptions)
        {
            _paymentRepo = paymentRepo;
            _bookingRepo = bookingRepo;
            _settingService = settingService;
            _gateways = gateways;
            _activator = activator;
            _codeGenerator = codeGenerator;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
            _paymentOptions = paymentOptions.Value;
            _vnPayOptions = vnPayOptions.Value;
        }

        // =============================================================================================
        // Xem
        // =============================================================================================

        public async Task<BookingPaymentSummaryModel> GetBookingPaymentsAsync(int bookingId, CurrentUser currentUser)
        {
            var booking = await LoadBookingAsync(bookingId, tracked: false);
            EnsureCanView(booking, currentUser);
            return BuildSummary(booking, await _settingService.GetPaymentSettingsAsync());
        }

        public async Task<CustomerBillingModel> GetMyBillingAsync(CurrentUser currentUser)
        {
            if (!currentUser.IsCustomer)
            {
                throw new ForbiddenException("Only customers have a billing page.");
            }

            var bookings = await _bookingRepo.GetQueryable()
                .AsNoTracking()
                .AsSplitQuery()
                .Include(b => b.Payments).ThenInclude(p => p.CreatedBy)
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.TripHorses).ThenInclude(th => th.Trip)
                .Where(b => b.CustomerUserId == currentUser.UserId
                    && (b.DepositPercent != null || b.ExtraCharges > 0 || b.Payments.Any()))
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return BuildCustomerBilling(bookings, await _settingService.GetPaymentSettingsAsync());
        }

        public CustomerBillingModel BuildCustomerBilling(IEnumerable<BookingRequest> bookings, PaymentSettingsModel settings)
        {
            var billing = new CustomerBillingModel { UsdVndRate = settings.UsdVndRate };

            foreach (var booking in bookings)
            {
                var summary = BuildSummary(booking, settings);
                billing.Payments.AddRange(summary.Payments);
                summary.Payments = new List<PaymentModel>();

                billing.Bookings.Add(new CustomerBillingBookingModel
                {
                    BookingId = booking.BookingId,
                    BookingCode = booking.BookingCode,
                    Status = booking.Status,
                    PickupCountryCode = booking.PickupCountryCode,
                    DropoffCountryCode = booking.DropoffCountryCode,
                    PickupAddress = booking.PickupAddress,
                    DropoffAddress = booking.DropoffAddress,
                    TotalHorses = booking.TotalHorses,
                    CreatedAt = booking.CreatedAt,
                    Summary = summary
                });

                billing.TotalPaid += summary.TotalPaid;
                billing.TotalRefunded += summary.RefundedAmount;
                billing.TotalOutstanding += summary.Outstanding;
                if (summary.NextPaymentType != null)
                {
                    billing.PayableNowCount++;
                    billing.TotalPayableNow += summary.NextPaymentAmount ?? 0;
                    billing.TotalPayableNowVnd += summary.NextPaymentAmountVnd ?? 0;
                }
                if (summary.ExtraOverdue)
                {
                    billing.OverdueCount++;
                }
            }

            billing.TotalOutstandingVnd = ToVnd(billing.TotalOutstanding, settings.UsdVndRate);
            billing.Payments = billing.Payments.OrderByDescending(p => p.CreatedAt).ToList();
            return billing;
        }

        public BookingPaymentSummaryModel BuildSummary(BookingRequest booking, PaymentSettingsModel settings)
        {
            var payments = booking.Payments.ToList();
            decimal Paid(string type) => payments
                .Where(p => p.PaymentType == type && p.Status == PaymentStatuses.Succeeded)
                .Sum(p => p.Amount);

            var summary = new BookingPaymentSummaryModel
            {
                PaymentRequired = booking.DepositPercent.HasValue,
                CurrencyCode = booking.CurrencyCode,
                UsdVndRate = settings.UsdVndRate,
                EstimatedCost = booking.EstimatedCost,
                DepositPercent = booking.DepositPercent,
                DepositAmount = booking.DepositPercent.HasValue ? booking.DepositAmount : 0,
                DepositPaid = Paid(PaymentTypes.Deposit),
                BalanceAmount = booking.DepositPercent.HasValue ? Math.Max(0, booking.EstimatedCost - booking.DepositAmount) : 0,
                BalancePaid = Paid(PaymentTypes.Balance),
                ExtraAmount = booking.ExtraCharges,
                ExtraPaid = Paid(PaymentTypes.Extra),
                ExtraDueAt = booking.ExtraDueAt,
                RefundedAmount = Paid(PaymentTypes.Refund),
                Payments = payments.OrderByDescending(p => p.CreatedAt).Select(ToModel).ToList()
            };
            summary.Payments.ForEach(p => p.BookingCode = booking.BookingCode);

            // Hạn trả phần còn lại: trước ngày xuất phát của chuyến sớm nhất còn hiệu lực chở ngựa của đơn
            var firstDeparture = booking.BookingHorses
                .SelectMany(bh => bh.TripHorses)
                .Where(th => th.Trip != null && th.Trip.OverallStatus != TripStatuses.Cancelled)
                .Select(th => (DateTimeOffset?)th.Trip!.PlannedStartDate)
                .Min();
            if (firstDeparture.HasValue)
            {
                summary.BalanceDueAt = firstDeparture.Value.AddDays(-settings.BalanceDueDaysBeforeDeparture);
            }

            summary.TotalDue = summary.DepositAmount + summary.BalanceAmount + summary.ExtraAmount;
            summary.TotalPaid = summary.DepositPaid + summary.BalancePaid + summary.ExtraPaid;

            bool closed = ClosedBookingStatuses.Contains(booking.Status);
            summary.Outstanding = closed ? 0 : Math.Max(0, summary.TotalDue - summary.TotalPaid);
            summary.ExtraOverdue = !closed && summary.ExtraAmount > summary.ExtraPaid
                && booking.ExtraDueAt.HasValue && booking.ExtraDueAt.Value < DateTimeOffset.UtcNow;

            var next = NextCharge(booking, summary);
            if (next != null)
            {
                summary.NextPaymentType = next.Value.Type;
                summary.NextPaymentAmount = next.Value.Amount;
                summary.NextPaymentAmountVnd = ToVnd(next.Value.Amount, settings.UsdVndRate);
            }

            return summary;
        }

        // Khoản khách được trả ngay theo trạng thái đơn
        private static (string Type, decimal Amount)? NextCharge(BookingRequest booking, BookingPaymentSummaryModel s)
        {
            if (!s.PaymentRequired && s.ExtraAmount <= s.ExtraPaid)
            {
                return null;
            }

            switch (booking.Status)
            {
                case BookingStatuses.AwaitingDeposit when s.DepositAmount > s.DepositPaid:
                    return (PaymentTypes.Deposit, s.DepositAmount - s.DepositPaid);
                case BookingStatuses.Approved or BookingStatuses.Assigned when s.BalanceAmount > s.BalancePaid:
                    return (PaymentTypes.Balance, s.BalanceAmount - s.BalancePaid);
                case BookingStatuses.AwaitingFinalPayment or BookingStatuses.Completed when s.ExtraAmount > s.ExtraPaid:
                    return (PaymentTypes.Extra, s.ExtraAmount - s.ExtraPaid);
                default:
                    return null;
            }
        }

        // =============================================================================================
        // Thanh toán online (VNPay / giả lập)
        // =============================================================================================

        public async Task<CreatePaymentResultModel> CreatePaymentAsync(int bookingId, string paymentType, string clientIp, CurrentUser currentUser)
        {
            if (!PaymentTypes.Charges.Contains(paymentType))
            {
                throw new BusinessException($"PaymentType '{paymentType}' is invalid. Allowed values: {string.Join(", ", PaymentTypes.Charges)}.");
            }

            var booking = await LoadBookingAsync(bookingId, tracked: true);
            if (booking.CustomerUserId != currentUser.UserId)
            {
                throw new ForbiddenException("You can only pay for your own bookings.");
            }

            var settings = await _settingService.GetPaymentSettingsAsync();
            var summary = BuildSummary(booking, settings);
            if (summary.NextPaymentType != paymentType || summary.NextPaymentAmount is not > 0)
            {
                throw new BusinessException(summary.NextPaymentType == null
                    ? $"Booking {booking.BookingCode} has nothing to pay right now (status: {booking.Status})."
                    : $"Booking {booking.BookingCode} is waiting for the {summary.NextPaymentType} payment, not {paymentType}.");
            }

            var gateway = _gateways.Current;
            if (gateway.ProviderName == PaymentProviders.VnPay && !_vnPayOptions.IsConfigured)
            {
                throw new BusinessException("VNPay is not configured on the server (VnPay:TmnCode / VnPay:HashSecret).");
            }

            decimal amount = summary.NextPaymentAmount.Value;
            long amountVnd = ToVnd(amount, settings.UsdVndRate);
            var now = DateTimeOffset.UtcNow;

            var payment = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Link cũ chưa trả của cùng khoản thì cho hết hạn để không trả trùng
                foreach (var old in booking.Payments.Where(p => p.PaymentType == paymentType && p.Status == PaymentStatuses.Pending))
                {
                    old.Status = PaymentStatuses.Expired;
                }

                string code = await _codeGenerator.NextAsync(CodePrefixes.Payment);
                var created = new Payment
                {
                    PaymentCode = code,
                    BookingId = booking.BookingId,
                    PaymentType = paymentType,
                    Provider = gateway.ProviderName,
                    Status = PaymentStatuses.Pending,
                    Amount = amount,
                    CurrencyCode = booking.CurrencyCode,
                    ExchangeRate = settings.UsdVndRate,
                    AmountVnd = amountVnd,
                    TxnRef = code.Replace("-", string.Empty),
                    CreatedByUserId = currentUser.UserId,
                    CreatedAt = now
                };
                await _paymentRepo.AddAsync(created);
                return created;
            });

            string url = gateway.CreatePaymentUrl(new GatewayPaymentRequest
            {
                TxnRef = payment.TxnRef,
                AmountVnd = amountVnd,
                OrderInfo = $"Thanh toan {TypeLabel(paymentType)} don {booking.BookingCode}",
                ReturnUrl = $"{ApiOrigin()}/api/payments/vnpay-return",
                ClientIp = string.IsNullOrWhiteSpace(clientIp) ? "127.0.0.1" : clientIp,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(Math.Max(5, _paymentOptions.PaymentExpireMinutes))
            });

            return new CreatePaymentResultModel { Payment = ToModel(payment), PaymentUrl = url };
        }

        public async Task<PaymentCallbackResultModel> HandleGatewayCallbackAsync(IReadOnlyDictionary<string, string> query)
        {
            var data = _gateways.Get(PaymentProviders.VnPay).ParseCallback(query);
            if (!data.IsSignatureValid)
            {
                return new PaymentCallbackResultModel { RspCode = "97", Message = "Invalid signature" };
            }

            var payment = await _paymentRepo.GetQueryable().FirstOrDefaultAsync(p => p.TxnRef == data.TxnRef);
            if (payment == null || payment.Provider != PaymentProviders.VnPay)
            {
                return new PaymentCallbackResultModel { RspCode = "01", Message = "Order not found" };
            }
            if (payment.AmountVnd != data.AmountVnd)
            {
                return new PaymentCallbackResultModel { RspCode = "04", Message = "Invalid amount", Payment = ToModel(payment) };
            }
            if (payment.Status != PaymentStatuses.Pending)
            {
                return new PaymentCallbackResultModel
                {
                    RspCode = "02",
                    Message = "Order already confirmed",
                    Success = payment.Status == PaymentStatuses.Succeeded,
                    Payment = ToModel(payment)
                };
            }

            payment.ResponseCode = data.ResponseCode;
            payment.ProviderTxnNo = data.TransactionNo;
            payment.ProviderPayDate = data.PayDate;
            payment.BankCode = data.BankCode;
            payment.RawResponse = JsonSerializer.Serialize(query);

            await CompletePaymentAsync(payment, data.IsSuccess);
            return new PaymentCallbackResultModel
            {
                RspCode = "00",
                Message = "Confirm Success",
                Success = data.IsSuccess,
                Payment = ToModel(payment)
            };
        }

        public async Task<PaymentCallbackResultModel> CompleteMockPaymentAsync(string txnRef, bool success, CurrentUser currentUser)
        {
            var payment = await _paymentRepo.GetQueryable()
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.TxnRef == txnRef);

            if (payment == null || payment.Provider != PaymentProviders.Mock)
            {
                throw new NotFoundException($"Mock payment {txnRef} was not found.");
            }
            if (payment.Booking!.CustomerUserId != currentUser.UserId)
            {
                throw new ForbiddenException("You can only complete your own payments.");
            }
            if (payment.Status != PaymentStatuses.Pending)
            {
                return new PaymentCallbackResultModel
                {
                    RspCode = "02",
                    Message = "Payment already processed",
                    Success = payment.Status == PaymentStatuses.Succeeded,
                    Payment = ToModel(payment)
                };
            }

            payment.ResponseCode = success ? "00" : "24"; // 24 = khách hủy giao dịch (mã VNPay)
            payment.ProviderTxnNo = $"MOCK{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            await CompletePaymentAsync(payment, success);

            return new PaymentCallbackResultModel { RspCode = "00", Message = "OK", Success = success, Payment = ToModel(payment) };
        }

        public async Task<PaymentModel> RecordManualPaymentAsync(int bookingId, ManualPaymentModel model, CurrentUser currentUser)
        {
            if (!PaymentTypes.Charges.Contains(model.PaymentType))
            {
                throw new BusinessException($"PaymentType '{model.PaymentType}' is invalid. Allowed values: {string.Join(", ", PaymentTypes.Charges)}.");
            }
            if (model.Amount <= 0)
            {
                throw new BusinessException("Amount must be greater than 0.");
            }

            var booking = await LoadBookingAsync(bookingId, tracked: true);
            if (ClosedBookingStatuses.Contains(booking.Status))
            {
                throw new BusinessException($"Booking {booking.BookingCode} is {booking.Status}; no payment can be recorded.");
            }

            var settings = await _settingService.GetPaymentSettingsAsync();
            var summary = BuildSummary(booking, settings);
            decimal outstanding = model.PaymentType switch
            {
                PaymentTypes.Deposit => summary.DepositAmount - summary.DepositPaid,
                PaymentTypes.Balance => summary.BalanceAmount - summary.BalancePaid,
                _ => summary.ExtraAmount - summary.ExtraPaid
            };
            if (outstanding <= 0)
            {
                throw new BusinessException($"Nothing is outstanding for the {model.PaymentType} payment of booking {booking.BookingCode}.");
            }
            if (model.Amount > outstanding)
            {
                throw new BusinessException($"Amount {model.Amount:N2} is more than the outstanding {model.PaymentType} amount {outstanding:N2} {booking.CurrencyCode}.");
            }

            string note = string.Join(" - ", new[] { model.Reference, model.Note }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()));
            var payment = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                string code = await _codeGenerator.NextAsync(CodePrefixes.Payment);
                var created = new Payment
                {
                    PaymentCode = code,
                    BookingId = booking.BookingId,
                    PaymentType = model.PaymentType,
                    Provider = PaymentProviders.Manual,
                    Status = PaymentStatuses.Pending,
                    Amount = Math.Round(model.Amount, 2),
                    CurrencyCode = booking.CurrencyCode,
                    ExchangeRate = settings.UsdVndRate,
                    AmountVnd = ToVnd(model.Amount, settings.UsdVndRate),
                    TxnRef = code.Replace("-", string.Empty),
                    Note = string.IsNullOrEmpty(note) ? null : note,
                    CreatedByUserId = currentUser.UserId,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _paymentRepo.AddAsync(created);
                return created;
            });

            await CompletePaymentAsync(payment, success: true);
            return ToModel(payment);
        }

        // Ghi kết quả giao dịch thu tiền và kéo theo thay đổi của đơn (cọc đủ -> kích hoạt, phát sinh đủ -> hoàn tất)
        private async Task CompletePaymentAsync(Payment payment, bool success)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                payment.Status = success ? PaymentStatuses.Succeeded : PaymentStatuses.Failed;
                payment.PaidAt = success ? DateTimeOffset.UtcNow : null;
                await _paymentRepo.UpdateAsync(payment);

                if (!success)
                {
                    return;
                }

                var booking = await LoadBookingAsync(payment.BookingId, tracked: true);
                var summary = BuildSummary(booking, await _settingService.GetPaymentSettingsAsync());

                await _notificationService.SendAsync(
                    booking.CustomerUserId,
                    $"Payment received for booking {booking.BookingCode}",
                    $"We received {payment.Amount:N2} {payment.CurrencyCode} ({TypeLabelEn(payment.PaymentType)}, {payment.AmountVnd:N0} VND). Payment code: {payment.PaymentCode}.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId,
                    emailCustomers: true);

                if (payment.PaymentType == PaymentTypes.Deposit
                    && booking.Status == BookingStatuses.AwaitingDeposit
                    && summary.DepositPaid >= summary.DepositAmount)
                {
                    await _activator.ActivateAsync(booking);
                }
                else if (payment.PaymentType == PaymentTypes.Extra
                    && booking.Status == BookingStatuses.AwaitingFinalPayment
                    && summary.ExtraPaid >= summary.ExtraAmount)
                {
                    booking.Status = BookingStatuses.Completed;
                    await _bookingRepo.UpdateAsync(booking);

                    await _notificationService.SendAsync(
                        booking.CustomerUserId,
                        $"Booking {booking.BookingCode} is completed",
                        "All charges have been paid and the booking is now completed. Thank you for travelling with us.",
                        NotificationReferenceTypes.Booking,
                        booking.BookingId);
                }
            });
        }

        // =============================================================================================
        // Hoàn tiền
        // =============================================================================================

        public async Task<List<PaymentModel>> RefundForCancellationAsync(BookingRequest booking, CurrentUser currentUser)
        {
            var settings = await _settingService.GetPaymentSettingsAsync();
            var charges = await _paymentRepo.GetQueryable()
                .Where(p => p.BookingId == booking.BookingId)
                .ToListAsync();

            var results = new List<PaymentModel>();
            foreach (var original in charges.Where(p => p.Status == PaymentStatuses.Succeeded && PaymentTypes.Charges.Contains(p.PaymentType)))
            {
                decimal percent = original.PaymentType == PaymentTypes.Deposit ? settings.CancelRefundPercent : 100;
                decimal alreadyRefunded = charges
                    .Where(r => r.RefundOfPaymentId == original.PaymentId && r.Status != PaymentStatuses.Failed && r.Status != PaymentStatuses.Expired)
                    .Sum(r => r.Amount);
                decimal refundAmount = Math.Round(original.Amount * percent / 100, 2) - alreadyRefunded;
                if (refundAmount <= 0)
                {
                    continue;
                }

                var refund = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    string code = await _codeGenerator.NextAsync(CodePrefixes.Payment);
                    var created = new Payment
                    {
                        PaymentCode = code,
                        BookingId = booking.BookingId,
                        PaymentType = PaymentTypes.Refund,
                        Provider = original.Provider,
                        Status = PaymentStatuses.Pending,
                        Amount = refundAmount,
                        CurrencyCode = original.CurrencyCode,
                        ExchangeRate = original.ExchangeRate, // hoàn theo đúng tỉ giá lúc khách trả
                        AmountVnd = ToVnd(refundAmount, original.ExchangeRate),
                        TxnRef = code.Replace("-", string.Empty),
                        RefundOfPaymentId = original.PaymentId,
                        Note = $"Refund {percent:0.##}% of {original.PaymentCode} ({TypeLabelEn(original.PaymentType)}) after cancellation",
                        CreatedByUserId = currentUser.UserId,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    await _paymentRepo.AddAsync(created);
                    return created;
                });
                refund.RefundOf = original;

                // Tiền nhận ngoài hệ thống thì Manager hoàn tay rồi bấm "Đã hoàn thủ công"
                if (original.Provider != PaymentProviders.Manual)
                {
                    await CallRefundGatewayAsync(refund, currentUser);
                }
                results.Add(ToModel(refund));
            }

            if (results.Count > 0)
            {
                decimal total = results.Where(r => r.Status == PaymentStatuses.Succeeded).Sum(r => r.Amount);
                bool anyPending = results.Any(r => r.Status != PaymentStatuses.Succeeded);
                await _notificationService.SendAsync(
                    booking.CustomerUserId,
                    $"Refund for booking {booking.BookingCode}",
                    anyPending
                        ? $"Your refund is being processed. Refunded so far: {total:N2} {booking.CurrencyCode}. Our staff will complete the rest and notify you."
                        : $"We refunded {total:N2} {booking.CurrencyCode} to your original payment method. Depending on your bank, the money arrives in 7-15 working days.",
                    NotificationReferenceTypes.Booking,
                    booking.BookingId,
                    emailCustomers: true);

                if (anyPending)
                {
                    await _notificationService.SendToRoleAsync(
                        UserRoles.LogisticsManager,
                        $"Refund needs attention: {booking.BookingCode}",
                        "A refund after cancellation failed or has to be done manually. Open the booking's payments to retry or mark it as refunded.",
                        NotificationReferenceTypes.Booking,
                        booking.BookingId);
                }
            }

            return results;
        }

        public async Task<PaymentModel> RetryRefundAsync(int refundPaymentId, CurrentUser currentUser)
        {
            var refund = await GetOpenRefundAsync(refundPaymentId);
            if (refund.Provider == PaymentProviders.Manual)
            {
                throw new BusinessException("This refund was paid outside the system; refund it manually and mark it as refunded.");
            }

            await CallRefundGatewayAsync(refund, currentUser, retry: true);
            return ToModel(refund);
        }

        public async Task<PaymentModel> MarkRefundedManuallyAsync(int refundPaymentId, string reference, string? note, CurrentUser currentUser)
        {
            var refund = await GetOpenRefundAsync(refundPaymentId);

            refund.Status = PaymentStatuses.Succeeded;
            refund.PaidAt = DateTimeOffset.UtcNow;
            refund.ProviderTxnNo = reference.Trim();
            refund.Note = string.Join(" - ", new[] { refund.Note, $"Refunded manually by user {currentUser.UserId}", note?.Trim() }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            await _paymentRepo.UpdateAsync(refund);

            await _notificationService.SendAsync(
                refund.Booking!.CustomerUserId,
                $"Refund completed for booking {refund.Booking.BookingCode}",
                $"We refunded {refund.Amount:N2} {refund.CurrencyCode} ({refund.AmountVnd:N0} VND). Reference: {refund.ProviderTxnNo}.",
                NotificationReferenceTypes.Booking,
                refund.BookingId,
                emailCustomers: true);

            return ToModel(refund);
        }

        private async Task CallRefundGatewayAsync(Payment refund, CurrentUser currentUser, bool retry = false)
        {
            var original = refund.RefundOf ?? await _paymentRepo.GetByIdAsync(refund.RefundOfPaymentId!.Value)
                ?? throw new NotFoundException($"Original payment of refund {refund.PaymentCode} was not found.");

            var gateway = _gateways.Get(original.Provider);
            var result = await gateway.RefundAsync(new GatewayRefundRequest
            {
                // vnp_RequestId không được trùng giữa các lần gọi: lần thử lại thêm hậu tố thời gian
                RefundTxnRef = retry ? $"{refund.TxnRef}{DateTimeOffset.UtcNow:HHmmss}" : refund.TxnRef,
                OriginalTxnRef = original.TxnRef,
                OriginalTransactionNo = original.ProviderTxnNo,
                OriginalPayDate = original.ProviderPayDate,
                AmountVnd = refund.AmountVnd,
                IsFullRefund = refund.AmountVnd >= original.AmountVnd,
                CreatedBy = $"user{currentUser.UserId}",
                OrderInfo = $"Hoan tien {original.PaymentCode}"
            });

            refund.Status = result.Success ? PaymentStatuses.Succeeded : PaymentStatuses.Failed;
            refund.ResponseCode = result.ResponseCode;
            refund.ProviderTxnNo = result.TransactionNo;
            refund.PaidAt = result.Success ? DateTimeOffset.UtcNow : null;
            refund.RawResponse = result.RawResponse == null ? null : ToJsonOrWrap(result.RawResponse);
            if (!result.Success)
            {
                refund.Note = $"{refund.Note} | Gateway: {result.ResponseCode} {result.Message}".Trim();
            }
            await _paymentRepo.UpdateAsync(refund);
        }

        private async Task<Payment> GetOpenRefundAsync(int refundPaymentId)
        {
            var refund = await _paymentRepo.GetQueryable()
                .Include(p => p.Booking)
                .Include(p => p.RefundOf)
                .FirstOrDefaultAsync(p => p.PaymentId == refundPaymentId);

            if (refund == null || refund.PaymentType != PaymentTypes.Refund)
            {
                throw new NotFoundException($"Refund with ID {refundPaymentId} was not found.");
            }
            if (refund.Status == PaymentStatuses.Succeeded)
            {
                throw new BusinessException($"Refund {refund.PaymentCode} has already been completed.");
            }
            return refund;
        }

        // =============================================================================================
        // Nợ quá hạn
        // =============================================================================================

        public async Task<int> SendOverdueRemindersAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var bookings = await _bookingRepo.GetQueryable()
                .Include(b => b.Payments)
                .Where(b => b.Status == BookingStatuses.AwaitingFinalPayment
                    && b.ExtraDueAt != null
                    && b.PaymentReminderCount < ReminderDaysAfterDue.Length)
                .ToListAsync();

            int sent = 0;
            foreach (var booking in bookings)
            {
                var remindAt = booking.ExtraDueAt!.Value.AddDays(ReminderDaysAfterDue[booking.PaymentReminderCount]);
                if (now < remindAt)
                {
                    continue;
                }

                decimal paid = booking.Payments
                    .Where(p => p.PaymentType == PaymentTypes.Extra && p.Status == PaymentStatuses.Succeeded)
                    .Sum(p => p.Amount);
                decimal outstanding = booking.ExtraCharges - paid;
                if (outstanding <= 0)
                {
                    continue;
                }

                bool lastReminder = booking.PaymentReminderCount == ReminderDaysAfterDue.Length - 1;
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    booking.PaymentReminderCount++;
                    await _bookingRepo.UpdateAsync(booking);

                    await _notificationService.SendAsync(
                        booking.CustomerUserId,
                        $"Payment reminder for booking {booking.BookingCode}",
                        $"The extra charges of {outstanding:N2} {booking.CurrencyCode} were due on {booking.ExtraDueAt:yyyy-MM-dd}. Please pay them from the booking page. New bookings are on hold until this is settled.",
                        NotificationReferenceTypes.Booking,
                        booking.BookingId,
                        emailCustomers: true);

                    if (lastReminder)
                    {
                        await _notificationService.SendToRoleAsync(
                            UserRoles.LogisticsManager,
                            $"Overdue payment: {booking.BookingCode}",
                            $"{outstanding:N2} {booking.CurrencyCode} of extra charges is still unpaid {ReminderDaysAfterDue[^1]} days after the due date.",
                            NotificationReferenceTypes.Booking,
                            booking.BookingId);
                    }
                });
                sent++;
            }

            return sent;
        }

        public async Task<bool> HasOverdueDebtAsync(int customerUserId)
        {
            var now = DateTimeOffset.UtcNow;
            return await _bookingRepo.GetQueryable().AnyAsync(b =>
                b.CustomerUserId == customerUserId
                && b.Status == BookingStatuses.AwaitingFinalPayment
                && b.ExtraDueAt != null
                && b.ExtraDueAt < now);
        }

        // =============================================================================================
        // Hàm phụ
        // =============================================================================================

        // Chỉ lấy scheme + host (+ port) của PublicApiBaseUrl: lỡ cấu hình dính cả đường dẫn
        // (vd ".../api/payments/vnpay-ipn") thì vnp_ReturnUrl vẫn đúng
        private string ApiOrigin()
        {
            return Uri.TryCreate(_paymentOptions.PublicApiBaseUrl, UriKind.Absolute, out var uri)
                ? uri.GetLeftPart(UriPartial.Authority)
                : _paymentOptions.PublicApiBaseUrl.TrimEnd('/');
        }

        private async Task<BookingRequest> LoadBookingAsync(int bookingId, bool tracked)
        {
            var query = _bookingRepo.GetQueryable()
                .AsSplitQuery()
                .Include(b => b.Payments).ThenInclude(p => p.CreatedBy)
                .Include(b => b.BookingHorses).ThenInclude(bh => bh.TripHorses).ThenInclude(th => th.Trip)
                .AsQueryable();
            if (!tracked)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync(b => b.BookingId == bookingId)
                ?? throw new NotFoundException($"Booking with ID {bookingId} was not found.");
        }

        private static void EnsureCanView(BookingRequest booking, CurrentUser currentUser)
        {
            if (currentUser.IsCustomer && booking.CustomerUserId != currentUser.UserId)
            {
                throw new ForbiddenException("You do not have access to this booking.");
            }
            if (currentUser.Role == UserRoles.DriverEscort)
            {
                throw new ForbiddenException("Drivers and escorts do not see booking payments.");
            }
        }

        public static long ToVnd(decimal amount, decimal rate) =>
            (long)Math.Round(amount * rate, 0, MidpointRounding.AwayFromZero);

        private static string TypeLabel(string type) => type switch
        {
            PaymentTypes.Deposit => "coc",
            PaymentTypes.Balance => "phan con lai",
            PaymentTypes.Extra => "chi phi phat sinh",
            _ => "hoan tien"
        };

        private static string TypeLabelEn(string type) => type switch
        {
            PaymentTypes.Deposit => "deposit",
            PaymentTypes.Balance => "remaining balance",
            PaymentTypes.Extra => "extra charges",
            _ => "refund"
        };

        private static string ToJsonOrWrap(string raw)
        {
            try
            {
                using var _ = JsonDocument.Parse(raw);
                return raw;
            }
            catch (JsonException)
            {
                return JsonSerializer.Serialize(new { raw });
            }
        }

        public static PaymentModel ToModel(Payment p) => new()
        {
            PaymentId = p.PaymentId,
            PaymentCode = p.PaymentCode,
            BookingId = p.BookingId,
            BookingCode = p.Booking?.BookingCode,
            PaymentType = p.PaymentType,
            Provider = p.Provider,
            Status = p.Status,
            Amount = p.Amount,
            CurrencyCode = p.CurrencyCode,
            ExchangeRate = p.ExchangeRate,
            AmountVnd = p.AmountVnd,
            TxnRef = p.TxnRef,
            ProviderTxnNo = p.ProviderTxnNo,
            BankCode = p.BankCode,
            ResponseCode = p.ResponseCode,
            RefundOfPaymentId = p.RefundOfPaymentId,
            Note = p.Note,
            CreatedByName = p.CreatedBy?.FullName,
            CreatedAt = p.CreatedAt,
            PaidAt = p.PaidAt
        };
    }
}
