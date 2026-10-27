-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT cho database đã dựng từ trước ngày 27/10/2026 (BE-15, R-16)
-- Người nhận ngựa trên đơn, xác nhận có mặt, khung giờ đã báo người nhận, người nhận thay.
-- Cần chạy sau 03_update_route_docs.sql (BE-06).
-- Database dựng mới bằng 01_schema.sql đã có sẵn các thay đổi này: không cần chạy file này.
-- Chạy lại nhiều lần không sao.
-- =====================================================================================

ALTER TABLE booking.booking_requests
    ADD COLUMN IF NOT EXISTS recipient_name                   varchar(100),
    ADD COLUMN IF NOT EXISTS recipient_phone                  varchar(20),
    ADD COLUMN IF NOT EXISTS recipient_email                  varchar(255),
    ADD COLUMN IF NOT EXISTS recipient_id_number              varchar(50),
    ADD COLUMN IF NOT EXISTS recipient_is_customer            boolean       NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS recipient_confirm_requested_at   timestamptz,
    ADD COLUMN IF NOT EXISTS recipient_confirmed_at           timestamptz,
    ADD COLUMN IF NOT EXISTS recipient_notified_window_start  timestamptz,
    ADD COLUMN IF NOT EXISTS recipient_notified_window_end    timestamptz,
    ADD COLUMN IF NOT EXISTS alternate_recipient_name         varchar(100),
    ADD COLUMN IF NOT EXISTS alternate_recipient_id_number    varchar(50),
    ADD COLUMN IF NOT EXISTS alternate_recipient_approved_at  timestamptz;
