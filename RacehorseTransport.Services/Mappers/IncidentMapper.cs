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
                RevisedRouteNotes = entity.RevisedRouteNotes,
                AdditionalCost = entity.AdditionalCost,
                ApprovedByUserId = entity.ApprovedByUserId,
                ApprovedByName = entity.ApprovedBy?.FullName,
                ApprovedAt = entity.ApprovedAt,
                ResolvedAt = entity.ResolvedAt
            };
        }
    }
}
