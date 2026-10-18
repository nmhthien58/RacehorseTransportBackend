-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT cho database đã dựng trước ngày 19/10/2026 (R-06 phân công escort theo chuồng)
--   - trip_horses: cột escort_user_id = escort phụ trách con ngựa (1 escort tối đa 3 con)
--   - Điền cho dữ liệu cũ: ngựa xếp theo số ô chuồng, cứ 3 con giao cho một escort của chuyến
--     (trưởng nhóm escort trước); thiếu escort thì phần dư giao cho escort cuối cùng
-- Database dựng mới bằng 01_schema.sql + 02_seed.sql đã có sẵn: không cần chạy file này.
-- Chạy được TRƯỚC khi deploy bản backend có R-06 (bản cũ không đọc cột mới nên vẫn chạy bình thường).
-- Chạy lại nhiều lần không sao.
-- =====================================================================================

ALTER TABLE fleet.trip_horses ADD COLUMN IF NOT EXISTS escort_user_id integer;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_trip_horses_escort') THEN
        ALTER TABLE fleet.trip_horses ADD CONSTRAINT fk_trip_horses_escort
            FOREIGN KEY (escort_user_id) REFERENCES identity.users (user_id);
    END IF;
END $$;

WITH escorts AS (
    SELECT trip_id, user_id,
           ROW_NUMBER() OVER (PARTITION BY trip_id ORDER BY is_lead DESC, trip_crew_id) AS escort_no,
           COUNT(*)     OVER (PARTITION BY trip_id) AS escort_count
    FROM fleet.trip_crew
    WHERE crew_role = 'Escort'
),
horses AS (
    SELECT trip_horse_id, trip_id,
           ROW_NUMBER() OVER (PARTITION BY trip_id ORDER BY stall_slot_number) AS horse_no
    FROM fleet.trip_horses
)
UPDATE fleet.trip_horses th
SET escort_user_id = e.user_id
FROM horses h
JOIN escorts e ON e.trip_id = h.trip_id
              AND e.escort_no = LEAST((h.horse_no + 2) / 3, e.escort_count)
WHERE th.trip_horse_id = h.trip_horse_id
  AND th.escort_user_id IS NULL;
