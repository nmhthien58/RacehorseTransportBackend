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
        public const string EmailVerification = "EmailVerification";
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
        public const string Approved = "Approved";
        public const string Assigned = "Assigned";
        public const string Completed = "Completed";
        public const string Rejected = "Rejected";
        public const string Expired = "Expired";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Submitted, Approved, Assigned, Completed, Rejected, Expired, Cancelled };
    }

    public static class BookingHorseStatuses
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Pending, Approved, Cancelled };
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

    public static class RequirementDirections
    {
        public const string Export = "Export";
        public const string Import = "Import";

        public static readonly string[] All = { Export, Import };
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
        public const string RestStop = "RestStop";
        public const string BorderGate = "BorderGate";
        public const string Destination = "Destination";

        public static readonly string[] All = { Origin, RestStop, BorderGate, Destination };
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
