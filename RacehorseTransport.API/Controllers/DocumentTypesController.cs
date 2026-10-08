using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.Mappers;
using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Controllers
{
    // CONTROLLER MẪU cho CRUD danh mục. Khi làm bảng mới, copy file này rồi đổi tên/trường.
    // Không cần try/catch và không cần kiểm tra ModelState: middleware và [ApiController] đã lo.
    [ApiController]
    [Route("api/document-types")]
    [Produces("application/json")]
    [Authorize] // mọi endpoint đều cần đăng nhập; endpoint ghi siết thêm theo Role bên dưới
    public class DocumentTypesController : ControllerBase
    {
        private const string ManageRoles = UserRoles.TransportSpecialist + "," + UserRoles.Admin;

        private readonly IDocumentTypeService _documentTypeService;

        public DocumentTypesController(IDocumentTypeService documentTypeService)
        {
            _documentTypeService = documentTypeService;
        }

        /// <summary>
        /// Get document types (search, sort, paging).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiCollectionResponse<DocumentTypeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll([FromQuery] QueryOptionsRequest queryOptions)
        {
            var pagedResult = await _documentTypeService.GetDocumentTypesAsync(queryOptions.ToBusinessOptions());
            var items = pagedResult.Items.Select(d => d.ToResponse()).ToList();

            return Ok(ApiCollectionResponse<DocumentTypeResponse>.SuccessResult(
                items, pagedResult.ToPagination(), "Document types retrieved successfully"));
        }

        /// <summary>
        /// Get all document types without paging (for dropdowns).
        /// </summary>
        [HttpGet("lookup")]
        [ProducesResponseType(typeof(ApiResponse<List<DocumentTypeResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLookup()
        {
            var models = await _documentTypeService.GetLookupAsync();
            var items = models.Select(d => d.ToResponse()).ToList();

            return Ok(ApiResponse<List<DocumentTypeResponse>>.SuccessResult(items));
        }

        /// <summary>
        /// Get document type by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<DocumentTypeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _documentTypeService.GetDocumentTypeByIdAsync(id);
            return Ok(ApiResponse<DocumentTypeResponse>.SuccessResult(model.ToResponse()));
        }

        /// <summary>
        /// Create document type.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = ManageRoles)]
        [ProducesResponseType(typeof(ApiResponse<DocumentTypeResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateDocumentTypeRequest request)
        {
            var created = await _documentTypeService.CreateDocumentTypeAsync(request.ToBusinessModel());
            var response = created.ToResponse();

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.DocTypeId },
                ApiResponse<DocumentTypeResponse>.SuccessResult(response, "Document type created successfully"));
        }

        /// <summary>
        /// Update document type.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = ManageRoles)]
        [ProducesResponseType(typeof(ApiResponse<DocumentTypeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDocumentTypeRequest request)
        {
            var updated = await _documentTypeService.UpdateDocumentTypeAsync(id, request.ToBusinessModel(id));
            return Ok(ApiResponse<DocumentTypeResponse>.SuccessResult(updated.ToResponse(), "Document type updated successfully"));
        }

        /// <summary>
        /// Delete document type.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = ManageRoles)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await _documentTypeService.DeleteDocumentTypeAsync(id);
            return Ok(ApiResponse<object?>.SuccessResult(null, "Document type deleted successfully"));
        }
    }
}
