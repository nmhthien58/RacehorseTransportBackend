using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.Helpers;
using RacehorseTransport.API.Mappers;
using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Controllers
{
    [ApiController]
    [Route("api/trips")]
    [Produces("application/json")]
    [Authorize(Roles = UserRoles.Staff)]
    public class TripsController : ControllerBase
    {
        // Lập kế hoạch chuyến là việc của Coordinator; Manager và Admin xem được để duyệt
        private const string PlanningRoles = UserRoles.FleetCoordinator + "," + UserRoles.LogisticsManager + "," + UserRoles.Admin;
        private const string PlanWriteRoles = UserRoles.FleetCoordinator + "," + UserRoles.Admin;
        private const string PlanReviewRoles = UserRoles.LogisticsManager + "," + UserRoles.Admin;

        private readonly ITripService _tripService;
        private readonly ITripPlanningService _planningService;

        public TripsController(ITripService tripService, ITripPlanningService planningService)
        {
            _tripService = tripService;
            _planningService = planningService;
        }

        /// <summary>
        /// Planning queue: horses of approved bookings that are not on any trip yet.
        /// </summary>
        [HttpGet("unplanned-horses")]
        [Authorize(Roles = PlanningRoles)]
        [ProducesResponseType(typeof(ApiResponse<List<UnplannedHorseResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUnplannedHorses([FromQuery] string? transportMode = null, [FromQuery] int? bookingId = null)
        {
            var horses = await _planningService.GetUnplannedHorsesAsync(transportMode, bookingId);
            var items = horses.Select(h => h.ToResponse()).ToList();

            return Ok(ApiResponse<List<UnplannedHorseResponse>>.SuccessResult(items));
        }

        /// <summary>
        /// Vehicles that are not in maintenance and not booked on another trip between 'from' and 'to'.
        /// </summary>
        [HttpGet("available-vehicles")]
        [Authorize(Roles = PlanningRoles)]
        [ProducesResponseType(typeof(ApiResponse<List<AvailableVehicleResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAvailableVehicles([FromQuery] AvailabilityQueryRequest query)
        {
            var vehicles = await _planningService.GetAvailableVehiclesAsync(query.From!.Value, query.To!.Value, query.TransportMode);
            var items = vehicles.Select(v => v.ToResponse()).ToList();

            return Ok(ApiResponse<List<AvailableVehicleResponse>>.SuccessResult(items));
        }

        /// <summary>
        /// Drivers / escorts that are active and not assigned to another trip between 'from' and 'to'.
        /// </summary>
        [HttpGet("available-crew")]
        [Authorize(Roles = PlanningRoles)]
        [ProducesResponseType(typeof(ApiResponse<List<UserResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAvailableCrew([FromQuery] AvailabilityQueryRequest query)
        {
            var crew = await _planningService.GetAvailableCrewAsync(query.From!.Value, query.To!.Value);
            var items = crew.Select(u => u.ToResponse()).ToList();

            return Ok(ApiResponse<List<UserResponse>>.SuccessResult(items));
        }

        /// <summary>
        /// Get trips. A driver / escort gets only the trips they are crew on; other staff get all trips.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<TripSummaryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll([FromQuery] TripQueryRequest queryOptions)
        {
            var pagedResult = await _tripService.GetTripsAsync(queryOptions.ToBusinessOptions(), User.ToCurrentUser());
            var items = pagedResult.Items.Select(t => t.ToResponse()).ToList();

            return Ok(ApiCollectionResponse<TripSummaryResponse>.SuccessResult(
                items, pagedResult.ToPagination(), "Trips retrieved successfully"));
        }

        /// <summary>
        /// Get trip detail: vehicle, crew, horses with care requirements, checkpoints, and the bookings carried.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<TripDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var trip = await _tripService.GetTripByIdAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<TripDetailResponse>.SuccessResult(trip.ToResponse()));
        }

        // ---------------------------------------------------------------------------------------------
        // Flow 3: lập kế hoạch (Coordinator) -> trình -> duyệt (Manager)
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Create a draft trip: vehicle, crew, horses (from one or more approved bookings) and optionally the route checkpoints.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = PlanWriteRoles)]
        [ProducesResponseType(typeof(ApiResponse<TripDetailResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] TripPlanRequest request)
        {
            var created = await _planningService.CreateTripAsync(request.ToBusinessModel(), User.ToCurrentUser());
            var response = created.ToResponse();

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.TripId },
                ApiResponse<TripDetailResponse>.SuccessResult(response, "Trip draft created successfully"));
        }

        /// <summary>
        /// Update a draft trip. Crew and horses are replaced; checkpoints are replaced only when provided.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = PlanWriteRoles)]
        [ProducesResponseType(typeof(ApiResponse<TripDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] TripPlanRequest request)
        {
            var trip = await _planningService.UpdateTripAsync(id, request.ToBusinessModel(), User.ToCurrentUser());
            return Ok(ApiResponse<TripDetailResponse>.SuccessResult(trip.ToResponse(), "Trip draft updated successfully"));
        }

        /// <summary>
        /// Add a checkpoint to a draft trip (at the end, or at 'position').
        /// </summary>
        [HttpPost("{id:int}/checkpoints")]
        [Authorize(Roles = PlanWriteRoles)]
        [ProducesResponseType(typeof(ApiResponse<TripDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddCheckpoint(int id, [FromBody] CheckpointRequest request)
        {
            var trip = await _planningService.AddCheckpointAsync(id, request.ToBusinessModel(), User.ToCurrentUser());
            return Ok(ApiResponse<TripDetailResponse>.SuccessResult(trip.ToResponse(), "Checkpoint added successfully"));
        }

        /// <summary>
        /// Update a checkpoint of a draft trip.
        /// </summary>
        [HttpPut("{id:int}/checkpoints/{checkpointId:int}")]
        [Authorize(Roles = PlanWriteRoles)]
        [ProducesResponseType(typeof(ApiResponse<TripDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCheckpoint(int id, int checkpointId, [FromBody] CheckpointRequest request)
        {
            var trip = await _planningService.UpdateCheckpointAsync(id, checkpointId, request.ToBusinessModel(), User.ToCurrentUser());
            return Ok(ApiResponse<TripDetailResponse>.SuccessResult(trip.ToResponse(), "Checkpoint updated successfully"));
        }

        /// <summary>
        /// Remove a checkpoint from a draft trip; the remaining checkpoints are renumbered.
        /// </summary>
        [HttpDelete("{id:int}/checkpoints/{checkpointId:int}")]
        [Authorize(Roles = PlanWriteRoles)]
        [ProducesResponseType(typeof(ApiResponse<TripDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCheckpoint(int id, int checkpointId)
        {
            var trip = await _planningService.DeleteCheckpointAsync(id, checkpointId, User.ToCurrentUser());
            return Ok(ApiResponse<TripDetailResponse>.SuccessResult(trip.ToResponse(), "Checkpoint deleted successfully"));
        }

        /// <summary>
        /// Set the order of all checkpoints of a draft trip.
        /// </summary>
        [HttpPut("{id:int}/checkpoints/order")]
        [Authorize(Roles = PlanWriteRoles)]
        [ProducesResponseType(typeof(ApiResponse<TripDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReorderCheckpoints(int id, [FromBody] ReorderCheckpointsRequest request)
        {
            var trip = await _planningService.ReorderCheckpointsAsync(id, request.CheckpointIds, User.ToCurrentUser());
            return Ok(ApiResponse<TripDetailResponse>.SuccessResult(trip.ToResponse(), "Checkpoints reordered successfully"));
        }

        /// <summary>
        /// Submit a draft trip for the manager's approval. The route must start with an Origin and end with a Destination.
        /// </summary>
        [HttpPost("{id:int}/submit")]
        [Authorize(Roles = PlanWriteRoles)]
        [ProducesResponseType(typeof(ApiResponse<TripDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Submit(int id)
        {
            var trip = await _planningService.SubmitTripAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<TripDetailResponse>.SuccessResult(trip.ToResponse(), "Trip plan submitted for approval"));
        }

        /// <summary>
        /// Approve a submitted trip plan. The trip becomes Scheduled and the crew is notified.
        /// </summary>
        [HttpPost("{id:int}/approve")]
        [Authorize(Roles = PlanReviewRoles)]
        [ProducesResponseType(typeof(ApiResponse<TripDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Approve(int id)
        {
            var trip = await _planningService.ApproveTripAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<TripDetailResponse>.SuccessResult(trip.ToResponse(), "Trip plan approved successfully"));
        }

        /// <summary>
        /// Return a submitted trip plan to the coordinator with a reason. The third rejection cancels the trip.
        /// </summary>
        [HttpPost("{id:int}/reject")]
        [Authorize(Roles = PlanReviewRoles)]
        [ProducesResponseType(typeof(ApiResponse<TripDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectTripRequest request)
        {
            var trip = await _planningService.RejectTripAsync(id, request.Reason, User.ToCurrentUser());
            return Ok(ApiResponse<TripDetailResponse>.SuccessResult(trip.ToResponse(), "Trip plan rejected"));
        }

        /// <summary>
        /// Cancel a trip that has not departed yet. Its horses return to the planning queue.
        /// </summary>
        [HttpPost("{id:int}/cancel")]
        [Authorize(Roles = PlanningRoles)]
        [ProducesResponseType(typeof(ApiResponse<TripDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(int id)
        {
            var trip = await _planningService.CancelTripAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<TripDetailResponse>.SuccessResult(trip.ToResponse(), "Trip cancelled successfully"));
        }
    }
}