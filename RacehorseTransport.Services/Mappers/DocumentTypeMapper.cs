using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Mappers
{
    // Entity <-> BusinessModel. Mỗi chức năng một file mapper riêng để hai người không sửa chung một file.
    public static class DocumentTypeMapper
    {
        public static DocumentTypeModel ToBusinessModel(this DocumentType entity)
        {
            return new DocumentTypeModel
            {
                DocTypeId = entity.DocTypeId,
                Code = entity.Code,
                Name = entity.Name,
                Description = entity.Description,
                IsMandatory = entity.IsMandatory
            };
        }

        public static DocumentType ToEntity(this DocumentTypeModel model)
        {
            return new DocumentType
            {
                DocTypeId = model.DocTypeId,
                Code = model.Code,
                Name = model.Name,
                Description = model.Description,
                IsMandatory = model.IsMandatory
            };
        }
    }
}
