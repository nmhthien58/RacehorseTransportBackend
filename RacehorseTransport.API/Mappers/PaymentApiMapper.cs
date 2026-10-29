using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class PaymentApiMapper
    {
        public static PaymentResponse ToResponse(this PaymentModel model)
        {
            return new PaymentResponse
            {
                PaymentId = model.PaymentId,
                PaymentCode = model.PaymentCode,
                BookingId = model.BookingId,
                BookingCode = model.BookingCode,
                PaymentType = model.PaymentType,
                Provider = model.Provider,
                Status = model.Status,
                Amount = model.Amount,
                CurrencyCode = model.CurrencyCode,
                ExchangeRate = model.ExchangeRate,
                AmountVnd = model.AmountVnd,
                ProviderTxnNo = model.ProviderTxnNo,
                BankCode = model.BankCode,
                ResponseCode = model.ResponseCode,
                RefundOfPaymentId = model.RefundOfPaymentId,
                Note = model.Note,
                CreatedByName = model.CreatedByName,
                CreatedAt = model.CreatedAt,
                PaidAt = model.PaidAt
            };
        }

        public static PaymentListItemResponse ToResponse(this PaymentListItemModel model)
        {
            var b = ((PaymentModel)model).ToResponse();
            return new PaymentListItemResponse
            {
                PaymentId = b.PaymentId,
                PaymentCode = b.PaymentCode,
                BookingId = b.BookingId,
                BookingCode = b.BookingCode,
                PaymentType = b.PaymentType,
                Provider = b.Provider,
                Status = b.Status,
                Amount = b.Amount,
                CurrencyCode = b.CurrencyCode,
                ExchangeRate = b.ExchangeRate,
                AmountVnd = b.AmountVnd,
                ProviderTxnNo = b.ProviderTxnNo,
                BankCode = b.BankCode,
                ResponseCode = b.ResponseCode,
                RefundOfPaymentId = b.RefundOfPaymentId,
                Note = b.Note,
                CreatedByName = b.CreatedByName,
                CreatedAt = b.CreatedAt,
                PaidAt = b.PaidAt,
                TxnRef = model.TxnRef,
                CustomerName = model.CustomerName
            };
        }

        public static PaymentListSummaryResponse ToResponse(this PaymentListSummaryModel model)
        {
            return new PaymentListSummaryResponse
            {
                SucceededCount = model.SucceededCount,
                SucceededAmount = model.SucceededAmount,
                PendingCount = model.PendingCount,
                PendingAmount = model.PendingAmount,
                FailedCount = model.FailedCount,
                ExpiredCount = model.ExpiredCount,
                RefundedAmount = model.RefundedAmount,
                RefundsNeedingActionCount = model.RefundsNeedingActionCount,
                CurrencyCode = model.CurrencyCode
            };
        }

        public static PaymentFilterOptions ToBusinessOptions(this PaymentQueryRequest request)
        {
            return new PaymentFilterOptions
            {
                Search = request.Search,
                Sort = request.Sort,
                Page = request.Page,
                Size = request.Size,
                Type = request.Type?.Trim(),
                Status = request.Status?.Trim(),
                From = request.From,
                To = request.To
            };
        }

        public static BookingPaymentSummaryResponse ToResponse(this BookingPaymentSummaryModel model)
        {
            return new BookingPaymentSummaryResponse
            {
                PaymentRequired = model.PaymentRequired,
                CurrencyCode = model.CurrencyCode,
                UsdVndRate = model.UsdVndRate,
                EstimatedCost = model.EstimatedCost,
                DepositPercent = model.DepositPercent,
                DepositAmount = model.DepositAmount,
                DepositPaid = model.DepositPaid,
                BalanceAmount = model.BalanceAmount,
                BalancePaid = model.BalancePaid,
                BalanceDueAt = model.BalanceDueAt,
                ExtraAmount = model.ExtraAmount,
                ExtraPaid = model.ExtraPaid,
                ExtraDueAt = model.ExtraDueAt,
                ExtraOverdue = model.ExtraOverdue,
                RefundedAmount = model.RefundedAmount,
                CompensationAmount = model.CompensationAmount,
                TotalDue = model.TotalDue,
                TotalPaid = model.TotalPaid,
                Outstanding = model.Outstanding,
                NextPaymentType = model.NextPaymentType,
                NextPaymentAmount = model.NextPaymentAmount,
                NextPaymentAmountVnd = model.NextPaymentAmountVnd,
                Payments = model.Payments.Select(p => p.ToResponse()).ToList()
            };
        }

        public static CustomerBillingResponse ToResponse(this CustomerBillingModel model)
        {
            return new CustomerBillingResponse
            {
                CurrencyCode = model.CurrencyCode,
                UsdVndRate = model.UsdVndRate,
                TotalPaid = model.TotalPaid,
                TotalRefunded = model.TotalRefunded,
                TotalOutstanding = model.TotalOutstanding,
                TotalOutstandingVnd = model.TotalOutstandingVnd,
                TotalPayableNow = model.TotalPayableNow,
                TotalPayableNowVnd = model.TotalPayableNowVnd,
                PayableNowCount = model.PayableNowCount,
                OverdueCount = model.OverdueCount,
                Bookings = model.Bookings.Select(b => new CustomerBillingBookingResponse
                {
                    BookingId = b.BookingId,
                    BookingCode = b.BookingCode,
                    Status = b.Status,
                    PickupCountryCode = b.PickupCountryCode,
                    DropoffCountryCode = b.DropoffCountryCode,
                    PickupAddress = b.PickupAddress,
                    DropoffAddress = b.DropoffAddress,
                    TotalHorses = b.TotalHorses,
                    CreatedAt = b.CreatedAt,
                    Summary = b.Summary.ToResponse()
                }).ToList(),
                Payments = model.Payments.Select(p => p.ToResponse()).ToList()
            };
        }

        public static CreatePaymentResponse ToResponse(this CreatePaymentResultModel model)
        {
            return new CreatePaymentResponse { Payment = model.Payment.ToResponse(), PaymentUrl = model.PaymentUrl };
        }

        public static PaymentCallbackResponse ToResponse(this PaymentCallbackResultModel model)
        {
            return new PaymentCallbackResponse
            {
                Success = model.Success,
                Message = model.Message,
                Payment = model.Payment?.ToResponse()
            };
        }

        public static PaymentSettingsResponse ToResponse(this PaymentSettingsModel model)
        {
            return new PaymentSettingsResponse
            {
                DepositPercent = model.DepositPercent,
                CancelRefundPercent = model.CancelRefundPercent,
                UsdVndRate = model.UsdVndRate,
                BalanceDueDaysBeforeDeparture = model.BalanceDueDaysBeforeDeparture,
                ExtraPaymentDueDays = model.ExtraPaymentDueDays,
                UpdatedAt = model.UpdatedAt,
                UpdatedByName = model.UpdatedByName
            };
        }

        public static PaymentSettingsModel ToBusinessModel(this UpdatePaymentSettingsRequest request)
        {
            return new PaymentSettingsModel
            {
                DepositPercent = request.DepositPercent,
                CancelRefundPercent = request.CancelRefundPercent,
                UsdVndRate = request.UsdVndRate,
                BalanceDueDaysBeforeDeparture = request.BalanceDueDaysBeforeDeparture,
                ExtraPaymentDueDays = request.ExtraPaymentDueDays
            };
        }

        public static ManualPaymentModel ToBusinessModel(this ManualPaymentRequest request)
        {
            return new ManualPaymentModel
            {
                PaymentType = request.PaymentType.Trim(),
                Amount = request.Amount,
                Reference = request.Reference,
                Note = request.Note
            };
        }
    }
}
