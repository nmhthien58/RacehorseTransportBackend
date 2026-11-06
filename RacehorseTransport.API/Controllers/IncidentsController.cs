using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.Helpers;
using RacehorseTransport.API.Mappers;
using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.Implementations;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Controllers
{
    // Flow 5. Đoàn xe báo sự cố, Coordinator đề xuất phương án và nắn tuyến, Manager duyệt.
    // Ai xem được sự cố nào (khách / đoàn xe / vận hành) kiểm tra trong IncidentService.
    [ApiController]
    [Route("api/incidents")]
    [Produces("application/json")]
    [Authorize]
    public class IncidentsController : ControllerBase
    {
        private const string ReportRoles = UserRoles.DriverEscort + "," + UserRoles.Admin;
        private const string PlanWriteRoles = UserRoles.FleetCoordinator + "," + UserRoles.Admin;
        private const string PlanReviewRoles = UserRoles.LogisticsManager + "," + UserRoles.Admin;

        private readonly IIncidentService _incidentService;

        public IncidentsController(IIncidentService incidentService)
        {
            _incidentService = incidentService;
        }

        /// <summary>
        /// Get incidents. Customer: incidents on trips carrying their horses. Driver / escort: their own trips. Other staff: all.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<IncidentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll([FromQuery] IncidentQueryRequest queryOptions)
        {
            var pagedResult = await _incidentService.GetIncidentsAsync(queryOptions.ToBusinessOptions(), User.ToCurrentUser());
            var items = pagedResult.Items.Select(i => i.ToResponse()).ToList();

            return Ok(ApiCollectionResponse<IncidentResponse>.SuccessResult(
                items, pagedResult.ToPagination(), "Incidents retrieved successfully"));
        }

        /// <summary>
        /// Get incident by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<IncidentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var incident = await _incidentService.GetIncidentByIdAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<IncidentResponse>.SuccessResult(incident.ToResponse()));
        }

        /// <summary>
        /// Get every incident of one trip, newest first (staff only).
        /// </summary>
        [HttpGet("~/api/trips/{tripId:int}/incidents")]
        [Authorize(Roles = UserRoles.Staff)]
        [ProducesResponseType(typeof(ApiResponse<List<IncidentResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByTrip(int tripId)
        {
            var incidents = await _incidentService.GetTripIncidentsAsync(tripId, User.ToCurrentUser());
            var items = incidents.Select(i => i.ToResponse()).ToList();

            return Ok(ApiResponse<List<IncidentResponse>>.SuccessResult(items));
        }

        /// <summary>
        /// Report an incident on a trip that is on the road (crew of the trip). The coordinator, managers and customers are notified.
        /// </summary>
        [HttpPost("~/api/trips/{tripId:int}/incidents")]
        [Authorize(Roles = ReportRoles)]
        [ProducesResponseType(typeof(ApiResponse<IncidentResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Report(int tripId, [FromBody] ReportIncidentRequest request)
        {
            var created = await _incidentService.ReportIncidentAsync(tripId, request.ToBusinessModel(), User.ToCurrentUser());
            var response = created.ToResponse();

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.IncidentId },
                ApiResponse<IncidentResponse>.SuccessResult(response, "Incident reported successfully"));
        }

        /// <summary>
        /// Propose a response plan (action, route notes, additional cost). Allowed for a reported incident or after a rejection.
        /// </summary>
        [HttpPost("{id:int}/propose")]
        [Authorize(Roles = PlanWriteRoles)]
        [ProducesResponseType(typeof(ApiResponse<IncidentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Propose(int id, [FromBody] ProposeIncidentPlanRequest request)
        {
            var incident = await _incidentService.ProposePlanAsync(id, request.ToBusinessModel(), User.ToCurrentUser());
            return Ok(ApiResponse<IncidentResponse>.SuccessResult(incident.ToResponse(), "Plan proposed successfully"));
        }

        /// <summary>
        /// Approve the proposed plan.
        /// </summary>
        [HttpPost("{id:int}/approve")]
        [Authorize(Roles = PlanReviewRoles)]
        [ProducesResponseType(typeof(ApiResponse<IncidentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Approve(int id)
        {
            var incident = await _incidentService.ApprovePlanAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<IncidentResponse>.SuccessResult(incident.ToResponse(), "Plan approved successfully"));
        }

        /// <summary>
        /// Customer with horses on the trip accepts or declines a detour that costs extra. All accept -> the detour is applied;
        /// one declines -> the plan becomes ReturnToOrigin.
        /// </summary>
        [HttpPost("{id:int}/customer-consent")]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<IncidentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CustomerConsent(int id, [FromBody] IncidentConsentRequest request)
        {
            var incident = await _incidentService.RecordCustomerConsentAsync(id, request.Accept!.Value, User.ToCurrentUser());
            return Ok(ApiResponse<IncidentResponse>.SuccessResult(incident.ToResponse(), "Your answer has been recorded"));
        }

        /// <summary>
        /// Reject the proposed plan with a reason. The coordinator can propose again.
        /// </summary>
        [HttpPost("{id:int}/reject")]
        [Authorize(Roles = PlanReviewRoles)]
        [ProducesResponseType(typeof(ApiResponse<IncidentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectIncidentPlanRequest request)
        {
            var incident = await _incidentService.RejectPlanAsync(id, request.Reason, User.ToCurrentUser());
            return Ok(ApiResponse<IncidentResponse>.SuccessResult(incident.ToResponse(), "Plan rejected successfully"));
        }

        /// <summary>
        /// Change the route after the plan is approved: remove checkpoints not reached yet and insert new ones. The trip becomes EmergencyRerouting.
        /// </summary>
        [HttpPost("{id:int}/apply-reroute")]
        [Authorize(Roles = PlanWriteRoles)]
        [ProducesResponseType(typeof(ApiResponse<TripDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApplyReroute(int id, [FromBody] ApplyRerouteRequest request)
        {
            var trip = await _incidentService.ApplyRerouteAsync(id, request.ToBusinessModel(), User.ToCurrentUser());
            return Ok(ApiResponse<TripDetailResponse>.SuccessResult(trip.ToResponse(), "Route updated successfully"));
        }

        /// <summary>
        /// Close an incident whose plan was approved, choosing who caused it (Company, Customer or ForceMajeure).
        /// The cause decides who pays and whether late delivery is compensated. A rerouted trip goes back to InTransit.
        /// </summary>
        [HttpPost("{id:int}/resolve")]
        [Authorize(Roles = PlanWriteRoles)]
        [ProducesResponseType(typeof(ApiResponse<IncidentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Resolve(int id, [FromBody] ResolveIncidentRequest request)
        {
            var incident = await _incidentService.ResolveIncidentAsync(id, request.FaultParty.Trim(), request.FaultNote, User.ToCurrentUser());
            return Ok(ApiResponse<IncidentResponse>.SuccessResult(incident.ToResponse(), "Incident resolved successfully"));
        }

        /// <summary>
        /// Attach a document to an incident: ScenePhoto, VetReport, DeathCertificate or FitToTravel.
        /// multipart/form-data; the file (pdf, jpg, png; up to 10 MB) goes in the "file" field.
        /// Staff only; the crew can only attach to incidents of their own trips. Works after the incident is resolved too
        /// (a vet report often arrives later). The trip planner and logistics managers are notified.
        /// A Deceased offload plan needs a VetReport or DeathCertificate attached here first.
        /// </summary>
        [HttpPost("{id:int}/documents")]
        [Authorize(Roles = UserRoles.Staff)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(SupabaseFileStorageService.MaxFileSizeBytes + 1024 * 1024)]
        [ProducesResponseType(typeof(ApiResponse<IncidentDocumentResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadDocument(int id, [FromForm] UploadIncidentDocumentRequest request)
        {
            using var file = request.File.ToUploadModel()!;
            var document = await _incidentService.UploadDocumentAsync(id, request.ToBusinessModel(file), User.ToCurrentUser());
            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<IncidentDocumentResponse>.SuccessResult(document.ToResponse(), "Incident document uploaded successfully"));
        }

        /// <summary>
        /// Remove a document uploaded by mistake. Only the uploader (or a logistics manager / admin), and only while the incident
        /// is still open (not Approved / Resolved): an approved plan keeps its evidence.
        /// </summary>
        [HttpDelete("{id:int}/documents/{documentId:int}")]
        [Authorize(Roles = UserRoles.Staff)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDocument(int id, int documentId)
        {
            await _incidentService.DeleteDocumentAsync(id, documentId, User.ToCurrentUser());
            return Ok(ApiResponse<object?>.SuccessResult(null, "Incident document deleted successfully"));
        }
    }
}
