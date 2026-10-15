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
    [Route("api/country-doc-requirements")]
    [Produces("application/json")]
    [Authorize]
    public class CountryDocRequirementsController : ControllerBase
    {
        private const string ManageRoles = UserRoles.TransportSpecialist + "," + UserRoles.Admin;
        private readonly ICountryDocRequirementService _requirementService;

        public CountryDocRequirementsController(ICountryDocRequirementService requirementService)
        {
            _requirementService = requirementService;
        }

        /// <summary>
        /// Lấy danh sách quy định giấy tờ theo quốc gia, có lọc theo quốc gia/hướng và phân trang.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<CountryDocRequirementResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll([FromQuery] CountryDocRequirementQueryRequest queryOptions)
        {
            var pagedResult = await _requirementService.GetCountryDocRequirementsAsync(
                queryOptions.ToBusinessOptions());
            var items = pagedResult.Items.Select(r => r.ToResponse()).ToList();

            return Ok(ApiCollectionResponse<CountryDocRequirementResponse>.SuccessResult(
                items,
                pagedResult.ToPagination(),
                "Country document requirements retrieved successfully"));
        }

        /// <summary>
        /// Lấy chi tiết một quy định giấy tờ theo mã định danh.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<CountryDocRequirementResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _requirementService.GetCountryDocRequirementByIdAsync(id);
            return Ok(ApiResponse<CountryDocRequirementResponse>.SuccessResult(model.ToResponse()));
        }

        /// <summary>
        /// Tạo một quy định giấy tờ theo quốc gia.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = ManageRoles)]
        [ProducesResponseType(typeof(ApiResponse<CountryDocRequirementResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] CreateCountryDocRequirementRequest request)
        {
            var created = await _requirementService.CreateCountryDocRequirementAsync(request.ToBusinessModel());
            var response = created.ToResponse();

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.RequirementId },
                ApiResponse<CountryDocRequirementResponse>.SuccessResult(
                    response,
                    "Country document requirement created successfully"));
        }

        /// <summary>
        /// Cập nhật một quy định giấy tờ theo quốc gia.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = ManageRoles)]
        [ProducesResponseType(typeof(ApiResponse<CountryDocRequirementResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCountryDocRequirementRequest request)
        {
            var updated = await _requirementService.UpdateCountryDocRequirementAsync(
                id,
                request.ToBusinessModel(id));

            return Ok(ApiResponse<CountryDocRequirementResponse>.SuccessResult(
                updated.ToResponse(),
                "Country document requirement updated successfully"));
        }

        /// <summary>
        /// Xóa một quy định giấy tờ theo quốc gia.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = ManageRoles)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await _requirementService.DeleteCountryDocRequirementAsync(id);
            return Ok(ApiResponse<object?>.SuccessResult(
                null,
                "Country document requirement deleted successfully"));
        }
    }
}
