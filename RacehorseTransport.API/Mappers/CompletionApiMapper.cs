using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class CompletionApiMapper
    {
        // Stream của ảnh chữ ký do controller giữ và giải phóng (request.Signature.ToUploadModel())
        public static CreateHandoverModel ToBusinessModel(this CreateHandoverRequest request, FileUploadModel signature)
        {
            return new CreateHandoverModel
            {
                BookingId = request.BookingId,
                RecipientName = request.RecipientName.Trim(),
                RecipientPhone = request.RecipientPhone.Trim(),
                RecipientIdCard = request.RecipientIdCard.Trim(),
                OverallCondition = request.OverallCondition.Trim(),
                Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim(),
                MicrochipNumbers = request.MicrochipNumbers,
                Signature = signature
            };
        }

        public static HandoverResponse ToResponse(this HandoverModel model)
        {
            return new HandoverResponse
            {
                HandoverId = model.HandoverId,
                TripId = model.TripId,
                TripCode = model.TripCode,
                BookingId = model.BookingId,
                BookingCode = model.BookingCode,
                CustomerName = model.CustomerName,
                DropoffAddress = model.DropoffAddress,
                HandoverDateTime = model.HandoverDateTime,
                RecipientName = model.RecipientName,
                RecipientPhone = model.RecipientPhone,
                RecipientIdCard = model.RecipientIdCard,
                SignatureUrl = model.SignatureUrl,
                OverallCondition = model.OverallCondition,
                Remarks = model.Remarks,
                DriverUserId = model.DriverUserId,
                DriverName = model.DriverName,
                Horses = model.Horses.Select(h => new HandoverHorseResponse
                {
                    TripHorseId = h.TripHorseId,
                    HorseId = h.HorseId,
                    HorseName = h.HorseName,
                    MicrochipNumber = h.MicrochipNumber
                }).ToList()
            };
        }

        public static ReportSummaryResponse ToResponse(this ReportSummaryModel model)
        {
            return new ReportSummaryResponse
            {
                From = model.From,
                To = model.To,
                CurrencyCode = model.CurrencyCode,
                CompletedTrips = model.CompletedTrips,
                CompletedBookings = model.CompletedBookings,
                HorsesDelivered = model.HorsesDelivered,
                Revenue = model.Revenue,
                OperatingCost = model.OperatingCost,
                GrossProfit = model.GrossProfit,
                OnTimeTrips = model.OnTimeTrips,
                DelayedForceMajeureTrips = model.DelayedForceMajeureTrips,
                DelayedOperationalFaultTrips = model.DelayedOperationalFaultTrips,
                OnTimeRate = model.OnTimeRate,
                AverageKpiScore = model.AverageKpiScore,
                AverageDelayMinutes = model.AverageDelayMinutes,
                TotalIncidents = model.TotalIncidents,
                IncidentAdditionalCost = model.IncidentAdditionalCost,
                IncidentsByType = model.IncidentsByType
                    .Select(i => new IncidentTypeCountResponse { IncidentType = i.IncidentType, Count = i.Count })
                    .ToList()
            };
        }
    }
}
