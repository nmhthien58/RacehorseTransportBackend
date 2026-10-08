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
    // Quyền theo từng con ngựa (khách chỉ đụng được ngựa của mình) kiểm tra trong HorseService,
    // nên controller truyền User.ToCurrentUser() xuống mọi lời gọi.
    [ApiController]
    [Route("api/horses")]
    [Produces("application/json")]
    [Authorize]
    public class HorsesController : ControllerBase
    {
        private readonly IHorseService _horseService;

        public HorsesController(IHorseService horseService)
        {
            _horseService = horseService;
        }

        /// <summary>
        /// Get horses. A customer gets only their own horses; staff get all (optionally filtered by owner).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<HorseResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll([FromQuery] HorseQueryRequest queryOptions)
        {
            var pagedResult = await _horseService.GetHorsesAsync(queryOptions.ToBusinessOptions(), User.ToCurrentUser());
            var items = pagedResult.Items.Select(h => h.ToResponse()).ToList();

            return Ok(ApiCollectionResponse<HorseResponse>.SuccessResult(
                items, pagedResult.ToPagination(), "Horses retrieved successfully"));
        }

        /// <summary>
        /// Get horse by ID (owner or staff).
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<HorseResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var horse = await _horseService.GetHorseByIdAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<HorseResponse>.SuccessResult(horse.ToResponse()));
        }

        /// <summary>
        /// Register a horse owned by the logged-in customer.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<HorseResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateHorseRequest request)
        {
            var created = await _horseService.CreateHorseAsync(request.ToBusinessModel(), User.ToCurrentUser());
            var response = created.ToResponse();

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.HorseId },
                ApiResponse<HorseResponse>.SuccessResult(response, "Horse created successfully"));
        }

        /// <summary>
        /// Update a horse (owner only).
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<HorseResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateHorseRequest request)
        {
            var updated = await _horseService.UpdateHorseAsync(id, request.ToBusinessModel(), User.ToCurrentUser());
            return Ok(ApiResponse<HorseResponse>.SuccessResult(updated.ToResponse(), "Horse updated successfully"));
        }

        /// <summary>
        /// Delete a horse (owner only, soft delete). Not allowed while the horse is in an open booking.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = UserRoles.Customer)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await _horseService.DeleteHorseAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<object?>.SuccessResult(null, "Horse deleted successfully"));
        }
    }
}
