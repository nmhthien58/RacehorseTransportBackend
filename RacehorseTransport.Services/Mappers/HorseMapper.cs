using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Mappers
{
    public static class HorseMapper
    {
        public static HorseModel ToBusinessModel(this Horse entity)
        {
            return new HorseModel
            {
                HorseId = entity.HorseId,
                OwnerUserId = entity.OwnerUserId,
                OwnerName = entity.Owner?.FullName,
                Name = entity.Name,
                MicrochipNumber = entity.MicrochipNumber,
                PassportNumber = entity.PassportNumber,
                Breed = entity.Breed,
                Gender = entity.Gender,
                DateOfBirth = entity.DateOfBirth,
                Color = entity.Color,
                SpecialCareRequirements = entity.SpecialCareRequirements,
                PhotoUrl = entity.PhotoUrl,
                IsPregnant = entity.IsPregnant,
                ExpectedFoalingDate = entity.ExpectedFoalingDate,
                LastFoalingDate = entity.LastFoalingDate,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
