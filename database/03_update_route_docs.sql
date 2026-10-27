-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT cho database đã dựng từ trước ngày 27/10/2026 (BE-06, R-10)
-- Hạn nộp giấy theo tuyến, nhắc / hủy khi quá hạn, đổi tuyến do công ty (khách được hủy hoàn 100%).
-- Cần chạy sau 03_update_booking_contracts.sql (BE-20).
-- Database dựng mới bằng 01_schema.sql + 02_seed.sql đã có sẵn các thay đổi này: không cần chạy file này.
-- Chạy lại nhiều lần không sao.
-- =====================================================================================

ALTER TABLE booking.booking_requests
    ADD COLUMN IF NOT EXISTS docs_due_at timestamptz,
    ADD COLUMN IF NOT EXISTS docs_reminders_sent integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS route_change_cancel_until timestamptz;

INSERT INTO common.system_settings (setting_key, setting_value, description) VALUES
('Dossier.SubmitDays', '7', 'Số ngày khách có để nộp đủ giấy tờ, tính từ lúc đặt cọc')
ON CONFLICT (setting_key) DO NOTHING;
