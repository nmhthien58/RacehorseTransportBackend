-- =====================================================================================
-- SCRIPT 03: Ngựa ở lại phòng khám thú y (sức khỏe ngựa, giai đoạn d; Chính sách mục 9.6)
-- - trip_horses: phòng khám (tên, địa chỉ, SĐT, bác sĩ, tọa độ) + viện phí và ai chịu (Company | Customer)
-- Ngựa WaitRecover từ nay kết thúc đơn tại phòng khám (hoàn phần cước chưa đi theo nguyên nhân),
-- khỏi bệnh thì đi tiếp bằng đơn nối tiếp; không còn được xếp lại trong đơn cũ.
-- Idempotent: chạy lại nhiều lần không sao. Database dựng mới bằng 01_schema.sql đã có sẵn.
-- =====================================================================================

ALTER TABLE fleet.trip_horses
    ADD COLUMN IF NOT EXISTS vet_clinic_name     varchar(150),
    ADD COLUMN IF NOT EXISTS vet_clinic_address  varchar(255),
    ADD COLUMN IF NOT EXISTS vet_clinic_phone    varchar(30),
    ADD COLUMN IF NOT EXISTS attending_vet_name  varchar(100),
    ADD COLUMN IF NOT EXISTS vet_clinic_lat      numeric(9,6),
    ADD COLUMN IF NOT EXISTS vet_clinic_lng      numeric(9,6),
    ADD COLUMN IF NOT EXISTS vet_expense_amount  numeric(18,2),
    ADD COLUMN IF NOT EXISTS vet_expense_bearer  varchar(15),
    ADD COLUMN IF NOT EXISTS vet_expense_note    varchar(500);

ALTER TABLE fleet.trip_horses DROP CONSTRAINT IF EXISTS ck_trip_horses_vet_expense_bearer;
ALTER TABLE fleet.trip_horses
    ADD CONSTRAINT ck_trip_horses_vet_expense_bearer CHECK (vet_expense_bearer IN ('Company', 'Customer'));
ALTER TABLE fleet.trip_horses DROP CONSTRAINT IF EXISTS ck_trip_horses_vet_expense_amount;
ALTER TABLE fleet.trip_horses
    ADD CONSTRAINT ck_trip_horses_vet_expense_amount CHECK (vet_expense_amount >= 0);
