namespace RacehorseTransport.Repositories.Constants
{
    // Các giá trị dưới đây phải khớp với CHECK constraint trong database/01_schema.sql.
    // Luôn dùng hằng số thay vì gõ chuỗi trực tiếp trong code.

    public static class UserRoles
    {
        public const string Customer = "Customer";
        public const string LogisticsManager = "LogisticsManager";
        public const string TransportSpecialist = "TransportSpecialist";
        public const string FleetCoordinator = "FleetCoordinator";
        public const string DriverEscort = "DriverEscort";
        public const string Admin = "Admin";

        // Nhóm role dùng cho [Authorize(Roles = ...)]
        public const string Staff = LogisticsManager + "," + TransportSpecialist + "," + FleetCoordinator + "," + DriverEscort + "," + Admin;
        public const string Operations = LogisticsManager + "," + TransportSpecialist + "," + FleetCoordinator + "," + Admin;

        public static readonly string[] All = { Customer, LogisticsManager, TransportSpecialist, FleetCoordinator, DriverEscort, Admin };
    }

    public static class UserTokenTypes
    {
        public const string Refresh = "Refresh";
        public const string PasswordReset = "PasswordReset";
    }

    public static class HorseGenders
    {
        public const string Stallion = "Stallion";
        public const string Mare = "Mare";
        public const string Gelding = "Gelding";

        public static readonly string[] All = { Stallion, Mare, Gelding };
    }

    public static class BookingStatuses
    {
        public const string Submitted = "Submitted";
        public const string AwaitingDeposit = "AwaitingDeposit";           // Manager đã duyệt, chờ khách đặt cọc
        public const string Approved = "Approved";                         // đã cọc: làm hồ sơ, xếp chuyến
        public const string Assigned = "Assigned";                         // trạng thái cũ, không còn tạo mới
        public const string AwaitingFinalPayment = "AwaitingFinalPayment"; // chuyến đã đóng, còn chi phí phát sinh chưa trả
        public const string Completed = "Completed";
        public const string Rejected = "Rejected";
        public const string Expired = "Expired";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All =
        {
            Submitted, AwaitingDeposit, Approved, Assigned, AwaitingFinalPayment, Completed, Rejected, Expired, Cancelled
        };
    }

    public static class PaymentTypes
    {
        public const string Deposit = "Deposit"; // cọc sau khi duyệt
        public const string Balance = "Balance"; // phần còn lại của giá báo, trả trước khi xuất phát
        public const string Extra = "Extra";     // chi phí phát sinh (sự cố) sau khi đóng chuyến
        public const string Refund = "Refund";   // hoàn tiền khi hủy đơn

        public static readonly string[] Charges = { Deposit, Balance, Extra };
        public static readonly string[] All = { Deposit, Balance, Extra, Refund };
    }

    public static class PaymentStatuses
    {
        public const string Pending = "Pending";
        public const string Succeeded = "Succeeded";
        public const string Failed = "Failed";
        public const string Expired = "Expired";

        public static readonly string[] All = { Pending, Succeeded, Failed, Expired };
    }

    public static class PaymentProviders
    {
        public const string VnPay = "VnPay";
        public const string Mock = "Mock";     // cổng giả lập khi chưa cấu hình VNPay
        public const string Manual = "Manual"; // Manager ghi nhận tiền nhận ngoài hệ thống

        public static readonly string[] All = { VnPay, Mock, Manual };
    }

    // Giai đoạn của đơn theo góc nhìn khách (R-07), suy ra từ đơn + thanh toán + hồ sơ + ngựa trên chuyến, không lưu DB
    public static class CustomerStages
    {
        public const string PendingReview = "PendingReview";       // chờ duyệt
        public const string AwaitingDeposit = "AwaitingDeposit";   // chờ đặt cọc
        public const string PreparingDossier = "PreparingDossier"; // đang làm hồ sơ kiểm dịch
        public const string AwaitingTrip = "AwaitingTrip";         // hồ sơ xong, chờ xếp chuyến
        public const string Scheduled = "Scheduled";               // đã xếp chuyến (đã duyệt), chờ ngày chạy
        public const string AwaitingPayment = "AwaitingPayment";   // chờ trả phần còn lại trước khi chạy / chi phí phát sinh
        public const string InTransit = "InTransit";               // ngựa đang trên xe
        public const string AwaitingHandover = "AwaitingHandover"; // xe đã tới điểm trả, chờ ký nhận
        public const string Completed = "Completed";
        public const string Closed = "Closed";                     // bị từ chối / hủy / quá hạn

