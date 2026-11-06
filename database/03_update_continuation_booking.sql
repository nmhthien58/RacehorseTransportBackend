-- =====================================================================================
-- SCRIPT 03: Đơn nối tiếp (sức khỏe ngựa, giai đoạn e; Chính sách mục 9.6)
-- Ngựa ở lại phòng khám khỏi bệnh -> đơn mới từ phòng khám tới đích cũ, liên kết đơn gốc.
-- is_free_of_charge = sự cố do lỗi công ty: báo giá chốt bằng 0.
-- Idempotent: chạy lại nhiều lần không sao. Database dựng mới bằng 01_schema.sql đã có sẵn.
-- =====================================================================================

ALTER TABLE booking.booking_requests
    ADD COLUMN IF NOT EXISTS parent_booking_id integer REFERENCES booking.booking_requests (booking_id),
    ADD COLUMN IF NOT EXISTS is_free_of_charge boolean NOT NULL DEFAULT false;

CREATE INDEX IF NOT EXISTS ix_booking_requests_parent ON booking.booking_requests (parent_booking_id);
