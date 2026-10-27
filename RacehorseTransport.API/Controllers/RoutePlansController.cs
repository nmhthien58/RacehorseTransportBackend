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
    // R-10: tuyến dự kiến của đơn. Coordinator lập trên bản đồ và trình; Manager duyệt / từ chối.
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    [Authorize]
    public class RoutePlansController : ControllerBase
    {
        private const string PlannerRoles = UserRoles.FleetCoordinator + "," + UserRoles.Admin;
        private const string ReviewRoles = UserRoles.LogisticsManager + "," + UserRoles.Admin;

        private readonly IRoutePlanService _service;

        public RoutePlansController(IRoutePlanService service)
        {
            _service = service;
        }

        /// <summary>
        /// Route plans of a booking, newest version first. Staff see every version; the customer sees only the approved route.
        /// </summary>
        [HttpGet("bookings/{bookingId:int}/route-plans")]
        [ProducesResponseType(typeof(ApiResponse<List<RoutePlanResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetForBooking(int bookingId)
        {
            var plans = await _service.GetForBookingAsync(bookingId, User.ToCurrentUser());
            return Ok(ApiResponse<List<RoutePlanResponse>>.SuccessResult(plans.Select(p => p.ToResponse()).ToList()));
        }

        /// <summary>
        /// One route plan.
        /// </summary>
        [HttpGet("route-plans/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<RoutePlanResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var plan = await _service.GetByIdAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<RoutePlanResponse>.SuccessResult(plan.ToResponse()));
        }

        /// <summary>
        /// Calculate a route (legs, transit countries, border warnings) without saving it. Call it while the coordinator edits the stops.
        /// </summary>
        [HttpPost("bookings/{bookingId:int}/route-plans/preview")]
        [Authorize(Roles = PlannerRoles + "," + UserRoles.LogisticsManager)]
        [ProducesResponseType(typeof(ApiResponse<RoutePlanResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Preview(int bookingId, [FromBody] SaveRoutePlanRequest request)
        {
            var plan = await _service.PreviewAsync(bookingId, request.ToBusinessModel(), User.ToCurrentUser());
            return Ok(ApiResponse<RoutePlanResponse>.SuccessResult(plan.ToResponse()));
        }

        /// <summary>
        /// Start a new draft route for a pre-approved booking (pickup and drop-off come from the booking).
        /// </summary>
        [HttpPost("bookings/{bookingId:int}/route-plans")]
        [Authorize(Roles = PlannerRoles)]
        [ProducesResponseType(typeof(ApiResponse<RoutePlanResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Create(int bookingId, [FromBody] SaveRoutePlanRequest request)
        {
            var plan = await _service.CreateAsync(bookingId, request.ToBusinessModel(), User.ToCurrentUser());
            return StatusCode(StatusCodes.Status201Created, ApiResponse<RoutePlanResponse>.SuccessResult(plan.ToResponse(), "Route plan created successfully"));
        }

        /// <summary>
        /// Replace the stops of a draft or rejected route (it becomes a draft again).
        /// </summary>
        [HttpPut("route-plans/{id:int}")]
        [Authorize(Roles = PlannerRoles)]
        [ProducesResponseType(typeof(ApiResponse<RoutePlanResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] SaveRoutePlanRequest request)
        {
            var plan = await _service.UpdateAsync(id, request.ToBusinessModel(), User.ToCurrentUser());
            return Ok(ApiResponse<RoutePlanResponse>.SuccessResult(plan.ToResponse(), "Route plan updated successfully"));
        }

        /// <summary>
        /// Submit a route for the manager's approval. Blocked while a leg changes country without a BorderGate stop.
        /// </summary>
        [HttpPost("route-plans/{id:int}/submit")]
        [Authorize(Roles = PlannerRoles)]
        [ProducesResponseType(typeof(ApiResponse<RoutePlanResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Submit(int id)
        {
            var plan = await _service.SubmitAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<RoutePlanResponse>.SuccessResult(plan.ToResponse(), "Route plan submitted successfully"));
        }

        /// <summary>
        /// Approve a submitted route: it becomes the booking's route (transit countries, distance) and the booking moves to RoutePlanned.
        /// </summary>
        [HttpPost("route-plans/{id:int}/approve")]
        [Authorize(Roles = ReviewRoles)]
        [ProducesResponseType(typeof(ApiResponse<RoutePlanResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Approve(int id)
        {
            var plan = await _service.ApproveAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<RoutePlanResponse>.SuccessResult(plan.ToResponse(), "Route plan approved successfully"));
        }

        /// <summary>
        /// Reject a submitted route with a reason; the coordinator edits it and submits again.
        /// </summary>
        [HttpPost("route-plans/{id:int}/reject")]
        [Authorize(Roles = ReviewRoles)]
        [ProducesResponseType(typeof(ApiResponse<RoutePlanResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectRoutePlanRequest request)
        {
            var plan = await _service.RejectAsync(id, request.Reason, User.ToCurrentUser());
            return Ok(ApiResponse<RoutePlanResponse>.SuccessResult(plan.ToResponse(), "Route plan rejected"));
        }
    }
}
