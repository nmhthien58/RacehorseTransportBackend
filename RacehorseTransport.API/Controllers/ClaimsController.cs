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
    // R-13 (BE-12): hồ sơ bồi thường bảo hiểm
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    [Authorize]
    public class ClaimsController : ControllerBase
    {
        private const string ManagerRoles = UserRoles.LogisticsManager + "," + UserRoles.Admin;
        private readonly IClaimService _claimService;

        public ClaimsController(IClaimService claimService)
        {
            _claimService = claimService;
        }

        /// <summary>
        /// Claims: customers see their own, staff see all. Filter by status or booking.
        /// </summary>
        [HttpGet("claims")]
        [ProducesResponseType(typeof(ApiResponse<List<ClaimModel>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] int? bookingId)
        {
            var list = await _claimService.ListAsync(status, bookingId, User.ToCurrentUser());
            return Ok(ApiResponse<List<ClaimModel>>.SuccessResult(list, "Claims retrieved successfully"));
        }

        /// <summary>
        /// Claim detail with temporary links to the evidence files.
        /// </summary>
        [HttpGet("claims/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<ClaimModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int id)
        {
            var claim = await _claimService.GetAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<ClaimModel>.SuccessResult(claim, "Claim retrieved successfully"));
        }

        /// <summary>
        /// Customer opens a claim within 24 hours after receiving the horse.
        /// </summary>
        [HttpPost("bookings/{id:int}/claims")]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<ClaimModel>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Open(int id, [FromBody] OpenClaimRequest request)
        {
            var claim = await _claimService.OpenByCustomerAsync(id, new OpenClaimModel
            {
                BookingHorseId = request.BookingHorseId,
                ClaimType = request.ClaimType.Trim(),
                Description = request.Description,
                RequestedAmount = request.RequestedAmount,
                EvidencePaths = request.EvidencePaths
            }, User.ToCurrentUser());
            return StatusCode(StatusCodes.Status201Created, ApiResponse<ClaimModel>.SuccessResult(claim, "Claim opened"));
        }

        /// <summary>
        /// Add evidence files (vet report, photos, vet invoices) to a claim.
        /// </summary>
        [HttpPost("claims/{id:int}/evidence")]
        [ProducesResponseType(typeof(ApiResponse<ClaimModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddEvidence(int id, [FromBody] ClaimEvidenceRequest request)
        {
            var claim = await _claimService.AddEvidenceAsync(id, request.EvidencePaths, User.ToCurrentUser());
            return Ok(ApiResponse<ClaimModel>.SuccessResult(claim, "Evidence added"));
        }

        /// <summary>
        /// Manager takes an open claim into review.
        /// </summary>
        [HttpPost("claims/{id:int}/review")]
        [Authorize(Roles = ManagerRoles)]
        [ProducesResponseType(typeof(ApiResponse<ClaimModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Review(int id)
        {
            var claim = await _claimService.StartReviewAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<ClaimModel>.SuccessResult(claim, "Claim under review"));
        }

        /// <summary>
        /// Manager approves an amount (must not exceed the cap of the horse's plan).
        /// </summary>
        [HttpPost("claims/{id:int}/approve")]
        [Authorize(Roles = ManagerRoles)]
        [ProducesResponseType(typeof(ApiResponse<ClaimModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveClaimRequest request)
        {
            var claim = await _claimService.ApproveAsync(id, request.ApprovedAmount, request.FaultParty?.Trim(), request.Reason, User.ToCurrentUser());
            return Ok(ApiResponse<ClaimModel>.SuccessResult(claim, "Claim approved"));
        }

        /// <summary>
        /// Manager rejects a claim with a reason (exclusions in section 8.4).
        /// </summary>
        [HttpPost("claims/{id:int}/reject")]
        [Authorize(Roles = ManagerRoles)]
        [ProducesResponseType(typeof(ApiResponse<ClaimModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectClaimRequest request)
        {
            var claim = await _claimService.RejectAsync(id, request.Reason, User.ToCurrentUser());
            return Ok(ApiResponse<ClaimModel>.SuccessResult(claim, "Claim rejected"));
        }

        /// <summary>
        /// Pay an approved claim: VNPay refund up to what the customer paid, the rest by bank transfer with a reference.
        /// </summary>
        [HttpPost("claims/{id:int}/pay")]
        [Authorize(Roles = ManagerRoles)]
        [ProducesResponseType(typeof(ApiResponse<ClaimModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Pay(int id, [FromBody] PayClaimRequest request)
        {
            var claim = await _claimService.PayAsync(id, request.ManualReference, User.ToCurrentUser());
            return Ok(ApiResponse<ClaimModel>.SuccessResult(claim, "Claim paid"));
        }
    }
}
