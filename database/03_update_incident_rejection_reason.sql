-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT cho database đã dựng từ trước ngày 08/10/2026
-- Thêm cột lưu lý do Manager từ chối phương án xử lý sự cố (Flow 5).
-- Database dựng mới bằng 01_schema.sql đã có sẵn cột này: không cần chạy file này.
-- Chạy lại nhiều lần không sao.
-- =====================================================================================

ALTER TABLE incident.trip_incidents
    ADD COLUMN IF NOT EXISTS rejection_reason varchar(500);
