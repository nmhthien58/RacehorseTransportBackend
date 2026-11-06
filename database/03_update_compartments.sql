-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT cho database đã dựng từ trước ngày 05/11/2026
-- Xe chia khoang (mỗi khoang 3 vị trí, một khoang một hạng chuồng: Shared 3, Comfort 2, Private 1):
--   transport_assets.compartment_count: số khoang của xe (1-4), dữ liệu cũ = ceil(capacity_horses / 3)
--   trip_horses.compartment_no / position_no: khoang và vị trí trong khoang, suy từ stall_slot_number
--   (ô n = khoang ceil(n / 3), vị trí ((n - 1) % 3) + 1). stall_slot_number giữ nguyên.
-- Database dựng mới bằng 01_schema.sql đã có sẵn các cột này: không cần chạy file này.
-- Chạy lại nhiều lần không sao.
-- =====================================================================================

ALTER TABLE fleet.transport_assets ADD COLUMN IF NOT EXISTS compartment_count integer;

UPDATE fleet.transport_assets
SET compartment_count = LEAST(4, GREATEST(1, (capacity_horses + 2) / 3))
WHERE compartment_count IS NULL;

ALTER TABLE fleet.transport_assets ALTER COLUMN compartment_count SET NOT NULL;
ALTER TABLE fleet.transport_assets DROP CONSTRAINT IF EXISTS ck_transport_assets_compartments;
ALTER TABLE fleet.transport_assets
    ADD CONSTRAINT ck_transport_assets_compartments CHECK (compartment_count BETWEEN 1 AND 4);

ALTER TABLE fleet.trip_horses
    ADD COLUMN IF NOT EXISTS compartment_no integer,
    ADD COLUMN IF NOT EXISTS position_no integer;

UPDATE fleet.trip_horses
SET compartment_no = (GREATEST(stall_slot_number, 1) - 1) / 3 + 1,
    position_no    = (GREATEST(stall_slot_number, 1) - 1) % 3 + 1
WHERE compartment_no IS NULL OR position_no IS NULL;

ALTER TABLE fleet.trip_horses ALTER COLUMN compartment_no SET NOT NULL;
ALTER TABLE fleet.trip_horses ALTER COLUMN position_no SET NOT NULL;
ALTER TABLE fleet.trip_horses DROP CONSTRAINT IF EXISTS ck_trip_horses_compartment;
ALTER TABLE fleet.trip_horses
    ADD CONSTRAINT ck_trip_horses_compartment CHECK (compartment_no >= 1);
ALTER TABLE fleet.trip_horses DROP CONSTRAINT IF EXISTS ck_trip_horses_position;
ALTER TABLE fleet.trip_horses
    ADD CONSTRAINT ck_trip_horses_position CHECK (position_no BETWEEN 1 AND 3);