        public static readonly string[] All =
        {
            PendingReview, AwaitingDeposit, PreparingDossier, AwaitingTrip, Scheduled, AwaitingPayment, InTransit, AwaitingHandover, Completed, Closed
        };
    }

    public static class BookingHorseStatuses
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Pending, Approved, Cancelled };
    }

    // Gói bảo hiểm của từng con ngựa trong đơn (R-13)
    public static class InsurancePlans
    {
        public const string None = "None"; // không mua: trách nhiệm cơ bản, chỉ khi lỗi công ty
        public const string Full = "Full"; // toàn giá trị thỏa thuận

        public static readonly string[] All = { None, Full };
    }

    public static class TransportModes
    {
        public const string Ground = "Ground";
        public const string Air = "Air";

        public static readonly string[] All = { Ground, Air };
    }

    public static class StallClasses
    {
        public const string Shared = "Shared";
        public const string Comfort = "Comfort";
        public const string Private = "Private";

        public static readonly string[] All = { Shared, Comfort, Private };
    }

    public static class DossierStatuses
    {
        public const string Draft = "Draft";
        public const string AwaitingDocs = "AwaitingDocs";
        public const string Reviewing = "Reviewing";
        public const string SubmittedToAuthorities = "SubmittedToAuthorities";
        public const string Cleared = "Cleared";
        public const string Issue = "Issue";

        public static readonly string[] All = { Draft, AwaitingDocs, Reviewing, SubmittedToAuthorities, Cleared, Issue };
    }

    public static class DocumentStatuses
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";

        public const int MaxRejectionCount = 3;

        public static readonly string[] All = { Pending, Approved, Rejected };
    }

    // Tình trạng hiệu lực của hồ sơ thú y lưu theo ngựa (tính từ ExpiryDate, không lưu trong DB)
    public static class HorseDocumentValidity
    {
        public const string Valid = "Valid";
        public const string ExpiringSoon = "ExpiringSoon"; // còn <= ExpiringSoonDays ngày
        public const string Expired = "Expired";
        public const string NoExpiry = "NoExpiry";

        public const int ExpiringSoonDays = 30;
    }

    public static class RequirementDirections
    {
        public const string Export = "Export";
        public const string Import = "Import";
        public const string Transit = "Transit"; // nước ngựa đi qua (thường là đường bộ)

        public static readonly string[] All = { Export, Import, Transit };
    }

    public static class AssetTypes
    {
        public const string HorseTruckAirSuspension = "HorseTruck_AirSuspension";
        public const string HorseVan = "HorseVan";
        public const string AirStall = "AirStall";

        public static readonly string[] All = { HorseTruckAirSuspension, HorseVan, AirStall };
    }

    public static class AssetStatuses
    {
        public const string Available = "Available";
        public const string InTransit = "InTransit";
        public const string Maintenance = "Maintenance";

        public static readonly string[] All = { Available, InTransit, Maintenance };
    }

    public static class TripStatuses
    {
        public const string Draft = "Draft";
        public const string PendingApproval = "PendingApproval";
        public const string Scheduled = "Scheduled";
        public const string InTransit = "InTransit";
        public const string EmergencyRerouting = "EmergencyRerouting";
        public const string ArrivedDestination = "ArrivedDestination";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";

        public const int MaxPlanRejectionCount = 3;

        public static readonly string[] All = { Draft, PendingApproval, Scheduled, InTransit, EmergencyRerouting, ArrivedDestination, Completed, Cancelled };
    }

    public static class OnTimeStatuses
    {
        public const string OnTime = "OnTime";
        public const string DelayedForceMajeure = "Delayed_AcceptableForceMajeure";
        public const string DelayedOperationalFault = "Delayed_OperationalFault";
    }

    public static class CrewRoles
    {
        public const string Driver = "Driver";
        public const string Escort = "Escort";

        public static readonly string[] All = { Driver, Escort };
    }

    public static class CheckpointTypes
    {
        public const string Origin = "Origin";
        public const string PickUp = "PickUp";       // đón ngựa của một đơn (R-04); đơn không có mốc này lên xe ở Origin
        public const string RestStop = "RestStop";
        public const string BorderGate = "BorderGate";
        public const string DropOff = "DropOff";     // trả ngựa của một đơn giữa đường; đơn không có mốc này giao ở Destination
        public const string Destination = "Destination";

        public static readonly string[] All = { Origin, PickUp, RestStop, BorderGate, DropOff, Destination };

        // Mốc gắn với một đơn (cột booking_id bắt buộc)
        public static readonly string[] BookingStops = { PickUp, DropOff };
    }

    // Trạng thái riêng của từng con ngựa trên chuyến (R-05)
    public static class TripHorseStatuses
    {
        public const string Planned = "Planned";       // đã xếp chuyến, chưa lên xe
        public const string InTransit = "InTransit";   // xe đã rời điểm đón của con ngựa
        public const string Delivered = "Delivered";   // đã ký biên bản bàn giao
        public const string Offloaded = "Offloaded";   // rớt lại giữa đường (sự cố / thú y), quay về hàng chờ xếp chuyến
        public const string Cancelled = "Cancelled";   // chuyến bị hủy

        public static readonly string[] All = { Planned, InTransit, Delivered, Offloaded, Cancelled };

        // Con ngựa đang được một chuyến "giữ" (chưa thể xếp lên chuyến khác)
        public static readonly string[] Holding = { Planned, InTransit, Delivered };

        // Con ngựa còn phải chở / giao trên chuyến
        public static readonly string[] OnBoard = { Planned, InTransit };
    }

    public static class CheckpointStatuses
    {
        public const string Pending = "Pending";
        public const string Arrived = "Arrived";
        public const string Cleared = "Cleared";
        public const string Departed = "Departed";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Pending, Arrived, Cleared, Departed, Cancelled };
    }

    public static class FeedStatuses
    {
        public const string Normal = "Normal";
        public const string Reduced = "Reduced";
        public const string Refused = "Refused";

        public static readonly string[] All = { Normal, Reduced, Refused };
    }

    public static class StressLevels
    {
        public const string Calm = "Calm";
        public const string MildStress = "MildStress";
        public const string Agitated = "Agitated";

        public static readonly string[] All = { Calm, MildStress, Agitated };
    }

    public static class IncidentTypes
    {
        public const string MechanicalBreakdown = "MechanicalBreakdown";
        public const string BorderCongestion = "BorderCongestion";
        public const string EquineHealthIssue = "EquineHealthIssue";
        public const string Weather = "Weather";
        public const string ClimateControlFailure = "ClimateControlFailure";
        public const string CustomsHold = "CustomsHold";
        public const string Other = "Other";

        public static readonly string[] All = { MechanicalBreakdown, BorderCongestion, EquineHealthIssue, Weather, ClimateControlFailure, CustomsHold, Other };
    }

    // Loại hành động trong phương án xử lý sự cố
    public static class IncidentResponseActions
    {
        public const string WaitAndContinue = "WaitAndContinue";     // chờ tại chỗ rồi đi tiếp
        public const string RepairAndContinue = "RepairAndContinue"; // sửa xe / thiết bị rồi đi tiếp
        public const string Reroute = "Reroute";                     // đổi tuyến: bắt buộc kèm thay đổi tuyến
        public const string VeterinaryCare = "VeterinaryCare";       // dừng chăm sóc thú y
        public const string OffloadHorse = "OffloadHorse";           // cho ngựa rời chuyến
        public const string Other = "Other";

        public static readonly string[] All = { WaitAndContinue, RepairAndContinue, Reroute, VeterinaryCare, OffloadHorse, Other };
    }

    public static class IncidentSeverities
    {
        public const string Minor = "Minor";
        public const string Moderate = "Moderate";
        public const string Major = "Major";
        public const string Critical = "Critical";

        public static readonly string[] All = { Minor, Moderate, Major, Critical };
    }

    public static class IncidentStatuses
    {
        public const string Reported = "Reported";
        public const string PlanProposed = "PlanProposed";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Resolved = "Resolved";

        public static readonly string[] All = { Reported, PlanProposed, Approved, Rejected, Resolved };
    }

    public static class HandoverConditions
    {
        public const string Excellent = "Excellent";
        public const string NormalFatigue = "NormalFatigue";
        public const string Injured = "Injured";

        public static readonly string[] All = { Excellent, NormalFatigue, Injured };
    }

    public static class PriceCategories
    {
        public const string Freight = "Freight";
        public const string StallClass = "StallClass";
        public const string Surcharge = "Surcharge";
        public const string Discount = "Discount";
        public const string Clearance = "Clearance";
        public const string Escort = "Escort";
        public const string Insurance = "Insurance";

        public static readonly string[] All = { Freight, StallClass, Surcharge, Discount, Clearance, Escort, Insurance };
    }

    public static class PriceCalcTypes
    {
        public const string PerHorse = "PerHorse";
        public const string PerKmPerHorse = "PerKmPerHorse";
        public const string Multiplier = "Multiplier";
        public const string Percent = "Percent";
        public const string PerBooking = "PerBooking";
        public const string PerGroom = "PerGroom";
        public const string PerGroomDay = "PerGroomDay";

        public static readonly string[] All = { PerHorse, PerKmPerHorse, Multiplier, Percent, PerBooking, PerGroom, PerGroomDay };
    }
}
