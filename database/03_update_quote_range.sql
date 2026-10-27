-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT cho database đã dựng từ trước ngày 27/10/2026 (BE-03, R-10)
-- Khoảng giá ước tính lúc tạo đơn. Đơn cũ để trống (đã có estimated_cost).
-- Database dựng mới bằng 01_schema.sql đã có sẵn các cột này: không cần chạy file này.
-- Chạy lại nhiều lần không sao.
-- =====================================================================================

ALTER TABLE booking.booking_requests
    ADD COLUMN IF NOT EXISTS quote_min numeric(18,2),
    ADD COLUMN IF NOT EXISTS quote_max numeric(18,2);
