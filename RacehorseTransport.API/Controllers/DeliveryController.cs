using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.Helpers;
using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Controllers
{
    // R-16 (BE-16): không giao được ngựa
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    [Authorize]
    public class DeliveryController : ControllerBase
    {
        private readonly IDeliveryService _deliveryService;

        public DeliveryController(IDeliveryService deliveryService)
        {
            _deliveryService = deliveryService;
        }

        /// <summary>
        /// Crew records that a booking could not be delivered at its drop-off point (absent / refused / injured / wrong horse).
        /// </summary>
        [HttpPost("trips/{tripId:int}/bookings/{bookingId:int}/delivery-failed")]
        [Authorize(Roles = UserRoles.DriverEscort + "," + UserRoles.Admin)]
        [ProducesResponseType(typeof(ApiResponse<DeliveryFailureModel>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RecordFailure(int tripId, int bookingId, [FromBody] RecordDeliveryFailureRequest request)
        {
            var failure = await _deliveryService.RecordFailureAsync(tripId, bookingId, new RecordDeliveryFailureModel
            {
                Reason = request.Reason.Trim(),
                Note = request.Note,
                PhotoPaths = request.PhotoPaths
            }, User.ToCurrentUser());
            return StatusCode(StatusCodes.Status201Created, ApiResponse<DeliveryFailureModel>.SuccessResult(failure, "Failed delivery recorded"));
        }

        /// <summary>
        /// Customer chooses what to do after a failed delivery (within Delivery.DecisionHours).
        /// </summary>
        [HttpPost("bookings/{id:int}/delivery-decision")]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<DeliveryFailureModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Decide(int id, [FromBody] DeliveryDecisionRequest request)
        {
            var failure = await _deliveryService.DecideAsync(id, new DeliveryDecisionModel
            {
                Decision = request.Decision.Trim(),
                RecipientName = request.RecipientName,
                RecipientPhone = request.RecipientPhone,
                Address = request.Address,
                Lat = request.Lat,
                Lng = request.Lng
            }, User.ToCurrentUser());
            return Ok(ApiResponse<DeliveryFailureModel>.SuccessResult(failure, "Decision recorded"));
        }

        /// <summary>
        /// Failed deliveries of a booking, newest first.
        /// </summary>
        [HttpGet("bookings/{id:int}/delivery-failures")]
        [ProducesResponseType(typeof(ApiResponse<List<DeliveryFailureModel>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetForBooking(int id)
        {
            var list = await _deliveryService.GetForBookingAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<List<DeliveryFailureModel>>.SuccessResult(list, "Failed deliveries retrieved successfully"));
        }
    }
}
