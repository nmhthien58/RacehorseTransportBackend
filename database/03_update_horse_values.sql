-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT cho database đã dựng từ trước ngày 27/10/2026 (BE-09, R-12 / R-13)
-- Giá trị từng con ngựa trong đơn: giá trị khai báo, giá trị thỏa thuận, chứng từ, gói bảo hiểm.
-- Đơn cũ giữ booking_requests.declared_value (tổng) như trước; các cột mới để trống.
-- Database dựng mới bằng 01_schema.sql đã có sẵn các cột này: không cần chạy file này.
-- Chạy lại nhiều lần không sao.
-- =====================================================================================

ALTER TABLE booking.booking_horses
    ADD COLUMN IF NOT EXISTS declared_value numeric(18,2),
    ADD COLUMN IF NOT EXISTS agreed_value numeric(18,2),
    ADD COLUMN IF NOT EXISTS value_proof_path varchar(500),
    ADD COLUMN IF NOT EXISTS insurance_plan varchar(10) NOT NULL DEFAULT 'None';

ALTER TABLE booking.booking_horses DROP CONSTRAINT IF EXISTS ck_booking_horses_declared;
ALTER TABLE booking.booking_horses ADD CONSTRAINT ck_booking_horses_declared CHECK (declared_value >= 0);

ALTER TABLE booking.booking_horses DROP CONSTRAINT IF EXISTS ck_booking_horses_agreed;
ALTER TABLE booking.booking_horses ADD CONSTRAINT ck_booking_horses_agreed CHECK (agreed_value >= 0);

ALTER TABLE booking.booking_horses DROP CONSTRAINT IF EXISTS ck_booking_horses_insurance;
ALTER TABLE booking.booking_horses ADD CONSTRAINT ck_booking_horses_insurance CHECK (insurance_plan IN ('None', 'Full'));
