using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.Mappers;
using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Controllers
{
    [ApiController]
    [Route("api/transport-assets")]
    [Produces("application/json")]
    [Authorize(Roles = UserRoles.Staff)]
    public class TransportAssetsController : ControllerBase
    {
        private const string ManageRoles = UserRoles.FleetCoordinator + "," + UserRoles.Admin;
        private readonly ITransportAssetService _transportAssetService;

        public TransportAssetsController(ITransportAssetService transportAssetService)
        {
            _transportAssetService = transportAssetService;
        }

        /// <summary>
        /// Lấy danh sách phương tiện, có hỗ trợ tìm kiếm, lọc trạng thái/loại, sắp xếp và phân trang.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<TransportAssetResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll([FromQuery] TransportAssetQueryRequest queryOptions)
        {
            var pagedResult = await _transportAssetService.GetTransportAssetsAsync(queryOptions.ToBusinessOptions());
            var items = pagedResult.Items.Select(a => a.ToResponse()).ToList();

            return Ok(ApiCollectionResponse<TransportAssetResponse>.SuccessResult(
                items,
                pagedResult.ToPagination(),
                "Transport assets retrieved successfully"));
        }

        /// <summary>
        /// Lấy chi tiết một phương tiện theo mã định danh.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<TransportAssetResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _transportAssetService.GetTransportAssetByIdAsync(id);
            return Ok(ApiResponse<TransportAssetResponse>.SuccessResult(model.ToResponse()));
        }

        /// <summary>
        /// Tạo một phương tiện mới.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = ManageRoles)]
        [ProducesResponseType(typeof(ApiResponse<TransportAssetResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] CreateTransportAssetRequest request)
        {
            var created = await _transportAssetService.CreateTransportAssetAsync(request.ToBusinessModel());
            var response = created.ToResponse();

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.AssetId },
                ApiResponse<TransportAssetResponse>.SuccessResult(response, "Transport asset created successfully"));
        }

        /// <summary>
        /// Cập nhật một phương tiện.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = ManageRoles)]
        [ProducesResponseType(typeof(ApiResponse<TransportAssetResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTransportAssetRequest request)
        {
            var updated = await _transportAssetService.UpdateTransportAssetAsync(id, request.ToBusinessModel(id));
            return Ok(ApiResponse<TransportAssetResponse>.SuccessResult(
                updated.ToResponse(),
                "Transport asset updated successfully"));
        }

        /// <summary>
        /// Xóa một phương tiện chưa từng được sử dụng cho chuyến đi.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = ManageRoles)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await _transportAssetService.DeleteTransportAssetAsync(id);
            return Ok(ApiResponse<object?>.SuccessResult(null, "Transport asset deleted successfully"));
        }
    }
}
