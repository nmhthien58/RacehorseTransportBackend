using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly IRepository<Trip> _tripRepo;
        private readonly IRepository<BookingRequest> _bookingRepo;
        private readonly IRepository<TripIncident> _incidentRepo;

        public ReportService(IRepository<Trip> tripRepo, IRepository<BookingRequest> bookingRepo, IRepository<TripIncident> incidentRepo)
        {
            _tripRepo = tripRepo;
            _bookingRepo = bookingRepo;
            _incidentRepo = incidentRepo;
        }

        // Chuyến được tính theo thời điểm đóng (ClosedAt); sự cố theo thời điểm báo (ReportedAt).
        // Doanh thu: các đơn đã Completed có ít nhất một chuyến đóng trong kỳ.
        public async Task<ReportSummaryModel> GetSummaryAsync(DateTimeOffset? from, DateTimeOffset? to)
        {
            var fromUtc = from?.ToUniversalTime();
            var toUtc = to?.ToUniversalTime();

            if (fromUtc.HasValue && toUtc.HasValue && toUtc.Value < fromUtc.Value)
            {
                throw new BusinessException("'to' must not be before 'from'.");
            }

            var tripQuery = _tripRepo.GetQueryable().AsNoTracking().Where(t => t.OverallStatus == TripStatuses.Completed);
            var incidentQuery = _incidentRepo.GetQueryable().AsNoTracking();

            if (fromUtc.HasValue)
            {
                tripQuery = tripQuery.Where(t => t.ClosedAt >= fromUtc.Value);
                incidentQuery = incidentQuery.Where(i => i.ReportedAt >= fromUtc.Value);
            }
            if (toUtc.HasValue)
            {
                tripQuery = tripQuery.Where(t => t.ClosedAt <= toUtc.Value);
                incidentQuery = incidentQuery.Where(i => i.ReportedAt <= toUtc.Value);
            }

            var trips = await tripQuery
                .Select(t => new { t.TripId, t.ActualCost, t.OnTimeStatus, t.KpiScore, t.DelayMinutes, HorseCount = t.TripHorses.Count })
                .ToListAsync();

            var tripIds = trips.Select(t => t.TripId).ToList();

            var bookings = await _bookingRepo.GetQueryable()
                .AsNoTracking()
                .Where(b => b.Status == BookingStatuses.Completed
                    && b.BookingHorses.Any(bh => bh.TripHorses.Any(th => tripIds.Contains(th.TripId))))
                .Select(b => new { b.BookingId, b.EstimatedCost })
                .ToListAsync();

            var incidents = await incidentQuery
                .Select(i => new { i.IncidentType, i.AdditionalCost, i.Status })
                .ToListAsync();

            decimal revenue = bookings.Sum(b => b.EstimatedCost);
            decimal operatingCost = trips.Sum(t => t.ActualCost);
            int onTime = trips.Count(t => t.OnTimeStatus == OnTimeStatuses.OnTime);

            return new ReportSummaryModel
            {
                From = fromUtc,
                To = toUtc,
                CompletedTrips = trips.Count,
                CompletedBookings = bookings.Count,
                HorsesDelivered = trips.Sum(t => t.HorseCount),
                Revenue = revenue,
                OperatingCost = operatingCost,
                GrossProfit = revenue - operatingCost,
                OnTimeTrips = onTime,
                DelayedForceMajeureTrips = trips.Count(t => t.OnTimeStatus == OnTimeStatuses.DelayedForceMajeure),
                DelayedOperationalFaultTrips = trips.Count(t => t.OnTimeStatus == OnTimeStatuses.DelayedOperationalFault),
                OnTimeRate = trips.Count == 0 ? 0 : Math.Round(100m * onTime / trips.Count, 1),
                AverageKpiScore = trips.Count == 0 ? 0 : Math.Round(trips.Average(t => t.KpiScore), 1),
                AverageDelayMinutes = trips.Count == 0 ? 0 : Math.Round((decimal)trips.Average(t => t.DelayMinutes), 1),
                TotalIncidents = incidents.Count,
                IncidentAdditionalCost = incidents
                    .Where(i => i.Status == IncidentStatuses.Approved || i.Status == IncidentStatuses.Resolved)
                    .Sum(i => i.AdditionalCost),
                IncidentsByType = incidents
                    .GroupBy(i => i.IncidentType)
                    .Select(g => new IncidentTypeCountModel { IncidentType = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count).ThenBy(x => x.IncidentType)
                    .ToList()
            };
        }
    }
}
