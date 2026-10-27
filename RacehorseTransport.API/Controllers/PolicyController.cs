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
    // BE-17: Chính sách dịch vụ. Trang công khai đọc phiên bản + giá trị cấu hình; Manager / Admin sửa cấu hình.
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    [Authorize]
    public class PolicyController : ControllerBase
    {
        private const string ManagerRoles = UserRoles.LogisticsManager + "," + UserRoles.Admin;

        private readonly ISystemSettingService _settingService;

        public PolicyController(ISystemSettingService settingService)
        {
            _settingService = settingService;
        }

        /// <summary>
        /// Public: current service policy version and every configured value the policy mentions (no login needed).
        /// </summary>
        [HttpGet("policy")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<PublicPolicyResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPolicy()
        {
            var policy = await _settingService.GetPublicPolicyAsync();
            return Ok(ApiResponse<PublicPolicyResponse>.SuccessResult(policy.ToResponse()));
        }

        /// <summary>
        /// Policy settings for the admin screen: quote acceptance days, delay compensation, insurance, horse age, delivery, policy version.
        /// </summary>
        [HttpGet("settings/policy")]
        [Authorize(Roles = UserRoles.Staff)]
        [ProducesResponseType(typeof(ApiResponse<PolicySettingsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPolicySettings()
        {
            var settings = await _settingService.GetPolicySettingsAsync();
            return Ok(ApiResponse<PolicySettingsResponse>.SuccessResult(settings.ToResponse()));
        }

        /// <summary>
        /// Change the policy settings (send every field). New values apply from now on; bookings keep the policy version they accepted.
        /// </summary>
        [HttpPut("settings/policy")]
        [Authorize(Roles = ManagerRoles)]
        [ProducesResponseType(typeof(ApiResponse<PolicySettingsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdatePolicySettings([FromBody] UpdatePolicySettingsRequest request)
        {
            var settings = await _settingService.UpdatePolicySettingsAsync(request.ToBusinessModel(), User.ToCurrentUser());
            return Ok(ApiResponse<PolicySettingsResponse>.SuccessResult(settings.ToResponse(), "Policy settings updated successfully"));
        }
    }
}
