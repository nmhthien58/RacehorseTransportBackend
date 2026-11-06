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
                InitialSeverity = entity.InitialSeverity,
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
                ActionPayload = ParseActionPayload(entity.ActionPayload),
                RouteChange = ParseRouteChange(entity.RouteChange),
                RevisedRouteNotes = entity.RevisedRouteNotes,
                AdditionalCost = entity.AdditionalCost,
                RejectionReason = entity.RejectionReason,
                FaultParty = entity.FaultParty,
                FaultNote = entity.FaultNote,
                ApprovedByUserId = entity.ApprovedByUserId,
                ApprovedByName = entity.ApprovedBy?.FullName,
                ApprovedAt = entity.ApprovedAt,
                ResolvedAt = entity.ResolvedAt,
                EmergencyOffload = entity.EmergencyOffload,
                ExtraCostForCustomer = entity.ExtraCostForCustomer,
                Consents = entity.Consents.Select(c => new IncidentConsentModel
                {
                    BookingId = c.BookingId,
                    BookingCode = c.Booking?.BookingCode ?? string.Empty,
                    Accepted = c.Accepted,
                    DecidedAt = c.DecidedAt
                }).ToList(),
                OffloadOutcome = entity.OffloadOutcome,
                VetReportPath = entity.VetReportPath,
                Documents = entity.Documents.Select(d => new IncidentDocumentModel
                {
                    IncidentDocumentId = d.IncidentDocumentId,
                    IncidentId = d.IncidentId,
                    FilePath = d.FilePath,
                    DocumentType = d.DocumentType,
                    Note = d.Note,
                    UploadedByUserId = d.UploadedByUserId,
                    UploadedByName = d.UploadedBy?.FullName,
                    UploadedAt = d.UploadedAt
                }).ToList()
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

        public static IncidentActionPayloadModel? ParseActionPayload(string? json)
        {
            return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<IncidentActionPayloadModel>(json);
        }

        public static string? SerializeActionPayload(IncidentActionPayloadModel? payload)
        {
            return payload == null ? null : JsonSerializer.Serialize(payload);
        }
    }
}
