using System.Text.Json;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Mappers
{
    public static class IncidentMapper
    {
        public static IncidentModel ToBusinessModel(this TripIncident entity)
        {
            return new IncidentModel
            {
                IncidentId = entity.IncidentId,
                IncidentCode = entity.IncidentCode,
                TripId = entity.TripId,
                TripCode = entity.Trip?.TripCode ?? string.Empty,
                TripStatus = entity.Trip?.OverallStatus ?? string.Empty,
                IncidentType = entity.IncidentType,
                Severity = entity.Severity,
                Status = entity.Status,
                Location = entity.Location,
                Description = entity.Description,
                AffectedTripHorseId = entity.AffectedTripHorseId,
                AffectedHorseName = entity.AffectedTripHorse?.BookingHorse?.Horse?.Name,
                ReportedByUserId = entity.ReportedByUserId,
                ReportedByName = entity.ReportedBy?.FullName,
                ReportedAt = entity.ReportedAt,
                ProposedAction = entity.ProposedAction,
                ResponseAction = entity.ResponseAction,
                RouteChange = ParseRouteChange(entity.RouteChange),
                RevisedRouteNotes = entity.RevisedRouteNotes,
                AdditionalCost = entity.AdditionalCost,
                RejectionReason = entity.RejectionReason,
                ApprovedByUserId = entity.ApprovedByUserId,
                ApprovedByName = entity.ApprovedBy?.FullName,
                ApprovedAt = entity.ApprovedAt,
                ResolvedAt = entity.ResolvedAt
            };
        }

        public static IncidentRouteChangeModel? ParseRouteChange(string? json)
        {
            return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<IncidentRouteChangeModel>(json);
        }

        public static string? SerializeRouteChange(IncidentRouteChangeModel? routeChange)
        {
            return routeChange == null ? null : JsonSerializer.Serialize(routeChange);
        }
    }
}
