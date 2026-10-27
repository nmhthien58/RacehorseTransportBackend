using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Mappers
{
    public static class BookingMapper
    {
        public static BookingSummaryModel ToSummaryModel(this BookingRequest entity)
        {
            var model = new BookingSummaryModel();
            FillSummary(model, entity);
            return model;
        }

        // Cần Include: Customer, AssignedSpecialist, ReviewedBy, BookingHorses -> Horse / Dossier / TripHorses -> Trip
        public static BookingDetailModel ToDetailModel(this BookingRequest entity, List<QuoteLineModel> quoteLines)
        {
            var model = new BookingDetailModel
            {
                CustomerEmail = entity.Customer?.Email,
                CustomerPhone = entity.Customer?.PhoneNumber,
                SpecialInstructions = entity.SpecialInstructions,
                DistanceKm = entity.DistanceKm,
                EstimatedTransitHours = entity.EstimatedTransitHours,
                IsExpress = entity.IsExpress,
                RequiresClimateControl = entity.RequiresClimateControl,
                DeclaredValue = entity.DeclaredValue,
                RejectionReason = entity.RejectionReason,
                ReviewedByUserId = entity.ReviewedByUserId,
                ReviewedByName = entity.ReviewedBy?.FullName,
                ReviewedAt = entity.ReviewedAt,
                QuoteLines = quoteLines,
                Horses = entity.BookingHorses
                    .OrderBy(bh => bh.BookingHorseId)
                    .Select(bh => bh.ToBusinessModel())
                    .ToList()
            };

            FillSummary(model, entity);
            return model;
        }

        public static BookingHorseModel ToBusinessModel(this BookingHorse entity)
        {
            return new BookingHorseModel
            {
                BookingHorseId = entity.BookingHorseId,
                HorseId = entity.HorseId,
                HorseName = entity.Horse?.Name ?? string.Empty,
                MicrochipNumber = entity.Horse?.MicrochipNumber ?? string.Empty,
                StallClass = entity.StallClass,
                Status = entity.Status,
                Notes = entity.Notes,
                DeclaredValue = entity.DeclaredValue,
                AgreedValue = entity.AgreedValue,
                ValueProofUrl = entity.ValueProofPath,
                InsurancePlan = entity.InsurancePlan,
                DossierId = entity.Dossier?.DossierId,
                DossierCode = entity.Dossier?.DossierCode,
                DossierStatus = entity.Dossier?.Status,
                Trips = entity.TripHorses
                    .Where(th => th.Trip != null)
                    .OrderBy(th => th.Trip!.PlannedStartDate)
                    .Select(th => new BookingHorseTripModel
                    {
                        TripId = th.TripId,
                        TripCode = th.Trip!.TripCode,
                        TripStatus = th.Trip.OverallStatus,
                        HorseStatus = th.Status,
                        StallSlotNumber = th.StallSlotNumber,
                        PlannedStartDate = th.Trip.PlannedStartDate,
                        PlannedEndDate = th.Trip.PlannedEndDate,
                        PickedUpAt = th.PickedUpAt,
                        DeliveredAt = th.DeliveredAt
                    })
                    .ToList()
            };
        }

        private static void FillSummary(BookingSummaryModel model, BookingRequest entity)
        {
            model.BookingId = entity.BookingId;
            model.BookingCode = entity.BookingCode;
            model.CustomerUserId = entity.CustomerUserId;
            model.CustomerName = entity.Customer?.FullName;
            model.PickupAddress = entity.PickupAddress;
            model.PickupCountryCode = entity.PickupCountryCode;
            model.DropoffAddress = entity.DropoffAddress;
            model.DropoffCountryCode = entity.DropoffCountryCode;
            model.TransitCountryCodes = entity.TransitCountries
                .OrderBy(t => t.SequenceOrder)
                .Select(t => t.CountryCode)
                .ToList();
            model.PickupWindowStart = entity.PickupWindowStart;
            model.PickupWindowEnd = entity.PickupWindowEnd;
            model.DeliveryDeadline = entity.DeliveryDeadline;
            model.TransportMode = entity.TransportMode;
            model.TotalHorses = entity.TotalHorses;
            model.EstimatedCost = entity.EstimatedCost;
            model.QuoteMin = entity.QuoteMin;
            model.QuoteMax = entity.QuoteMax;
            model.CurrencyCode = entity.CurrencyCode;
            model.Status = entity.Status;
            model.AssignedSpecialistId = entity.AssignedSpecialistId;
            model.AssignedSpecialistName = entity.AssignedSpecialist?.FullName;
            model.CreatedAt = entity.CreatedAt;
        }
    }
}
