using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.Exceptions;

namespace RacehorseTransport.Services.Implementations
{
    public static class IncidentReplacementRules
    {
        public static void ValidateVehicle(
            TransportAsset currentVehicle,
            TransportAsset replacement,
            IReadOnlyCollection<string> stallClassesToCarry,
            bool requiresClimateControl)
        {
            if (replacement.AssetId == currentVehicle.AssetId)
            {
                throw new BusinessException("The replacement vehicle must be different from the current vehicle.");
            }
            if (replacement.AssetType != currentVehicle.AssetType)
            {
                throw new BusinessException($"Replacement vehicle must have the same asset type as {currentVehicle.AssetCode} ({currentVehicle.AssetType}).");
            }
            if (replacement.Status != AssetStatuses.Available)
            {
                throw new BusinessException($"Replacement vehicle {replacement.AssetCode} is not available (current status: {replacement.Status}).");
            }
            if (replacement.CapacityHorses < stallClassesToCarry.Count)
            {
                throw new BusinessException($"Replacement vehicle {replacement.AssetCode} has only {replacement.CapacityHorses} stall(s), but {stallClassesToCarry.Count} horse(s) still need transport.");
            }
            int requiredCompartments = CompartmentRules.RequiredCompartments(stallClassesToCarry);
            if (replacement.CompartmentCount < requiredCompartments)
            {
                throw new BusinessException(
                    $"Replacement vehicle {replacement.AssetCode} has {replacement.CompartmentCount} compartment(s) but the horses still on board need {requiredCompartments} " +
                    "(a Shared compartment takes 3 horses, Comfort 2, Private 1).");
            }
            if (requiresClimateControl && !replacement.HasClimateControl)
            {
                throw new BusinessException($"Replacement vehicle {replacement.AssetCode} must have climate control because an onboard booking requires it.");
            }
        }

        public static TripCrew ValidateCrew(
            IReadOnlyCollection<TripCrew> currentCrew,
            int removeUserId,
            User replacement,
            string crewRole,
            string? conflictingTripCode)
        {
            if (!CrewRoles.All.Contains(crewRole))
            {
                throw new BusinessException($"CrewRole must be one of: {string.Join(", ", CrewRoles.All)}.");
            }
            if (removeUserId == replacement.UserId)
            {
                throw new BusinessException("The replacement crew member must be different from the person being removed.");
            }

            var removed = currentCrew.FirstOrDefault(c => c.UserId == removeUserId && c.CrewRole == crewRole);
            if (removed == null)
            {
                throw new BusinessException($"User {removeUserId} is not assigned to this trip as {crewRole}.");
            }
            if (!replacement.IsActive || replacement.Role != UserRoles.DriverEscort)
            {
                throw new BusinessException($"User {replacement.UserId} is not an active driver / escort.");
            }
            if (currentCrew.Any(c => c.UserId == replacement.UserId && c.CrewRole == crewRole))
            {
                throw new BusinessException($"User {replacement.UserId} is already assigned to this trip as {crewRole}.");
            }
            if (!string.IsNullOrWhiteSpace(conflictingTripCode))
            {
                throw new BusinessException($"Replacement crew member {replacement.FullName} is already assigned to trip {conflictingTripCode} during this period.");
            }

            return removed;
        }
    }
}
