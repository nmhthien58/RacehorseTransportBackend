using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class CountryDocRequirementApiMapper
    {
        public static CountryDocRequirementQueryOptions ToBusinessOptions(
            this CountryDocRequirementQueryRequest request)
        {
            return new CountryDocRequirementQueryOptions
            {
                CountryCode = request.CountryCode?.Trim().ToUpperInvariant(),
                Direction = request.Direction?.Trim(),
                Page = request.Page,
                Size = request.Size
            };
        }

        public static CountryDocRequirementModel ToBusinessModel(
            this CreateCountryDocRequirementRequest request)
        {
            return new CountryDocRequirementModel
            {
                CountryCode = request.CountryCode.Trim().ToUpperInvariant(),
                Direction = request.Direction.Trim(),
                DocTypeId = request.DocTypeId,
                ValidityDays = request.ValidityDays,
                IsMandatory = request.IsMandatory,
                RegulationNote = request.RegulationNote?.Trim()
            };
        }

        public static CountryDocRequirementModel ToBusinessModel(
            this UpdateCountryDocRequirementRequest request,
            int id)
        {
            return new CountryDocRequirementModel
            {
                RequirementId = id,
                CountryCode = request.CountryCode.Trim().ToUpperInvariant(),
                Direction = request.Direction.Trim(),
                DocTypeId = request.DocTypeId,
                ValidityDays = request.ValidityDays,
                IsMandatory = request.IsMandatory,
                RegulationNote = request.RegulationNote?.Trim()
            };
        }

        public static CountryDocRequirementResponse ToResponse(this CountryDocRequirementModel model)
        {
            return new CountryDocRequirementResponse
            {
                RequirementId = model.RequirementId,
                CountryCode = model.CountryCode,
                Direction = model.Direction,
                DocTypeId = model.DocTypeId,
                DocTypeCode = model.DocTypeCode,
                DocTypeName = model.DocTypeName,
                ValidityDays = model.ValidityDays,
                IsMandatory = model.IsMandatory,
                RegulationNote = model.RegulationNote
            };
        }
    }
}
