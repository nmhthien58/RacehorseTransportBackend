-- R-14 (BE-14a): phương án đổi xe / đổi nhân sự trong sự cố.
-- Idempotent: có thể chạy nhiều lần trên database hiện có.

ALTER TABLE incident.trip_incidents
    ADD COLUMN IF NOT EXISTS action_payload jsonb;

ALTER TABLE incident.trip_incidents
    DROP CONSTRAINT IF EXISTS ck_trip_incidents_response_action;

ALTER TABLE incident.trip_incidents
    ADD CONSTRAINT ck_trip_incidents_response_action
    CHECK (response_action IN ('WaitAndContinue', 'RepairAndContinue', 'Reroute', 'VeterinaryCare', 'OffloadHorse', 'ReplaceVehicle', 'ReplaceCrew', 'ReturnToOrigin', 'HoldAtStable', 'Other'));
