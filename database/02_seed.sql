-- =====================================================================================
-- TẬP LỆNH 02: DỮ LIỆU MẪU (chạy sau tập lệnh 01, trên cơ sở dữ liệu còn trống)
-- Hệ thống: Hệ thống vận chuyển ngựa đua xuyên biên giới (SWP391 - Nhóm 5)
--   A. Danh mục: loại giấy tờ, quy định theo quốc gia, bảng giá, phương tiện
--   B. Tài khoản môi trường phát triển (6 vai trò), nhân sự bổ sung và hồ sơ ngựa của 3 khách hàng
--   C. Một kịch bản đã chạy trọn 6 luồng: BKG-2026-0001 / TRP-2026-0001 (Hà Nội -> Quảng Châu)
-- Mật khẩu chung của mọi tài khoản dữ liệu mẫu: Racehorse@123
-- CHỈ dùng cho môi trường phát triển/trình diễn.
-- =====================================================================================

-- ---------- A.1 Loại giấy tờ ----------
-- Danh mục theo mẫu giấy chứng nhận vận chuyển ngựa quốc tế của WOAH và yêu cầu nhập khẩu công bố của
-- một số nước (Singapore AVS, Hong Kong AFCD...). Dữ liệu minh họa cho đồ án, không thay thế quy định hiện hành.
-- is_mandatory: loại giấy mặc định bắt buộc khi tuyến đi chưa có quy định riêng trong country_doc_requirements.
INSERT INTO clearance.document_types (doc_type_id, code, name, description, is_mandatory, provided_by) VALUES
(1, 'HORSE_PASSPORT',     'Hộ chiếu ngựa',                                   'Hộ chiếu định danh cá thể (FEI hoặc hộ chiếu quốc gia có số UELN), có sơ đồ nhận dạng và lịch tiêm phòng', true, 'Customer'),
(2, 'COGGINS_EIA',        'Xét nghiệm thiếu máu truyền nhiễm (EIA/Coggins)', 'Kết quả âm tính bằng AGID hoặc ELISA, lấy mẫu trong vòng 30 ngày trước ngày đi', true, 'Customer'),
(3, 'HEALTH_CERT',        'Giấy chứng nhận kiểm dịch xuất khẩu',             'Cơ quan thú y nước xuất khẩu cấp theo mẫu của nước nhập khẩu (ở Việt Nam: Cục Chăn nuôi và Thú y)', true, 'Company'),
(4, 'VACCINE',            'Chứng nhận tiêm phòng cúm ngựa',                  'Mũi nhắc gần nhất trong vòng 6 tháng và không tiêm trong 7 ngày trước ngày đi', true, 'Customer'),
(5, 'IMPORT_PERMIT',      'Giấy phép nhập khẩu',                             'Cơ quan thú y nước nhập khẩu cấp trước khi xuất phát (ví dụ: AVS Singapore, AFCD Hong Kong)', false, 'Company'),
(6, 'GLANDERS_TEST',      'Xét nghiệm bệnh tỵ (Glanders)',                   'Kết quả âm tính bằng phản ứng kết hợp bổ thể (CFT)', false, 'Customer'),
(7, 'PIROPLASMOSIS_TEST', 'Xét nghiệm lê dạng trùng (Piroplasmosis)',        'Âm tính với Theileria equi và Babesia caballi (cELISA hoặc IFAT)', false, 'Customer'),
(8, 'EVA_TEST',           'Xét nghiệm viêm động mạch do virus (EVA)',        'Áp dụng cho ngựa đực chưa thiến ở các nước yêu cầu', false, 'Customer'),
(9, 'TRANSIT_PERMIT',     'Giấy phép quá cảnh động vật',                     'Cơ quan thú y của nước ngựa đi qua cấp trước khi xuất phát, ghi rõ cửa khẩu vào / ra', false, 'Company'),
-- BE-10: bắt buộc cho mọi ngựa nhưng kiểm lúc xuất phát (TripExecutionService), không nằm trong checklist hồ sơ
-- nên is_mandatory = false; khách nộp vào hồ sơ thú y của ngựa (racehorse.horse_documents) trong 48 giờ trước giờ đi
(10, 'FIT_EXAM',          'Khám lâm sàng đủ sức khỏe',                        'Bác sĩ thú y khám lâm sàng, xác nhận ngựa đủ sức khỏe để vận chuyển; cấp trong 48 giờ trước giờ xuất phát', false, 'Customer');

