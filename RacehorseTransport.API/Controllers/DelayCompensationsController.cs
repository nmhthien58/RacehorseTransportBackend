using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.Helpers;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.API.Mappers;
using RacehorseTransport.API.RequestModels;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Controllers
{
    // R-09: bồi thường giao trễ. Hệ thống tạo khi đóng chuyến trễ do lỗi công ty; Manager duyệt / sửa số tiền / từ chối.
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    [Authorize]
    public class DelayCompensationsController : ControllerBase
    {
        private const string ManagerRoles = UserRoles.LogisticsManager + "," + UserRoles.Admin;

        private readonly IDelayCompensationService _service;

        public DelayCompensationsController(IDelayCompensationService service)
        {
            _service = service;
        }

        /// <summary>
        /// Delay compensations calculated when trips were closed. Filter by status: AwaitingApproval, Approved, Rejected.
        /// </summary>
        [HttpGet("delay-compensations")]
        [Authorize(Roles = ManagerRoles)]
        [ProducesResponseType(typeof(ApiResponse<List<DelayCompensationResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            var rows = await _service.GetAllAsync(status, User.ToCurrentUser());
            return Ok(ApiResponse<List<DelayCompensationResponse>>.SuccessResult(rows.Select(r => r.ToResponse()).ToList()));
        }

        /// <summary>
        /// Delay compensations of one booking (the customer of the booking or staff).
        /// </summary>
        [HttpGet("bookings/{bookingId:int}/delay-compensations")]
        [ProducesResponseType(typeof(ApiResponse<List<DelayCompensationResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetForBooking(int bookingId)
        {
            var rows = await _service.GetForBookingAsync(bookingId, User.ToCurrentUser());
            return Ok(ApiResponse<List<DelayCompensationResponse>>.SuccessResult(rows.Select(r => r.ToResponse()).ToList()));
        }

        /// <summary>
        /// Approve a delay compensation (optionally with another amount and a reason). The amount is refunded to the customer's payment method.
        /// </summary>
        [HttpPost("delay-compensations/{id:int}/approve")]
        [Authorize(Roles = ManagerRoles)]
        [ProducesResponseType(typeof(ApiResponse<DelayCompensationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveDelayCompensationRequest request)
        {
            var row = await _service.ApproveAsync(id, request.Amount!.Value, request.Reason, User.ToCurrentUser());
            return Ok(ApiResponse<DelayCompensationResponse>.SuccessResult(row.ToResponse(), "Delay compensation approved"));
        }

        /// <summary>
        /// Reject a delay compensation with a reason.
        /// </summary>
        [HttpPost("delay-compensations/{id:int}/reject")]
        [Authorize(Roles = ManagerRoles)]
        [ProducesResponseType(typeof(ApiResponse<DelayCompensationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectDelayCompensationRequest request)
        {
            var row = await _service.RejectAsync(id, request.Reason, User.ToCurrentUser());
            return Ok(ApiResponse<DelayCompensationResponse>.SuccessResult(row.ToResponse(), "Delay compensation rejected"));
        }
    }
}
