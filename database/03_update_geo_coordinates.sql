-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT TỌA ĐỘ VÀ QUÃNG ĐƯỜNG CHẶNG (BE-02 / R-11)
-- Thêm tọa độ điểm đón / trả cho đơn (booking_requests) và tọa độ, quãng đường từng chặng cho mốc (route_checkpoints).
-- Database dựng mới bằng 01_schema.sql đã có sẵn các cột này: không cần chạy file này.
-- Chạy lại nhiều lần không sao.
-- =====================================================================================

ALTER TABLE booking.booking_requests
    ADD COLUMN IF NOT EXISTS pickup_lat numeric(9,6),
    ADD COLUMN IF NOT EXISTS pickup_lng numeric(9,6),
    ADD COLUMN IF NOT EXISTS dropoff_lat numeric(9,6),
    ADD COLUMN IF NOT EXISTS dropoff_lng numeric(9,6);

ALTER TABLE fleet.route_checkpoints
    ADD COLUMN IF NOT EXISTS latitude numeric(9,6),
    ADD COLUMN IF NOT EXISTS longitude numeric(9,6),
    ADD COLUMN IF NOT EXISTS distance_km_from_previous numeric(10,2);
