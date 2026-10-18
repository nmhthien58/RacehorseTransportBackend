-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT cho database đã dựng trước ngày 19/10/2026 (R-04 ghép nhiều đơn vào một chuyến, R-05 trạng thái từng ngựa)
--   - route_checkpoints: loại mốc mới PickUp / DropOff (đón / trả ngựa của một đơn) + cột booking_id
--   - trip_horses: trạng thái riêng từng con (Planned / InTransit / Delivered / Offloaded / Cancelled)
--     và các mốc thời gian đón, giao, rớt lại; handover_id trỏ tới biên bản e-POD
--   - handover_acceptances: cột checkpoint_id (mốc nơi ký nhận)
--   - Điền trạng thái cho dữ liệu cũ theo trạng thái chuyến và biên bản đã ký
-- Database dựng mới bằng 01_schema.sql + 02_seed.sql đã có sẵn: không cần chạy file này.
-- Chạy được TRƯỚC khi deploy bản backend có R-04 / R-05 (bản cũ không đọc các cột mới nên vẫn chạy bình thường).
-- Chạy lại nhiều lần không sao.
-- =====================================================================================

-- ---------- Mốc lộ trình ----------
ALTER TABLE fleet.route_checkpoints ADD COLUMN IF NOT EXISTS booking_id integer;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_route_checkpoints_booking') THEN
        ALTER TABLE fleet.route_checkpoints ADD CONSTRAINT fk_route_checkpoints_booking
            FOREIGN KEY (booking_id) REFERENCES booking.booking_requests (booking_id);
    END IF;
END $$;

ALTER TABLE fleet.route_checkpoints DROP CONSTRAINT IF EXISTS ck_route_checkpoints_type;
ALTER TABLE fleet.route_checkpoints ADD CONSTRAINT ck_route_checkpoints_type
    CHECK (checkpoint_type IN ('Origin', 'PickUp', 'RestStop', 'BorderGate', 'DropOff', 'Destination'));

ALTER TABLE fleet.route_checkpoints DROP CONSTRAINT IF EXISTS ck_route_checkpoints_booking;
ALTER TABLE fleet.route_checkpoints ADD CONSTRAINT ck_route_checkpoints_booking
    CHECK ((checkpoint_type IN ('PickUp', 'DropOff')) = (booking_id IS NOT NULL));

-- ---------- Biên bản bàn giao ----------
ALTER TABLE acceptance.handover_acceptances ADD COLUMN IF NOT EXISTS checkpoint_id integer;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_handover_acceptances_checkpoint') THEN
        ALTER TABLE acceptance.handover_acceptances ADD CONSTRAINT fk_handover_acceptances_checkpoint
            FOREIGN KEY (checkpoint_id) REFERENCES fleet.route_checkpoints (checkpoint_id);
    END IF;
END $$;

-- Biên bản cũ đều ký ở mốc cuối của chuyến
UPDATE acceptance.handover_acceptances h
SET checkpoint_id = c.checkpoint_id
FROM fleet.route_checkpoints c
WHERE h.checkpoint_id IS NULL
  AND c.trip_id = h.trip_id
  AND c.checkpoint_type = 'Destination'
  AND c.is_active;

-- ---------- Ngựa trên chuyến ----------
ALTER TABLE fleet.trip_horses ADD COLUMN IF NOT EXISTS status varchar(20) NOT NULL DEFAULT 'Planned';
ALTER TABLE fleet.trip_horses ADD COLUMN IF NOT EXISTS picked_up_at timestamptz;
ALTER TABLE fleet.trip_horses ADD COLUMN IF NOT EXISTS delivered_at timestamptz;
ALTER TABLE fleet.trip_horses ADD COLUMN IF NOT EXISTS handover_id integer;
ALTER TABLE fleet.trip_horses ADD COLUMN IF NOT EXISTS offloaded_at timestamptz;
ALTER TABLE fleet.trip_horses ADD COLUMN IF NOT EXISTS offload_reason varchar(300);

ALTER TABLE fleet.trip_horses DROP CONSTRAINT IF EXISTS ck_trip_horses_status;
ALTER TABLE fleet.trip_horses ADD CONSTRAINT ck_trip_horses_status
    CHECK (status IN ('Planned', 'InTransit', 'Delivered', 'Offloaded', 'Cancelled'));

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_trip_horses_handover') THEN
        ALTER TABLE fleet.trip_horses ADD CONSTRAINT fk_trip_horses_handover
            FOREIGN KEY (handover_id) REFERENCES acceptance.handover_acceptances (handover_id);
    END IF;
END $$;

-- Điền trạng thái cho dữ liệu cũ (chỉ những dòng còn mặc định Planned)
-- 1. Chuyến đã hủy -> ngựa Cancelled
UPDATE fleet.trip_horses th
SET status = 'Cancelled'
FROM fleet.trips t
WHERE th.trip_id = t.trip_id AND th.status = 'Planned' AND t.overall_status = 'Cancelled';

-- 2. Đơn đã ký biên bản trên chuyến -> ngựa Delivered
UPDATE fleet.trip_horses th
SET status = 'Delivered',
    delivered_at = h.handover_date_time,
    handover_id = h.handover_id,
    picked_up_at = COALESCE(th.picked_up_at, t.actual_start_date)
FROM booking.booking_horses bh, acceptance.handover_acceptances h, fleet.trips t
WHERE th.booking_horse_id = bh.booking_horse_id
  AND h.trip_id = th.trip_id
  AND h.booking_id = bh.booking_id
  AND t.trip_id = th.trip_id
  AND th.status = 'Planned';

-- 3. Chuyến đã xuất phát mà chưa bàn giao -> ngựa đang trên xe
UPDATE fleet.trip_horses th
SET status = 'InTransit',
    picked_up_at = COALESCE(th.picked_up_at, t.actual_start_date)
FROM fleet.trips t
WHERE th.trip_id = t.trip_id
  AND th.status = 'Planned'
  AND t.overall_status IN ('InTransit', 'EmergencyRerouting', 'ArrivedDestination', 'Completed');
