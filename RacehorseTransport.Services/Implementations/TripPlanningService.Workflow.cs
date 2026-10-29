using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Repositories.Entities;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Constants;
using RacehorseTransport.Services.Exceptions;

namespace RacehorseTransport.Services.Implementations
{
    // Flow 3: Coordinator lập bản nháp chuyến (xe, đoàn, ngựa, mốc) -> trình -> Manager duyệt hoặc trả về.
    public partial class TripPlanningService
    {
        // Tạm dùng khi đánh lại thứ tự mốc, xem ResequenceAsync
        private const int ResequenceOffset = 100000;

        // =============================================================================================
        // Bản nháp
        // =============================================================================================

        public async Task<TripDetailModel> CreateTripAsync(TripPlanModel plan, CurrentUser currentUser)
        {
            await ValidatePlanAsync(plan, excludeTripId: null);
            if (plan.Checkpoints == null || plan.Checkpoints.Count == 0)
            {
                plan.Checkpoints = await CopyApprovedRouteWhenSingleBookingAsync(plan);
            }
            if (plan.Checkpoints != null)
            {
                ValidateCheckpointInputs(plan.Checkpoints);
            }

            int tripId = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var trip = new Trip
                {
                    TripCode = await _codeGenerator.NextAsync(CodePrefixes.Trip),
                    VehicleId = plan.VehicleId,
                    PlannedStartDate = plan.PlannedStartDate,
                    PlannedEndDate = plan.PlannedEndDate,
                    PlannedCost = plan.PlannedCost,
                    OverallStatus = TripStatuses.Draft,
                    PlannedByUserId = currentUser.UserId,
                    Crew = BuildCrew(plan.Crew),
                    TripHorses = BuildTripHorses(plan.Horses),
                    Checkpoints = BuildCheckpoints(plan.Checkpoints ?? new List<CheckpointInputModel>())
                };

                await _tripRepo.AddAsync(trip);
                return trip.TripId;
            });

