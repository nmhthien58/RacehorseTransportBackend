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
        private readonly ITripService _tripService;

        public TripsController(ITripService tripService)
        {
            _tripService = tripService;
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
