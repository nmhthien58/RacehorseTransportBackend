using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.Exceptions;

namespace RacehorseTransport.Services.Implementations
{
    // Đơn nối tiếp (Chính sách 9.6): chỉ cho ngựa còn ở phòng khám, sự cố đã chốt nguyên nhân và đã có giấy đủ sức đi tiếp.
    // Hàm thuần để dễ viết unit test.
    public static class ContinuationRules
    {
        public static void EnsureCanContinue(TripIncident incident, TripHorse tripHorse)
        {
            string horseName = tripHorse.BookingHorse?.Horse?.Name ?? $"Trip horse {tripHorse.TripHorseId}";
            if (tripHorse.Status != TripHorseStatuses.Offloaded || tripHorse.OffloadOutcome != OffloadOutcomes.WaitRecover)
            {
                throw new BusinessException($"{horseName} is {tripHorse.Status}; only a horse recovering at a vet clinic can get a continuation booking.");
            }
            if (incident.Status != IncidentStatuses.Resolved || incident.FaultParty == null)
            {
                throw new BusinessException($"Resolve incident {incident.IncidentCode} first (choose the cause): it decides whether the continuation is free.");
            }
            if (!incident.Documents.Any(d => d.DocumentType == IncidentDocumentTypes.FitToTravel))
            {
                throw new BusinessException($"Attach the vet's FitToTravel certificate to incident {incident.IncidentCode} before creating the continuation booking.");
            }
        }

        // Lỗi công ty thì miễn cước chặng nối tiếp
        public static bool IsFree(string? faultParty) => faultParty == FaultParties.Company;
    }
}
