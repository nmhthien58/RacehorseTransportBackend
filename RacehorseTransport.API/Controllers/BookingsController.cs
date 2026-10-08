using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.Helpers;
using RacehorseTransport.API.Mappers;
using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    [Produces("application/json")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IPricingService _pricingService;

        public BookingsController(IBookingService bookingService, IPricingService pricingService)
        {
            _bookingService = bookingService;
            _pricingService = pricingService;
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
        /// Calculate a price quote without saving anything (used before submitting a booking).
        /// </summary>
        [HttpPost("quote-preview")]
        [ProducesResponseType(typeof(ApiResponse<QuoteResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PreviewQuote([FromBody] QuotePreviewRequest request)
        {
            var quote = await _pricingService.CalculateQuoteAsync(request.ToBusinessModel());
            return Ok(ApiResponse<QuoteResponse>.SuccessResult(quote.ToResponse(), "Quote calculated successfully"));
        }
    }
}
