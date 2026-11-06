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
                ReplacementVehicleId = request.ReplacementVehicleId,
                RemoveUserId = request.RemoveUserId,
                AddUserId = request.AddUserId,
                CrewRole = string.IsNullOrWhiteSpace(request.CrewRole) ? null : request.CrewRole.Trim(),
                RouteChange = request.RouteChange?.ToBusinessModel(),
                RevisedRouteNotes = string.IsNullOrWhiteSpace(request.RevisedRouteNotes) ? null : request.RevisedRouteNotes.Trim(),
                AdditionalCost = request.AdditionalCost,
                ExtraCostForCustomer = request.ExtraCostForCustomer,
                AffectedTripHorseId = request.AffectedTripHorseId,
                OffloadOutcome = string.IsNullOrWhiteSpace(request.OffloadOutcome) ? null : request.OffloadOutcome.Trim(),
                VetReportPath = string.IsNullOrWhiteSpace(request.VetReportPath) ? null : request.VetReportPath.Trim(),
                RequiresEuthanasiaConsent = request.RequiresEuthanasiaConsent,
                VetClinic = request.VetClinic == null ? null : new VetClinicModel
                {
                    Name = request.VetClinic.Name.Trim(),
                    Address = request.VetClinic.Address.Trim(),
                    Phone = string.IsNullOrWhiteSpace(request.VetClinic.Phone) ? null : request.VetClinic.Phone.Trim(),
                    AttendingVetName = string.IsNullOrWhiteSpace(request.VetClinic.AttendingVetName) ? null : request.VetClinic.AttendingVetName.Trim(),
                    Latitude = request.VetClinic.Latitude,
                    Longitude = request.VetClinic.Longitude
                }
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
                InitialSeverity = model.InitialSeverity,
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
                ActionPayload = model.ActionPayload,
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
                        MandatoryRestMinutes = c.MandatoryRestMinutes,
                        Latitude = c.Latitude,
                        Longitude = c.Longitude
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
                MyExtraCostShare = model.MyExtraCostShare,
                MyHorsesOnBoard = model.MyHorsesOnBoard,
                HorsesOnBoard = model.HorsesOnBoard,
                Consents = model.Consents,
                OffloadOutcome = model.OffloadOutcome,
                VetReportUrl = model.VetReportUrl,
                Documents = model.Documents.Select(d => d.ToResponse()).ToList(),
                RequiresEuthanasiaConsent = model.RequiresEuthanasiaConsent,
                PendingConsentType = model.PendingConsentType,
                ConsentRequestedAt = model.ConsentRequestedAt,
                ConsentDeadline = model.ConsentDeadline,
                EmergencyAuthorizedAt = model.EmergencyAuthorizedAt,
                EmergencyAuthorizedByName = model.EmergencyAuthorizedByName,
                EmergencyAuthorizationNote = model.EmergencyAuthorizationNote
            };
        }

        // Stream của file do controller giữ và giải phóng (request.File.ToUploadModel())
        public static UploadIncidentDocumentModel ToBusinessModel(this UploadIncidentDocumentRequest request, FileUploadModel file)
        {
            return new UploadIncidentDocumentModel
            {
                DocumentType = request.DocumentType.Trim(),
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                File = file
            };
        }

        public static IncidentDocumentResponse ToResponse(this IncidentDocumentModel model)
        {
            return new IncidentDocumentResponse
            {
                IncidentDocumentId = model.IncidentDocumentId,
                IncidentId = model.IncidentId,
                FilePath = model.FilePath,
                FileUrl = model.FileUrl,
                DocumentType = model.DocumentType,
                Note = model.Note,
                UploadedByUserId = model.UploadedByUserId,
                UploadedByName = model.UploadedByName,
                UploadedAt = model.UploadedAt
            };
        }
    }
}
