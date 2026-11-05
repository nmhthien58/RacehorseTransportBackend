using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.Exceptions;

namespace RacehorseTransport.Services.Implementations
{
    // Ngựa rời chuyến chỉ qua sự cố (R-12). Hàm thuần để dễ viết unit test.
    //   - Phương án OffloadHorse phải chọn con ngựa và hướng xử lý tiếp (OffloadOutcomes).
    //   - Deceased: chỉ trên sự cố mức Critical và bắt buộc biên bản thú y.
    //   - Tài xế dỡ khẩn cấp lúc báo sự cố: ngựa Offloaded ngay, hướng xử lý để trống tới khi phương án được duyệt.
    public static class OffloadRules
    {
        // Kiểm tra phần riêng của phương án OffloadHorse lúc Coordinator đề xuất
        public static void ValidatePlan(string? outcome, IEnumerable<string> incidentDocumentTypes)
        {
            if (string.IsNullOrWhiteSpace(outcome))
            {
                throw new BusinessException($"OffloadOutcome is required for an OffloadHorse plan. Allowed values: {string.Join(", ", OffloadOutcomes.All)}.");
            }
            if (!OffloadOutcomes.All.Contains(outcome))
            {
                throw new BusinessException($"OffloadOutcome '{outcome}' is invalid. Allowed values: {string.Join(", ", OffloadOutcomes.All)}.");
            }
            if (outcome == OffloadOutcomes.Deceased)
            {
                bool hasVetDoc = incidentDocumentTypes.Any(t => t == IncidentDocumentTypes.VetReport || t == IncidentDocumentTypes.DeathCertificate);
                if (!hasVetDoc)
                {
                    throw new BusinessException("A veterinary report or death certificate is required when a horse is recorded as deceased. Upload it with POST /api/incidents/{id}/documents.");
                }
            }
        }

        // Tương thích ngược: kiểm tra theo đường dẫn báo cáo thú y hoặc tài liệu
        public static void ValidatePlan(string? outcome, string incidentSeverity, string? vetReportPath)
        {
            var docTypes = new List<string>();
            if (!string.IsNullOrWhiteSpace(vetReportPath))
            {
                docTypes.Add(IncidentDocumentTypes.VetReport);
            }
            ValidatePlan(outcome, docTypes);
        }

        // Con ngựa có thể được chọn cho phương án OffloadHorse: còn trên xe, hoặc đã được dỡ khẩn cấp trong chính sự cố này
        public static bool CanBeOffloaded(TripHorse tripHorse, TripIncident incident)
        {
            if (TripHorseStatuses.OnBoard.Contains(tripHorse.Status))
            {
                return true;
            }
            return incident.EmergencyOffload
                && incident.AffectedTripHorseId == tripHorse.TripHorseId
                && tripHorse.Status == TripHorseStatuses.Offloaded
                && tripHorse.OffloadOutcome == null;
        }

        // Trạng thái của con ngựa trên chuyến sau khi phương án được duyệt
        public static string StatusFor(string outcome)
        {
            return outcome == OffloadOutcomes.Deceased ? TripHorseStatuses.Deceased : TripHorseStatuses.Offloaded;
        }

        // Con ngựa đã xong với đơn hàng: đã giao (kể cả giao tại chỗ), chết, hoặc được đưa về nơi đi.
        // Mọi con của đơn xong thì đơn được hoàn tất.
        public static bool IsFinished(TripHorse tripHorse)
        {
            return tripHorse.Status == TripHorseStatuses.Delivered
                || tripHorse.Status == TripHorseStatuses.Deceased
                || (tripHorse.Status == TripHorseStatuses.Offloaded && tripHorse.OffloadOutcome == OffloadOutcomes.ReturnToOrigin);
        }

        // Con ngựa còn việc phải làm trên chuyến trước khi đóng chuyến:
        // còn trên xe, chờ ký e-POD tại chỗ, hoặc đã dỡ khẩn cấp mà chưa có phương án được duyệt
        public static bool IsPendingOnTrip(TripHorse tripHorse)
        {
            if (TripHorseStatuses.OnBoard.Contains(tripHorse.Status))
            {
                return true;
            }
            return tripHorse.Status == TripHorseStatuses.Offloaded
                && (tripHorse.OffloadOutcome == null || tripHorse.OffloadOutcome == OffloadOutcomes.HandOverHere);
        }
    }
}
