using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.Exceptions;

namespace RacehorseTransport.Services.Implementations
{
    // Đồng ý an tử (Chính sách mục 9.6): bác sĩ thú y khuyến nghị an tử khi ngựa còn sống thì phải hỏi chủ ngựa;
    // quá Incident.EuthanasiaConsentMinutes mà chủ ngựa chưa trả lời thì mới được ủy quyền khẩn cấp. Hàm thuần để dễ viết unit test.
    public static class EuthanasiaConsentRules
    {
        public static void ValidatePlan(string responseAction, string? offloadOutcome, bool requiresConsent)
        {
            if (requiresConsent && (responseAction != IncidentResponseActions.OffloadHorse || offloadOutcome != OffloadOutcomes.Deceased))
            {
                throw new BusinessException("RequiresEuthanasiaConsent is only used with an OffloadHorse plan whose outcome is Deceased (the vet recommends euthanasia).");
            }
        }

        // Sự cố cũ chưa có PendingConsentType là chờ đồng ý đi đường vòng
        public static string PendingType(TripIncident incident) => incident.PendingConsentType ?? ConsentTypes.Detour;

        public static DateTimeOffset Deadline(DateTimeOffset requestedAt, int consentMinutes) => requestedAt.AddMinutes(consentMinutes);

        // Số phút còn phải chờ trước khi được ủy quyền khẩn cấp (0 = đã quá hạn)
        public static int MinutesLeft(DateTimeOffset requestedAt, DateTimeOffset now, int consentMinutes)
        {
            double left = (Deadline(requestedAt, consentMinutes) - now).TotalMinutes;
            return left <= 0 ? 0 : (int)Math.Ceiling(left);
        }

        public static void EnsureCanEmergencyAuthorize(TripIncident incident, DateTimeOffset now, int consentMinutes)
        {
            if (incident.Status != IncidentStatuses.AwaitingConsent || PendingType(incident) != ConsentTypes.Euthanasia || incident.ConsentRequestedAt == null)
            {
                throw new BusinessException($"Incident {incident.IncidentCode} is not waiting for the owner's decision on euthanasia (status: {incident.Status}).");
            }
            int left = MinutesLeft(incident.ConsentRequestedAt.Value, now, consentMinutes);
            if (left > 0)
            {
                throw new BusinessException($"The owner still has {left} minute(s) to answer. Emergency authorisation is only allowed after {consentMinutes} minutes without an answer.");
            }
            bool hasVetDoc = incident.Documents.Any(d => d.DocumentType == IncidentDocumentTypes.VetReport || d.DocumentType == IncidentDocumentTypes.DeathCertificate);
            if (!hasVetDoc)
            {
                throw new BusinessException("Emergency authorisation needs the veterinarian's report attached to the incident (VetReport).");
            }
        }
    }
}
