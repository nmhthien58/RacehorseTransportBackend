-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT cho database đã dựng từ trước ngày 26/10/2026 (BE-11, R-12)
-- Ngựa rời chuyến chỉ qua sự cố, có hướng xử lý tiếp, thêm trạng thái Deceased:
--   - trip_horses: trạng thái Deceased, cột offload_outcome
--   - booking_horses: trạng thái Deceased
--   - trip_incidents: offload_outcome, vet_report_path, emergency_offload
--   - handover_acceptances: bỏ ràng buộc một biên bản / đơn / chuyến (giao tại chỗ cần biên bản riêng)
-- Database dựng mới bằng 01_schema.sql đã có sẵn các thay đổi này: không cần chạy file này.
-- Chạy lại nhiều lần không sao.
-- =====================================================================================

-- Ngựa trên chuyến
ALTER TABLE fleet.trip_horses ADD COLUMN IF NOT EXISTS offload_outcome varchar(20);

ALTER TABLE fleet.trip_horses DROP CONSTRAINT IF EXISTS ck_trip_horses_status;
ALTER TABLE fleet.trip_horses
    ADD CONSTRAINT ck_trip_horses_status
    CHECK (status IN ('Planned', 'InTransit', 'Delivered', 'Offloaded', 'Deceased', 'Cancelled'));

ALTER TABLE fleet.trip_horses DROP CONSTRAINT IF EXISTS ck_trip_horses_offload_outcome;
ALTER TABLE fleet.trip_horses
    ADD CONSTRAINT ck_trip_horses_offload_outcome
    CHECK (offload_outcome IN ('WaitRecover', 'ReturnToOrigin', 'HandOverHere', 'Deceased'));

-- Ngựa trong đơn
ALTER TABLE booking.booking_horses DROP CONSTRAINT IF EXISTS ck_booking_horses_status;
ALTER TABLE booking.booking_horses
    ADD CONSTRAINT ck_booking_horses_status
    CHECK (status IN ('Pending', 'Approved', 'Cancelled', 'Deceased'));

-- Sự cố
ALTER TABLE incident.trip_incidents
    ADD COLUMN IF NOT EXISTS offload_outcome varchar(20),
    ADD COLUMN IF NOT EXISTS vet_report_path varchar(500),
    ADD COLUMN IF NOT EXISTS emergency_offload boolean NOT NULL DEFAULT false;

ALTER TABLE incident.trip_incidents DROP CONSTRAINT IF EXISTS ck_trip_incidents_offload_outcome;
ALTER TABLE incident.trip_incidents
    ADD CONSTRAINT ck_trip_incidents_offload_outcome
    CHECK (offload_outcome IN ('WaitRecover', 'ReturnToOrigin', 'HandOverHere', 'Deceased'));

-- Biên bản bàn giao: một đơn có thể có nhiều biên bản trên cùng chuyến
ALTER TABLE acceptance.handover_acceptances DROP CONSTRAINT IF EXISTS uq_handover_acceptances_trip_booking;
CREATE INDEX IF NOT EXISTS ix_handover_acceptances_trip_booking ON acceptance.handover_acceptances (trip_id, booking_id);

-- Ngựa đã rời chuyến bằng nút "Rời chuyến" cũ được đưa về hàng chờ xếp chuyến: giữ nguyên nghĩa đó (WaitRecover).
-- Bỏ qua con được dỡ khẩn cấp qua sự cố (đang chờ phương án), để chạy lại script không ghi đè.
UPDATE fleet.trip_horses th
SET offload_outcome = 'WaitRecover'
WHERE th.status = 'Offloaded'
  AND th.offload_outcome IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM incident.trip_incidents i
      WHERE i.affected_trip_horse_id = th.trip_horse_id AND i.emergency_offload);
