using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class DelayCompensationApiMapper
    {
        public static DelayCompensationResponse ToResponse(this DelayCompensationModel m) => new()
        {
            DelayCompensationId = m.DelayCompensationId,
            BookingId = m.BookingId,
            BookingCode = m.BookingCode,
            CustomerName = m.CustomerName,
            TripId = m.TripId,
            TripCode = m.TripCode,
            DelayHours = m.DelayHours,
            FreightAmount = m.FreightAmount,
            Percent = m.Percent,
            CalculatedAmount = m.CalculatedAmount,
            ApprovedAmount = m.ApprovedAmount,
            CurrencyCode = m.CurrencyCode,
            Status = m.Status,
            ReviewReason = m.ReviewReason,
            ReviewedByName = m.ReviewedByName,
            ReviewedAt = m.ReviewedAt,
            CreatedAt = m.CreatedAt
        };
    }
}
