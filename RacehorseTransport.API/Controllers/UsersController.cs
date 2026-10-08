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
    [Route("api/users")]
    [Produces("application/json")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        // Đặt role trên từng action: nhiều [Authorize] chồng nhau sẽ phải thỏa TẤT CẢ, nên không đặt Admin ở cấp class
        // (endpoint /staff cần mở cho cả Manager, Specialist, Coordinator).
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Get accounts (search, filter by role / active state, sort, paging). Admin only.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = UserRoles.Admin)]
        [ProducesResponseType(typeof(ApiCollectionResponse<UserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll([FromQuery] UserQueryRequest queryOptions)
        {
            var pagedResult = await _userService.GetUsersAsync(queryOptions.ToBusinessOptions());
            var items = pagedResult.Items.Select(u => u.ToResponse()).ToList();

            return Ok(ApiCollectionResponse<UserResponse>.SuccessResult(
                items, pagedResult.ToPagination(), "Users retrieved successfully"));
        }

        /// <summary>
        /// Get active staff, optionally by role (for "assign to" dropdowns).
        /// </summary>
        [HttpGet("staff")]
        [Authorize(Roles = UserRoles.Operations)]
        [ProducesResponseType(typeof(ApiResponse<List<UserResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetStaff([FromQuery] string? role = null)
        {
            var staff = await _userService.GetActiveStaffAsync(role);
            var items = staff.Select(u => u.ToResponse()).ToList();

            return Ok(ApiResponse<List<UserResponse>>.SuccessResult(items));
        }

        /// <summary>
        /// Get account by ID. Admin only.
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = UserRoles.Admin)]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            return Ok(ApiResponse<UserResponse>.SuccessResult(user.ToResponse()));
        }

        /// <summary>
        /// Create a staff account (any role except Customer). Admin only.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = UserRoles.Admin)]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateStaffRequest request)
        {
            var created = await _userService.CreateStaffAsync(request.ToBusinessModel());
            var response = created.ToResponse();

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.UserId },
                ApiResponse<UserResponse>.SuccessResult(response, "Staff account created successfully"));
        }

        /// <summary>
        /// Update name, phone number and role of an account. Admin only.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = UserRoles.Admin)]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
        {
            var updated = await _userService.UpdateUserAsync(id, request.ToBusinessModel(), User.GetUserId());
            return Ok(ApiResponse<UserResponse>.SuccessResult(updated.ToResponse(), "User updated successfully"));
        }

        /// <summary>
        /// Activate or deactivate an account. Admin only.
        /// </summary>
        [HttpPatch("{id:int}/active")]
        [Authorize(Roles = UserRoles.Admin)]
        [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetActive(int id, [FromBody] SetUserActiveRequest request)
        {
            var updated = await _userService.SetActiveAsync(id, request.IsActive!.Value, User.GetUserId());
            string message = updated.IsActive ? "User activated successfully" : "User deactivated successfully";

            return Ok(ApiResponse<UserResponse>.SuccessResult(updated.ToResponse(), message));
        }

        /// <summary>
        /// Set a new password for an account. Admin only.
        /// </summary>
        [HttpPut("{id:int}/password")]
        [Authorize(Roles = UserRoles.Admin)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordByAdminRequest request)
        {
            await _userService.ResetPasswordAsync(id, request.NewPassword);
            return Ok(ApiResponse<object?>.SuccessResult(null, "Password reset successfully"));
        }
    }
}
