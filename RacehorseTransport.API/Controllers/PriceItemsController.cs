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
    [Route("api/price-items")]
    [Produces("application/json")]
    [Authorize(Roles = ManageRoles)]
    public class PriceItemsController : ControllerBase
    {
        private const string ManageRoles = UserRoles.LogisticsManager + "," + UserRoles.Admin;

        private readonly IPriceItemService _priceItemService;

        public PriceItemsController(IPriceItemService priceItemService)
        {
            _priceItemService = priceItemService;
        }

        /// <summary>
        /// Lấy danh sách giá, có hỗ trợ lọc, tìm kiếm, sắp xếp và phân trang.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<PriceItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll([FromQuery] PriceItemQueryRequest request)
        {
            var pagedResult = await _priceItemService.GetPriceItemsAsync(request.ToBusinessOptions());
            var items = pagedResult.Items.Select(p => p.ToResponse()).ToList();

            return Ok(ApiCollectionResponse<PriceItemResponse>.SuccessResult(
                items, pagedResult.ToPagination(), "Price items retrieved successfully"));
        }

        /// <summary>
        /// Lấy chi tiết một dòng giá theo mã định danh.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<PriceItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _priceItemService.GetPriceItemByIdAsync(id);
            return Ok(ApiResponse<PriceItemResponse>.SuccessResult(model.ToResponse()));
        }

        /// <summary>
        /// Tạo một dòng giá mới.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<PriceItemResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] CreatePriceItemRequest request)
        {
            var created = await _priceItemService.CreatePriceItemAsync(request.ToBusinessModel());
            var response = created.ToResponse();

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.PriceItemId },
                ApiResponse<PriceItemResponse>.SuccessResult(response, "Price item created successfully"));
        }

        /// <summary>
        /// Cập nhật một dòng giá.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<PriceItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePriceItemRequest request)
        {
            var updated = await _priceItemService.UpdatePriceItemAsync(id, request.ToBusinessModel(id));
            return Ok(ApiResponse<PriceItemResponse>.SuccessResult(updated.ToResponse(), "Price item updated successfully"));
        }

        /// <summary>
        /// Bật hoặc ngừng sử dụng một dòng giá.
        /// </summary>
        [HttpPatch("{id:int}/active")]
        [ProducesResponseType(typeof(ApiResponse<PriceItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SetActive(int id, [FromBody] SetPriceItemActiveRequest request)
        {
            var updated = await _priceItemService.SetActiveAsync(id, request.IsActive!.Value);
            return Ok(ApiResponse<PriceItemResponse>.SuccessResult(
                updated.ToResponse(), "Price item active status updated successfully"));
        }
    }
}
