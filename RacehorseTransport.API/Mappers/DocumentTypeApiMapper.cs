using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    // Request -> BusinessModel -> Response. Chuẩn hóa dữ liệu nhập (Trim, viết hoa mã) làm ở đây.
    public static class DocumentTypeApiMapper
    {
        public static DocumentTypeModel ToBusinessModel(this CreateDocumentTypeRequest request)
        {
            return new DocumentTypeModel
            {
                Code = request.Code.Trim().ToUpperInvariant(),
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                IsMandatory = request.IsMandatory,
                ProvidedBy = request.ProvidedBy
            };
        }

        public static DocumentTypeModel ToBusinessModel(this UpdateDocumentTypeRequest request, int id)
        {
            return new DocumentTypeModel
            {
                DocTypeId = id,
                Code = request.Code.Trim().ToUpperInvariant(),
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                IsMandatory = request.IsMandatory,
                ProvidedBy = request.ProvidedBy
            };
        }

        public static DocumentTypeResponse ToResponse(this DocumentTypeModel model)
        {
            return new DocumentTypeResponse
            {
                DocTypeId = model.DocTypeId,
                Code = model.Code,
                Name = model.Name,
                Description = model.Description,
                IsMandatory = model.IsMandatory,
                ProvidedBy = model.ProvidedBy
            };
        }
    }
}
