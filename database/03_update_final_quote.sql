-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT cho database đã dựng từ trước ngày 27/10/2026 (BE-05, R-10)
-- Báo giá chốt, hạn chấp nhận, đồng ý chính sách; trạng thái đơn Quoted / QuoteExpired.
-- Cần chạy sau 03_update_route_plans.sql (BE-04).
-- Database dựng mới bằng 01_schema.sql đã có sẵn các thay đổi này: không cần chạy file này.
-- Chạy lại nhiều lần không sao.
-- =====================================================================================

ALTER TABLE booking.booking_requests
    ADD COLUMN IF NOT EXISTS quoted_at timestamptz,
    ADD COLUMN IF NOT EXISTS quote_expires_at timestamptz,
    ADD COLUMN IF NOT EXISTS quote_reminder_sent_at timestamptz,
    ADD COLUMN IF NOT EXISTS quote_accepted_at timestamptz,
    ADD COLUMN IF NOT EXISTS policy_version varchar(20),
    ADD COLUMN IF NOT EXISTS policy_accepted_at timestamptz;

ALTER TABLE booking.booking_requests DROP CONSTRAINT IF EXISTS ck_booking_requests_status;
ALTER TABLE booking.booking_requests
    ADD CONSTRAINT ck_booking_requests_status CHECK (status IN ('Submitted', 'PreApproved', 'RoutePlanned', 'Quoted', 'AwaitingDeposit', 'Approved', 'Assigned', 'AwaitingFinalPayment', 'Completed', 'Rejected', 'Expired', 'QuoteExpired', 'Cancelled'));
