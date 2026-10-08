using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.Helpers;
using RacehorseTransport.API.Mappers;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Controllers
{
    // Check-in tại từng mốc của chuyến đang chạy. Chỉ đoàn xe của chuyến đó (kiểm tra trong service) hoặc Admin.
    [ApiController]
    [Route("api/checkpoints")]
    [Produces("application/json")]
    [Authorize(Roles = UserRoles.DriverEscort + "," + UserRoles.Admin)]
    public class CheckpointsController : ControllerBase
    {
        private readonly ITripExecutionService _executionService;

        public CheckpointsController(ITripExecutionService executionService)
        {
            _executionService = executionService;
        }

        /// <summary>
        /// Record arrival at a checkpoint. Checkpoints must be visited in order; arriving at the Destination marks the trip as ArrivedDestination.
        /// </summary>
        [HttpPost("{id:int}/arrive")]
        [ProducesResponseType(typeof(ApiResponse<TripDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Arrive(int id)
        {
            var trip = await _executionService.ArriveAtCheckpointAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<TripDetailResponse>.SuccessResult(trip.ToResponse(), "Arrival recorded successfully"));
        }

        /// <summary>
        /// Mark a border gate as cleared (customs and quarantine done). Required before departing from a border gate.
        /// </summary>
        [HttpPost("{id:int}/clear")]
        [ProducesResponseType(typeof(ApiResponse<TripDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Clear(int id)
        {
            var trip = await _executionService.ClearCheckpointAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<TripDetailResponse>.SuccessResult(trip.ToResponse(), "Clearance recorded successfully"));
        }

        /// <summary>
        /// Record departure from a checkpoint. Not allowed before the mandatory rest time has passed.
        /// </summary>
        [HttpPost("{id:int}/depart")]
        [ProducesResponseType(typeof(ApiResponse<TripDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Depart(int id)
        {
            var trip = await _executionService.DepartCheckpointAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<TripDetailResponse>.SuccessResult(trip.ToResponse(), "Departure recorded successfully"));
        }
    }
}
