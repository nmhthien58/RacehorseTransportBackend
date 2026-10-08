using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class HorseApiMapper
    {
        public static HorseFilterOptions ToBusinessOptions(this HorseQueryRequest request)
        {
            return new HorseFilterOptions
            {
                Search = request.Search,
                Sort = request.Sort,
                Page = request.Page,
                Size = request.Size,
                OwnerUserId = request.OwnerUserId,
                IncludeInactive = request.IncludeInactive
            };
        }

        // Dùng cho cả tạo mới và cập nhật (UpdateHorseRequest kế thừa CreateHorseRequest).
        // Mã microchip và số hộ chiếu lưu viết hoa để so trùng không lệ thuộc cách gõ.
        public static HorseModel ToBusinessModel(this CreateHorseRequest request)
        {
            return new HorseModel
            {
                Name = request.Name.Trim(),
                MicrochipNumber = request.MicrochipNumber.Trim().ToUpperInvariant(),
                PassportNumber = request.PassportNumber.Trim().ToUpperInvariant(),
                Breed = string.IsNullOrWhiteSpace(request.Breed) ? "Thoroughbred" : request.Breed.Trim(),
                Gender = request.Gender.Trim(),
                DateOfBirth = request.DateOfBirth!.Value,
                Color = request.Color.Trim(),
                SpecialCareRequirements = string.IsNullOrWhiteSpace(request.SpecialCareRequirements) ? null : request.SpecialCareRequirements.Trim(),
                PhotoUrl = string.IsNullOrWhiteSpace(request.PhotoUrl) ? null : request.PhotoUrl.Trim()
            };
        }

        public static HorseResponse ToResponse(this HorseModel model)
        {
            return new HorseResponse
            {
                HorseId = model.HorseId,
                OwnerUserId = model.OwnerUserId,
                OwnerName = model.OwnerName,
                Name = model.Name,
                MicrochipNumber = model.MicrochipNumber,
                PassportNumber = model.PassportNumber,
                Breed = model.Breed,
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth,
                Color = model.Color,
                SpecialCareRequirements = model.SpecialCareRequirements,
                PhotoUrl = model.PhotoUrl,
                IsActive = model.IsActive,
                CreatedAt = model.CreatedAt
            };
        }
    }
}
