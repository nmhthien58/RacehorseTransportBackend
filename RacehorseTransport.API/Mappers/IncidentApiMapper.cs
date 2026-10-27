using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class IncidentApiMapper
    {
        public static IncidentFilterOptions ToBusinessOptions(this IncidentQueryRequest request)
        {
            return new IncidentFilterOptions
            {
                Search = request.Search,
                Sort = request.Sort,
                Page = request.Page,
                Size = request.Size,
                Status = request.Status,
                Severity = request.Severity,
                TripId = request.TripId
            };
        }

        public static ReportIncidentModel ToBusinessModel(this ReportIncidentRequest request)
        {
            return new ReportIncidentModel
            {
                IncidentType = request.IncidentType.Trim(),
                Severity = request.Severity.Trim(),
                Location = request.Location.Trim(),
                Description = request.Description.Trim(),
                AffectedTripHorseId = request.AffectedTripHorseId,
                EmergencyOffload = request.EmergencyOffload
            };
        }

        public static ProposeIncidentPlanModel ToBusinessModel(this ProposeIncidentPlanRequest request)
        {
            return new ProposeIncidentPlanModel
            {
                ProposedAction = request.ProposedAction.Trim(),
                ResponseAction = request.ResponseAction.Trim(),
                RouteChange = request.RouteChange?.ToBusinessModel(),
                RevisedRouteNotes = string.IsNullOrWhiteSpace(request.RevisedRouteNotes) ? null : request.RevisedRouteNotes.Trim(),
                AdditionalCost = request.AdditionalCost,
                ExtraCostForCustomer = request.ExtraCostForCustomer,
                AffectedTripHorseId = request.AffectedTripHorseId,
                OffloadOutcome = string.IsNullOrWhiteSpace(request.OffloadOutcome) ? null : request.OffloadOutcome.Trim(),
                VetReportPath = string.IsNullOrWhiteSpace(request.VetReportPath) ? null : request.VetReportPath.Trim()
            };
        }

        public static ApplyRerouteModel ToBusinessModel(this ApplyRerouteRequest request)
        {
            return new ApplyRerouteModel
            {
                InsertAfterCheckpointId = request.InsertAfterCheckpointId,
                DeactivateCheckpointIds = request.DeactivateCheckpointIds,
                NewCheckpoints = request.NewCheckpoints.Select(c => c.ToBusinessModel()).ToList()
            };
        }

        public static IncidentResponse ToResponse(this IncidentModel model)
        {
            return new IncidentResponse
            {
                IncidentId = model.IncidentId,
                IncidentCode = model.IncidentCode,
                TripId = model.TripId,
                TripCode = model.TripCode,
                TripStatus = model.TripStatus,
                IncidentType = model.IncidentType,
                Severity = model.Severity,
                Status = model.Status,
                Location = model.Location,
                Description = model.Description,
                AffectedTripHorseId = model.AffectedTripHorseId,
                AffectedHorseName = model.AffectedHorseName,
                ReportedByUserId = model.ReportedByUserId,
                ReportedByName = model.ReportedByName,
                ReportedAt = model.ReportedAt,
                ProposedAction = model.ProposedAction,
                ResponseAction = model.ResponseAction,
                RouteChange = model.RouteChange == null ? null : new IncidentRouteChangeResponse
                {
                    InsertAfterCheckpointId = model.RouteChange.InsertAfterCheckpointId,
                    InsertAfterName = model.RouteChange.InsertAfterName,
                    AppliedAt = model.RouteChange.AppliedAt,
                    ReturnUnusedRatios = model.RouteChange.ReturnUnusedRatios,
                    TurnAroundCheckpointId = model.RouteChange.TurnAroundCheckpointId,
                    RemovedCheckpoints = model.RouteChange.RemovedCheckpoints.Select(c => new RouteChangeRemovedCheckpointResponse
                    {
                        CheckpointId = c.CheckpointId,
                        CheckpointName = c.CheckpointName,
                        PlannedTime = c.PlannedTime
                    }).ToList(),
                    NewCheckpoints = model.RouteChange.NewCheckpoints.Select(c => new RouteChangeNewCheckpointResponse
                    {
                        CheckpointId = c.CheckpointId,
                        CheckpointName = c.CheckpointName,
                        CheckpointType = c.CheckpointType,
                        Address = c.Address,
                        PlannedTime = c.PlannedTime,
                        MandatoryRestMinutes = c.MandatoryRestMinutes
                    }).ToList()
                },
                RevisedRouteNotes = model.RevisedRouteNotes,
                AdditionalCost = model.AdditionalCost,
                RejectionReason = model.RejectionReason,
                FaultParty = model.FaultParty,
                FaultNote = model.FaultNote,
                ApprovedByUserId = model.ApprovedByUserId,
                ApprovedByName = model.ApprovedByName,
                ApprovedAt = model.ApprovedAt,
                ResolvedAt = model.ResolvedAt,
                EmergencyOffload = model.EmergencyOffload,
                ExtraCostForCustomer = model.ExtraCostForCustomer,
                Consents = model.Consents,
                OffloadOutcome = model.OffloadOutcome,
                VetReportUrl = model.VetReportUrl
            };
        }
    }
}
