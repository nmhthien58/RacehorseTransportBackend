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
    // Thẩm định từng giấy tờ trong hồ sơ. Chỉ Specialist đang phụ trách hồ sơ đó (kiểm tra trong service) hoặc Admin.
    [ApiController]
    [Route("api/documents")]
    [Produces("application/json")]
    [Authorize(Roles = UserRoles.TransportSpecialist + "," + UserRoles.Admin)]
    public class DocumentsController : ControllerBase
    {
        private readonly IDossierService _dossierService;

        public DocumentsController(IDossierService dossierService)
        {
            _dossierService = dossierService;
        }

        /// <summary>
        /// Approve a pending document. Not allowed if the document expires before the departure date.
        /// </summary>
        [HttpPost("{id:int}/approve")]
        [ProducesResponseType(typeof(ApiResponse<DossierDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Approve(int id)
        {
            var dossier = await _dossierService.ApproveDocumentAsync(id, User.ToCurrentUser());
            return Ok(ApiResponse<DossierDetailResponse>.SuccessResult(dossier.ToResponse(), "Document approved successfully"));
        }

        /// <summary>
        /// Reject a pending document with a correction note. The third rejection flags the dossier with an issue.
        /// </summary>
        [HttpPost("{id:int}/reject")]
        [ProducesResponseType(typeof(ApiResponse<DossierDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectDocumentRequest request)
        {
            var dossier = await _dossierService.RejectDocumentAsync(id, request.CorrectionNote, User.ToCurrentUser());
            return Ok(ApiResponse<DossierDetailResponse>.SuccessResult(dossier.ToResponse(), "Document rejected"));
        }
    }
}