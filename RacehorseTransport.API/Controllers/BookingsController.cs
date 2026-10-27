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
    [ApiController]
    [Route("api/bookings")]
    [Produces("application/json")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private const string ReviewRoles = UserRoles.LogisticsManager + "," + UserRoles.Admin;

        private readonly IBookingService _bookingService;
        private readonly IPricingService _pricingService;
        private readonly IBookingQuoteService _quoteService;
        private readonly IBookingChangeService _changeService;

        public BookingsController(IBookingService bookingService, IPricingService pricingService, IBookingQuoteService quoteService, IBookingChangeService changeService)
        {
            _bookingService = bookingService;
            _pricingService = pricingService;
            _quoteService = quoteService;
            _changeService = changeService;
        }

        /// <summary>
        /// Get bookings. Customer: own bookings. Specialist: bookings assigned to them. Manager / Coordinator / Admin: all.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<BookingSummaryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll([FromQuery] BookingQueryRequest queryOptions)
        {
            var pagedResult = await _bookingService.GetBookingsAsync(queryOptions.ToBusinessOptions(), User.ToCurrentUser());
            var items = pagedResult.Items.Select(b => b.ToResponse()).ToList();

            return Ok(ApiCollectionResponse<BookingSummaryResponse>.SuccessResult(
                items, pagedResult.ToPagination(), "Bookings retrieved successfully"));
        }

        /// <summary>
        /// Get booking detail: quote lines, review info, and for each horse its dossier and the trips it is placed on.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<BookingDetailResponse>.SuccessResult(booking.ToResponse()));
        }

        /// <summary>
        /// Submit a transport request for one or more of the customer's own horses. The price is calculated by the server.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateBookingRequest request)
        {
            var created = await _bookingService.CreateBookingAsync(request.ToBusinessModel(), User.ToCurrentUser());
            var response = created.ToResponse();

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.BookingId },
                ApiResponse<BookingDetailResponse>.SuccessResult(response, "Booking submitted successfully"));
        }

        /// <summary>
        /// Approve a submitted booking and choose the specialist in charge. Opens one clearance dossier per horse.
        /// </summary>
        [HttpPost("{id:int}/approve")]
        [Authorize(Roles = ReviewRoles)]
        [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Approve(int id, [FromBody] AssignSpecialistRequest request)
        {
            var booking = await _bookingService.ApproveBookingAsync(id, request.SpecialistUserId!.Value, User.ToCurrentUser());
            return Ok(ApiResponse<BookingDetailResponse>.SuccessResult(booking.ToResponse(), "Booking approved successfully"));
        }

        /// <summary>
        /// Confirm or change the transit countries of a submitted booking (re-calculates the quote).
        /// </summary>
        [HttpPut("{id:int}/transit-countries")]
        [Authorize(Roles = ReviewRoles)]
        [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTransitCountries(int id, [FromBody] UpdateTransitCountriesRequest request)
        {
            var booking = await _bookingService.UpdateTransitCountriesAsync(id, request.TransitCountryCodes, User.ToCurrentUser());
            return Ok(ApiResponse<BookingDetailResponse>.SuccessResult(booking.ToResponse(), "Transit countries updated successfully"));
        }

        /// <summary>
        /// Set the agreed value of each horse (default = the declared value) before the quote is sent.
        /// Insurance and compensation use the agreed value.
        /// </summary>
        [HttpPut("{id:int}/horse-values")]
        [Authorize(Roles = ReviewRoles)]
        [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateHorseValues(int id, [FromBody] UpdateHorseValuesRequest request)
        {
            var booking = await _bookingService.UpdateHorseValuesAsync(id, request.ToBusinessModel(), User.ToCurrentUser());
            return Ok(ApiResponse<BookingDetailResponse>.SuccessResult(booking.ToResponse(), "Agreed horse values updated successfully"));
        }

        /// <summary>
        /// Customer accepts the final quote and the draft contract (contractId + SHA-256 they reviewed, current service policy version).
        /// The contract is concluded and the booking then waits for the deposit until the quote deadline.
        /// </summary>
        [HttpPost("{id:int}/accept-quote")]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AcceptQuote(int id, [FromBody] AcceptQuoteRequest request)
        {
            var booking = await _quoteService.AcceptQuoteAsync(id, new ContractAcceptanceModel
            {
                ContractId = request.ContractId,
                ContractSha256 = request.ContractSha256,
                AcceptPolicy = request.AcceptPolicy,
                PolicyVersion = request.PolicyVersion,
                IpAddress = HttpContext.GetClientIp(),
                UserAgent = HttpContext.GetUserAgent()
            }, User.ToCurrentUser());
            return Ok(ApiResponse<BookingDetailResponse>.SuccessResult(booking.ToResponse(), "Quote accepted, please pay the deposit"));
        }

        /// <summary>
        /// Customer changes the pickup / drop-off before paying anything. The estimate is recalculated; a planned route or quote is withdrawn
        /// and the booking goes back to route planning (PreApproved), then receives a new final quote.
        /// </summary>
        [HttpPut("{id:int}/locations")]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeLocations(int id, [FromBody] ChangeLocationsRequest request)
        {
            var booking = await _changeService.ChangeLocationsAsync(id, new ChangeLocationsModel
            {
                PickupAddress = request.PickupAddress,
                PickupCountryCode = request.PickupCountryCode.Trim().ToUpperInvariant(),
                PickupLat = request.PickupLat,
                PickupLng = request.PickupLng,
                DropoffAddress = request.DropoffAddress,
                DropoffCountryCode = request.DropoffCountryCode.Trim().ToUpperInvariant(),
                DropoffLat = request.DropoffLat,
                DropoffLng = request.DropoffLng
            }, User.ToCurrentUser());
            return Ok(ApiResponse<BookingDetailResponse>.SuccessResult(booking.ToResponse(), "Pickup / drop-off updated"));
        }

        /// <summary>
        /// Customer cancels because the company changed the route to a new country whose documents they cannot provide: 100% refund.
        /// Allowed until routeChangeCancelUntil.
        /// </summary>
        [HttpPost("{id:int}/cancel-route-change")]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelForRouteChange(int id)
        {
            var booking = await _changeService.CancelForRouteChangeAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<BookingDetailResponse>.SuccessResult(booking.ToResponse(), "Booking cancelled with a full refund"));
        }

        /// <summary>
        /// Customer declines the final quote: the booking is cancelled without any fee.
        /// </summary>
        [HttpPost("{id:int}/decline-quote")]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeclineQuote(int id, [FromBody] DeclineQuoteRequest? request)
        {
            var booking = await _quoteService.DeclineQuoteAsync(id, request?.Reason, User.ToCurrentUser());
            return Ok(ApiResponse<BookingDetailResponse>.SuccessResult(booking.ToResponse(), "Quote declined, the booking is cancelled"));
        }

        /// <summary>
        /// Reject a submitted booking with a reason.
        /// </summary>
        [HttpPost("{id:int}/reject")]
        [Authorize(Roles = ReviewRoles)]
        [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectBookingRequest request)
        {
            var booking = await _bookingService.RejectBookingAsync(id, request.Reason, User.ToCurrentUser());
            return Ok(ApiResponse<BookingDetailResponse>.SuccessResult(booking.ToResponse(), "Booking rejected successfully"));
        }

        /// <summary>
        /// Change the specialist in charge of an approved booking (open dossiers move to the new specialist).
        /// </summary>
        [HttpPost("{id:int}/reassign-specialist")]
        [Authorize(Roles = ReviewRoles)]
        [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReassignSpecialist(int id, [FromBody] AssignSpecialistRequest request)
        {
            var booking = await _bookingService.ReassignSpecialistAsync(id, request.SpecialistUserId!.Value, User.ToCurrentUser());
            return Ok(ApiResponse<BookingDetailResponse>.SuccessResult(booking.ToResponse(), "Specialist reassigned successfully"));
        }

        /// <summary>
        /// Cancel own booking while it has not been reviewed yet.
        /// </summary>
        [HttpPost("{id:int}/cancel")]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<BookingDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _bookingService.CancelBookingAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<BookingDetailResponse>.SuccessResult(booking.ToResponse(), "Booking cancelled successfully"));
        }

        /// <summary>
        /// Calculate a price quote without saving anything (used before submitting a booking).
        /// </summary>
        [HttpPost("quote-preview")]
        [ProducesResponseType(typeof(ApiResponse<QuoteResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PreviewQuote([FromBody] QuotePreviewRequest request)
        {
            var quote = await _pricingService.CalculateQuoteRangeAsync(request.ToBusinessModel());
            return Ok(ApiResponse<QuoteResponse>.SuccessResult(quote.ToResponse(), "Quote calculated successfully"));
        }

        /// <summary>
        /// Estimate ground transit distance (km) and duration based on pickup and dropoff locations.
        /// </summary>
        [HttpPost("estimate-distance")]
        [ProducesResponseType(typeof(ApiResponse<DistanceEstimateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EstimateDistance([FromBody] EstimateDistanceRequest request, [FromServices] IDistanceService distanceService)
        {
            var result = await distanceService.EstimateGroundDistanceAsync(request.ToBusinessModel());
            return Ok(ApiResponse<DistanceEstimateResponse>.SuccessResult(result.ToResponse(), "Distance estimated successfully"));
        }
    }
}
