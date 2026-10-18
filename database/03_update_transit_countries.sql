-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT cho database đã dựng trước ngày 18/10/2026 (R-03 nước quá cảnh)
--   - Bảng booking.booking_transit_countries (nước ngựa đi qua của từng đơn)
--   - country_doc_requirements: hướng 'Transit' + cột processing_days (số ngày cần để có giấy)
--   - Loại giấy TRANSIT_PERMIT, yêu cầu quá cảnh của CN / LA / KH / TH / MY, yêu cầu nhập khẩu Nga
--   - Giá: phí thủ tục quá cảnh FEE_TRANSIT, cước đường bộ đi Nga; đổi tên phụ phí gấp
-- Database dựng mới bằng 01_schema.sql + 02_seed.sql đã có sẵn: không cần chạy file này.
-- Chạy được TRƯỚC khi deploy bản backend có R-03 (bản cũ không đọc các cột / bảng mới nên vẫn chạy bình thường).
-- Chạy lại nhiều lần không sao.
-- =====================================================================================

CREATE TABLE IF NOT EXISTS booking.booking_transit_countries (
    booking_id      integer NOT NULL REFERENCES booking.booking_requests (booking_id),
    sequence_order  integer NOT NULL CONSTRAINT ck_booking_transit_countries_seq CHECK (sequence_order > 0),
    country_code    char(2) NOT NULL,
    CONSTRAINT pk_booking_transit_countries PRIMARY KEY (booking_id, sequence_order),
    CONSTRAINT uq_booking_transit_countries UNIQUE (booking_id, country_code)
);

ALTER TABLE clearance.country_doc_requirements DROP CONSTRAINT IF EXISTS ck_country_doc_requirements_direction;
ALTER TABLE clearance.country_doc_requirements ADD CONSTRAINT ck_country_doc_requirements_direction
    CHECK (direction IN ('Export', 'Import', 'Transit'));

ALTER TABLE clearance.country_doc_requirements ADD COLUMN IF NOT EXISTS processing_days integer;
ALTER TABLE clearance.country_doc_requirements DROP CONSTRAINT IF EXISTS ck_country_doc_requirements_processing;
ALTER TABLE clearance.country_doc_requirements ADD CONSTRAINT ck_country_doc_requirements_processing
    CHECK (processing_days >= 0);

INSERT INTO clearance.document_types (code, name, description, is_mandatory)
SELECT 'TRANSIT_PERMIT', 'Giấy phép quá cảnh động vật',
       'Cơ quan thú y của nước ngựa đi qua cấp trước khi xuất phát, ghi rõ cửa khẩu vào / ra', false
WHERE NOT EXISTS (SELECT 1 FROM clearance.document_types WHERE code = 'TRANSIT_PERMIT');

