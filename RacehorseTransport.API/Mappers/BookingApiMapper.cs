using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    public static class BookingApiMapper
    {
        public static BookingFilterOptions ToBusinessOptions(this BookingQueryRequest request)
        {
            return new BookingFilterOptions
            {
                Search = request.Search,
                Sort = request.Sort,
                Page = request.Page,
                Size = request.Size,
                Status = request.Status,
                CustomerUserId = request.CustomerUserId,
                DepartureFrom = request.DepartureFrom,
                DepartureTo = request.DepartureTo
            };
        }

        public static CreateBookingModel ToBusinessModel(this CreateBookingRequest request)
        {
            return new CreateBookingModel
            {
                PickupAddress = FormatAddress(request.PickupAddress, request.PickupCity),
                PickupCity = request.PickupCity?.Trim(),
                PickupCountryCode = request.PickupCountryCode.Trim().ToUpperInvariant(),
                DropoffAddress = FormatAddress(request.DropoffAddress, request.DropoffCity),
                DropoffCity = request.DropoffCity?.Trim(),
                DropoffCountryCode = request.DropoffCountryCode.Trim().ToUpperInvariant(),
                DepartureDate = request.DepartureDate!.Value,
                DeliveryDate = request.DeliveryDate!.Value,
                TransportMode = request.TransportMode.Trim(),
                DistanceKm = request.DistanceKm,
                RequiresClimateControl = request.RequiresClimateControl,
                DeclaredValue = request.DeclaredValue,
                SpecialInstructions = string.IsNullOrWhiteSpace(request.SpecialInstructions) ? null : request.SpecialInstructions.Trim(),
                Horses = request.Horses.Select(h => new CreateBookingHorseModel
                {
                    HorseId = h.HorseId,
                    StallClass = h.StallClass.Trim(),
                    Notes = string.IsNullOrWhiteSpace(h.Notes) ? null : h.Notes.Trim()
                }).ToList()
            };
        }

        public static QuoteInputModel ToBusinessModel(this QuotePreviewRequest request)
        {
            return new QuoteInputModel
            {
                TransportMode = request.TransportMode.Trim(),
                PickupCountryCode = request.PickupCountryCode?.Trim().ToUpperInvariant(),
                PickupCity = request.PickupCity?.Trim(),
                PickupAddress = request.PickupAddress?.Trim(),
                DropoffCountryCode = request.DropoffCountryCode.Trim().ToUpperInvariant(),
                DropoffCity = request.DropoffCity?.Trim(),
                DropoffAddress = request.DropoffAddress?.Trim(),
                DistanceKm = request.DistanceKm,
                RequiresClimateControl = request.RequiresClimateControl,
                DeclaredValue = request.DeclaredValue,
                DepartureDate = request.DepartureDate!.Value,
                DeliveryDate = request.DeliveryDate!.Value,
                StallClasses = request.Horses.Select(h => h.StallClass.Trim()).ToList()
            };
        }

        public static DistanceEstimateInputModel ToBusinessModel(this EstimateDistanceRequest request)
        {
            return new DistanceEstimateInputModel
            {
                PickupCountryCode = string.IsNullOrWhiteSpace(request.PickupCountryCode) ? "VN" : request.PickupCountryCode.Trim().ToUpperInvariant(),
                PickupCity = request.PickupCity?.Trim(),
                PickupAddress = request.PickupAddress?.Trim(),
                DropoffCountryCode = request.DropoffCountryCode.Trim().ToUpperInvariant(),
                DropoffCity = request.DropoffCity?.Trim(),
                DropoffAddress = request.DropoffAddress?.Trim()
            };
        }

        public static DistanceEstimateResponse ToResponse(this DistanceEstimateResultModel model)
        {
            return new DistanceEstimateResponse
            {
                DistanceKm = model.DistanceKm,
                EstimatedHours = model.EstimatedHours,
                RouteSummary = model.RouteSummary,
                Source = model.Source
            };
        }

        public static QuoteLineResponse ToResponse(this QuoteLineModel model)
        {
            return new QuoteLineResponse
            {
                Code = model.Code,
                Name = model.Name,
                Quantity = model.Quantity,
                UnitPrice = model.UnitPrice,
                Amount = model.Amount
            };
        }

        public static QuoteResponse ToResponse(this QuoteModel model)
        {
            return new QuoteResponse
            {
                Lines = model.Lines.Select(l => l.ToResponse()).ToList(),
                Total = model.Total,
                CurrencyCode = model.CurrencyCode,
                HorseCount = model.HorseCount,
                GroomCount = model.GroomCount,
                TransitDays = model.TransitDays,
                IsExpress = model.IsExpress,
                DistanceKm = model.DistanceKm,
                EstimatedHours = model.EstimatedHours,
                RouteSummary = model.RouteSummary
            };
        }

        private static string FormatAddress(string address, string? city)
        {
            string trimmed = (address ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(city)) return trimmed;
            string cityTrimmed = city.Trim();
            if (trimmed.Contains(cityTrimmed, StringComparison.OrdinalIgnoreCase)) return trimmed;
            return string.IsNullOrWhiteSpace(trimmed) ? cityTrimmed : $"{trimmed}, {cityTrimmed}";
        }

        public static BookingSummaryResponse ToResponse(this BookingSummaryModel model)
        {
            var response = new BookingSummaryResponse();
            FillSummary(response, model);
            return response;
        }

        public static BookingDetailResponse ToResponse(this BookingDetailModel model)
        {
            var response = new BookingDetailResponse
            {
                CustomerEmail = model.CustomerEmail,
                CustomerPhone = model.CustomerPhone,
                SpecialInstructions = model.SpecialInstructions,
                DistanceKm = model.DistanceKm,
                IsExpress = model.IsExpress,
                RequiresClimateControl = model.RequiresClimateControl,
                DeclaredValue = model.DeclaredValue,
                RejectionReason = model.RejectionReason,
                ReviewedByUserId = model.ReviewedByUserId,
                ReviewedByName = model.ReviewedByName,
                ReviewedAt = model.ReviewedAt,
                QuoteLines = model.QuoteLines.Select(l => l.ToResponse()).ToList(),
                Horses = model.Horses.Select(h => new BookingHorseResponse
                {
                    BookingHorseId = h.BookingHorseId,
                    HorseId = h.HorseId,
                    HorseName = h.HorseName,
                    MicrochipNumber = h.MicrochipNumber,
                    StallClass = h.StallClass,
                    Status = h.Status,
                    Notes = h.Notes,
                    DossierId = h.DossierId,
                    DossierCode = h.DossierCode,
                    DossierStatus = h.DossierStatus,
                    Trips = h.Trips.Select(t => new BookingHorseTripResponse
                    {
                        TripId = t.TripId,
                        TripCode = t.TripCode,
                        TripStatus = t.TripStatus,
                        StallSlotNumber = t.StallSlotNumber,
                        PlannedStartDate = t.PlannedStartDate,
                        PlannedEndDate = t.PlannedEndDate
                    }).ToList()
                }).ToList()
            };

            FillSummary(response, model);
            return response;
        }

        private static void FillSummary(BookingSummaryResponse response, BookingSummaryModel model)
        {
            response.BookingId = model.BookingId;
            response.BookingCode = model.BookingCode;
            response.CustomerUserId = model.CustomerUserId;
            response.CustomerName = model.CustomerName;
            response.PickupAddress = model.PickupAddress;
            response.PickupCountryCode = model.PickupCountryCode;
            response.DropoffAddress = model.DropoffAddress;
            response.DropoffCountryCode = model.DropoffCountryCode;
            response.DepartureDate = model.DepartureDate;
            response.DeliveryDate = model.DeliveryDate;
            response.TransportMode = model.TransportMode;
            response.TotalHorses = model.TotalHorses;
            response.EstimatedCost = model.EstimatedCost;
            response.CurrencyCode = model.CurrencyCode;
            response.Status = model.Status;
            response.AssignedSpecialistId = model.AssignedSpecialistId;
            response.AssignedSpecialistName = model.AssignedSpecialistName;
            response.CreatedAt = model.CreatedAt;
        }
    }
}
