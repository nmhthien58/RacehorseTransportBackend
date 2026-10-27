using System.Globalization;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Implementations
{
    // Hàm thuần cho dịch vụ bản đồ (BE-01), để dễ viết unit test
    public static class GeoMath
    {
        private const double EarthRadiusKm = 6371.0088;

        // Khoảng cách đường chim bay giữa hai tọa độ (km)
        public static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
        {
            double dLat = ToRad(lat2 - lat1);
            double dLng = ToRad(lng2 - lng1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return 2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        }

        public static double HaversineKm(GeoPointModel a, GeoPointModel b) => HaversineKm(a.Lat, a.Lng, b.Lat, b.Lng);

        // Quãng đường cộng dồn tới từng điểm của đường vẽ (km), phần tử đầu = 0
        public static double[] CumulativeKm(IReadOnlyList<GeoPointModel> points)
        {
            var result = new double[points.Count];
            for (int i = 1; i < points.Count; i++)
            {
                result[i] = result[i - 1] + HaversineKm(points[i - 1], points[i]);
            }
            return result;
        }

        // Giải mã "encoded polyline" (định dạng Google, Goong và OSRM đều dùng; precision 5 = 1e5)
        public static List<GeoPointModel> DecodePolyline(string? encoded, int precision = 5)
        {
            var points = new List<GeoPointModel>();
            if (string.IsNullOrEmpty(encoded))
            {
                return points;
            }

            double factor = Math.Pow(10, precision);
            int index = 0, lat = 0, lng = 0;
            while (index < encoded.Length)
            {
                lat += NextValue(encoded, ref index);
                if (index >= encoded.Length)
                {
                    break;
                }
                lng += NextValue(encoded, ref index);
                points.Add(new GeoPointModel(lat / factor, lng / factor));
            }
            return points;
        }

        private static int NextValue(string encoded, ref int index)
        {
            int result = 0, shift = 0, b;
            do
            {
                b = encoded[index++] - 63;
                result |= (b & 0x1f) << shift;
                shift += 5;
            }
            while (b >= 0x20 && index < encoded.Length);
            return (result & 1) != 0 ? ~(result >> 1) : result >> 1;
        }

        // Rút gọn đường vẽ còn tối đa maxPoints điểm: giữ điểm đầu, điểm cuối và các điểm cách đều nhau theo quãng đường
        public static List<GeoPointModel> Simplify(IReadOnlyList<GeoPointModel> points, int maxPoints)
        {
            if (points.Count <= maxPoints || maxPoints < 2)
            {
                return points.ToList();
            }

            var cumulative = CumulativeKm(points);
            double step = cumulative[^1] / (maxPoints - 1);
            var result = new List<GeoPointModel> { points[0] };
            double next = step;
            for (int i = 1; i < points.Count - 1; i++)
            {
                if (cumulative[i] >= next)
                {
                    result.Add(points[i]);
                    next = cumulative[i] + step;
                }
            }
            result.Add(points[^1]);
            return result;
        }

        // Khóa cache theo tọa độ làm tròn 5 chữ số (~1 m)
        public static string Key(double lat, double lng) =>
            string.Create(CultureInfo.InvariantCulture, $"{Math.Round(lat, 5):0.00000},{Math.Round(lng, 5):0.00000}");

        public static string Key(GeoPointModel p) => Key(p.Lat, p.Lng);

        public static string Num(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

        private static double ToRad(double degrees) => degrees * Math.PI / 180;
    }
}
