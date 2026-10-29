using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Implementations
{
    // BE-10 (R-12, R-15): gom mọi vấn đề trước khi xuất phát vào một danh sách.
    // Dùng chung cho GET /api/trips/{id}/departure-check và StartTripAsync để hai chỗ luôn khớp.
    public static class DepartureCheckRules
    {
        public static DepartureCheckModel Evaluate(DepartureCheckInput input)
        {
            var issues = new List<DepartureIssueModel>();

            if (input.TripStatus != TripStatuses.Scheduled)
            {
                issues.Add(Error(DepartureIssueCodes.TripNotScheduled, DepartureIssueCategories.Trip,
                    $"Only a scheduled trip can depart (current status: {input.TripStatus})."));
            }

            // Hạn giấy chỉ ghi theo ngày: giấy hết hạn đúng ngày xuất phát vẫn còn dùng được trong ngày đó
            var departureDay = DateOnly.FromDateTime(input.DepartureAt.UtcDateTime);
            var arrivalDay = DateOnly.FromDateTime(input.PlannedArrivalAt.UtcDateTime);

            foreach (var horse in input.Horses.OrderBy(h => h.BookingCode).ThenBy(h => h.HorseName))
            {
                if (horse.DossierStatus != DossierStatuses.Cleared)
                {
                    issues.Add(Error(DepartureIssueCodes.DossierNotCleared, DepartureIssueCategories.Dossier,
                        $"{horse.HorseName}: clearance is not complete (dossier status: {horse.DossierStatus ?? "none"}).", horse));
                }

                foreach (var document in horse.Documents.Where(d => d.ExpiryDate.HasValue).OrderBy(d => d.DocTypeName))
                {
                    if (document.ExpiryDate!.Value < departureDay)
                    {
                        issues.Add(Error(DepartureIssueCodes.DocumentExpired, DepartureIssueCategories.Documents,
                            $"{horse.HorseName}: '{document.DocTypeName}' expired on {document.ExpiryDate:yyyy-MM-dd}, before departure. Ask the specialist and the customer to renew it.", horse));
                    }
                    else if (document.ExpiryDate.Value < arrivalDay)
                    {
                        // Cơ quan nước đến / quá cảnh có thể kiểm lại giấy ở cửa khẩu (R-12.2): nhắc, không chặn
                        issues.Add(Warning(DepartureIssueCodes.DocumentExpiresBeforeArrival, DepartureIssueCategories.Documents,
                            $"{horse.HorseName}: '{document.DocTypeName}' expires on {document.ExpiryDate:yyyy-MM-dd}, before the planned arrival {arrivalDay:yyyy-MM-dd}.", horse));
                    }
                }

                if (input.FitExamTypeConfigured && !HasValidFitExam(horse.FitExamIssueDates, input.DepartureAt, input.FitExamValidHours))
                {
                    // Khách đã nộp nhưng chuyên viên chưa duyệt: báo riêng để tài xế biết đang chờ ai
                    issues.Add(horse.HasPendingFitExam
                        ? Error(DepartureIssueCodes.FitExamPendingReview, DepartureIssueCategories.Documents,
                            $"{horse.HorseName}: the fit-to-travel exam is waiting for the specialist's review", horse)
                        : Error(DepartureIssueCodes.FitExamMissing, DepartureIssueCategories.Documents,
                            $"{horse.HorseName}: no fit-to-travel exam within {input.FitExamValidHours} hours", horse));
                }
            }

            if (!input.FitExamTypeConfigured && input.Horses.Count > 0)
            {
                issues.Add(Warning(DepartureIssueCodes.FitExamTypeNotConfigured, DepartureIssueCategories.Documents,
                    $"Document type {DocumentTypeCodes.FitExam} is not set up yet, so the fit-to-travel exam was not checked."));
            }

            foreach (var booking in input.Bookings.Where(b => b.Paid < b.EstimatedCost).OrderBy(b => b.BookingCode))
            {
                issues.Add(new DepartureIssueModel
                {
                    Code = DepartureIssueCodes.BalanceUnpaid,
                    Category = DepartureIssueCategories.Payment,
                    Severity = DepartureIssueSeverities.Error,
                    BookingCode = booking.BookingCode,
                    Message = $"{booking.BookingCode}: the remaining balance is not paid ({booking.EstimatedCost - booking.Paid:0.##} {booking.CurrencyCode} outstanding)."
                });
            }

            if (input.IsGround && input.DrivingHours.HasValue
                && TripAssignmentRules.NeedsMoreDrivers(input.DrivingHours.Value, input.DriverCount, input.SingleDriverLimitHours))
            {
                issues.Add(Error(DepartureIssueCodes.NotEnoughDrivers, DepartureIssueCategories.Crew,
                    TripAssignmentRules.DriverCountMessage(input.DrivingHours.Value, input.DriverCount)));
            }

            return new DepartureCheckModel
            {
                DepartureAt = input.DepartureAt,
                Issues = issues,
                IsReady = issues.All(i => i.Severity != DepartureIssueSeverities.Error)
            };
        }

        public static bool HasValidFitExam(IEnumerable<DateOnly> issueDates, DateTimeOffset departureAt, int validHours) =>
            issueDates.Any(d => FitExamRules.IsIssuedInWindow(d, departureAt, validHours));

        // Thông báo lỗi khi bấm xuất phát: liệt kê mọi lỗi chặn trong một câu
        public static string ToBlockingMessage(DepartureCheckModel check) =>
            "The trip cannot depart: " + string.Join(" ", check.Issues
                .Where(i => i.Severity == DepartureIssueSeverities.Error)
                .Select(i => i.Message.TrimEnd('.') + "."));

        private static DepartureIssueModel Error(string code, string category, string message, DepartureHorseInput? horse = null) =>
            Issue(DepartureIssueSeverities.Error, code, category, message, horse);

        private static DepartureIssueModel Warning(string code, string category, string message, DepartureHorseInput? horse = null) =>
            Issue(DepartureIssueSeverities.Warning, code, category, message, horse);

        private static DepartureIssueModel Issue(string severity, string code, string category, string message, DepartureHorseInput? horse) =>
            new()
            {
                Code = code,
                Category = category,
                Severity = severity,
                Message = message,
                HorseName = horse?.HorseName,
                BookingCode = horse?.BookingCode
            };
    }
}
