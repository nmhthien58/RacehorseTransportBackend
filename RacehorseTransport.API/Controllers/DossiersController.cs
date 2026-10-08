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
    [Route("api/dossiers")]
    [Produces("application/json")]
    [Authorize]
    public class DossiersController : ControllerBase
    {
        // Chỉ Specialist đang phụ trách hồ sơ mới thao tác được (kiểm tra trong service); Admin thao tác được mọi hồ sơ
        private const string SpecialistRoles = UserRoles.TransportSpecialist + "," + UserRoles.Admin;

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

        /// <summary>
        /// Ask the customer to upload the missing documents. The dossier becomes AwaitingDocs.
        /// </summary>
        [HttpPost("{id:int}/request-documents")]
        [Authorize(Roles = SpecialistRoles)]
        [ProducesResponseType(typeof(ApiResponse<DossierDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RequestDocuments(int id, [FromBody] RequestDocumentsRequest? request)
        {
            var dossier = await _dossierService.RequestDocumentsAsync(id, request?.Note, User.ToCurrentUser());
            return Ok(ApiResponse<DossierDetailResponse>.SuccessResult(dossier.ToResponse(), "Documents requested successfully"));
        }

        /// <summary>
        /// Upload (or re-upload after a rejection) one document. Upload the file with POST /api/files first and send its path.
        /// </summary>
        [HttpPost("{id:int}/documents")]
        [Authorize(Roles = UserRoles.Customer + "," + SpecialistRoles)]
        [ProducesResponseType(typeof(ApiResponse<DossierDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadDocument(int id, [FromBody] UploadDocumentRequest request)
        {
            var dossier = await _dossierService.UploadDocumentAsync(id, request.ToBusinessModel(), User.ToCurrentUser());
            return Ok(ApiResponse<DossierDetailResponse>.SuccessResult(dossier.ToResponse(), "Document uploaded successfully"));
        }

        /// <summary>
        /// Submit the dossier to the authorities. Every mandatory document must be approved.
        /// </summary>
        [HttpPost("{id:int}/submit-to-authorities")]
        [Authorize(Roles = SpecialistRoles)]
        [ProducesResponseType(typeof(ApiResponse<DossierDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SubmitToAuthorities(int id)
        {
            var dossier = await _dossierService.SubmitToAuthoritiesAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<DossierDetailResponse>.SuccessResult(dossier.ToResponse(), "Dossier submitted to the authorities"));
        }

        /// <summary>
        /// Record that the authorities cleared the horse for travel.
        /// </summary>
        [HttpPost("{id:int}/clear")]
        [Authorize(Roles = SpecialistRoles)]
        [ProducesResponseType(typeof(ApiResponse<DossierDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Clear(int id, [FromBody] ClearDossierRequest request)
        {
            var dossier = await _dossierService.ClearDossierAsync(id, request.ClearanceNumber, User.ToCurrentUser());
            return Ok(ApiResponse<DossierDetailResponse>.SuccessResult(dossier.ToResponse(), "Dossier cleared successfully"));
        }

        /// <summary>
        /// Flag a problem with the dossier (for example the authorities refused it).
        /// </summary>
        [HttpPost("{id:int}/issue")]
        [Authorize(Roles = SpecialistRoles)]
        [ProducesResponseType(typeof(ApiResponse<DossierDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkIssue(int id, [FromBody] DossierIssueRequest request)
        {
            var dossier = await _dossierService.MarkIssueAsync(id, request.Reason, User.ToCurrentUser());
            return Ok(ApiResponse<DossierDetailResponse>.SuccessResult(dossier.ToResponse(), "Issue recorded successfully"));
        }
    }
}