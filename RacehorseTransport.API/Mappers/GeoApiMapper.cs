using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class GeoApiMapper
    {
        public static GeoPlaceResponse ToResponse(this GeoPlaceModel model)
        {
            return new GeoPlaceResponse
            {
                DisplayName = model.DisplayName,
                Lat = model.Lat,
                Lng = model.Lng,
                CountryCode = model.CountryCode,
                Provider = model.Provider
            };
        }

        public static List<GeoPointModel> ToBusinessModel(this GeoRouteRequest request)
        {
            return request.Points
                .Select(p => new GeoPointModel(p.Lat, p.Lng, string.IsNullOrWhiteSpace(p.CountryCode) ? null : p.CountryCode.Trim().ToUpperInvariant()))
                .ToList();
        }

        public static GeoRouteResponse ToResponse(this GeoRouteModel model)
        {
            return new GeoRouteResponse
            {
                Legs = model.Legs.Select(l => new GeoRouteLegResponse
                {
                    FromIndex = l.FromIndex,
                    ToIndex = l.ToIndex,
                    DistanceKm = l.DistanceKm,
                    DurationHours = l.DurationHours,
                    Provider = l.Provider,
                    IsEstimate = l.IsEstimate,
                    Polyline = ToPairs(l.Polyline)
                }).ToList(),
                TotalDistanceKm = model.TotalDistanceKm,
                TotalDurationHours = model.TotalDurationHours,
                IsEstimate = model.IsEstimate,
                Polyline = ToPairs(model.Polyline),
                CountryCodes = model.CountryCodes
            };
        }

        private static List<double[]> ToPairs(IEnumerable<GeoPointModel> points) =>
            points.Select(p => new[] { Math.Round(p.Lat, 6), Math.Round(p.Lng, 6) }).ToList();
    }
}
