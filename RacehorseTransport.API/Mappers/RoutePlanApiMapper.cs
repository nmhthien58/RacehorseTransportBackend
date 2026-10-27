using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class RoutePlanApiMapper
    {
        public static SaveRoutePlanModel ToBusinessModel(this SaveRoutePlanRequest request)
        {
            return new SaveRoutePlanModel
            {
                Notes = request.Notes,
                OriginLat = request.OriginLat,
                OriginLng = request.OriginLng,
                DestinationLat = request.DestinationLat,
                DestinationLng = request.DestinationLng,
                Stops = request.Stops.Select(s => new RoutePlanStopInputModel
                {
                    Name = s.Name.Trim(),
                    StopType = s.StopType.Trim(),
                    Address = s.Address?.Trim() ?? string.Empty,
                    Lat = s.Lat,
                    Lng = s.Lng,
                    CountryCode = string.IsNullOrWhiteSpace(s.CountryCode) ? null : s.CountryCode.Trim().ToUpperInvariant(),
                    RestMinutes = s.RestMinutes
                }).ToList()
            };
        }

        public static RoutePlanResponse ToResponse(this RoutePlanModel model)
        {
            return new RoutePlanResponse
            {
                RoutePlanId = model.RoutePlanId,
                BookingId = model.BookingId,
                BookingCode = model.BookingCode,
                Version = model.Version,
                Status = model.Status,
                Stops = model.Stops.Select(s => new RoutePlanStopResponse
                {
                    SequenceOrder = s.SequenceOrder,
                    Name = s.Name,
                    StopType = s.StopType,
                    Address = s.Address,
                    Lat = s.Lat,
                    Lng = s.Lng,
                    CountryCode = s.CountryCode,
                    DistanceKmFromPrevious = s.DistanceKmFromPrevious,
                    DurationHoursFromPrevious = s.DurationHoursFromPrevious,
                    RestMinutes = s.RestMinutes
                }).ToList(),
                TransitCountryCodes = model.TransitCountryCodes,
                TotalDistanceKm = model.TotalDistanceKm,
                EstimatedHours = model.EstimatedHours,
                IsEstimate = model.IsEstimate,
                Polyline = model.Polyline.Select(p => new[] { Math.Round(p.Lat, 6), Math.Round(p.Lng, 6) }).ToList(),
                Warnings = model.Warnings.Select(w => new RoutePlanWarningResponse
                {
                    FromIndex = w.FromIndex,
                    ToIndex = w.ToIndex,
                    CountryCodes = w.CountryCodes,
                    Message = w.Message
                }).ToList(),
                Notes = model.Notes,
                PlannedByUserId = model.PlannedByUserId,
                PlannedByName = model.PlannedByName,
                ReviewedByName = model.ReviewedByName,
                ReviewedAt = model.ReviewedAt,
                RejectionReason = model.RejectionReason,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }
    }
}
