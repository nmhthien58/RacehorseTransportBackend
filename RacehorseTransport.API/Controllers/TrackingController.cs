using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.Helpers;
using RacehorseTransport.API.Mappers;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Controllers
{
    [ApiController]
    [Route("api/tracking")]
    [Produces("application/json")]
    [Authorize]
    public class TrackingController : ControllerBase
    {
        private readonly ITrackingService _trackingService;

        public TrackingController(ITrackingService trackingService)
        {
            _trackingService = trackingService;
        }

        /// <summary>
        /// Operations dashboard: actual progress versus plan for each trip. Without 'status' it returns all trips that are not closed yet.
        /// </summary>
        [HttpGet("trips")]
        [Authorize(Roles = UserRoles.FleetCoordinator + "," + UserRoles.LogisticsManager + "," + UserRoles.Admin)]
        [ProducesResponseType(typeof(ApiResponse<List<TripProgressResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetTripProgress([FromQuery] string? status = null)
        {
            var progress = await _trackingService.GetTripProgressAsync(status);
            var items = progress.Select(p => p.ToResponse()).ToList();

            return Ok(ApiResponse<List<TripProgressResponse>>.SuccessResult(items));
        }

        /// <summary>
        /// Follow a booking: the checkpoint timeline of each trip carrying its horses, and the welfare logs of those horses.
        /// </summary>
        [HttpGet("bookings/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<BookingTrackingResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBookingTracking(int id)
        {
            var tracking = await _trackingService.GetBookingTrackingAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<BookingTrackingResponse>.SuccessResult(tracking.ToResponse()));
        }
    }
}
