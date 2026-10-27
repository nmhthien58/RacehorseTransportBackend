namespace RacehorseTransport.Repositories.Entities
{
    // booking.route_plan_stops: các mốc của tuyến dự kiến theo thứ tự (Origin ... Destination)
    public class RoutePlanStop
    {
        public int RoutePlanStopId { get; set; }
        public int RoutePlanId { get; set; }
        public int SequenceOrder { get; set; }
        public string StopName { get; set; } = string.Empty;
        public string StopType { get; set; } = string.Empty;   // RouteStopTypes
        public string Address { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? CountryCode { get; set; }
        public decimal? DistanceKmFromPrevious { get; set; }   // null với mốc đầu
        public decimal? DurationHoursFromPrevious { get; set; }
        public int RestMinutes { get; set; }

        // Navigation properties
        public virtual RoutePlan? RoutePlan { get; set; }
    }
}
