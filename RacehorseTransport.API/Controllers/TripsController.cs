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
    }
}
