using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Implementations
{
    // R-17: vận đơn hàng không (air waybill) của chuyến chở ngựa đơn Air. Coordinator nhập số và tải file lên.
    public class AirWaybillService : IAirWaybillService
    {
        private readonly IRepository<Trip> _tripRepo;
        private readonly IRepository<TripHorse> _tripHorseRepo;
        private readonly IFileStorageService _storage;

        public AirWaybillService(IRepository<Trip> tripRepo, IRepository<TripHorse> tripHorseRepo, IFileStorageService storage)
        {
            _tripRepo = tripRepo;
            _tripHorseRepo = tripHorseRepo;
            _storage = storage;
        }

        public async Task<AirWaybillModel> GetAsync(int tripId, CurrentUser currentUser)
        {
            var trip = await _tripRepo.GetQueryable().AsNoTracking().FirstOrDefaultAsync(t => t.TripId == tripId)
                ?? throw new NotFoundException($"Trip with ID {tripId} was not found.");
            if (currentUser.IsCustomer)
            {
                // Khách chỉ xem được vận đơn của chuyến có ngựa của mình
                bool own = await _tripHorseRepo.GetQueryable()
                    .AnyAsync(th => th.TripId == tripId && th.BookingHorse!.Booking!.CustomerUserId == currentUser.UserId);
                if (!own)
                {
                    throw new ForbiddenException("You do not have access to this trip.");
                }
            }
            return await ToModelAsync(trip);
        }

        public async Task<AirWaybillModel> UpdateAsync(int tripId, string number, string? filePath)
        {
            var trip = await _tripRepo.GetQueryable().FirstOrDefaultAsync(t => t.TripId == tripId)
                ?? throw new NotFoundException($"Trip with ID {tripId} was not found.");
            if (trip.OverallStatus == TripStatuses.Completed || trip.OverallStatus == TripStatuses.Cancelled)
            {
                throw new BusinessException($"Trip {trip.TripCode} is already {trip.OverallStatus}.");
            }

            bool hasAirBooking = await _tripHorseRepo.GetQueryable()
                .AnyAsync(th => th.TripId == tripId && th.BookingHorse!.Booking!.TransportMode == TransportModes.Air);
            if (!hasAirBooking)
            {
                throw new BusinessException($"Trip {trip.TripCode} carries no horse of an Air booking, so it has no air waybill.");
            }

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                if (!_storage.IsValidPath(filePath.Trim(), FileCategories.Documents))
                {
                    throw new BusinessException("AirWaybillFilePath is not a valid uploaded file path (upload it with POST /api/files, category 'documents').");
                }
                trip.AirWaybillFilePath = filePath.Trim();
            }
            trip.AirWaybillNumber = number.Trim();
            await _tripRepo.SaveChangesAsync();
            return await ToModelAsync(trip);
        }

        private async Task<AirWaybillModel> ToModelAsync(Trip trip)
        {
            string? url = null;
            if (!string.IsNullOrEmpty(trip.AirWaybillFilePath))
            {
                var urls = await _storage.GetViewUrlsAsync(new[] { trip.AirWaybillFilePath });
                urls.TryGetValue(trip.AirWaybillFilePath, out url);
            }
            return new AirWaybillModel
            {
                TripId = trip.TripId,
                TripCode = trip.TripCode,
                AirWaybillNumber = trip.AirWaybillNumber,
                AirWaybillFilePath = trip.AirWaybillFilePath,
                AirWaybillFileUrl = url
            };
        }
    }
}
