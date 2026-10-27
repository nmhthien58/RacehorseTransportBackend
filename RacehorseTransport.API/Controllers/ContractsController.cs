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
    // R-17: hợp đồng vận chuyển PDF của đơn + vận đơn hàng không của chuyến
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    [Authorize]
    public class ContractsController : ControllerBase
    {
        private const string AirWaybillEditors = UserRoles.FleetCoordinator + "," + UserRoles.Admin;

        private readonly IContractService _contractService;
        private readonly IAirWaybillService _airWaybillService;

        public ContractsController(IContractService contractService, IAirWaybillService airWaybillService)
        {
            _contractService = contractService;
            _airWaybillService = airWaybillService;
        }

        /// <summary>
        /// Current contract of a booking (the draft waiting for acceptance, otherwise the accepted one) with its SHA-256 and a temporary download link.
        /// </summary>
        [HttpGet("bookings/{id:int}/contract")]
        [ProducesResponseType(typeof(ApiResponse<ContractResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrent(int id)
        {
            var contract = await _contractService.GetCurrentAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<ContractResponse>.SuccessResult(contract.ToResponse(), "Contract retrieved successfully"));
        }

        /// <summary>
        /// All contract versions of a booking, newest first.
        /// </summary>
        [HttpGet("bookings/{id:int}/contracts")]
        [ProducesResponseType(typeof(ApiResponse<List<ContractResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetHistory(int id)
        {
            var contracts = await _contractService.GetHistoryAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<List<ContractResponse>>.SuccessResult(contracts.Select(c => c.ToResponse()).ToList(), "Contracts retrieved successfully"));
        }

        /// <summary>
        /// Download the PDF of one contract version: redirects (302) to a temporary storage link.
        /// </summary>
        [HttpGet("contracts/{contractId:int}/file")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFile(int contractId)
        {
            var url = await _contractService.GetFileUrlAsync(contractId, User.ToCurrentUser());
            return Redirect(url.Url);
        }

        /// <summary>
        /// Customer accepts an amended contract version (the first version is accepted with POST /api/bookings/{id}/accept-quote).
        /// </summary>
        [HttpPost("contracts/{contractId:int}/accept")]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<ContractResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Accept(int contractId, [FromBody] AcceptContractRequest request)
        {
            var contract = await _contractService.AcceptAmendmentAsync(new ContractAcceptanceModel
            {
                ContractId = contractId,
                ContractSha256 = request.ContractSha256,
                AcceptPolicy = request.AcceptPolicy,
                PolicyVersion = request.PolicyVersion,
                IpAddress = HttpContext.GetClientIp(),
                UserAgent = HttpContext.GetUserAgent()
            }, User.ToCurrentUser());
            return Ok(ApiResponse<ContractResponse>.SuccessResult(contract.ToResponse(), "Contract accepted"));
        }

        /// <summary>
        /// Air waybill of a trip (staff, or a customer with horses on the trip).
        /// </summary>
        [HttpGet("trips/{id:int}/air-waybill")]
        [Authorize(Roles = UserRoles.Customer + "," + UserRoles.LogisticsManager + "," + UserRoles.FleetCoordinator + "," + UserRoles.TransportSpecialist + "," + UserRoles.Admin)]
        [ProducesResponseType(typeof(ApiResponse<AirWaybillResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAirWaybill(int id)
        {
            var model = await _airWaybillService.GetAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<AirWaybillResponse>.SuccessResult(model.ToResponse(), "Air waybill retrieved successfully"));
        }

        /// <summary>
        /// Coordinator records the air waybill number (and the uploaded scan) of a trip carrying Air bookings.
        /// </summary>
        [HttpPut("trips/{id:int}/air-waybill")]
        [Authorize(Roles = AirWaybillEditors)]
        [ProducesResponseType(typeof(ApiResponse<AirWaybillResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAirWaybill(int id, [FromBody] UpdateAirWaybillRequest request)
        {
            var model = await _airWaybillService.UpdateAsync(id, request.AirWaybillNumber, request.AirWaybillFilePath);
            return Ok(ApiResponse<AirWaybillResponse>.SuccessResult(model.ToResponse(), "Air waybill updated successfully"));
        }
    }
}
