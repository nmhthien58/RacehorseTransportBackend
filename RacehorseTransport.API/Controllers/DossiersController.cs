using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.Helpers;
using RacehorseTransport.API.Mappers;
using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Controllers
{
    [ApiController]
    [Route("api/dossiers")]
    [Produces("application/json")]
    [Authorize]
    public class DossiersController : ControllerBase
    {
        private readonly IDossierService _dossierService;

        public DossiersController(IDossierService dossierService)
        {
            _dossierService = dossierService;
        }

        /// <summary>
        /// Get clearance dossiers. Customer: dossiers of their own horses. Specialist: dossiers they are in charge of. Manager / Coordinator / Admin: all.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<DossierSummaryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll([FromQuery] DossierQueryRequest queryOptions)
        {
            var pagedResult = await _dossierService.GetDossiersAsync(queryOptions.ToBusinessOptions(), User.ToCurrentUser());
            var items = pagedResult.Items.Select(d => d.ToResponse()).ToList();

            return Ok(ApiCollectionResponse<DossierSummaryResponse>.SuccessResult(
                items, pagedResult.ToPagination(), "Dossiers retrieved successfully"));
        }

        /// <summary>
        /// Get dossier detail: the checklist of documents required by the origin and destination countries, matched against what has been uploaded.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<DossierDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var dossier = await _dossierService.GetDossierByIdAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<DossierDetailResponse>.SuccessResult(dossier.ToResponse()));
        }
    }
}
