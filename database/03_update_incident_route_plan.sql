-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT cho database đã dựng từ trước ngày 22/10/2026
-- Phương án xử lý sự cố có loại hành động và thay đổi tuyến đi kèm (Flow 5):
--   response_action: loại hành động; route_change: các mốc bỏ / thêm, áp vào lộ trình khi Manager duyệt.
-- Database dựng mới bằng 01_schema.sql đã có sẵn 2 cột này: không cần chạy file này.
-- Chạy lại nhiều lần không sao.
-- =====================================================================================

ALTER TABLE incident.trip_incidents
    ADD COLUMN IF NOT EXISTS response_action varchar(30),
    ADD COLUMN IF NOT EXISTS route_change jsonb;

ALTER TABLE incident.trip_incidents DROP CONSTRAINT IF EXISTS ck_trip_incidents_response_action;
ALTER TABLE incident.trip_incidents
    ADD CONSTRAINT ck_trip_incidents_response_action
    CHECK (response_action IN ('WaitAndContinue', 'RepairAndContinue', 'Reroute', 'VeterinaryCare', 'OffloadHorse', 'Other'));
