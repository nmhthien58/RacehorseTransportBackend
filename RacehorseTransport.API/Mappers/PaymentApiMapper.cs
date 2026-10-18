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
                TotalDue = model.TotalDue,
                TotalPaid = model.TotalPaid,
                Outstanding = model.Outstanding,
                NextPaymentType = model.NextPaymentType,
                NextPaymentAmount = model.NextPaymentAmount,
                NextPaymentAmountVnd = model.NextPaymentAmountVnd,
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
