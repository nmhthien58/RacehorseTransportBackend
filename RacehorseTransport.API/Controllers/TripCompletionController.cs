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
    // Flow 6. Đoàn xe lập biên bản bàn giao (e-POD) cho từng đơn trên chuyến; Manager đóng chuyến và chốt KPI.
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    [Authorize]
    public class TripCompletionController : ControllerBase
    {
        private const string HandoverRoles = UserRoles.DriverEscort + "," + UserRoles.Admin;
        private const string CloseRoles = UserRoles.LogisticsManager + "," + UserRoles.Admin;

        private readonly ITripCompletionService _completionService;

        public TripCompletionController(ITripCompletionService completionService)
        {
            _completionService = completionService;
        }

        /// <summary>
        /// Record the signed handover (e-POD) of one booking on a trip that has arrived. multipart/form-data; attach the recipient's signature image (jpg, png; up to 10 MB) in the "signature" field and repeat "microchipNumbers" for every horse scanned.
        /// </summary>
        [HttpPost("trips/{tripId:int}/handover")]
        [Authorize(Roles = HandoverRoles)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(SupabaseFileStorageService.MaxFileSizeBytes + 1024 * 1024)]
        [ProducesResponseType(typeof(ApiResponse<HandoverResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateHandover(int tripId, [FromForm] CreateHandoverRequest request)
        {
            using var signature = request.Signature.ToUploadModel()!;
            var handover = await _completionService.CreateHandoverAsync(tripId, request.ToBusinessModel(signature), User.ToCurrentUser());

            return StatusCode(
                StatusCodes.Status201Created,
                ApiResponse<HandoverResponse>.SuccessResult(handover.ToResponse(), "Handover recorded successfully"));
        }

        /// <summary>
        /// Get the handover records of a trip (staff; a driver / escort only for their own trips).
        /// </summary>
        [HttpGet("trips/{tripId:int}/handovers")]
        [Authorize(Roles = UserRoles.Staff)]
        [ProducesResponseType(typeof(ApiResponse<List<HandoverResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTripHandovers(int tripId)
        {
            var handovers = await _completionService.GetTripHandoversAsync(tripId, User.ToCurrentUser());
            return Ok(ApiResponse<List<HandoverResponse>>.SuccessResult(handovers.Select(h => h.ToResponse()).ToList()));
        }

        /// <summary>
        /// Get the handover records of a booking (the customer who owns it, or staff allowed to see the booking).
        /// </summary>
        [HttpGet("bookings/{bookingId:int}/handovers")]
        [ProducesResponseType(typeof(ApiResponse<List<HandoverResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBookingHandovers(int bookingId)
        {
            var handovers = await _completionService.GetBookingHandoversAsync(bookingId, User.ToCurrentUser());
            return Ok(ApiResponse<List<HandoverResponse>>.SuccessResult(handovers.Select(h => h.ToResponse()).ToList()));
        }

        /// <summary>
        /// Close a trip after every booking on it has been handed over: settles delay, on-time status, KPI and actual cost; frees the vehicle; completes finished bookings.
        /// </summary>
        [HttpPost("trips/{tripId:int}/close")]
        [Authorize(Roles = CloseRoles)]
        [ProducesResponseType(typeof(ApiResponse<TripDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CloseTrip(int tripId, [FromBody] CloseTripRequest? request)
        {
            var trip = await _completionService.CloseTripAsync(tripId, request?.ExecutiveRemarks, User.ToCurrentUser());
            return Ok(ApiResponse<TripDetailResponse>.SuccessResult(trip.ToResponse(), "Trip closed successfully"));
        }
    }
}
