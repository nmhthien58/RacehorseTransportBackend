using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.Helpers;
using RacehorseTransport.API.Mappers;
using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Controllers
{
    // R-16 (BE-15): người nhận ngựa
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    [Authorize]
    public class RecipientsController : ControllerBase
    {
        private readonly IRecipientService _recipientService;
        private readonly IBookingService _bookingService;

        public RecipientsController(IRecipientService recipientService, IBookingService bookingService)
        {
            _recipientService = recipientService;
            _bookingService = bookingService;
        }

        /// <summary>
        /// Customer sets or changes the recipient of a booking (until the vehicle arrives at the drop-off point).
        /// </summary>
        [HttpPut("bookings/{id:int}/recipient")]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRecipient(int id, [FromBody] UpdateRecipientRequest request)
        {
            var user = User.ToCurrentUser();
            await _recipientService.UpdateRecipientAsync(id, new RecipientInputModel
            {
                RecipientIsCustomer = request.RecipientIsCustomer,
                RecipientName = request.RecipientName,
                RecipientPhone = request.RecipientPhone,
                RecipientEmail = request.RecipientEmail,
                RecipientIdNumber = request.RecipientIdNumber
            }, user);
            var booking = await _bookingService.GetBookingByIdAsync(id, user);
            return Ok(ApiResponse<BookingDetailResponse>.SuccessResult(booking.ToResponse(), "Recipient updated"));
        }

        /// <summary>
        /// Customer confirms that the recipient will be at the drop-off point (asked 24 hours before the ETA).
        /// </summary>
        [HttpPost("bookings/{id:int}/confirm-recipient")]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ConfirmRecipient(int id)
        {
            var user = User.ToCurrentUser();
            await _recipientService.ConfirmRecipientAsync(id, user);
            var booking = await _bookingService.GetBookingByIdAsync(id, user);
            return Ok(ApiResponse<BookingDetailResponse>.SuccessResult(booking.ToResponse(), "Recipient confirmed"));
        }

        /// <summary>
        /// Customer allows another person to sign the e-POD instead of the recipient.
        /// </summary>
        [HttpPost("bookings/{id:int}/approve-alternate-recipient")]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ApproveAlternateRecipient(int id, [FromBody] ApproveAlternateRecipientRequest request)
        {
            var user = User.ToCurrentUser();
            await _recipientService.ApproveAlternateRecipientAsync(id, request.Name, request.IdNumber, user);
            var booking = await _bookingService.GetBookingByIdAsync(id, user);
            return Ok(ApiResponse<BookingDetailResponse>.SuccessResult(booking.ToResponse(), "Alternate recipient approved"));
        }

        /// <summary>
        /// Recipients of every booking on a trip, for the crew (prefilled e-POD, call button) and staff.
        /// </summary>
        [HttpGet("trips/{id:int}/recipients")]
        [ProducesResponseType(typeof(ApiResponse<List<TripRecipientModel>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTripRecipients(int id)
        {
            var list = await _recipientService.GetTripRecipientsAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<List<TripRecipientModel>>.SuccessResult(list, "Recipients retrieved successfully"));
        }
    }
}
