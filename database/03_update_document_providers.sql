-- R-19: phân rõ giấy nào khách nộp, giấy nào công ty xin.
-- Customer = giấy của con ngựa (hộ chiếu, tiêm phòng, xét nghiệm), khách tải lên và chuyên viên thẩm định.
-- Company  = giấy phép của cơ quan nhà nước (kiểm dịch xuất khẩu, nhập khẩu, quá cảnh), chuyên viên xin rồi tải lên.
-- Chạy được nhiều lần.

ALTER TABLE clearance.document_types
    ADD COLUMN IF NOT EXISTS provided_by varchar(10) NOT NULL DEFAULT 'Customer';

ALTER TABLE clearance.document_types DROP CONSTRAINT IF EXISTS ck_document_types_provided_by;
ALTER TABLE clearance.document_types
    ADD CONSTRAINT ck_document_types_provided_by CHECK (provided_by IN ('Customer', 'Company'));

UPDATE clearance.document_types
SET provided_by = 'Company'
WHERE code IN ('HEALTH_CERT', 'IMPORT_PERMIT', 'TRANSIT_PERMIT');
