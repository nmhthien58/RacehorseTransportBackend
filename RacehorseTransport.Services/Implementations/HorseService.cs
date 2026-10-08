using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Extensions;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Mappers;

namespace RacehorseTransport.Services.Implementations
{
    public class HorseService : IHorseService
    {
        // Đơn ở các trạng thái này còn đang chạy: ngựa trong đơn chưa được xóa
        private static readonly string[] OpenBookingStatuses =
        {
            BookingStatuses.Submitted, BookingStatuses.Approved, BookingStatuses.Assigned
        };

        private readonly IRepository<Horse> _horseRepo;
        private readonly IRepository<BookingHorse> _bookingHorseRepo;

        public HorseService(IRepository<Horse> horseRepo, IRepository<BookingHorse> bookingHorseRepo)
        {
            _horseRepo = horseRepo;
            _bookingHorseRepo = bookingHorseRepo;
        }

        public async Task<PagedBusinessResult<HorseModel>> GetHorsesAsync(HorseFilterOptions options, CurrentUser currentUser)
        {
            var query = _horseRepo.GetQueryable().AsNoTracking().Include(h => h.Owner).AsQueryable();

            // Khách hàng chỉ thấy ngựa của chính mình, bất kể tham số gửi lên
            if (currentUser.IsCustomer)
            {
                query = query.Where(h => h.OwnerUserId == currentUser.UserId);
            }
            else if (options.OwnerUserId.HasValue)
            {
                query = query.Where(h => h.OwnerUserId == options.OwnerUserId.Value);
            }

            if (!options.IncludeInactive)
            {
                query = query.Where(h => h.IsActive);
            }

            // Searching
            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                string search = options.Search.Trim().ToLower();
                query = query.Where(h => h.Name.ToLower().Contains(search)
                    || h.MicrochipNumber.ToLower().Contains(search)
                    || h.PassportNumber.ToLower().Contains(search));
            }

            // Sorting
            query = query.ApplySort(options.Sort, nameof(Horse.HorseId));

            // Paging
            return await query.ToPagedResultAsync(options, h => h.ToBusinessModel());
        }

        public async Task<HorseModel> GetHorseByIdAsync(int id, CurrentUser currentUser)
        {
            var horse = await _horseRepo.GetQueryable()
                .AsNoTracking()
                .Include(h => h.Owner)
                .FirstOrDefaultAsync(h => h.HorseId == id);

            if (horse == null)
            {
                throw new NotFoundException($"Horse with ID {id} was not found.");
            }

            EnsureCanAccess(horse, currentUser);
            return horse.ToBusinessModel();
        }

        public async Task<HorseModel> CreateHorseAsync(HorseModel model, CurrentUser currentUser)
        {
            ValidateFields(model);
            await EnsureIdentifiersAreUniqueAsync(model, excludeHorseId: null);

            var horse = new Horse
            {
                OwnerUserId = currentUser.UserId, // chủ ngựa luôn là người đang đăng nhập, không nhận từ client
                Name = model.Name,
                MicrochipNumber = model.MicrochipNumber,
                PassportNumber = model.PassportNumber,
                Breed = model.Breed,
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth,
                Color = model.Color,
                SpecialCareRequirements = model.SpecialCareRequirements,
                PhotoUrl = model.PhotoUrl
            };

            await _horseRepo.AddAsync(horse);
            return horse.ToBusinessModel();
        }

        public async Task<HorseModel> UpdateHorseAsync(int id, HorseModel model, CurrentUser currentUser)
        {
            var horse = await GetOwnedHorseAsync(id, currentUser);

            ValidateFields(model);
            await EnsureIdentifiersAreUniqueAsync(model, excludeHorseId: id);

            horse.Name = model.Name;
            horse.MicrochipNumber = model.MicrochipNumber;
            horse.PassportNumber = model.PassportNumber;
            horse.Breed = model.Breed;
            horse.Gender = model.Gender;
            horse.DateOfBirth = model.DateOfBirth;
            horse.Color = model.Color;
            horse.SpecialCareRequirements = model.SpecialCareRequirements;
            horse.PhotoUrl = model.PhotoUrl;

            await _horseRepo.UpdateAsync(horse);
            return horse.ToBusinessModel();
        }

        // Xóa mềm: ngựa đã từng đi chuyến vẫn phải còn trong lịch sử đơn và hồ sơ
        public async Task DeleteHorseAsync(int id, CurrentUser currentUser)
        {
            var horse = await GetOwnedHorseAsync(id, currentUser);

            bool inOpenBooking = await _bookingHorseRepo.ExistsAsync(bh =>
                bh.HorseId == id
                && bh.Status != BookingHorseStatuses.Cancelled
                && OpenBookingStatuses.Contains(bh.Booking!.Status));

            if (inOpenBooking)
            {
                throw new BusinessException("This horse is part of a booking that is still in progress and cannot be deleted.");
            }

            horse.IsActive = false;
            await _horseRepo.UpdateAsync(horse);
        }

        // Sửa / xóa: chỉ chủ ngựa (nhân sự chỉ được xem)
        private async Task<Horse> GetOwnedHorseAsync(int id, CurrentUser currentUser)
        {
            var horse = await _horseRepo.GetByIdAsync(id);
            if (horse == null || !horse.IsActive)
            {
                throw new NotFoundException($"Horse with ID {id} was not found.");
            }

            if (horse.OwnerUserId != currentUser.UserId)
            {
                throw new ForbiddenException("You can only modify your own horses.");
            }

            return horse;
        }

        private static void EnsureCanAccess(Horse horse, CurrentUser currentUser)
        {
            if (currentUser.IsCustomer && horse.OwnerUserId != currentUser.UserId)
            {
                throw new ForbiddenException("You can only view your own horses.");
            }
        }

        private static void ValidateFields(HorseModel model)
        {
            if (!HorseGenders.All.Contains(model.Gender))
            {
                throw new BusinessException($"Gender '{model.Gender}' is invalid. Allowed values: {string.Join(", ", HorseGenders.All)}.");
            }

            if (model.DateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow))
            {
                throw new BusinessException("DateOfBirth must be in the past.");
            }
        }

        // Mã microchip và số hộ chiếu định danh duy nhất một con ngựa trên toàn hệ thống (kể cả ngựa đã xóa mềm)
        private async Task EnsureIdentifiersAreUniqueAsync(HorseModel model, int? excludeHorseId)
        {
            if (await _horseRepo.ExistsAsync(h => h.HorseId != excludeHorseId && h.MicrochipNumber == model.MicrochipNumber))
            {
                throw new BusinessException($"Microchip number '{model.MicrochipNumber}' is already registered.");
            }

            if (await _horseRepo.ExistsAsync(h => h.HorseId != excludeHorseId && h.PassportNumber == model.PassportNumber))
            {
                throw new BusinessException($"Passport number '{model.PassportNumber}' is already registered.");
            }
        }
    }
}
