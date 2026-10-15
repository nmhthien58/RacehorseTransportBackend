using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Entities;

namespace RacehorseTransport.Services.Extensions
{
    // Một đơn có ngựa trên chuyến. Một chuyến chở ngựa của nhiều đơn, nên email gửi riêng từng đơn
    // để điền đúng mã đơn và tên ngựa của đơn đó.
    public record TripBookingRecipient(int BookingId, string BookingCode, int CustomerUserId, string HorseNames);

    public static class TripHorseQueryExtensions
    {
        public static async Task<List<TripBookingRecipient>> GetBookingRecipientsAsync(this IQueryable<TripHorse> tripHorses, int tripId)
        {
            var rows = await tripHorses
                .Where(th => th.TripId == tripId)
                .Select(th => new
                {
                    th.BookingHorse!.BookingId,
                    th.BookingHorse.Booking!.BookingCode,
                    th.BookingHorse.Booking.CustomerUserId,
                    HorseName = th.BookingHorse.Horse!.Name
                })
                .ToListAsync();

            return rows
                .GroupBy(r => new { r.BookingId, r.BookingCode, r.CustomerUserId })
                .OrderBy(g => g.Key.BookingCode)
                .Select(g => new TripBookingRecipient(
                    g.Key.BookingId,
                    g.Key.BookingCode,
                    g.Key.CustomerUserId,
                    string.Join(", ", g.Select(r => r.HorseName).Distinct())))
                .ToList();
        }
    }
}