-- ---------- A.2 Quy định giấy tờ theo quốc gia ----------
-- validity_days: giấy phải được cấp / lấy mẫu trong vòng N ngày trước ngày đi (NULL = còn hiệu lực là được)
INSERT INTO clearance.country_doc_requirements (country_code, direction, doc_type_id, validity_days, is_mandatory, regulation_note)
SELECT c.country_code, c.direction, d.doc_type_id, c.validity_days, c.is_mandatory, c.note
FROM (VALUES
    -- Xuất khẩu từ Việt Nam
    ('VN', 'Export', 'HORSE_PASSPORT',     NULL, true,  'Hộ chiếu ngựa còn hiệu lực'),
    ('VN', 'Export', 'HEALTH_CERT',        10,   true,  'Cục Chăn nuôi và Thú y cấp trong 10 ngày trước ngày đi'),
    -- Đông Nam Á
    ('TH', 'Import', 'HORSE_PASSPORT',     NULL, true,  'Hộ chiếu ngựa còn hiệu lực'),
    ('TH', 'Import', 'IMPORT_PERMIT',      NULL, true,  'Giấy phép nhập khẩu của Cục Phát triển Chăn nuôi Thái Lan'),
    ('TH', 'Import', 'HEALTH_CERT',        10,   true,  'Chứng nhận kiểm dịch trong 10 ngày trước khi nhập cảnh'),
    ('TH', 'Import', 'COGGINS_EIA',        30,   true,  'EIA âm tính trong 30 ngày'),
    ('TH', 'Import', 'GLANDERS_TEST',      30,   true,  'Glanders CFT âm tính trong 30 ngày'),
    ('TH', 'Import', 'VACCINE',            180,  true,  'Tiêm phòng cúm ngựa trong 6 tháng'),
    ('SG', 'Import', 'HORSE_PASSPORT',     NULL, true,  'Hộ chiếu ngựa được AVS chấp nhận'),
    ('SG', 'Import', 'IMPORT_PERMIT',      NULL, true,  'Giấy phép nhập khẩu AVS cho từng chuyến'),
    ('SG', 'Import', 'HEALTH_CERT',        7,    true,  'Chứng nhận kiểm dịch trong 7 ngày trước khi xuất'),
    ('SG', 'Import', 'COGGINS_EIA',        30,   true,  'EIA âm tính trong 30 ngày'),
    ('SG', 'Import', 'GLANDERS_TEST',      30,   true,  'Glanders CFT âm tính trong 30 ngày'),
    ('SG', 'Import', 'VACCINE',            180,  true,  'Tiêm phòng cúm ngựa trong 6 tháng'),
    ('MY', 'Import', 'HORSE_PASSPORT',     NULL, true,  'Hộ chiếu ngựa còn hiệu lực'),
    ('MY', 'Import', 'IMPORT_PERMIT',      NULL, true,  'Giấy phép nhập khẩu của DVS Malaysia'),
    ('MY', 'Import', 'HEALTH_CERT',        7,    true,  'Chứng nhận kiểm dịch trong 7 ngày trước khi xuất'),
    ('MY', 'Import', 'COGGINS_EIA',        30,   true,  'EIA âm tính trong 30 ngày'),
    ('MY', 'Import', 'GLANDERS_TEST',      30,   true,  'Glanders CFT âm tính trong 30 ngày'),
    ('MY', 'Import', 'VACCINE',            180,  true,  'Tiêm phòng cúm ngựa trong 6 tháng'),
    ('KH', 'Import', 'HORSE_PASSPORT',     NULL, true,  'Hộ chiếu ngựa còn hiệu lực'),
    ('KH', 'Import', 'HEALTH_CERT',        10,   true,  'Chứng nhận kiểm dịch trong 10 ngày trước khi nhập cảnh'),
    ('KH', 'Import', 'COGGINS_EIA',        30,   true,  'EIA âm tính trong 30 ngày'),
    ('KH', 'Import', 'VACCINE',            180,  true,  'Tiêm phòng cúm ngựa trong 6 tháng'),
    ('LA', 'Import', 'HORSE_PASSPORT',     NULL, true,  'Hộ chiếu ngựa còn hiệu lực'),
    ('LA', 'Import', 'HEALTH_CERT',        10,   true,  'Chứng nhận kiểm dịch trong 10 ngày trước khi nhập cảnh'),
    ('LA', 'Import', 'COGGINS_EIA',        30,   true,  'EIA âm tính trong 30 ngày'),
    ('LA', 'Import', 'VACCINE',            180,  true,  'Tiêm phòng cúm ngựa trong 6 tháng'),
    -- Đông Á
    ('HK', 'Import', 'HORSE_PASSPORT',     NULL, true,  'Hộ chiếu ngựa còn hiệu lực'),
    ('HK', 'Import', 'IMPORT_PERMIT',      NULL, true,  'Special Permit của AFCD, xin trước khi xuất'),
    ('HK', 'Import', 'HEALTH_CERT',        10,   true,  'Chứng nhận kiểm dịch bằng tiếng Anh, đi kèm ngựa'),
    ('HK', 'Import', 'COGGINS_EIA',        30,   true,  'EIA âm tính, lấy mẫu sau ít nhất 7 ngày cách ly trước xuất'),
    ('HK', 'Import', 'GLANDERS_TEST',      30,   true,  'Glanders CFT âm tính trong 30 ngày'),
    ('HK', 'Import', 'VACCINE',            180,  true,  'Không tiêm phòng trong 14 ngày trước khi xuất'),
    ('CN', 'Import', 'HORSE_PASSPORT',     NULL, true,  'Hộ chiếu ngựa còn hiệu lực'),
    ('CN', 'Import', 'IMPORT_PERMIT',      NULL, true,  'Giấy phép kiểm dịch nhập khẩu của GACC'),
    ('CN', 'Import', 'HEALTH_CERT',        10,   true,  'Chứng nhận kiểm dịch trong 10 ngày trước khi nhập cảnh'),
    ('CN', 'Import', 'COGGINS_EIA',        30,   true,  'EIA âm tính trong 30 ngày'),
    ('CN', 'Import', 'GLANDERS_TEST',      30,   true,  'Glanders CFT âm tính trong 30 ngày'),
    ('CN', 'Import', 'PIROPLASMOSIS_TEST', 30,   true,  'Piroplasmosis cELISA âm tính trong 30 ngày'),
    ('CN', 'Import', 'VACCINE',            180,  true,  'Tiêm phòng cúm ngựa trong 6 tháng'),
    ('KR', 'Import', 'HORSE_PASSPORT',     NULL, true,  'Hộ chiếu ngựa còn hiệu lực'),
    ('KR', 'Import', 'HEALTH_CERT',        10,   true,  'Chứng nhận kiểm dịch trong 10 ngày trước khi nhập cảnh'),
    ('KR', 'Import', 'COGGINS_EIA',        30,   true,  'EIA âm tính trong 30 ngày'),
    ('KR', 'Import', 'GLANDERS_TEST',      30,   true,  'Glanders CFT âm tính trong 30 ngày'),
    ('KR', 'Import', 'VACCINE',            180,  true,  'Tiêm phòng cúm ngựa trong 6 tháng'),
    ('JP', 'Import', 'HORSE_PASSPORT',     NULL, true,  'Hộ chiếu ngựa còn hiệu lực'),
    ('JP', 'Import', 'HEALTH_CERT',        10,   true,  'Chứng nhận kiểm dịch trong 10 ngày trước khi nhập cảnh'),
    ('JP', 'Import', 'COGGINS_EIA',        30,   true,  'EIA âm tính trong 30 ngày'),
    ('JP', 'Import', 'GLANDERS_TEST',      30,   true,  'Glanders CFT âm tính trong 30 ngày'),
    ('JP', 'Import', 'PIROPLASMOSIS_TEST', 30,   true,  'Piroplasmosis âm tính trong 30 ngày'),
    ('JP', 'Import', 'VACCINE',            180,  true,  'Tiêm phòng cúm ngựa trong 6 tháng'),
    -- Trung Đông, Úc
    ('AE', 'Import', 'HORSE_PASSPORT',     NULL, true,  'Hộ chiếu ngựa còn hiệu lực'),
    ('AE', 'Import', 'IMPORT_PERMIT',      NULL, true,  'Giấy phép nhập khẩu của Bộ Biến đổi Khí hậu và Môi trường UAE'),
    ('AE', 'Import', 'HEALTH_CERT',        10,   true,  'Chứng nhận kiểm dịch trong 10 ngày trước khi nhập cảnh'),
    ('AE', 'Import', 'COGGINS_EIA',        30,   true,  'EIA âm tính trong 30 ngày'),
    ('AE', 'Import', 'GLANDERS_TEST',      30,   true,  'Glanders CFT âm tính trong 30 ngày'),
    ('AE', 'Import', 'PIROPLASMOSIS_TEST', 30,   true,  'Piroplasmosis cELISA âm tính trong 30 ngày'),
    ('AE', 'Import', 'VACCINE',            180,  true,  'Tiêm phòng cúm ngựa trong 6 tháng'),
    ('AU', 'Import', 'HORSE_PASSPORT',     NULL, true,  'Hộ chiếu ngựa còn hiệu lực'),
    ('AU', 'Import', 'IMPORT_PERMIT',      NULL, true,  'Giấy phép nhập khẩu của Bộ Nông nghiệp Úc'),
    ('AU', 'Import', 'HEALTH_CERT',        2,    true,  'Chứng nhận kiểm dịch trong 48 giờ trước khi bay'),
    ('AU', 'Import', 'COGGINS_EIA',        30,   true,  'EIA âm tính trong thời gian cách ly trước xuất'),
    ('AU', 'Import', 'GLANDERS_TEST',      30,   true,  'Glanders CFT âm tính trong thời gian cách ly trước xuất'),
    ('AU', 'Import', 'PIROPLASMOSIS_TEST', 30,   true,  'Âm tính bằng cELISA, IFAT và PCR'),
    ('AU', 'Import', 'EVA_TEST',           30,   false, 'Chỉ ngựa đực chưa thiến'),
    ('AU', 'Import', 'VACCINE',            180,  true,  'Tiêm phòng cúm ngựa trong 6 tháng'),
    -- Châu Âu, Mỹ
    ('FR', 'Import', 'HORSE_PASSPORT',     NULL, true,  'Quy định (EU) 2020/692'),
    ('FR', 'Import', 'HEALTH_CERT',        2,    true,  'Chứng nhận kiểm dịch trong 48 giờ trước khi bay'),
    ('FR', 'Import', 'COGGINS_EIA',        30,   true,  'Quy định (EU) 2020/692'),
    ('FR', 'Import', 'GLANDERS_TEST',      30,   true,  'Quy định (EU) 2020/692'),
    ('FR', 'Import', 'EVA_TEST',           28,   false, 'Chỉ ngựa đực chưa thiến'),
    ('FR', 'Import', 'VACCINE',            180,  true,  'Tiêm phòng cúm ngựa trong 6 tháng'),
    ('DE', 'Import', 'HORSE_PASSPORT',     NULL, true,  'Quy định (EU) 2020/692'),
    ('DE', 'Import', 'HEALTH_CERT',        2,    true,  'Chứng nhận kiểm dịch trong 48 giờ trước khi bay'),
    ('DE', 'Import', 'COGGINS_EIA',        30,   true,  'Quy định (EU) 2020/692'),
    ('DE', 'Import', 'GLANDERS_TEST',      30,   true,  'Quy định (EU) 2020/692'),
    ('DE', 'Import', 'EVA_TEST',           28,   false, 'Chỉ ngựa đực chưa thiến'),
    ('DE', 'Import', 'VACCINE',            180,  true,  'Tiêm phòng cúm ngựa trong 6 tháng'),
    ('GB', 'Import', 'HORSE_PASSPORT',     NULL, true,  'Hộ chiếu ngựa còn hiệu lực'),
    ('GB', 'Import', 'HEALTH_CERT',        2,    true,  'Chứng nhận kiểm dịch (EHC) trong 48 giờ trước khi bay'),
    ('GB', 'Import', 'COGGINS_EIA',        30,   true,  'EIA âm tính trong 30 ngày'),
    ('GB', 'Import', 'GLANDERS_TEST',      30,   true,  'Glanders CFT âm tính trong 30 ngày'),
    ('GB', 'Import', 'EVA_TEST',           28,   false, 'Chỉ ngựa đực chưa thiến'),
    ('GB', 'Import', 'VACCINE',            180,  true,  'Tiêm phòng cúm ngựa trong 6 tháng'),
    ('US', 'Import', 'HORSE_PASSPORT',     NULL, true,  'Hộ chiếu ngựa còn hiệu lực'),
    ('US', 'Import', 'HEALTH_CERT',        10,   true,  'Chứng nhận kiểm dịch trong 10 ngày trước khi nhập cảnh'),
    ('US', 'Import', 'COGGINS_EIA',        30,   true,  'EIA âm tính trong 30 ngày'),
    ('US', 'Import', 'GLANDERS_TEST',      30,   true,  'Glanders CFT âm tính trong 30 ngày'),
    ('US', 'Import', 'PIROPLASMOSIS_TEST', 30,   true,  'Piroplasmosis cELISA âm tính trong 30 ngày'),
    ('US', 'Import', 'VACCINE',            180,  true,  'Tiêm phòng cúm ngựa trong 6 tháng'),
    -- Nga (ví dụ tuyến đường bộ dài VN -> TQ -> Nga)
    ('RU', 'Import', 'HORSE_PASSPORT',     NULL, true,  'Hộ chiếu ngựa còn hiệu lực'),
    ('RU', 'Import', 'IMPORT_PERMIT',      NULL, true,  'Giấy phép nhập khẩu của Rosselkhoznadzor'),
    ('RU', 'Import', 'HEALTH_CERT',        5,    true,  'Chứng nhận thú y theo mẫu Liên minh Kinh tế Á-Âu'),
    ('RU', 'Import', 'COGGINS_EIA',        30,   true,  'EIA âm tính trong 30 ngày'),
    ('RU', 'Import', 'GLANDERS_TEST',      30,   true,  'Glanders CFT âm tính trong 30 ngày'),
    ('RU', 'Import', 'PIROPLASMOSIS_TEST', 30,   true,  'Piroplasmosis âm tính trong 30 ngày'),
    ('RU', 'Import', 'VACCINE',            180,  true,  'Tiêm phòng cúm ngựa trong 6 tháng'),
    -- Quá cảnh (ngựa chỉ đi qua): giấy phép quá cảnh + chứng nhận sức khỏe còn hạn khi tới cửa khẩu
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
JOIN clearance.document_types d ON d.code = c.doc_code;

-- Số ngày cần để có từng loại giấy (dữ liệu minh họa): dùng tính ngày pickup sớm nhất của tuyến
-- = max(processing_days) trên các yêu cầu Export + Transit + Import của đơn.
UPDATE clearance.country_doc_requirements r
SET processing_days = CASE
        WHEN d.code = 'IMPORT_PERMIT' AND r.country_code IN ('AU', 'JP') THEN 30 -- cách ly trước xuất dài
        WHEN d.code = 'IMPORT_PERMIT' AND r.country_code IN ('CN', 'RU') THEN 21
        WHEN d.code IN ('IMPORT_PERMIT', 'TRANSIT_PERMIT') THEN 14
        WHEN d.code IN ('COGGINS_EIA', 'GLANDERS_TEST', 'PIROPLASMOSIS_TEST', 'EVA_TEST') THEN 7
        WHEN d.code = 'HEALTH_CERT' THEN 3
        ELSE NULL
    END
FROM clearance.document_types d
WHERE d.doc_type_id = r.doc_type_id;

-- ---------- A.3 Bảng giá ----------
INSERT INTO pricing.price_items (category, item_code, item_name, transport_mode, dest_country_code, calc_type, value, min_horses) VALUES
-- Cước tuyến đường bay (USD / ngựa)
('Freight', 'FREIGHT', 'Zone A - Đông Nam Á (Thái Lan)',   'Air', 'TH', 'PerHorse', 3000, NULL),
('Freight', 'FREIGHT', 'Zone A - Đông Nam Á (Singapore)',  'Air', 'SG', 'PerHorse', 3000, NULL),
('Freight', 'FREIGHT', 'Zone A - Đông Nam Á (Malaysia)',   'Air', 'MY', 'PerHorse', 3000, NULL),
('Freight', 'FREIGHT', 'Zone B - Đông Á (Hong Kong)',      'Air', 'HK', 'PerHorse', 5000, NULL),
('Freight', 'FREIGHT', 'Zone B - Đông Á (Hàn Quốc)',       'Air', 'KR', 'PerHorse', 5000, NULL),
('Freight', 'FREIGHT', 'Zone B - Đông Á (Nhật Bản)',       'Air', 'JP', 'PerHorse', 5000, NULL),
('Freight', 'FREIGHT', 'Zone C - Trung Đông (UAE)',        'Air', 'AE', 'PerHorse', 6500, NULL),
('Freight', 'FREIGHT', 'Zone C - Châu Úc (Úc)',            'Air', 'AU', 'PerHorse', 6500, NULL),
('Freight', 'FREIGHT', 'Zone D - Châu Âu (Pháp)',          'Air', 'FR', 'PerHorse', 8000, NULL),
('Freight', 'FREIGHT', 'Zone D - Châu Âu (Anh)',           'Air', 'GB', 'PerHorse', 8000, NULL),
('Freight', 'FREIGHT', 'Zone D - Châu Âu (Đức)',           'Air', 'DE', 'PerHorse', 8000, NULL),
('Freight', 'FREIGHT', 'Zone E - Châu Mỹ (Mỹ)',            'Air', 'US', 'PerHorse', 10500, NULL),
-- Cước tuyến đường bộ (USD / km / ngựa)
('Freight', 'FREIGHT', 'Zone G1 - Đường bộ (Campuchia)',   'Ground', 'KH', 'PerKmPerHorse', 1.00, NULL),
('Freight', 'FREIGHT', 'Zone G1 - Đường bộ (Lào)',         'Ground', 'LA', 'PerKmPerHorse', 1.00, NULL),
('Freight', 'FREIGHT', 'Zone G1 - Đường bộ (Thái Lan)',    'Ground', 'TH', 'PerKmPerHorse', 1.00, NULL),
('Freight', 'FREIGHT', 'Zone G1 - Đường bộ (Trung Quốc)',  'Ground', 'CN', 'PerKmPerHorse', 1.00, NULL),
('Freight', 'FREIGHT', 'Zone G2 - Đường bộ (Nga)',         'Ground', 'RU', 'PerKmPerHorse', 1.10, NULL),
-- Hệ số hạng chuồng
('StallClass', 'STALL_SHARED',  'Chuồng ghép (Shared) - 3 ngựa / pallet',   NULL, NULL, 'Multiplier', 1.00, NULL),
('StallClass', 'STALL_COMFORT', 'Chuồng rưỡi (Comfort) - 2 ngựa / pallet',  NULL, NULL, 'Multiplier', 1.40, NULL),
('StallClass', 'STALL_PRIVATE', 'Chuồng riêng (Private) - 1 ngựa / pallet', NULL, NULL, 'Multiplier', 2.20, NULL),
-- Phụ phí trên cước
('Surcharge', 'SUR_FUEL',    'Phụ phí nhiên liệu',                          NULL, NULL, 'Percent', 8,  NULL),
('Surcharge', 'SUR_CLIMATE', 'Phụ phí khoang điều hòa / chăm sóc đặc biệt', NULL, NULL, 'Percent', 10, NULL),
('Surcharge', 'SUR_EXPRESS', 'Phụ phí gấp (hồ sơ khẩn / hạn giao sát)',     NULL, NULL, 'Percent', 20, NULL),
-- Chiết khấu số lượng trên cước
('Discount', 'DISC_VOL_2', 'Chiết khấu 2-3 ngựa',  NULL, NULL, 'Percent', 5,  2),
('Discount', 'DISC_VOL_4', 'Chiết khấu 4-6 ngựa',  NULL, NULL, 'Percent', 10, 4),
('Discount', 'DISC_VOL_7', 'Chiết khấu từ 7 ngựa', NULL, NULL, 'Percent', 15, 7),
-- Thủ tục, thông quan, kiểm dịch
('Clearance', 'FEE_DOCS',       'Phí hồ sơ & chứng nhận sức khỏe xuất khẩu', NULL,     NULL, 'PerHorse',   350,  NULL),
('Clearance', 'FEE_CLEARANCE',  'Phí phục vụ sân bay & thông quan',          'Air',    NULL, 'PerHorse',   600,  NULL),
('Clearance', 'FEE_CLEARANCE',  'Phí thông quan cửa khẩu',                   'Ground', NULL, 'PerHorse',   250,  NULL),
('Clearance', 'FEE_QUARANTINE', 'Phí kiểm dịch nước đến (mức chuẩn)',        'Air',    NULL, 'PerHorse',   700,  NULL),
('Clearance', 'FEE_QUARANTINE', 'Phí kiểm dịch Nhật Bản (cách ly dài)',      'Air',    'JP', 'PerHorse',   2500, NULL),
('Clearance', 'FEE_QUARANTINE', 'Phí kiểm dịch Úc (cách ly dài)',            'Air',    'AU', 'PerHorse',   2500, NULL),
('Clearance', 'FEE_QUARANTINE', 'Phí kiểm dịch Trung Quốc',                  'Ground', 'CN', 'PerHorse',   700,  NULL),
('Clearance', 'FEE_PICKUP',     'Phí điều xe đến điểm nhận',                 'Ground', NULL, 'PerBooking', 100,  NULL),
('Clearance', 'FEE_TRANSIT',    'Phí thủ tục quá cảnh (mỗi nước đi qua)',    NULL,     NULL, 'PerHorse',   200,  NULL),
-- Áp tải (1 nhân viên áp tải / 3 ngựa)
('Escort', 'ESCORT', 'Groom bay kèm',                     'Air',    NULL, 'PerGroom',    900, NULL),
('Escort', 'ESCORT', 'Groom đi kèm đường bộ (theo ngày)', 'Ground', NULL, 'PerGroomDay', 120, NULL),
-- Bảo hiểm
('Insurance', 'INS_TRANSIT', 'Bảo hiểm hành trình (% giá trị khai báo)', NULL, NULL, 'Percent', 1, NULL);

-- ---------- A.4 Phương tiện ----------
INSERT INTO fleet.transport_assets (asset_id, asset_code, asset_type, capacity_horses, has_climate_control, has_gps_tracker, status, last_sanitization_date) VALUES
(1, '29B-888.99',       'HorseTruck_AirSuspension', 6, true,  true,  'Available',   now()),
(2, '51C-246.80',       'HorseVan',                 2, false, true,  'Available',   '2026-09-25 08:00:00+07'),
(3, 'AIR-STALL-SGN-01', 'AirStall',                 3, true,  true,  'Available',   '2026-09-27 14:00:00+07'),
(4, '30H-567.89',       'HorseTruck_AirSuspension', 8, true,  true,  'Maintenance', '2026-09-10 09:00:00+07'),
(5, 'AIR-STALL-HAN-02', 'AirStall',                 2, true,  false, 'Available',   '2026-09-29 10:30:00+07');

-- ---------- B.1 Tài khoản môi trường phát triển (cùng một mật khẩu, đã băm bằng BCrypt) ----------
INSERT INTO identity.users (user_id, full_name, email, password_hash, phone_number, role) VALUES
(1, 'Trần Quốc Bảo',     'bao.tran@logistics.com',        '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84912000001', 'LogisticsManager'),
(2, 'Nguyễn Thị Mai',    'mai.nguyen@clearance.com',      '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84912000002', 'TransportSpecialist'),
(3, 'Lê Hoàng Nam',      'nam.le@fleet.com',              '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84912000003', 'FleetCoordinator'),
(4, 'Phạm Văn Đức',      'duc.pham@transport.com',        '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84912000004', 'DriverEscort'),
(5, 'Jane Smith',        'jane.smith@racehorseowner.com', '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84988111222', 'Customer'),
(6, 'Quản trị hệ thống', 'admin@logistics.com',           '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84912000000', 'Admin'),
-- Mỗi vai trò nhân sự có thêm một tài khoản để minh họa việc phân công và phân quyền.
(7, 'Nguyễn Minh Khôi',  'khoi.nguyen@logistics.com',     '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84912000007', 'LogisticsManager'),
(8, 'Lê Thanh Hà',       'ha.le@clearance.com',           '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84912000008', 'TransportSpecialist'),
(9, 'Phạm Gia Huy',      'huy.pham@fleet.com',            '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84912000009', 'FleetCoordinator'),
(10, 'Võ Quốc Thịnh',    'thinh.vo@transport.com',        '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84912000010', 'DriverEscort'),
(11, 'Đặng Thu Trang',   'trang.dang@logistics.com',      '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84912000011', 'Admin'),
-- Hai khách hàng mới; tất cả tài khoản dữ liệu mẫu cùng dùng mật khẩu Racehorse@123.
(12, 'Trần Ngọc Anh',    'ngocanh.tran@horseowner.com',   '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84988111223', 'Customer'),
(13, 'Hoàng Minh Châu',  'minhchau.hoang@horseowner.com', '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84988111224', 'Customer'),
-- Mỗi vai trò một tài khoản dễ nhớ (tên = vai trò) để demo và kiểm thử nhanh.
(14, 'Customer',             'customer@racehorsetransport.com',    '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000001', 'Customer'),
(15, 'Logistics Manager',    'manager@racehorsetransport.com',     '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000002', 'LogisticsManager'),
(16, 'Transport Specialist', 'specialist@racehorsetransport.com',  '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000003', 'TransportSpecialist'),
(17, 'Fleet Coordinator',    'coordinator@racehorsetransport.com', '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000004', 'FleetCoordinator'),
(18, 'Driver Escort',        'driver@racehorsetransport.com',      '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000005', 'DriverEscort'),
(19, 'Admin',                'admin@racehorsetransport.com',       '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000006', 'Admin');

-- ---------- B.2 Ngựa của Jane Smith ----------
INSERT INTO racehorse.horses (horse_id, owner_user_id, name, breed, gender, date_of_birth, color, microchip_number, passport_number, special_care_requirements) VALUES
(1, 5, 'Red Flash (Tia Chớp Đỏ)',  'Thoroughbred', 'Stallion', '2020-04-15', 'Hồng sắc', '982000412345671', '704001000000001', 'Cần lót đệm rơm dày, nhiệt độ cabin 16-19°C'),
(2, 5, 'Golden Pegasus (Kim Mã)', 'Thoroughbred', 'Mare',     '2021-03-10', 'Bạch sắc', '982000412345672', '704001000000002', 'Dễ say xe nhẹ, cần treo bao cỏ tươi');

-- ---------- B.3 Ngựa của hai khách hàng mới ----------
INSERT INTO racehorse.horses (horse_id, owner_user_id, name, breed, gender, date_of_birth, color, microchip_number, passport_number, special_care_requirements) VALUES
(3, 12, 'Silver Comet',   'Thoroughbred', 'Stallion', '2019-05-21', 'Xám bạc',  '982000412345673', '704001000000003', 'Cần không gian yên tĩnh khi lên xe'),
(4, 12, 'Emerald Belle',  'Thoroughbred', 'Mare',     '2020-11-08', 'Nâu sẫm',  '982000412345674', '704001000000004', 'Bổ sung nước điện giải mỗi 4 giờ'),
(5, 13, 'Midnight Crown', 'Thoroughbred', 'Gelding',  '2018-07-14', 'Đen tuyền','982000412345675', '704001000000005', 'Không đứng cạnh ngựa đực chưa thiến'),
(6, 13, 'Sakura Wind',    'Thoroughbred', 'Mare',     '2021-02-27', 'Hạt dẻ',   '982000412345676', '704001000000006', 'Duy trì nhiệt độ cabin 17-20°C'),
(7, 13, 'Royal Thunder',  'Thoroughbred', 'Stallion', '2019-09-03', 'Nâu đỏ',   '982000412345677', '704001000000007', 'Dùng lưới cỏ chậm ăn trong suốt hành trình');

-- =====================================================================================
-- C. KỊCH BẢN ĐÃ HOÀN TẤT 6 LUỒNG
-- =====================================================================================

-- Luồng 1: Jane Smith đặt chuyến 2 ngựa sang Quảng Châu; Quản lý hậu cần Bảo duyệt và giao Mai phụ trách hồ sơ.
-- Không phân điều phối viên theo đơn; Nam trở thành người phụ trách khi tạo chuyến ở Luồng 3.
INSERT INTO booking.booking_requests (booking_id, booking_code, customer_user_id, pickup_address, pickup_country_code, dropoff_address, dropoff_country_code,
                                      pickup_window_start, pickup_window_end, delivery_deadline, total_horses, transport_mode, distance_km, estimated_cost, status,
                                      reviewed_by_user_id, reviewed_at, assigned_specialist_id, created_at) VALUES
(1, 'BKG-2026-0001', 5, 'Trang trại Yên Bài, Ba Vì, Hà Nội', 'VN', 'Trường đua Tùng Hóa, TP. Quảng Châu', 'CN',
 '2026-09-30 00:00:00+07', '2026-10-03 00:00:00+07', '2026-10-04 18:00:00+08', 2, 'Ground', 1100, 7500.00, 'Completed',
 1, '2026-09-20 09:00:00+07', 2, '2026-09-19 14:00:00+07');

INSERT INTO booking.booking_horses (booking_horse_id, booking_id, horse_id, notes, status) VALUES
(1, 1, 1, 'Chuồng 1 bên trái', 'Approved'),
(2, 1, 2, 'Chuồng 2 ở giữa',   'Approved');

-- Luồng 2: Mai lập hồ sơ, khách nộp giấy tờ, Mai duyệt, cơ quan chức năng thông quan
INSERT INTO clearance.digital_dossiers (dossier_id, dossier_code, booking_horse_id, specialist_user_id, status, clearance_number, cleared_at, created_at) VALUES
(1, 'DOS-2026-0001', 1, 2, 'Cleared', 'VET-CERT-VN-CN-2026-001', '2026-09-28 16:00:00+07', '2026-09-20 10:00:00+07'),
(2, 'DOS-2026-0002', 2, 2, 'Cleared', 'VET-CERT-VN-CN-2026-001', '2026-09-28 16:00:00+07', '2026-09-20 10:00:00+07');

INSERT INTO clearance.dossier_documents (document_id, dossier_id, doc_type_id, document_number, file_url, uploaded_by_user_id, uploaded_at, status, reviewed_by_user_id, reviewed_at) VALUES
(1, 1, 1, '704001000000001',              'https://storage.racehorse.vn/docs/rf_passport.pdf', 5, '2026-09-22 08:00:00+07', 'Approved', 2, '2026-09-23 10:00:00+07'),
(2, 1, 2, 'COGGINS-LAB-2026-888',         'https://storage.racehorse.vn/docs/rf_coggins.pdf',  5, '2026-09-22 08:05:00+07', 'Approved', 2, '2026-09-23 10:05:00+07'),
(3, 2, 1, '704001000000002',              'https://storage.racehorse.vn/docs/gp_passport.pdf', 5, '2026-09-22 08:10:00+07', 'Approved', 2, '2026-09-23 10:10:00+07'),
(4, 1, 3, 'HEALTH-CERT-RF-2026-001',      'https://storage.racehorse.vn/docs/rf_health.pdf',   5, '2026-09-22 08:15:00+07', 'Approved', 2, '2026-09-23 10:15:00+07'),
(5, 1, 4, 'VACCINE-RF-2026-001',          'https://storage.racehorse.vn/docs/rf_vaccine.pdf',  5, '2026-09-22 08:20:00+07', 'Approved', 2, '2026-09-23 10:20:00+07'),
(6, 2, 2, 'COGGINS-LAB-2026-889',         'https://storage.racehorse.vn/docs/gp_coggins.pdf',  5, '2026-09-22 08:25:00+07', 'Approved', 2, '2026-09-23 10:25:00+07'),
(7, 2, 3, 'HEALTH-CERT-GP-2026-002',      'https://storage.racehorse.vn/docs/gp_health.pdf',   5, '2026-09-22 08:30:00+07', 'Approved', 2, '2026-09-23 10:30:00+07'),
(8, 2, 4, 'VACCINE-GP-2026-002',          'https://storage.racehorse.vn/docs/gp_vaccine.pdf',  5, '2026-09-22 08:35:00+07', 'Approved', 2, '2026-09-23 10:35:00+07'),
(9,  1, 5, 'GACC-IP-2026-0915',            'https://storage.racehorse.vn/docs/rf_permit.pdf',   5, '2026-09-22 08:40:00+07', 'Approved', 2, '2026-09-23 10:40:00+07'),
(10, 1, 6, 'GLANDERS-LAB-2026-501',        'https://storage.racehorse.vn/docs/rf_glanders.pdf', 5, '2026-09-22 08:45:00+07', 'Approved', 2, '2026-09-23 10:45:00+07'),
(11, 1, 7, 'PIRO-LAB-2026-502',            'https://storage.racehorse.vn/docs/rf_piro.pdf',     5, '2026-09-22 08:50:00+07', 'Approved', 2, '2026-09-23 10:50:00+07'),
(12, 2, 5, 'GACC-IP-2026-0915',            'https://storage.racehorse.vn/docs/gp_permit.pdf',   5, '2026-09-22 08:55:00+07', 'Approved', 2, '2026-09-23 10:55:00+07'),
(13, 2, 6, 'GLANDERS-LAB-2026-503',        'https://storage.racehorse.vn/docs/gp_glanders.pdf', 5, '2026-09-22 09:00:00+07', 'Approved', 2, '2026-09-23 11:00:00+07'),
(14, 2, 7, 'PIRO-LAB-2026-504',            'https://storage.racehorse.vn/docs/gp_piro.pdf',     5, '2026-09-22 09:05:00+07', 'Approved', 2, '2026-09-23 11:05:00+07');

-- Hồ sơ thú y lưu theo ngựa (chủ ngựa tự bổ sung, dùng lại khi nộp hồ sơ cho từng đơn)
INSERT INTO racehorse.horse_documents (document_id, horse_id, doc_type_id, document_number, issued_by, issue_date, expiry_date, file_url, notes, uploaded_by_user_id, uploaded_at) VALUES
(1, 1, 1, '704001000000001',      'Liên đoàn Đua ngựa Việt Nam',     '2023-03-01', '2033-03-01', 'https://storage.racehorse.vn/docs/rf_passport.pdf', NULL,                                5, '2026-09-01 09:00:00+07'),
(2, 1, 4, 'VACCINE-RF-2026-001',  'BS. Trần Minh Khoa - Phòng khám Thú y Phú Thọ', '2026-08-20', '2027-02-20', 'https://storage.racehorse.vn/docs/rf_vaccine.pdf', 'Mũi nhắc cúm ngựa, không phản ứng phụ', 5, '2026-09-01 09:05:00+07'),
(3, 2, 1, '704001000000002',      'Liên đoàn Đua ngựa Việt Nam',     '2023-05-10', '2033-05-10', 'https://storage.racehorse.vn/docs/gp_passport.pdf', NULL,                                5, '2026-09-01 09:10:00+07'),
(4, 2, 4, 'VACCINE-GP-2026-002',  'BS. Trần Minh Khoa - Phòng khám Thú y Phú Thọ', '2026-08-20', '2027-02-20', 'https://storage.racehorse.vn/docs/gp_vaccine.pdf', NULL,                                5, '2026-09-01 09:15:00+07');

-- Luồng 3: Nam lập kế hoạch chuyến, Bảo duyệt. Luồng 6: Bảo đóng chuyến
-- (trễ 30 phút, chưa quá ngưỡng 30 nên trạng thái vẫn là OnTime; chi phí thực tế = kế hoạch 4200 + phát sinh sự cố 400)
INSERT INTO fleet.trips (trip_id, trip_code, vehicle_id, planned_start_date, planned_end_date, actual_start_date, actual_end_date,
                         planned_cost, actual_cost, overall_status, delay_minutes, on_time_status, kpi_score,
                         closed_by_user_id, closed_at, executive_remarks, created_at,
                         planned_by_user_id, approved_by_user_id, approved_at) VALUES
(1, 'TRP-2026-0001', 1, '2026-10-01 06:00:00+07', '2026-10-02 18:00:00+08', '2026-10-01 06:30:00+07', '2026-10-02 18:30:00+08',
 4200.00, 4600.00, 'Completed', 30, 'OnTime', 100.00,
 1, '2026-10-02 20:00:00+08', 'Chuyến đi vận chuyển an toàn thành công. Sự cố tắc biên được nắn tuyến kịp thời, tính lý do bất khả kháng Force Majeure.', '2026-09-24 09:00:00+07',
 3, 1, '2026-09-25 09:00:00+07');

INSERT INTO fleet.trip_crew (trip_id, user_id, crew_role, is_lead) VALUES
(1, 4, 'Driver', true),
(1, 4, 'Escort', true);

-- Phạm Văn Đức vừa lái chính vừa là escort phụ trách cả 2 con (R-06)
INSERT INTO fleet.trip_horses (trip_horse_id, trip_id, booking_horse_id, stall_slot_number, notes, escort_user_id, status, picked_up_at) VALUES
(1, 1, 1, 1, 'Red Flash - Chuồng số 1',      4, 'InTransit', '2026-10-01 06:30:00+07'),
(2, 1, 2, 2, 'Golden Pegasus - Chuồng số 2', 4, 'InTransit', '2026-10-01 06:30:00+07');

-- Luồng 4 + 5: mốc Hữu Nghị (2) bị hủy do tắc biên, thay bằng mốc Tân Thanh (5)
INSERT INTO fleet.route_checkpoints (checkpoint_id, trip_id, sequence_order, checkpoint_name, checkpoint_type, address, planned_time, mandatory_rest_minutes,
                                     actual_arrival_time, actual_departure_time, status, is_active) VALUES
(1, 1, 1, 'Điểm xuất phát Hà Nội',             'Origin',      'Trại Ba Vì, Hà Nội',           '2026-10-01 06:00:00+07', 0,   '2026-10-01 06:00:00+07', '2026-10-01 06:30:00+07', 'Departed',  true),
(2, 1, 2, 'Cửa khẩu Quốc tế Hữu Nghị',         'BorderGate',  'Đồng Đăng, Lạng Sơn',          '2026-10-01 11:30:00+07', 120, NULL,                     NULL,                     'Cancelled', false),
(3, 1, 3, 'Trạm nghỉ cao tốc Nam Ninh',        'RestStop',    'Dịch vụ Nam Ninh, Quảng Tây',  '2026-10-01 20:00:00+08', 360, NULL,                     NULL,                     'Pending',   true),
(4, 1, 4, 'Trường đua Tùng Hóa (Đích)',        'Destination', 'Tùng Hóa, TP. Quảng Châu',     '2026-10-02 18:00:00+08', 0,   '2026-10-02 18:30:00+08', NULL,                     'Arrived',   true),
(5, 1, 2, 'Cửa khẩu Tân Thanh (Đã nắn tuyến)', 'BorderGate',  'Tân Thanh, Lạng Sơn',          '2026-10-01 12:00:00+07', 90,  '2026-10-01 12:15:00+07', '2026-10-01 13:45:00+07', 'Cleared',   true);

INSERT INTO tracking.horse_welfare_logs (log_id, trip_horse_id, checkpoint_id, recorded_by_user_id, cabin_temp, water_intake_liters, feed_status, stress_level, notes, photo_url, logged_at) VALUES
(1, 1, 1, 4, 17.5, 10.0, 'Normal', 'Calm', 'Red Flash thể trạng tốt, ăn uống bình thường', 'https://storage.racehorse.vn/logs/rf_01.jpg', '2026-10-01 06:20:00+07'),
(2, 2, 1, 4, 17.5, 8.0,  'Normal', 'Calm', 'Golden Pegasus ổn định, không say xe',         'https://storage.racehorse.vn/logs/gp_01.jpg', '2026-10-01 06:25:00+07');

INSERT INTO incident.trip_incidents (incident_id, incident_code, trip_id, reported_by_user_id, incident_type, description, location,
                                     proposed_action, revised_route_notes, additional_cost, status, approved_by_user_id, approved_at, reported_at, severity, resolved_at) VALUES
(1, 'INC-2026-0001', 1, 4, 'BorderCongestion', 'Tắc biên Hữu Nghị kéo dài hơn 8 tiếng do hỏng máy soi hải quan', 'Cửa khẩu Hữu Nghị',
 'Nắn tuyến sang Cửa khẩu Tân Thanh cách 18km', 'Đã liên hệ trạm thú y Tân Thanh hỗ trợ làn xanh ưu tiên', 400.00, 'Resolved',
 1, '2026-10-01 11:20:00+07', '2026-10-01 11:00:00+07', 'Moderate', '2026-10-01 13:45:00+07');

-- Luồng 6: khách ký biên bản e-POD
INSERT INTO acceptance.handover_acceptances (handover_id, trip_id, booking_id, handover_date_time, recipient_name, recipient_phone, recipient_id_card, digital_signature_url, overall_condition, remarks, driver_user_id, checkpoint_id) VALUES
(1, 1, 1, '2026-10-02 19:00:00+08', 'Jane Smith', '+84988111222', 'PASSPORT-US-991283', 'https://storage.racehorse.vn/signatures/epod_trip1.png', 'Excellent', '2 cá thể ngựa đến nơi an toàn tuyệt đối, không trầy xước.', 4, 4);

UPDATE fleet.trip_horses SET status = 'Delivered', delivered_at = '2026-10-02 19:00:00+08', handover_id = 1 WHERE trip_id = 1;

-- Thanh toán (R-02) của đơn đã hoàn tất: cọc 30% sau khi duyệt, phần còn lại trước khi xuất phát,
-- chi phí phát sinh 400 USD (sự cố INC-2026-0001) trả sau khi đóng chuyến. Tỉ giá minh họa 26.000 VND / USD.
UPDATE booking.booking_requests
SET deposit_percent = 30, deposit_amount = 2250.00, extra_charges = 400.00, extra_due_at = '2026-10-10 19:00:00+07'
WHERE booking_id = 1;

INSERT INTO billing.payments (payment_id, payment_code, booking_id, payment_type, provider, status, amount, currency_code, exchange_rate, amount_vnd,
                              txn_ref, provider_txn_no, provider_pay_date, bank_code, response_code, note, created_by_user_id, created_at, paid_at) VALUES
(1, 'PAY-2026-0001', 1, 'Deposit', 'VnPay',  'Succeeded', 2250.00, 'USD', 26000, 58500000,  'PAY20260001', '14612001', '20260920101500', 'NCB', '00', NULL, 5, '2026-09-20 10:10:00+07', '2026-09-20 10:15:00+07'),
(2, 'PAY-2026-0002', 1, 'Balance', 'VnPay',  'Succeeded', 5250.00, 'USD', 26000, 136500000, 'PAY20260002', '14630017', '20260928170200', 'NCB', '00', NULL, 5, '2026-09-28 17:00:00+07', '2026-09-28 17:02:00+07'),
(3, 'PAY-2026-0003', 1, 'Extra',   'Manual', 'Succeeded', 400.00,  'USD', 26000, 10400000,  'PAY20260003', NULL,       NULL,             NULL,  NULL, 'Khách chuyển khoản phí phát sinh nắn tuyến, mã CK VCB-88123', 1, '2026-10-05 09:00:00+07', '2026-10-05 09:00:00+07');

-- Cấu hình thanh toán mặc định (Admin / Manager sửa được bằng PUT /api/settings/payment)
INSERT INTO common.system_settings (setting_key, setting_value, description) VALUES
('Payment.DepositPercent',               '30',    '% đặt cọc trên giá báo, thu sau khi Manager duyệt đơn'),
('Payment.CancelRefundPercent',          '50',    '% tiền cọc được hoàn khi khách hủy đơn trước khi xuất phát'),
('Payment.UsdVndRate',                   '26000', 'Tỉ giá quy đổi USD -> VND khi thanh toán qua VNPay'),
('Payment.BalanceDueDaysBeforeDeparture','2',     'Phần còn lại phải trả trước ngày xuất phát bao nhiêu ngày'),
('Payment.ExtraPaymentDueDays',          '7',     'Hạn trả chi phí phát sinh (ngày) tính từ khi đóng chuyến'),
-- BE-17: giá trị cấu hình của Chính sách dịch vụ (docs/CHINH_SACH_DICH_VU.md)
('Policy.Version',                       '1.1',   'Phiên bản Chính sách dịch vụ hiện hành (lưu vào đơn khi khách đồng ý)'),
('Booking.QuoteAcceptDays',              '3',     'Số ngày khách được xem và chấp nhận báo giá chốt + đặt cọc'),
('Dossier.SubmitDays',                   '7',     'Số ngày khách có để nộp đủ giấy tờ, tính từ lúc đặt cọc'),
('Stable.FeePerHorsePerDayUsd',          '50',    'Phí chuồng tạm + chăm sóc (USD / con / ngày)'),
('Delivery.ExtraKmFeeUsd',               '2',     'Phí mỗi km thêm khi giao cho người nhận / địa chỉ khác'),
('Delay.GraceHours',                     '6',     'Số giờ trễ được miễn bồi thường'),
('Delay.PercentPer12Hours',              '5',     '% tiền cước bồi thường cho mỗi 12 giờ trễ sau ân hạn'),
('Delay.MaxPercent',                     '25',    '% tiền cước bồi thường giao trễ tối đa'),
('Insurance.PremiumPercent',             '1',     '% phí bảo hiểm toàn giá trị trên giá trị thỏa thuận mỗi con'),
('Insurance.BaseLiabilityUsd',           '5000',  'Trách nhiệm tối đa (USD / con) khi ngựa chết / mất mà không mua bảo hiểm'),
('Insurance.BaseVetCapUsd',              '1000',  'Chi phí thú y tối đa (USD / con) khi ngựa bị thương mà không mua bảo hiểm'),
('Insurance.VetCapPercent',              '20',    '% giá trị thỏa thuận tối đa cho chi phí thú y khi có bảo hiểm'),
('Insurance.ProofThresholdUsd',          '50000', 'Giá trị khai báo (USD / con) từ mức này phải kèm chứng từ'),
('Horse.MinAgeMonths',                   '6',     'Tuổi tối thiểu (tháng) của ngựa, tính tới ngày cuối của khoảng đón'),
('Horse.GestationDays',                  '340',   'Số ngày thai kỳ trung bình của ngựa'),
('Horse.LatePregnancyPercent',           '90',    'Không chở ngựa cái đã quá % thai kỳ này (tính tại ngày cuối của khoảng đón)'),
('Horse.PostFoalingDays',                '7',     'Không chở ngựa cái vừa đẻ trong số ngày này'),
('Delivery.FreeWaitingHours',            '2',     'Số giờ xe chờ miễn phí tại điểm giao'),
('Delivery.WaitingFeePerHourUsd',        '40',    'Phí chờ (USD / giờ) sau thời gian chờ miễn phí'),
('Delivery.MaxWaitingHours',             '24',    'Số giờ chờ tính phí tối đa'),
('Delivery.DecisionHours',               '24',    'Số giờ khách có để chọn cách xử lý khi không giao được'),
('Delivery.DeadlineBufferDays',          '2',     'Số ngày dự phòng cộng vào hạn giao sớm nhất khách được chọn'),
('Incident.EuthanasiaConsentMinutes',    '60',    'Số phút chờ chủ ngựa trả lời khuyến nghị an tử trước khi được áp dụng ủy quyền khẩn cấp');

INSERT INTO common.system_notifications (recipient_user_id, title, message, reference_type, reference_id, is_read, created_at) VALUES
(5, 'Đơn BKG-2026-0001 đã được phê duyệt', 'Yêu cầu vận chuyển của bạn đã được phê duyệt và phân công nhân sự phụ trách.', 'Booking', 1, true,  '2026-09-20 09:00:00+07'),
(5, 'Ngựa đã tới điểm bàn giao',           'Chuyến TRP-2026-0001 đã tới Trường đua Tùng Hóa. Vui lòng kiểm tra và ký biên bản bàn giao.', 'Trip', 1, false, '2026-10-02 18:30:00+08');

-- Bộ đếm sinh mã: khớp với các mã đã dùng ở trên (BKG-2026-0001, DOS-2026-0002, TRP-2026-0001, INC-2026-0001)
INSERT INTO common.code_counters (prefix, year, last_value) VALUES
('BKG', 2026, 1),
('DOS', 2026, 2),
('TRP', 2026, 1),
('INC', 2026, 1),
('PAY', 2026, 3);

-- =====================================================================================
-- Các dòng trên chèn ID tường minh nên phải đẩy bộ đếm IDENTITY lên sau giá trị lớn nhất,
-- nếu không lần INSERT đầu tiên từ API sẽ trùng khóa chính.
-- =====================================================================================
SELECT setval(pg_get_serial_sequence('identity.users', 'user_id'),                     (SELECT max(user_id) FROM identity.users));
SELECT setval(pg_get_serial_sequence('racehorse.horses', 'horse_id'),                  (SELECT max(horse_id) FROM racehorse.horses));
SELECT setval(pg_get_serial_sequence('booking.booking_requests', 'booking_id'),        (SELECT max(booking_id) FROM booking.booking_requests));
SELECT setval(pg_get_serial_sequence('booking.booking_horses', 'booking_horse_id'),    (SELECT max(booking_horse_id) FROM booking.booking_horses));
SELECT setval(pg_get_serial_sequence('clearance.document_types', 'doc_type_id'),       (SELECT max(doc_type_id) FROM clearance.document_types));
SELECT setval(pg_get_serial_sequence('clearance.digital_dossiers', 'dossier_id'),      (SELECT max(dossier_id) FROM clearance.digital_dossiers));
SELECT setval(pg_get_serial_sequence('clearance.dossier_documents', 'document_id'),    (SELECT max(document_id) FROM clearance.dossier_documents));
SELECT setval(pg_get_serial_sequence('racehorse.horse_documents', 'document_id'),     (SELECT max(document_id) FROM racehorse.horse_documents));
SELECT setval(pg_get_serial_sequence('fleet.transport_assets', 'asset_id'),            (SELECT max(asset_id) FROM fleet.transport_assets));
SELECT setval(pg_get_serial_sequence('fleet.trips', 'trip_id'),                        (SELECT max(trip_id) FROM fleet.trips));
SELECT setval(pg_get_serial_sequence('fleet.trip_horses', 'trip_horse_id'),            (SELECT max(trip_horse_id) FROM fleet.trip_horses));
SELECT setval(pg_get_serial_sequence('fleet.route_checkpoints', 'checkpoint_id'),      (SELECT max(checkpoint_id) FROM fleet.route_checkpoints));
SELECT setval(pg_get_serial_sequence('tracking.horse_welfare_logs', 'log_id'),         (SELECT max(log_id) FROM tracking.horse_welfare_logs));
SELECT setval(pg_get_serial_sequence('incident.trip_incidents', 'incident_id'),        (SELECT max(incident_id) FROM incident.trip_incidents));
-- Mức ban đầu của sự cố mẫu = mức hiện tại; biên bản thú y mẫu cũng là tài liệu VetReport
UPDATE incident.trip_incidents SET initial_severity = severity WHERE initial_severity IS NULL;
INSERT INTO incident.incident_documents (incident_id, file_path, document_type, uploaded_by_user_id, uploaded_at)
SELECT incident_id, vet_report_path, 'VetReport', COALESCE(approved_by_user_id, reported_by_user_id), COALESCE(approved_at, reported_at)
FROM incident.trip_incidents WHERE vet_report_path IS NOT NULL;
SELECT setval(pg_get_serial_sequence('acceptance.handover_acceptances', 'handover_id'), (SELECT max(handover_id) FROM acceptance.handover_acceptances));
SELECT setval(pg_get_serial_sequence('billing.payments', 'payment_id'),                 (SELECT max(payment_id) FROM billing.payments));

-- ---------- Phạm vi dịch vụ: quốc gia, sân bay, cước và giấy tờ cho các tuyến mới (R-18) ----------
-- Các nước trong phạm vi dịch vụ. Mông Cổ, Kazakhstan chủ yếu để quá cảnh (không có cước nên không làm nước đến).
INSERT INTO common.countries (country_code, name_vi, name_en, sort_order) VALUES
('VN', 'Việt Nam',        'Vietnam',              1),
('CN', 'Trung Quốc',      'China',                2),
('LA', 'Lào',             'Laos',                 3),
('KH', 'Campuchia',       'Cambodia',             4),
('TH', 'Thái Lan',        'Thailand',             5),
('MY', 'Malaysia',        'Malaysia',             6),
('SG', 'Singapore',       'Singapore',            7),
('HK', 'Hong Kong',       'Hong Kong',            8),
('JP', 'Nhật Bản',        'Japan',                9),
('KR', 'Hàn Quốc',        'South Korea',          10),
('AE', 'UAE (Dubai)',     'United Arab Emirates', 11),
('AU', 'Úc',              'Australia',            12),
('FR', 'Pháp',            'France',               13),
('GB', 'Vương quốc Anh',  'United Kingdom',       14),
('DE', 'Đức',             'Germany',              15),
('US', 'Hoa Kỳ',          'United States',        16),
('RU', 'Nga',             'Russia',               17),
('MN', 'Mông Cổ',         'Mongolia',             18),
('KZ', 'Kazakhstan',      'Kazakhstan',           19)
ON CONFLICT (country_code) DO NOTHING;

-- Sân bay nhận ngựa. Lào, Campuchia không có (gần như không có chuyến bay chở hàng nhận ngựa): chỉ đi đường bộ.
INSERT INTO common.airports (country_code, iata_code, name, city) VALUES
('VN', 'HAN', 'Sân bay Quốc tế Nội Bài',              'Hà Nội'),
('VN', 'SGN', 'Sân bay Quốc tế Tân Sơn Nhất',         'TP. Hồ Chí Minh'),
('VN', 'DAD', 'Sân bay Quốc tế Đà Nẵng',              'Đà Nẵng'),
('CN', 'CAN', 'Sân bay Quốc tế Bạch Vân',             'Quảng Châu'),
('CN', 'PVG', 'Sân bay Quốc tế Phố Đông',             'Thượng Hải'),
('CN', 'PEK', 'Sân bay Quốc tế Thủ đô Bắc Kinh',      'Bắc Kinh'),
('TH', 'BKK', 'Sân bay Quốc tế Suvarnabhumi',         'Bangkok'),
('TH', 'DMK', 'Sân bay Don Mueang',                   'Bangkok'),
('MY', 'KUL', 'Sân bay Quốc tế Kuala Lumpur',         'Kuala Lumpur'),
('SG', 'SIN', 'Sân bay Quốc tế Changi',               'Singapore'),
('HK', 'HKG', 'Sân bay Quốc tế Hong Kong',            'Hong Kong'),
('JP', 'NRT', 'Sân bay Quốc tế Narita',               'Tokyo'),
('JP', 'HND', 'Sân bay Quốc tế Haneda',               'Tokyo'),
('JP', 'KIX', 'Sân bay Quốc tế Kansai',               'Osaka'),
('KR', 'ICN', 'Sân bay Quốc tế Incheon',              'Seoul'),
('AE', 'DXB', 'Sân bay Quốc tế Dubai',                'Dubai'),
('AE', 'DWC', 'Sân bay Quốc tế Al Maktoum',           'Dubai'),
('AU', 'SYD', 'Sân bay Quốc tế Sydney',               'Sydney'),
('AU', 'MEL', 'Sân bay Quốc tế Melbourne',            'Melbourne'),
('AU', 'BNE', 'Sân bay Quốc tế Brisbane',             'Brisbane'),
('FR', 'CDG', 'Sân bay Charles de Gaulle',            'Paris'),
('FR', 'ORY', 'Sân bay Orly',                         'Paris'),
('GB', 'STN', 'Sân bay Stansted',                     'London'),
('GB', 'LHR', 'Sân bay Heathrow',                     'London'),
('DE', 'FRA', 'Sân bay Quốc tế Frankfurt',            'Frankfurt'),
('DE', 'MUC', 'Sân bay Quốc tế Munich',               'Munich'),
('US', 'JFK', 'Sân bay Quốc tế John F. Kennedy',      'New York'),
('US', 'LAX', 'Sân bay Quốc tế Los Angeles',          'Los Angeles'),
('US', 'ORD', 'Sân bay Quốc tế O''Hare',              'Chicago'),
('US', 'MIA', 'Sân bay Quốc tế Miami',                'Miami'),
('RU', 'SVO', 'Sân bay Quốc tế Sheremetyevo',         'Moscow')
ON CONFLICT (iata_code) DO NOTHING;

-- Cước mới theo quy tắc: đi liền đất và khả thi thì có đường bộ, đi được cả hai thì mở cả hai.
--   Đường bộ: Malaysia, Singapore (qua Campuchia, Thái Lan), Hong Kong (qua Trung Quốc), Việt Nam (đơn từ nước khác về VN).
--   Đường bay: Trung Quốc, Nga (Moscow), Việt Nam.
INSERT INTO pricing.price_items (category, item_code, item_name, transport_mode, dest_country_code, calc_type, value, min_horses)
SELECT v.category, v.item_code, v.item_name, v.transport_mode, v.dest_country_code, v.calc_type, v.value, NULL
FROM (VALUES
    ('Freight', 'FREIGHT', 'Zone G1 - Đường bộ (Malaysia)',  'Ground', 'MY', 'PerKmPerHorse', 1.00),
    ('Freight', 'FREIGHT', 'Zone G1 - Đường bộ (Singapore)', 'Ground', 'SG', 'PerKmPerHorse', 1.00),
    ('Freight', 'FREIGHT', 'Zone G1 - Đường bộ (Hong Kong)', 'Ground', 'HK', 'PerKmPerHorse', 1.00),
    ('Freight', 'FREIGHT', 'Zone G1 - Đường bộ (Việt Nam)',  'Ground', 'VN', 'PerKmPerHorse', 1.00),
    ('Freight', 'FREIGHT', 'Zone A - Đông Nam Á (Việt Nam)', 'Air',    'VN', 'PerHorse',      3000),
    ('Freight', 'FREIGHT', 'Zone B - Đông Á (Trung Quốc)',   'Air',    'CN', 'PerHorse',      5000),
    ('Freight', 'FREIGHT', 'Zone D - Châu Âu (Nga)',         'Air',    'RU', 'PerHorse',      8000)
) AS v (category, item_code, item_name, transport_mode, dest_country_code, calc_type, value)
WHERE NOT EXISTS (
    SELECT 1 FROM pricing.price_items p
    WHERE p.item_code = v.item_code AND p.transport_mode = v.transport_mode AND p.dest_country_code = v.dest_country_code);

-- Giấy tờ (dữ liệu minh họa) cho các tuyến mới mở:
--   nhập khẩu vào Việt Nam, quá cảnh Mông Cổ / Kazakhstan (tuyến đường bộ đi Nga).
INSERT INTO clearance.country_doc_requirements (country_code, direction, doc_type_id, validity_days, is_mandatory, regulation_note)
SELECT c.country_code, c.direction, d.doc_type_id, c.validity_days, c.is_mandatory, c.note
FROM (VALUES
    ('VN', 'Import',  'HORSE_PASSPORT', NULL, true, 'Hộ chiếu ngựa còn hiệu lực'),
    ('VN', 'Import',  'IMPORT_PERMIT',  NULL, true, 'Giấy phép kiểm dịch nhập khẩu của Cục Chăn nuôi và Thú y'),
    ('VN', 'Import',  'HEALTH_CERT',    10,   true, 'Chứng nhận kiểm dịch trong 10 ngày trước khi nhập cảnh'),
    ('VN', 'Import',  'COGGINS_EIA',    30,   true, 'EIA âm tính trong 30 ngày'),
    ('VN', 'Import',  'VACCINE',        180,  true, 'Tiêm phòng cúm ngựa trong 6 tháng'),
    ('MN', 'Transit', 'TRANSIT_PERMIT', NULL, true, 'Giấy phép quá cảnh của Tổng cục Thú y Mông Cổ'),
    ('MN', 'Transit', 'HEALTH_CERT',    10,   true, 'Chứng nhận kiểm dịch còn hạn khi vào Mông Cổ'),
    ('KZ', 'Transit', 'TRANSIT_PERMIT', NULL, true, 'Giấy phép quá cảnh của Ủy ban Kiểm soát Thú y Kazakhstan'),
    ('KZ', 'Transit', 'HEALTH_CERT',    10,   true, 'Chứng nhận kiểm dịch còn hạn khi vào Kazakhstan')
) AS c (country_code, direction, doc_code, validity_days, is_mandatory, note)
JOIN clearance.document_types d ON d.code = c.doc_code
ON CONFLICT (country_code, direction, doc_type_id) DO NOTHING;

-- Khách được đặt từ mọi nước trong phạm vi dịch vụ: nước nào chưa có quy định xuất khẩu thì thêm mức tối thiểu
-- (hộ chiếu ngựa + chứng nhận kiểm dịch xuất khẩu của cơ quan thú y nước đó). Manager sửa / bổ sung ở trang Quốc gia.
INSERT INTO clearance.country_doc_requirements (country_code, direction, doc_type_id, validity_days, is_mandatory, regulation_note)
SELECT c.country_code, 'Export', d.doc_type_id,
       CASE WHEN d.code = 'HEALTH_CERT' THEN 10 END, true,
       CASE WHEN d.code = 'HEALTH_CERT' THEN 'Cơ quan thú y nước xuất khẩu cấp trong 10 ngày trước ngày đi'
            ELSE 'Hộ chiếu ngựa còn hiệu lực' END
FROM common.countries c
CROSS JOIN clearance.document_types d
WHERE d.code IN ('HORSE_PASSPORT', 'HEALTH_CERT')
  AND NOT EXISTS (SELECT 1 FROM clearance.country_doc_requirements r WHERE r.country_code = c.country_code AND r.direction = 'Export')
ON CONFLICT (country_code, direction, doc_type_id) DO NOTHING;

-- Số ngày cần để có giấy cho các dòng mới (không ghi đè số liệu đã sửa)
UPDATE clearance.country_doc_requirements r
SET processing_days = CASE
        WHEN d.code IN ('IMPORT_PERMIT', 'TRANSIT_PERMIT') THEN 14
        WHEN d.code IN ('COGGINS_EIA', 'GLANDERS_TEST', 'PIROPLASMOSIS_TEST', 'EVA_TEST') THEN 7
        WHEN d.code = 'HEALTH_CERT' THEN 3
        ELSE NULL
    END
FROM clearance.document_types d
WHERE d.doc_type_id = r.doc_type_id
  AND r.processing_days IS NULL;