INSERT INTO clearance.country_doc_requirements (country_code, direction, doc_type_id, validity_days, is_mandatory, regulation_note)
SELECT c.country_code, c.direction, d.doc_type_id, c.validity_days, c.is_mandatory, c.note
FROM (VALUES
    ('RU', 'Import', 'HORSE_PASSPORT',     NULL, true,  'Hộ chiếu ngựa còn hiệu lực'),
    ('RU', 'Import', 'IMPORT_PERMIT',      NULL, true,  'Giấy phép nhập khẩu của Rosselkhoznadzor'),
    ('RU', 'Import', 'HEALTH_CERT',        5,    true,  'Chứng nhận thú y theo mẫu Liên minh Kinh tế Á-Âu'),
    ('RU', 'Import', 'COGGINS_EIA',        30,   true,  'EIA âm tính trong 30 ngày'),
    ('RU', 'Import', 'GLANDERS_TEST',      30,   true,  'Glanders CFT âm tính trong 30 ngày'),
    ('RU', 'Import', 'PIROPLASMOSIS_TEST', 30,   true,  'Piroplasmosis âm tính trong 30 ngày'),
    ('RU', 'Import', 'VACCINE',            180,  true,  'Tiêm phòng cúm ngựa trong 6 tháng'),
    ('CN', 'Transit', 'TRANSIT_PERMIT',    NULL, true,  'Giấy phép quá cảnh của GACC, ghi cửa khẩu vào / ra'),
    ('CN', 'Transit', 'HEALTH_CERT',       10,   true,  'Chứng nhận kiểm dịch còn hạn khi vào cửa khẩu Trung Quốc'),
    ('LA', 'Transit', 'TRANSIT_PERMIT',    NULL, true,  'Giấy phép quá cảnh của Cục Chăn nuôi và Thú y Lào'),
    ('LA', 'Transit', 'HEALTH_CERT',       10,   true,  'Chứng nhận kiểm dịch còn hạn khi vào Lào'),
    ('KH', 'Transit', 'TRANSIT_PERMIT',    NULL, true,  'Giấy phép quá cảnh của Tổng cục Sản xuất và Thú y Campuchia'),
    ('KH', 'Transit', 'HEALTH_CERT',       10,   true,  'Chứng nhận kiểm dịch còn hạn khi vào Campuchia'),
    ('TH', 'Transit', 'TRANSIT_PERMIT',    NULL, true,  'Giấy phép quá cảnh của Cục Phát triển Chăn nuôi Thái Lan'),
    ('TH', 'Transit', 'HEALTH_CERT',       10,   true,  'Chứng nhận kiểm dịch còn hạn khi vào Thái Lan'),
    ('MY', 'Transit', 'TRANSIT_PERMIT',    NULL, true,  'Giấy phép quá cảnh của DVS Malaysia'),
    ('MY', 'Transit', 'HEALTH_CERT',       7,    true,  'Chứng nhận kiểm dịch còn hạn khi vào Malaysia')
) AS c (country_code, direction, doc_code, validity_days, is_mandatory, note)
JOIN clearance.document_types d ON d.code = c.doc_code
ON CONFLICT (country_code, direction, doc_type_id) DO NOTHING;

-- Chỉ điền số ngày cho dòng chưa có (không ghi đè số liệu Admin đã sửa)
UPDATE clearance.country_doc_requirements r
SET processing_days = CASE
        WHEN d.code = 'IMPORT_PERMIT' AND r.country_code IN ('AU', 'JP') THEN 30
        WHEN d.code = 'IMPORT_PERMIT' AND r.country_code IN ('CN', 'RU') THEN 21
        WHEN d.code IN ('IMPORT_PERMIT', 'TRANSIT_PERMIT') THEN 14
        WHEN d.code IN ('COGGINS_EIA', 'GLANDERS_TEST', 'PIROPLASMOSIS_TEST', 'EVA_TEST') THEN 7
        WHEN d.code = 'HEALTH_CERT' THEN 3
        ELSE NULL
    END
FROM clearance.document_types d
WHERE d.doc_type_id = r.doc_type_id
  AND r.processing_days IS NULL;

INSERT INTO pricing.price_items (category, item_code, item_name, transport_mode, dest_country_code, calc_type, value, min_horses)
SELECT 'Freight', 'FREIGHT', 'Zone G2 - Đường bộ (Nga)', 'Ground', 'RU', 'PerKmPerHorse', 1.10, NULL
WHERE NOT EXISTS (SELECT 1 FROM pricing.price_items WHERE item_code = 'FREIGHT' AND transport_mode = 'Ground' AND dest_country_code = 'RU');

INSERT INTO pricing.price_items (category, item_code, item_name, transport_mode, dest_country_code, calc_type, value, min_horses)
SELECT 'Clearance', 'FEE_TRANSIT', 'Phí thủ tục quá cảnh (mỗi nước đi qua)', NULL, NULL, 'PerHorse', 200, NULL
WHERE NOT EXISTS (SELECT 1 FROM pricing.price_items WHERE item_code = 'FEE_TRANSIT');

UPDATE pricing.price_items
SET item_name = 'Phụ phí gấp (hồ sơ khẩn / hạn giao sát)'
WHERE item_code = 'SUR_EXPRESS' AND item_name = 'Phụ phí gấp (khởi hành dưới 7 ngày)';