            return await _tripService.GetTripByIdAsync(tripId, currentUser);
        }

        // Sửa bản nháp: thay toàn bộ đoàn xe và danh sách ngựa; các mốc chỉ bị thay khi plan.Checkpoints có giá trị
        public async Task<TripDetailModel> UpdateTripAsync(int tripId, TripPlanModel plan, CurrentUser currentUser)
        {
            var trip = await GetDraftForEditAsync(tripId, currentUser);

            await ValidatePlanAsync(plan, excludeTripId: tripId);
            if (trip.Checkpoints.Count == 0 && (plan.Checkpoints == null || plan.Checkpoints.Count == 0))
            {
                plan.Checkpoints = await CopyApprovedRouteWhenSingleBookingAsync(plan);
            }
            if (plan.Checkpoints != null)
            {
                ValidateCheckpointInputs(plan.Checkpoints);
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Xóa trước, thêm sau thành hai lần lưu để không vướng ràng buộc duy nhất (ô chuồng, thứ tự mốc)
                await _crewRepo.DeleteRangeAsync(trip.Crew.ToList());
                await _tripHorseRepo.DeleteRangeAsync(trip.TripHorses.ToList());
                if (plan.Checkpoints != null)
                {
                    await _checkpointRepo.DeleteRangeAsync(trip.Checkpoints.ToList());
                }

                trip.VehicleId = plan.VehicleId;
                trip.PlannedStartDate = plan.PlannedStartDate;
                trip.PlannedEndDate = plan.PlannedEndDate;
                trip.PlannedCost = plan.PlannedCost;

                foreach (var member in BuildCrew(plan.Crew))
                {
                    trip.Crew.Add(member);
                }
                foreach (var tripHorse in BuildTripHorses(plan.Horses))
                {
                    trip.TripHorses.Add(tripHorse);
                }
                if (plan.Checkpoints != null)
                {
                    foreach (var checkpoint in BuildCheckpoints(plan.Checkpoints))
                    {
                        trip.Checkpoints.Add(checkpoint);
                    }
                }

                await _tripRepo.UpdateAsync(trip);
            });

            return await _tripService.GetTripByIdAsync(tripId, currentUser);
        }

        // =============================================================================================
        // Mốc lộ trình của bản nháp
        // =============================================================================================

        public async Task<TripDetailModel> AddCheckpointAsync(int tripId, CheckpointInputModel input, CurrentUser currentUser)
        {
            var trip = await GetDraftForEditAsync(tripId, currentUser);
            ValidateCheckpointInputs(new[] { input });

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var ordered = trip.Checkpoints.OrderBy(c => c.SequenceOrder).ToList();

                var checkpoint = ToCheckpoint(input, sequenceOrder: ordered.Count + 1);
                checkpoint.TripId = tripId;
                await _checkpointRepo.AddAsync(checkpoint);

                // Chèn vào giữa thì đánh lại thứ tự cả danh sách
                if (input.Position.HasValue && input.Position.Value <= ordered.Count)
                {
                    ordered.Insert(Math.Max(1, input.Position.Value) - 1, checkpoint);
                    await ResequenceAsync(ordered);
                }
            });

            return await _tripService.GetTripByIdAsync(tripId, currentUser);
        }

        public async Task<TripDetailModel> UpdateCheckpointAsync(int tripId, int checkpointId, CheckpointInputModel input, CurrentUser currentUser)
        {
            var trip = await GetDraftForEditAsync(tripId, currentUser);
            var checkpoint = FindCheckpoint(trip, checkpointId);
            ValidateCheckpointInputs(new[] { input });

            checkpoint.CheckpointName = input.CheckpointName;
            checkpoint.CheckpointType = input.CheckpointType;
            checkpoint.Address = input.Address;
            checkpoint.Latitude = input.Latitude;
            checkpoint.Longitude = input.Longitude;
            checkpoint.DistanceKmFromPrevious = input.DistanceKmFromPrevious;
            checkpoint.PlannedTime = input.PlannedTime;
            checkpoint.MandatoryRestMinutes = input.MandatoryRestMinutes;
            checkpoint.BookingId = input.BookingId;

            await _checkpointRepo.UpdateAsync(checkpoint);
            return await _tripService.GetTripByIdAsync(tripId, currentUser);
        }

        public async Task<TripDetailModel> DeleteCheckpointAsync(int tripId, int checkpointId, CurrentUser currentUser)
        {
            var trip = await GetDraftForEditAsync(tripId, currentUser);
            var checkpoint = FindCheckpoint(trip, checkpointId);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _checkpointRepo.DeleteAsync(checkpoint);
                await ResequenceAsync(trip.Checkpoints.OrderBy(c => c.SequenceOrder).ToList());
            });

            return await _tripService.GetTripByIdAsync(tripId, currentUser);
        }

        public async Task<TripDetailModel> ReorderCheckpointsAsync(int tripId, List<int> checkpointIds, CurrentUser currentUser)
        {
            var trip = await GetDraftForEditAsync(tripId, currentUser);

            var existingIds = trip.Checkpoints.Select(c => c.CheckpointId).OrderBy(id => id).ToList();
            if (checkpointIds.Count != checkpointIds.Distinct().Count()
                || !existingIds.SequenceEqual(checkpointIds.OrderBy(id => id)))
            {
                throw new BusinessException("CheckpointIds must contain every checkpoint of this trip exactly once.");
            }

            var ordered = checkpointIds.Select(id => trip.Checkpoints.First(c => c.CheckpointId == id)).ToList();
            await _unitOfWork.ExecuteInTransactionAsync(() => ResequenceAsync(ordered));

            return await _tripService.GetTripByIdAsync(tripId, currentUser);
        }

        // =============================================================================================
        // Trình và duyệt kế hoạch
        // =============================================================================================

        public async Task<TripDetailModel> SubmitTripAsync(int tripId, CurrentUser currentUser)
        {
            var trip = await GetDraftForEditAsync(tripId, currentUser);

            // Kiểm tra lại toàn bộ: từ lúc lập nháp tới giờ xe, người hoặc đơn có thể đã thay đổi
            var plan = ToPlan(trip);
            var bookings = await ValidatePlanAsync(plan, excludeTripId: tripId);
            ValidateRoute(trip, bookings);
            await ValidateApprovedRouteAndDriversAsync(trip, bookings);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                CopyEscortAssignments(plan, trip);
                trip.OverallStatus = TripStatuses.PendingApproval;
                await _tripRepo.UpdateAsync(trip);

                await _notificationService.SendToRoleAsync(
                    UserRoles.LogisticsManager,
                    $"Trip plan {trip.TripCode} awaiting approval",
                    $"A transport plan for {trip.TripHorses.Count} horse(s) has been submitted for your review.",
                    NotificationReferenceTypes.Trip,
                    trip.TripId);
            });

            return await _tripService.GetTripByIdAsync(tripId, currentUser);
        }

        public async Task<TripDetailModel> ApproveTripAsync(int tripId, CurrentUser currentUser)
        {
            var trip = await GetTrackedTripAsync(tripId);
            EnsureStatus(trip, TripStatuses.PendingApproval, "approved");
            var plan = ToPlan(trip);
            var bookings = await ValidatePlanAsync(plan, excludeTripId: tripId);
            ValidateRoute(trip, bookings);
            await ValidateApprovedRouteAndDriversAsync(trip, bookings);

            var customerIds = await _tripHorseRepo.GetQueryable()
                .Where(th => th.TripId == tripId)
                .Select(th => th.BookingHorse!.Booking!.CustomerUserId)
                .Distinct()
                .ToListAsync();

            // R-02: nhắc khách trả phần còn lại trước ngày xuất phát
            var paymentSettings = await _settingService.GetPaymentSettingsAsync();
            var balanceDueAt = trip.PlannedStartDate.AddDays(-paymentSettings.BalanceDueDaysBeforeDeparture);

            // Phần còn lại chưa trả của từng đơn trên chuyến (đơn cũ không có DepositPercent thì không bắt trả)
            var balances = (await _tripHorseRepo.GetQueryable()
                .AsNoTracking()
                .Where(th => th.TripId == tripId && th.BookingHorse!.Booking!.DepositPercent != null)
                .Select(th => new
                {
                    th.BookingHorse!.Booking!.BookingId,
                    th.BookingHorse.Booking.BookingCode,
                    th.BookingHorse.Booking.CustomerUserId,
                    th.BookingHorse.Booking.CurrencyCode,
                    Remaining = th.BookingHorse.Booking.EstimatedCost - th.BookingHorse.Booking.DepositAmount
                        - th.BookingHorse.Booking.Payments
                            .Where(p => p.PaymentType == PaymentTypes.Balance && p.Status == PaymentStatuses.Succeeded)
                            .Sum(p => p.Amount)
                })
                .Distinct()
                .ToListAsync())
                .Where(b => b.Remaining > 0)
                .ToList();

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                CopyEscortAssignments(plan, trip);
                trip.OverallStatus = TripStatuses.Scheduled;
                trip.ApprovedByUserId = currentUser.UserId;
                trip.ApprovedAt = DateTimeOffset.UtcNow;
                trip.PlanRejectionReason = null;
                await _tripRepo.UpdateAsync(trip);

                await _notificationService.SendToUsersAsync(
                    trip.Crew.Select(c => c.UserId),
                    $"You are assigned to trip {trip.TripCode}",
                    $"Departure is planned for {trip.PlannedStartDate:yyyy-MM-dd HH:mm} UTC. Open the trip to see the schedule, horses and checkpoints.",
                    NotificationReferenceTypes.Trip,
                    trip.TripId);

                if (trip.PlannedByUserId.HasValue)
                {
                    await _notificationService.SendAsync(
                        trip.PlannedByUserId.Value,
                        $"Trip plan {trip.TripCode} approved",
                        "The transport plan has been approved and the crew has been notified.",
                        NotificationReferenceTypes.Trip,
                        trip.TripId);
                }

                await _notificationService.SendToUsersAsync(
                    customerIds,
                    $"Your horses are scheduled on trip {trip.TripCode}",
                    $"Transport has been scheduled. Planned departure: {trip.PlannedStartDate:yyyy-MM-dd HH:mm} UTC. If your booking still has a remaining balance, please pay it before {balanceDueAt:yyyy-MM-dd}; the horses cannot depart until it is paid.",
                    NotificationReferenceTypes.Trip,
                    trip.TripId);

                // R-16: email người nhận "chuyến đã lên lịch"
                await _recipientService.OnTripApprovedAsync(trip.TripId);

                foreach (var b in balances)
                {
                    await _notificationService.EmailCustomerAsync(
                        b.CustomerUserId,
                        EmailTemplateNames.BalanceDue,
                        CustomerPages.Booking(b.BookingId),
                        new Dictionary<string, string>
                        {
                            ["BookingCode"] = b.BookingCode,
                            ["TripCode"] = trip.TripCode,
                            ["DepartureDate"] = trip.PlannedStartDate.ToString("dd/MM/yyyy"),
                            ["Amount"] = $"{b.Remaining:N2} {b.CurrencyCode}",
                            ["AmountVnd"] = $"{Math.Round(b.Remaining * paymentSettings.UsdVndRate, 0):N0} đ",
                            ["DueDate"] = balanceDueAt.ToString("dd/MM/yyyy")
                        });
                }
            });

            return await _tripService.GetTripByIdAsync(tripId, currentUser);
        }

        // Trả kế hoạch về cho Coordinator sửa. Bị trả lần thứ 3 thì chuyến bị hủy, ngựa quay lại hàng chờ xếp chuyến.
        public async Task<TripDetailModel> RejectTripAsync(int tripId, string reason, CurrentUser currentUser)
        {
            var trip = await GetTrackedTripAsync(tripId);
            EnsureStatus(trip, TripStatuses.PendingApproval, "rejected");

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                trip.PlanRejectionCount += 1;
                trip.PlanRejectionReason = reason.Trim();
                bool cancelled = trip.PlanRejectionCount >= TripStatuses.MaxPlanRejectionCount;
                trip.OverallStatus = cancelled ? TripStatuses.Cancelled : TripStatuses.Draft;
                if (cancelled)
                {
                    ReleaseHorses(trip);
                }
                await _tripRepo.UpdateAsync(trip);

                if (trip.PlannedByUserId.HasValue)
                {
                    await _notificationService.SendAsync(
                        trip.PlannedByUserId.Value,
                        cancelled ? $"Trip plan {trip.TripCode} cancelled" : $"Trip plan {trip.TripCode} returned",
                        cancelled
                            ? $"The plan was rejected {trip.PlanRejectionCount} times and has been cancelled. Reason: {trip.PlanRejectionReason}"
                            : $"The plan needs changes ({trip.PlanRejectionCount}/{TripStatuses.MaxPlanRejectionCount}). Reason: {trip.PlanRejectionReason}",
                        NotificationReferenceTypes.Trip,
                        trip.TripId);
                }
            });

            return await _tripService.GetTripByIdAsync(tripId, currentUser);
        }

        // Hủy chuyến chưa khởi hành: người lập kế hoạch, Manager hoặc Admin
        public async Task<TripDetailModel> CancelTripAsync(int tripId, CurrentUser currentUser)
        {
            var trip = await GetTrackedTripAsync(tripId);

            bool isPlanner = currentUser.Role == UserRoles.FleetCoordinator && trip.PlannedByUserId == currentUser.UserId;
            if (!isPlanner && !currentUser.IsInRole(UserRoles.LogisticsManager, UserRoles.Admin))
            {
                throw new ForbiddenException("Only the coordinator who planned this trip or a manager can cancel it.");
            }

            if (trip.OverallStatus != TripStatuses.Draft
                && trip.OverallStatus != TripStatuses.PendingApproval
                && trip.OverallStatus != TripStatuses.Scheduled)
            {
                throw new BusinessException($"A trip can only be cancelled before departure (current status: {trip.OverallStatus}).");
            }

            bool wasScheduled = trip.OverallStatus == TripStatuses.Scheduled;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                trip.OverallStatus = TripStatuses.Cancelled;
                ReleaseHorses(trip);
                await _tripRepo.UpdateAsync(trip);

                var recipients = new List<int>();
                if (wasScheduled)
                {
                    recipients.AddRange(trip.Crew.Select(c => c.UserId)); // đoàn xe đã được báo lịch thì phải báo hủy
                }
                if (trip.PlannedByUserId.HasValue && trip.PlannedByUserId != currentUser.UserId)
                {
                    recipients.Add(trip.PlannedByUserId.Value);
                }

                await _notificationService.SendToUsersAsync(
                    recipients,
                    $"Trip {trip.TripCode} cancelled",
                    "This trip has been cancelled. Its horses are back in the planning queue.",
                    NotificationReferenceTypes.Trip,
                    trip.TripId);
            });

            return await _tripService.GetTripByIdAsync(tripId, currentUser);
        }

        // =============================================================================================
        // Kiểm tra kế hoạch
        // =============================================================================================

        // Mọi quy tắc về xe, ngựa và đoàn xe của một kế hoạch. excludeTripId: bỏ qua chính chuyến đang sửa khi dò trùng lịch.
        // Trả về các đơn có ngựa trên chuyến (dùng cho quy tắc lộ trình khi trình / duyệt).
        private async Task<List<RouteBookingRule>> ValidatePlanAsync(TripPlanModel plan, int? excludeTripId)
        {
            var startUtc = plan.PlannedStartDate.ToUniversalTime();
            var endUtc = plan.PlannedEndDate.ToUniversalTime();

            if (endUtc <= startUtc)
            {
                throw new BusinessException("PlannedEndDate must be after PlannedStartDate.");
            }
            if (startUtc <= DateTimeOffset.UtcNow)
            {
                throw new BusinessException("PlannedStartDate must be in the future.");
            }
            if (plan.PlannedCost < 0)
            {
                throw new BusinessException("PlannedCost cannot be negative.");
            }

            // ---------- Ngựa ----------
            if (plan.Horses.Count == 0)
            {
                throw new BusinessException("A trip needs at least one horse.");
            }

            var bookingHorseIds = plan.Horses.Select(h => h.BookingHorseId).ToList();
            if (bookingHorseIds.Count != bookingHorseIds.Distinct().Count())
            {
                throw new BusinessException("The same horse appears more than once in this trip.");
            }

            var bookingHorses = await _bookingHorseRepo.GetQueryable()
                .AsNoTracking()
                .Include(bh => bh.Horse)
                .Include(bh => bh.Booking)
                .Where(bh => bookingHorseIds.Contains(bh.BookingHorseId))
                .ToListAsync();

            var unknownIds = bookingHorseIds.Except(bookingHorses.Select(bh => bh.BookingHorseId)).ToList();
            if (unknownIds.Count > 0)
            {
                throw new BusinessException($"Booking horse(s) with ID {string.Join(", ", unknownIds)} were not found.");
            }

            var deceased = bookingHorses.FirstOrDefault(bh => bh.Status == BookingHorseStatuses.Deceased);
            if (deceased != null)
            {
                throw new BusinessException($"Horse '{deceased.Horse!.Name}' is recorded as deceased and cannot be placed on a trip.");
            }

            var notPlannable = bookingHorses.FirstOrDefault(bh =>
                bh.Status == BookingHorseStatuses.Cancelled || bh.Booking!.Status != BookingStatuses.Approved);
            if (notPlannable != null)
            {
                throw new BusinessException(
                    $"Horse '{notPlannable.Horse!.Name}' cannot be planned: booking {notPlannable.Booking!.BookingCode} is {notPlannable.Booking.Status}. Only horses of approved bookings can be placed on a trip.");
            }

            // Xe không thể đón đơn nào sau khi khoảng pickup của đơn đã qua (ngày cuối khoảng tính trọn ngày).
            // Giờ đón cụ thể (Origin hoặc mốc PickUp của đơn) và hạn giao theo mốc trả của từng đơn được kiểm tra
            // ở TripRouteRules khi trình / duyệt, vì chuyến ghép đón / trả mỗi đơn ở một mốc khác nhau (R-04).
            var bookings = bookingHorses.Select(bh => bh.Booking!).DistinctBy(b => b.BookingId).ToList();
            foreach (var booking in bookings)
            {
                if (startUtc >= booking.PickupWindowEnd.AddDays(1))
                {
                    throw new BusinessException(
                        $"Booking {booking.BookingCode} can only be picked up between {booking.PickupWindowStart:yyyy-MM-dd} and {booking.PickupWindowEnd:yyyy-MM-dd}; PlannedStartDate is after this window.");
                }
                if (startUtc > booking.DeliveryDeadline)
                {
                    throw new BusinessException(
                        $"Booking {booking.BookingCode} must be delivered by {booking.DeliveryDeadline:yyyy-MM-dd HH:mm zzz}; PlannedStartDate is already later than that.");
                }
            }

            var modes = bookingHorses.Select(bh => bh.Booking!.TransportMode).Distinct().ToList();
            if (modes.Count > 1)
            {
                throw new BusinessException("Ground and air bookings cannot be combined on the same trip.");
            }
            string transportMode = modes[0];

            // Mỗi con ngựa trong đơn chỉ nằm trên một chuyến còn hiệu lực
            // (con rời chuyến chỉ được xếp lại khi hướng xử lý là WaitRecover, R-12)
            var alreadyPlanned = await _tripHorseRepo.GetQueryable()
                .Where(th => bookingHorseIds.Contains(th.BookingHorseId)
                    && th.TripId != excludeTripId
                    && th.Trip!.OverallStatus != TripStatuses.Cancelled
                    && (TripHorseStatuses.Holding.Contains(th.Status)
                        || (th.Status == TripHorseStatuses.Offloaded && th.OffloadOutcome != OffloadOutcomes.WaitRecover)))
                .Select(th => th.BookingHorse!.Horse!.Name + " (" + th.Trip!.TripCode + ")")
                .ToListAsync();

            if (alreadyPlanned.Count > 0)
            {
                throw new BusinessException($"These horses are already on another trip: {string.Join(", ", alreadyPlanned)}.");
            }

            // ---------- Xe ----------
            var vehicle = await _assetRepo.GetByIdAsync(plan.VehicleId);
            if (vehicle == null)
            {
                throw new BusinessException($"Vehicle with ID {plan.VehicleId} was not found.");
            }
            if (vehicle.Status == AssetStatuses.Maintenance)
            {
                throw new BusinessException($"Vehicle {vehicle.AssetCode} is under maintenance.");
            }

            bool isAirStall = vehicle.AssetType == AssetTypes.AirStall;
            if ((transportMode == TransportModes.Air) != isAirStall)
            {
                throw new BusinessException(transportMode == TransportModes.Air
                    ? "Air bookings must be planned on an air stall."
                    : "Ground bookings must be planned on a truck or van, not an air stall.");
            }

            if (plan.Horses.Count > vehicle.CapacityHorses)
            {
                throw new BusinessException($"Vehicle {vehicle.AssetCode} carries at most {vehicle.CapacityHorses} horse(s); this plan has {plan.Horses.Count}.");
            }

            var slots = plan.Horses.Select(h => h.StallSlotNumber).ToList();
            if (slots.Any(s => s < 1 || s > vehicle.CapacityHorses))
            {
                throw new BusinessException($"StallSlotNumber must be between 1 and {vehicle.CapacityHorses} for vehicle {vehicle.AssetCode}.");
            }
            if (slots.Count != slots.Distinct().Count())
            {
                throw new BusinessException("Two horses cannot share the same stall slot.");
            }

            var otherTrips = OverlappingTrips(startUtc, endUtc).Where(t => t.TripId != excludeTripId);

            var vehicleConflict = await otherTrips.Where(t => t.VehicleId == plan.VehicleId).Select(t => t.TripCode).FirstOrDefaultAsync();
            if (vehicleConflict != null)
            {
                throw new BusinessException($"Vehicle {vehicle.AssetCode} is already booked on trip {vehicleConflict} during this period.");
            }

            // ---------- Đoàn xe ----------
            if (plan.Crew.Any(c => !CrewRoles.All.Contains(c.CrewRole)))
            {
                throw new BusinessException($"CrewRole must be one of: {string.Join(", ", CrewRoles.All)}.");
            }
            if (plan.Crew.GroupBy(c => new { c.UserId, c.CrewRole }).Any(g => g.Count() > 1))
            {
                throw new BusinessException("A crew member is listed twice with the same role.");
            }
            if (plan.Crew.GroupBy(c => c.CrewRole).Any(g => g.Count(c => c.IsLead) > 1))
            {
                throw new BusinessException("There can be only one lead driver and one lead escort.");
            }

            int driverCount = plan.Crew.Count(c => c.CrewRole == CrewRoles.Driver);
            int escortCount = plan.Crew.Count(c => c.CrewRole == CrewRoles.Escort);
            int requiredEscorts = (int)Math.Ceiling(plan.Horses.Count / (double)PricingService.HorsesPerGroom);

            if (driverCount == 0)
            {
                throw new BusinessException("A trip needs at least one driver.");
            }
            if (escortCount < requiredEscorts)
            {
                throw new BusinessException($"{plan.Horses.Count} horse(s) need at least {requiredEscorts} escort(s) (one per {PricingService.HorsesPerGroom} horses); this plan has {escortCount}.");
            }

            // R-06: mỗi con một escort của chuyến, mỗi escort tối đa 3 con; con chưa chọn escort được tự phân
            EscortAssignmentRules.Apply(plan.Horses, plan.Crew);

            var crewUserIds = plan.Crew.Select(c => c.UserId).Distinct().ToList();
            var crewUsers = await _userRepo.GetQueryable()
                .AsNoTracking()
                .Where(u => crewUserIds.Contains(u.UserId) && u.IsActive && u.Role == UserRoles.DriverEscort)
                .ToListAsync();

            var invalidCrewIds = crewUserIds.Except(crewUsers.Select(u => u.UserId)).ToList();
            if (invalidCrewIds.Count > 0)
            {
                throw new BusinessException($"User(s) with ID {string.Join(", ", invalidCrewIds)} are not active drivers / escorts.");
            }

            var busyCrew = await otherTrips
                .SelectMany(t => t.Crew, (t, c) => new { c.UserId, c.User!.FullName, t.TripCode })
                .Where(x => crewUserIds.Contains(x.UserId))
                .Select(x => x.FullName + " (" + x.TripCode + ")")
                .Distinct()
                .ToListAsync();

            if (busyCrew.Count > 0)
            {
                throw new BusinessException($"These crew members are already assigned during this period: {string.Join(", ", busyCrew)}.");
            }

            return bookings.Select(b => new RouteBookingRule
            {
                BookingId = b.BookingId,
                BookingCode = b.BookingCode,
                PickupWindowStart = b.PickupWindowStart,
                PickupWindowEnd = b.PickupWindowEnd,
                DeliveryDeadline = b.DeliveryDeadline,
                TransportMode = b.TransportMode
            }).ToList();
        }

        // Một đơn trên chuyến chưa có mốc: chép nguyên tuyến Approved để Coordinator không phải nhập lại.
        // Chuyến ghép nhiều đơn tiếp tục dùng suggest-route vì còn phải sắp thứ tự đón / trả.
        private async Task<List<CheckpointInputModel>?> CopyApprovedRouteWhenSingleBookingAsync(TripPlanModel tripPlan)
        {
            var horseIds = tripPlan.Horses.Select(h => h.BookingHorseId).Distinct().ToList();
            var bookingIds = await _bookingHorseRepo.GetQueryable()
                .AsNoTracking()
                .Where(bh => horseIds.Contains(bh.BookingHorseId))
                .Select(bh => bh.BookingId)
                .Distinct()
                .ToListAsync();

            if (bookingIds.Count != 1)
            {
                return null;
            }

            int bookingId = bookingIds[0];
            var approved = await _routePlanRepo.GetQueryable()
                .AsNoTracking()
                .Include(p => p.Booking)
                .Include(p => p.Stops)
                .FirstOrDefaultAsync(p => p.BookingId == bookingId && p.Status == RoutePlanStatuses.Approved);
            // Đơn đường bay không có tuyến trên bản đồ: Coordinator tự nhập mốc. Đơn đường bộ thiếu tuyến bị chặn khi trình duyệt.
            if (approved == null)
            {
                return null;
            }

            var ordered = approved.Stops.OrderBy(s => s.SequenceOrder).ToList();
            if (ordered.Count < 2)
            {
                throw new BusinessException($"The approved route for booking {approved.Booking!.BookingCode} has fewer than two stops.");
            }

            var current = tripPlan.PlannedStartDate > approved.Booking!.PickupWindowStart
                ? tripPlan.PlannedStartDate
                : approved.Booking.PickupWindowStart;
            var checkpoints = new List<CheckpointInputModel>();
            for (int i = 0; i < ordered.Count; i++)
            {
                var stop = ordered[i];
                if (i > 0)
                {
                    current = current.AddMinutes(ordered[i - 1].RestMinutes)
                        .AddHours((double)(stop.DurationHoursFromPrevious ?? 0.01m));
                }
                checkpoints.Add(new CheckpointInputModel
                {
                    CheckpointName = stop.StopName,
                    CheckpointType = stop.StopType,
                    Address = stop.Address,
                    Latitude = stop.Latitude,
                    Longitude = stop.Longitude,
                    DistanceKmFromPrevious = stop.DistanceKmFromPrevious,
                    PlannedTime = current,
                    MandatoryRestMinutes = stop.RestMinutes
                });
            }
            return checkpoints;
        }

        private async Task ValidateApprovedRouteAndDriversAsync(Trip trip, List<RouteBookingRule> bookings)
        {
            // Đường bay không lập tuyến trên bản đồ (báo giá chốt phát hành ngay khi duyệt sơ bộ) và không có chặng lái dài
            // như đường bộ: không đòi tuyến đã duyệt, không áp quy tắc số tài xế.
            string mode = bookings.Select(b => b.TransportMode).Distinct().Single();
            if (mode != TransportModes.Ground)
            {
                return;
            }

            var route = trip.Checkpoints.Where(c => c.IsActive).OrderBy(c => c.SequenceOrder).ToList();
            var bookingIds = bookings.Select(b => b.BookingId).ToList();
            var plans = await _routePlanRepo.GetQueryable()
                .AsNoTracking()
                .Include(p => p.Stops)
                .Where(p => bookingIds.Contains(p.BookingId) && p.Status == RoutePlanStatuses.Approved)
                .ToListAsync();

            var missing = bookings.Where(b => plans.All(p => p.BookingId != b.BookingId)).Select(b => b.BookingCode).ToList();
            if (missing.Count > 0)
            {
                throw new BusinessException($"Approved route plan is missing for booking(s): {string.Join(", ", missing)}.");
            }

            decimal distance = await EstimateTripDistanceKmAsync(route, plans);
            int driverCount = trip.Crew.Count(c => c.CrewRole == CrewRoles.Driver);
            var estimate = _transitEstimator.Estimate(new TransitEstimateInputModel
            {
                TransportMode = mode,
                DistanceKm = distance > 0 ? (int)Math.Ceiling(distance) : null,
                DriverCount = driverCount,
                Stops = route.Count(c => c.CheckpointType == CheckpointTypes.PickUp || c.CheckpointType == CheckpointTypes.DropOff) + 2
            });
            TripAssignmentRules.EnsureDriverCount(estimate.DrivingHours, driverCount, _transitOptions.MaxDrivingHoursPerDaySingleDriver);

            foreach (var booking in bookings)
            {
                // Chỉ dò nước đi qua khi mọi mốc của đơn có tọa độ (mốc nhập tay, mốc cửa khẩu gợi ý có thể chưa có).
                // Thiếu tọa độ hoặc bản đồ không trả được nước thì bỏ qua, không chặn trình duyệt chuyến.
                var segment = SegmentForBooking(route, booking.BookingId);
                if (!segment.All(HasCoordinates))
                {
                    continue;
                }
                var actual = await _geoService.RouteAsync(RoutePoints(segment), includeCountries: true);
                if (actual.CountryCodes == null || actual.CountryCodes.Count == 0)
                {
                    continue;
                }
                var approved = plans.Single(p => p.BookingId == booking.BookingId).Stops
                    .Where(s => !string.IsNullOrWhiteSpace(s.CountryCode))
                    .Select(s => s.CountryCode!);
                TripAssignmentRules.EnsureCountriesAreApproved(booking.BookingCode, actual.CountryCodes, approved);
            }
        }

        // Quãng đường để ước tính giờ lái: ưu tiên km từng chặng đã lưu, rồi chỉ đường qua tọa độ các mốc;
        // mốc chưa có tọa độ thì lấy tuyến đã duyệt dài nhất của các đơn trên chuyến (chuyến ít nhất phải chạy hết tuyến đó).
        private async Task<decimal> EstimateTripDistanceKmAsync(List<RouteCheckpoint> route, List<RoutePlan> plans)
        {
            if (route.Count > 1 && route.Skip(1).All(c => c.DistanceKmFromPrevious.HasValue))
            {
                return route.Skip(1).Sum(c => c.DistanceKmFromPrevious!.Value);
            }
            if (route.Count > 1 && route.All(HasCoordinates))
            {
                var routed = await _geoService.RouteAsync(RoutePoints(route), includeCountries: false);
                if (routed.TotalDistanceKm > 0)
                {
                    return (decimal)routed.TotalDistanceKm;
                }
            }
            return plans.Count == 0 ? 0 : plans.Max(p => p.TotalDistanceKm);
        }

        private static bool HasCoordinates(RouteCheckpoint checkpoint) =>
            checkpoint.Latitude.HasValue && checkpoint.Longitude.HasValue;

        private static List<RouteCheckpoint> SegmentForBooking(List<RouteCheckpoint> route, int bookingId)
        {
            var pickup = TripRouteRules.PickupCheckpoint(route, bookingId)!;
            var dropoff = TripRouteRules.DropoffCheckpoint(route, bookingId)!;
            int start = route.IndexOf(pickup);
            int count = route.IndexOf(dropoff) - start + 1;
            return route.GetRange(start, count);
        }

        private static List<GeoPointModel> RoutePoints(IEnumerable<RouteCheckpoint> route)
        {
            var checkpoints = route.ToList();
            if (checkpoints.Any(c => !c.Latitude.HasValue || !c.Longitude.HasValue))
            {
                throw new BusinessException("Every checkpoint needs latitude and longitude before the trip can be submitted.");
            }
            return checkpoints.Select(c => new GeoPointModel((double)c.Latitude!.Value, (double)c.Longitude!.Value)).ToList();
        }

        private static void ValidateCheckpointInputs(IEnumerable<CheckpointInputModel> checkpoints)
        {
            foreach (var checkpoint in checkpoints)
            {
                if (!CheckpointTypes.All.Contains(checkpoint.CheckpointType))
                {
                    throw new BusinessException($"CheckpointType '{checkpoint.CheckpointType}' is invalid. Allowed values: {string.Join(", ", CheckpointTypes.All)}.");
                }
                if (checkpoint.MandatoryRestMinutes < 0)
                {
                    throw new BusinessException("MandatoryRestMinutes cannot be negative.");
                }
                if (checkpoint.Latitude.HasValue != checkpoint.Longitude.HasValue)
                {
                    throw new BusinessException($"Checkpoint '{checkpoint.CheckpointName}' must provide both latitude and longitude.");
                }

                bool isBookingStop = CheckpointTypes.BookingStops.Contains(checkpoint.CheckpointType);
                if (isBookingStop && checkpoint.BookingId == null)
                {
                    throw new BusinessException($"A {checkpoint.CheckpointType} checkpoint needs BookingId: the booking picked up / dropped off there.");
                }
                if (!isBookingStop && checkpoint.BookingId != null)
                {
                    throw new BusinessException($"Only PickUp and DropOff checkpoints can have a BookingId ('{checkpoint.CheckpointName}' is {checkpoint.CheckpointType}).");
                }
            }
        }

        // Lộ trình hoàn chỉnh mới được trình (xem TripRouteRules)
        private static void ValidateRoute(Trip trip, List<RouteBookingRule> bookings)
        {
            var route = trip.Checkpoints.Where(c => c.IsActive).OrderBy(c => c.SequenceOrder).ToList();
            TripRouteRules.Validate(route, bookings, trip.PlannedStartDate, trip.PlannedEndDate);
        }

        // Chuyến bị hủy: ngựa không còn bị giữ, quay lại hàng chờ xếp chuyến
        private static void ReleaseHorses(Trip trip)
        {
            foreach (var tripHorse in trip.TripHorses.Where(th => th.Status == TripHorseStatuses.Planned))
            {
                tripHorse.Status = TripHorseStatuses.Cancelled;
            }
        }

        // =============================================================================================
        // Hàm phụ
        // =============================================================================================

        // Chuyến được nạp kèm đoàn xe, ngựa và mốc; chỉ bản nháp của chính người lập (hoặc Admin) mới sửa được
        private async Task<Trip> GetDraftForEditAsync(int tripId, CurrentUser currentUser)
        {
            var trip = await GetTrackedTripAsync(tripId);

            bool isPlanner = currentUser.Role == UserRoles.FleetCoordinator && trip.PlannedByUserId == currentUser.UserId;
            if (!isPlanner && currentUser.Role != UserRoles.Admin)
            {
                throw new ForbiddenException("Only the coordinator who planned this trip can change it.");
            }

            if (trip.OverallStatus != TripStatuses.Draft)
            {
                throw new BusinessException($"Only a draft trip can be changed (current status: {trip.OverallStatus}).");
            }

            return trip;
        }

        private async Task<Trip> GetTrackedTripAsync(int tripId)
        {
            var trip = await _tripRepo.GetQueryable()
                .Include(t => t.Crew)
                .Include(t => t.TripHorses)
                .Include(t => t.Checkpoints)
                .FirstOrDefaultAsync(t => t.TripId == tripId);

            if (trip == null)
            {
                throw new NotFoundException($"Trip with ID {tripId} was not found.");
            }
            return trip;
        }

        private static RouteCheckpoint FindCheckpoint(Trip trip, int checkpointId)
        {
            var checkpoint = trip.Checkpoints.FirstOrDefault(c => c.CheckpointId == checkpointId);
            if (checkpoint == null)
            {
                throw new NotFoundException($"Checkpoint with ID {checkpointId} was not found on trip {trip.TripCode}.");
            }
            return checkpoint;
        }

        private static void EnsureStatus(Trip trip, string requiredStatus, string action)
        {
            if (trip.OverallStatus != requiredStatus)
            {
                throw new BusinessException($"Only a trip in status {requiredStatus} can be {action} (current status: {trip.OverallStatus}).");
            }
        }

        // DB cấm hai mốc đang hiệu lực của một chuyến trùng thứ tự, và kiểm tra ngay từng câu lệnh.
        // Đổi chỗ trực tiếp (1 <-> 2) sẽ trùng giữa chừng, nên đẩy tất cả ra vùng số tạm rồi mới gán thứ tự thật.
        private async Task ResequenceAsync(List<RouteCheckpoint> ordered)
        {
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].SequenceOrder = ResequenceOffset + i;
            }
            await _checkpointRepo.SaveChangesAsync();

            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].SequenceOrder = i + 1;
            }
            await _checkpointRepo.SaveChangesAsync();
        }

        // Mỗi vai trò có đúng một trưởng nhóm: nếu chưa chọn thì người đầu tiên của vai trò đó là trưởng nhóm
        private static List<TripCrew> BuildCrew(List<TripCrewInputModel> crew)
        {
            var result = crew.Select(c => new TripCrew
            {
                UserId = c.UserId,
                CrewRole = c.CrewRole,
                IsLead = c.IsLead,
                Duty = c.Duty
            }).ToList();

            foreach (var roleGroup in result.GroupBy(c => c.CrewRole))
            {
                if (!roleGroup.Any(c => c.IsLead))
                {
                    roleGroup.First().IsLead = true;
                }
            }

            return result;
        }

        private static List<TripHorse> BuildTripHorses(List<TripHorseInputModel> horses)
        {
            return horses.Select(h => new TripHorse
            {
                BookingHorseId = h.BookingHorseId,
                StallSlotNumber = h.StallSlotNumber,
                Notes = h.Notes,
                EscortUserId = h.EscortUserId
            }).ToList();
        }

        // Chuyến lập trước R-06 chưa có escort cho từng con: lưu lại phần tự phân lúc trình / duyệt
        private static void CopyEscortAssignments(TripPlanModel plan, Trip trip)
        {
            foreach (var tripHorse in trip.TripHorses)
            {
                tripHorse.EscortUserId = plan.Horses.First(h => h.BookingHorseId == tripHorse.BookingHorseId).EscortUserId;
            }
        }

        private static List<RouteCheckpoint> BuildCheckpoints(List<CheckpointInputModel> checkpoints)
        {
            return checkpoints.Select((c, index) => ToCheckpoint(c, index + 1)).ToList();
        }

        private static RouteCheckpoint ToCheckpoint(CheckpointInputModel input, int sequenceOrder)
        {
            return new RouteCheckpoint
            {
                SequenceOrder = sequenceOrder,
                CheckpointName = input.CheckpointName,
                CheckpointType = input.CheckpointType,
                Address = input.Address,
                Latitude = input.Latitude,
                Longitude = input.Longitude,
                DistanceKmFromPrevious = input.DistanceKmFromPrevious,
                PlannedTime = input.PlannedTime,
                MandatoryRestMinutes = input.MandatoryRestMinutes,
                BookingId = input.BookingId,
                Status = CheckpointStatuses.Pending,
                IsActive = true
            };
        }

        private static TripPlanModel ToPlan(Trip trip)
        {
            return new TripPlanModel
            {
                VehicleId = trip.VehicleId,
                PlannedStartDate = trip.PlannedStartDate,
                PlannedEndDate = trip.PlannedEndDate,
                PlannedCost = trip.PlannedCost,
                Crew = trip.Crew.Select(c => new TripCrewInputModel { UserId = c.UserId, CrewRole = c.CrewRole, IsLead = c.IsLead, Duty = c.Duty }).ToList(),
                Horses = trip.TripHorses.Select(h => new TripHorseInputModel { BookingHorseId = h.BookingHorseId, StallSlotNumber = h.StallSlotNumber, Notes = h.Notes, EscortUserId = h.EscortUserId }).ToList()
            };
        }
    }
}
