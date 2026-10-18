using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Mappers
{
    public static class CountryDocRequirementMapper
    {
        public static CountryDocRequirementModel ToBusinessModel(
            this CountryDocRequirement entity,
            DocumentType? documentType = null)
        {
            var relatedDocumentType = documentType ?? entity.DocumentType;

            return new CountryDocRequirementModel
            {
                RequirementId = entity.RequirementId,
                CountryCode = entity.CountryCode,
                Direction = entity.Direction,
                DocTypeId = entity.DocTypeId,
                DocTypeCode = relatedDocumentType?.Code ?? string.Empty,
                DocTypeName = relatedDocumentType?.Name ?? string.Empty,
                ValidityDays = entity.ValidityDays,
                ProcessingDays = entity.ProcessingDays,
                IsMandatory = entity.IsMandatory,
                RegulationNote = entity.RegulationNote
            };
        }

        public static CountryDocRequirement ToEntity(this CountryDocRequirementModel model)
        {
            return new CountryDocRequirement
            {
                RequirementId = model.RequirementId,
                CountryCode = model.CountryCode,
                Direction = model.Direction,
                DocTypeId = model.DocTypeId,
                ValidityDays = model.ValidityDays,
                ProcessingDays = model.ProcessingDays,
                IsMandatory = model.IsMandatory,
                RegulationNote = model.RegulationNote
            };
        }
    }
}
