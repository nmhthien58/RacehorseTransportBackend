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
INSERT INTO fleet.transport_assets (asset_id, asset_code, asset_type, capacity_horses, compartment_count, has_climate_control, has_gps_tracker, status, last_sanitization_date) VALUES
(1, '29B-888.99',       'HorseTruck_AirSuspension', 6, 2, true,  true,  'Available',   now()),
(2, '51C-246.80',       'HorseVan',                 2, 1, false, true,  'Available',   '2026-09-25 08:00:00+07'),
(3, 'AIR-STALL-SGN-01', 'AirStall',                 3, 1, true,  true,  'Available',   '2026-09-27 14:00:00+07'),
(4, '30H-567.89',       'HorseTruck_AirSuspension', 8, 3, true,  true,  'Maintenance', '2026-09-10 09:00:00+07'),
(5, 'AIR-STALL-HAN-02', 'AirStall',                 2, 1, true,  false, 'Available',   '2026-09-29 10:30:00+07');

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
INSERT INTO fleet.trip_horses (trip_horse_id, trip_id, booking_horse_id, compartment_no, position_no, stall_slot_number, notes, escort_user_id, status, picked_up_at) VALUES
(1, 1, 1, 1, 1, 1, 'Red Flash - Chuồng số 1',      4, 'InTransit', '2026-10-01 06:30:00+07'),
(2, 1, 2, 1, 2, 2, 'Golden Pegasus - Chuồng số 2', 4, 'InTransit', '2026-10-01 06:30:00+07');

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
('Policy.Version',                       '1.2',   'Phiên bản Chính sách dịch vụ hiện hành (lưu vào đơn khi khách đồng ý)'),
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

-- BE-18: dữ liệu demo cho các luồng R09-R16.
-- Script chỉ thêm dữ liệu mẫu, không đổi schema, và có thể chạy nhiều lần.

BEGIN;

-- 1) Đơn đã duyệt sơ bộ, đang chờ Coordinator lập và trình tuyến để phát hành báo giá chốt.
INSERT INTO booking.booking_requests
    (booking_code, customer_user_id, pickup_address, pickup_country_code, dropoff_address, dropoff_country_code,
     pickup_window_start, pickup_window_end, delivery_deadline, total_horses, transport_mode, distance_km,
     estimated_cost, quote_min, quote_max, status, reviewed_by_user_id, reviewed_at,
     assigned_specialist_id, assigned_coordinator_id, policy_version, policy_accepted_at, created_at)
SELECT 'BKG-2026-0101', u.user_id, 'Trại ngựa Ba Vì, Hà Nội', 'VN', 'Vientiane Racecourse, Viêng Chăn', 'LA',
       now() + interval '14 days', now() + interval '16 days', now() + interval '20 days',
       1, 'Ground', 760, 0, 2600, 3200, 'PreApproved', m.user_id, now() - interval '1 day',
       s.user_id, c.user_id, '1.1', now() - interval '2 days', now() - interval '2 days'
FROM identity.users u
JOIN identity.users m ON m.email = 'manager@racehorsetransport.com'
JOIN identity.users s ON s.email = 'specialist@racehorsetransport.com'
JOIN identity.users c ON c.email = 'coordinator@racehorsetransport.com'
WHERE u.email = 'customer@racehorsetransport.com'
ON CONFLICT (booking_code) DO NOTHING;

-- 2) Đơn đã có báo giá chốt, còn 12 giờ là hết hạn.
INSERT INTO booking.booking_requests
    (booking_code, customer_user_id, pickup_address, pickup_country_code, dropoff_address, dropoff_country_code,
     pickup_window_start, pickup_window_end, delivery_deadline, total_horses, transport_mode, distance_km,
     estimated_cost, quote_min, quote_max, quoted_at, quote_expires_at, quote_breakdown,
     status, reviewed_by_user_id, reviewed_at, assigned_specialist_id, assigned_coordinator_id,
     policy_version, policy_accepted_at, estimated_transit_hours, created_at)
SELECT 'BKG-2026-0102', u.user_id, 'Trại ngựa Củ Chi, TP.HCM', 'VN', 'Royal Bangkok Sports Club, Bangkok', 'TH',
       now() + interval '10 days', now() + interval '12 days', now() + interval '17 days',
       1, 'Ground', 920, 4100, 3700, 4500, now() - interval '60 hours', now() + interval '12 hours',
       '[{"code":"FREIGHT","name":"Cước vận chuyển","amount":3900},{"code":"FEE_CLEARANCE","name":"Phí thủ tục","amount":200}]'::jsonb,
       'Quoted', m.user_id, now() - interval '4 days', s.user_id, c.user_id,
       '1.1', now() - interval '5 days', 26, now() - interval '5 days'
FROM identity.users u
JOIN identity.users m ON m.email = 'manager@racehorsetransport.com'
JOIN identity.users s ON s.email = 'specialist@racehorsetransport.com'
JOIN identity.users c ON c.email = 'coordinator@racehorsetransport.com'
WHERE u.email = 'customer@racehorsetransport.com'
ON CONFLICT (booking_code) DO NOTHING;

-- 3-5) Các đơn nền cho sự cố đổi xe + claim, giao trễ có bồi thường, và giao thất bại có phí chờ.
INSERT INTO booking.booking_requests
    (booking_code, customer_user_id, pickup_address, pickup_country_code, dropoff_address, dropoff_country_code,
     pickup_window_start, pickup_window_end, delivery_deadline, total_horses, transport_mode, distance_km,
     estimated_cost, status, reviewed_by_user_id, reviewed_at, assigned_specialist_id,
     assigned_coordinator_id, policy_version, policy_accepted_at, deposit_percent, deposit_amount, created_at)
SELECT x.booking_code, u.user_id, x.pickup_address, 'VN', x.dropoff_address, x.country_code,
       now() - x.start_ago, now() - x.end_ago, now() - x.deadline_ago, 1, 'Ground', x.distance_km,
       x.estimated_cost, x.status, m.user_id, now() - interval '20 days', s.user_id, c.user_id,
       '1.1', now() - interval '25 days', 30, x.estimated_cost * 0.30, now() - interval '25 days'
FROM (VALUES
    ('BKG-2026-0103', 'Trại ngựa Sóc Sơn, Hà Nội', 'Trường đua Nam Ninh, Quảng Tây', 'CN', interval '4 days', interval '3 days', interval '1 day', 860, 4800::numeric, 'Approved'),
    ('BKG-2026-0104', 'Trại ngựa Đức Hòa, Long An', 'Singapore Turf Club, Singapore', 'SG', interval '10 days', interval '9 days', interval '3 days', 2050, 8200::numeric, 'Completed'),
    ('BKG-2026-0105', 'Trại ngựa Hóc Môn, TP.HCM', 'Selangor Turf Club, Kuala Lumpur', 'MY', interval '7 days', interval '6 days', interval '1 day', 1480, 6500::numeric, 'AwaitingFinalPayment')
) AS x(booking_code, pickup_address, dropoff_address, country_code, start_ago, end_ago, deadline_ago, distance_km, estimated_cost, status)
JOIN identity.users u ON u.email = 'customer@racehorsetransport.com'
JOIN identity.users m ON m.email = 'manager@racehorsetransport.com'
JOIN identity.users s ON s.email = 'specialist@racehorsetransport.com'
JOIN identity.users c ON c.email = 'coordinator@racehorsetransport.com'
ON CONFLICT (booking_code) DO NOTHING;

-- Mỗi đơn demo dùng một ngựa khác nhau để màn hình dễ phân biệt.
INSERT INTO booking.booking_horses (booking_id, horse_id, notes, status, stall_class, declared_value, agreed_value, insurance_plan)
SELECT b.booking_id, h.horse_id, x.notes, x.status, x.stall_class, x.declared_value, x.agreed_value, x.insurance_plan
FROM (VALUES
    ('BKG-2026-0101', '704001000000003', 'Đơn chờ báo giá chốt', 'Approved', 'Shared', 25000::numeric, 25000::numeric, 'None'),
    ('BKG-2026-0102', '704001000000004', 'Báo giá hết hạn trong 12 giờ', 'Approved', 'Comfort', 45000::numeric, 45000::numeric, 'Full'),
    ('BKG-2026-0103', '704001000000005', 'Ngựa trên chuyến có sự cố đổi xe', 'Approved', 'Private', 80000::numeric, 75000::numeric, 'Full'),
    ('BKG-2026-0104', '704001000000006', 'Đơn giao trễ được tính bồi thường', 'Approved', 'Comfort', 40000::numeric, 40000::numeric, 'Full'),
    ('BKG-2026-0105', '704001000000007', 'Người nhận vắng mặt, phát sinh phí chờ', 'Approved', 'Shared', 30000::numeric, 30000::numeric, 'None')
) AS x(booking_code, passport_number, notes, status, stall_class, declared_value, agreed_value, insurance_plan)
JOIN booking.booking_requests b ON b.booking_code = x.booking_code
JOIN racehorse.horses h ON h.passport_number = x.passport_number
ON CONFLICT (booking_id, horse_id) DO NOTHING;

-- Tuyến đã duyệt của đơn có báo giá sắp hết hạn.
INSERT INTO booking.route_plans
    (booking_id, version, status, transit_country_codes, total_distance_km, estimated_hours,
     is_estimate, notes, planned_by_user_id, reviewed_by_user_id, reviewed_at, created_at, updated_at)
SELECT b.booking_id, 1, 'Approved', '["KH"]'::jsonb, 920, 26, false,
       'Tuyến demo đã duyệt để phát hành báo giá chốt', c.user_id, m.user_id,
       now() - interval '61 hours', now() - interval '4 days', now() - interval '61 hours'
FROM booking.booking_requests b
JOIN identity.users c ON c.email = 'coordinator@racehorsetransport.com'
JOIN identity.users m ON m.email = 'manager@racehorsetransport.com'
WHERE b.booking_code = 'BKG-2026-0102'
ON CONFLICT (booking_id, version) DO NOTHING;

INSERT INTO booking.route_plan_stops
    (route_plan_id, sequence_order, stop_name, stop_type, address, latitude, longitude, country_code,
     distance_km_from_previous, duration_hours_from_previous, rest_minutes)
SELECT rp.route_plan_id, x.sequence_order, x.stop_name, x.stop_type, x.address, x.latitude, x.longitude,
       x.country_code, x.distance_km, x.duration_hours, x.rest_minutes
FROM booking.route_plans rp
JOIN booking.booking_requests b ON b.booking_id = rp.booking_id AND b.booking_code = 'BKG-2026-0102'
CROSS JOIN (VALUES
    (1, 'Củ Chi', 'Origin', 'Củ Chi, TP.HCM', 11.006000::numeric, 106.514000::numeric, 'VN', 0::numeric, 0::numeric, 0),
    (2, 'Cửa khẩu Mộc Bài', 'BorderGate', 'Bến Cầu, Tây Ninh', 11.079000::numeric, 106.171000::numeric, 'VN', 95::numeric, 2::numeric, 60),
    (3, 'Royal Bangkok Sports Club', 'Destination', 'Bangkok, Thailand', 13.733000::numeric, 100.535000::numeric, 'TH', 825::numeric, 24::numeric, 0)
) AS x(sequence_order, stop_name, stop_type, address, latitude, longitude, country_code, distance_km, duration_hours, rest_minutes)
WHERE rp.version = 1
ON CONFLICT (route_plan_id, sequence_order) DO NOTHING;

INSERT INTO booking.booking_contracts
    (contract_code, booking_id, version, status, reason, file_path, file_sha256, policy_version, created_at)
SELECT 'CTR-2026-0102', b.booking_id, 1, 'Draft', 'Initial',
       'contracts/BKG-2026-0102/v1.pdf', repeat('0', 64), '1.1', now() - interval '60 hours'
FROM booking.booking_requests b WHERE b.booking_code = 'BKG-2026-0102'
ON CONFLICT (contract_code) DO NOTHING;

-- Ba chuyến demo nghiệp vụ sau vận chuyển.
INSERT INTO fleet.trips
    (trip_code, vehicle_id, planned_start_date, planned_end_date, actual_start_date, actual_end_date,
     planned_cost, actual_cost, overall_status, delay_minutes, on_time_status, kpi_score,
     closed_by_user_id, closed_at, executive_remarks, created_at, planned_by_user_id, approved_by_user_id, approved_at)
SELECT x.trip_code, a.asset_id, now() - x.planned_start_ago, now() - x.planned_end_ago,
       now() - x.actual_start_ago, CASE WHEN x.actual_end_ago IS NULL THEN NULL ELSE now() - x.actual_end_ago END,
       x.planned_cost, x.actual_cost, x.overall_status, x.delay_minutes, x.on_time_status, x.kpi_score,
       CASE WHEN x.closed THEN m.user_id END, CASE WHEN x.closed THEN now() - interval '2 days' END,
       x.remarks, now() - interval '30 days', c.user_id, m.user_id, now() - interval '28 days'
FROM (VALUES
    ('TRP-2026-0101', '29B-888.99', interval '2 days', interval '-1 day', interval '2 days', NULL::interval, 3000::numeric, 3500::numeric, 'InTransit', 0, 'OnTime', 100::numeric, false, 'Đang xử lý sự cố đổi xe'),
    ('TRP-2026-0102', 'AIR-STALL-SGN-01', interval '8 days', interval '5 days', interval '8 days', interval '2 days', 6000::numeric, 6400::numeric, 'Completed', 72 * 60, 'LateCompanyFault', 70::numeric, true, 'Giao trễ 72 giờ do lỗi công ty; đã tạo khoản bồi thường'),
    ('TRP-2026-0103', 'AIR-STALL-HAN-02', interval '5 days', interval '2 days', interval '5 days', NULL::interval, 4500::numeric, 4800::numeric, 'ArrivedDestination', 0, 'OnTime', 100::numeric, false, 'Chờ khách chọn cách xử lý giao thất bại')
) AS x(trip_code, asset_code, planned_start_ago, planned_end_ago, actual_start_ago, actual_end_ago, planned_cost, actual_cost, overall_status, delay_minutes, on_time_status, kpi_score, closed, remarks)
JOIN fleet.transport_assets a ON a.asset_code = x.asset_code
JOIN identity.users c ON c.email = 'coordinator@racehorsetransport.com'
JOIN identity.users m ON m.email = 'manager@racehorsetransport.com'
ON CONFLICT (trip_code) DO NOTHING;

INSERT INTO fleet.trip_horses (trip_id, booking_horse_id, stall_slot_number, notes, escort_user_id, status, picked_up_at)
SELECT t.trip_id, bh.booking_horse_id, 1, x.notes, d.user_id, x.status, now() - x.picked_up_ago
FROM (VALUES
    ('TRP-2026-0101', 'BKG-2026-0103', 'Sự cố đổi xe giữa hành trình', 'InTransit', interval '2 days'),
    ('TRP-2026-0102', 'BKG-2026-0104', 'Đã giao trễ', 'Delivered', interval '8 days'),
    ('TRP-2026-0103', 'BKG-2026-0105', 'Đang chờ bàn giao lại', 'InTransit', interval '5 days')
) AS x(trip_code, booking_code, notes, status, picked_up_ago)
JOIN fleet.trips t ON t.trip_code = x.trip_code
JOIN booking.booking_requests b ON b.booking_code = x.booking_code
JOIN booking.booking_horses bh ON bh.booking_id = b.booking_id
JOIN identity.users d ON d.email = 'driver@racehorsetransport.com'
ON CONFLICT (trip_id, booking_horse_id) DO NOTHING;

INSERT INTO fleet.route_checkpoints
    (trip_id, sequence_order, checkpoint_name, checkpoint_type, address, latitude, longitude,
     planned_time, mandatory_rest_minutes, actual_arrival_time, status, is_active)
SELECT t.trip_id, 1, 'Selangor Turf Club', 'Destination', 'Kuala Lumpur, Malaysia',
       3.139000, 101.687000, now() - interval '2 days', 0, now() - interval '26 hours', 'Arrived', true
FROM fleet.trips t WHERE t.trip_code = 'TRP-2026-0103'
  AND NOT EXISTS (SELECT 1 FROM fleet.route_checkpoints rc WHERE rc.trip_id = t.trip_id AND rc.sequence_order = 1 AND rc.is_active);

-- Sự cố đã được duyệt: xe cũ hỏng, xe thay thế là 29B-888.99 (vehicle hiện tại của chuyến).
INSERT INTO incident.trip_incidents
    (incident_code, trip_id, reported_by_user_id, incident_type, description, location, proposed_action,
     response_action, action_payload, additional_cost, status, approved_by_user_id, approved_at,
     reported_at, severity, affected_trip_horse_id, fault_party, fault_note)
SELECT 'INC-2026-0101', t.trip_id, d.user_id, 'MechanicalBreakdown',
       'Xe 30H-567.89 hỏng hệ thống treo, không thể tiếp tục hành trình', 'QL1A, Lạng Sơn',
       'Điều xe 29B-888.99 thay thế và chuyển ngựa sang xe mới', 'ReplaceVehicle',
       jsonb_build_object('previousVehicleId', old_asset.asset_id, 'replacementVehicleId', replacement.asset_id),
       500, 'Approved', m.user_id, now() - interval '20 hours', now() - interval '21 hours',
       'Major', th.trip_horse_id, 'Company', 'Hỏng phương tiện thuộc trách nhiệm công ty'
FROM fleet.trips t
JOIN fleet.trip_horses th ON th.trip_id = t.trip_id
JOIN fleet.transport_assets old_asset ON old_asset.asset_code = '30H-567.89'
JOIN fleet.transport_assets replacement ON replacement.asset_code = '29B-888.99'
JOIN identity.users d ON d.email = 'driver@racehorsetransport.com'
JOIN identity.users m ON m.email = 'manager@racehorsetransport.com'
WHERE t.trip_code = 'TRP-2026-0101'
ON CONFLICT (incident_code) DO NOTHING;

-- Hồ sơ bồi thường bảo hiểm cho con ngựa bị thương trong sự cố đổi xe.
INSERT INTO billing.insurance_claims
    (claim_code, booking_id, booking_horse_id, incident_id, claim_type, insurance_plan, agreed_value,
     fault_party, description, requested_amount, status, evidence_paths, opened_by_user_id, opened_at)
SELECT 'CLM-2026-0101', b.booking_id, bh.booking_horse_id, i.incident_id, 'Injury', 'Full', bh.agreed_value,
       'Company', 'Ngựa bị xây xát khi chuyển sang xe thay thế; yêu cầu thanh toán chi phí thú y',
       1200, 'UnderReview', '["claims/CLM-2026-0101/vet-report.pdf","claims/CLM-2026-0101/photo.jpg"]'::jsonb,
       u.user_id, now() - interval '18 hours'
FROM booking.booking_requests b
JOIN booking.booking_horses bh ON bh.booking_id = b.booking_id
JOIN fleet.trip_horses th ON th.booking_horse_id = bh.booking_horse_id
JOIN incident.trip_incidents i ON i.trip_id = th.trip_id AND i.incident_code = 'INC-2026-0101'
JOIN identity.users u ON u.email = 'customer@racehorsetransport.com'
WHERE b.booking_code = 'BKG-2026-0103'
ON CONFLICT (claim_code) DO NOTHING;

-- Đơn giao trễ do lỗi công ty, khoản bồi thường đang chờ Manager duyệt.
INSERT INTO billing.delay_compensations
    (booking_id, trip_id, delay_hours, freight_amount, percent, calculated_amount,
     currency_code, status, created_at)
SELECT b.booking_id, t.trip_id, 72, 7600, 25, 1900, 'USD', 'AwaitingApproval', now() - interval '2 days'
FROM booking.booking_requests b
JOIN booking.booking_horses bh ON bh.booking_id = b.booking_id
JOIN fleet.trip_horses th ON th.booking_horse_id = bh.booking_horse_id
JOIN fleet.trips t ON t.trip_id = th.trip_id
WHERE b.booking_code = 'BKG-2026-0104'
ON CONFLICT (booking_id, trip_id) DO NOTHING;

-- Người nhận vắng mặt sau 5 giờ chờ: miễn 2 giờ, thu 3 x 40 USD = 120 USD.
INSERT INTO acceptance.delivery_failures
    (trip_id, booking_id, checkpoint_id, reason, note, photo_paths, recorded_by_user_id,
     recorded_at, waiting_hours, waiting_fee, customer_pays, decision_due_at, decision_cost)
SELECT t.trip_id, b.booking_id, rc.checkpoint_id, 'RecipientAbsent',
       'Không liên lạc được người nhận sau 5 giờ chờ tại điểm giao',
       '["delivery-failures/BKG-2026-0105/arrival.jpg"]'::jsonb, d.user_id,
       now() - interval '21 hours', 5, 120, true, now() + interval '3 hours', 120
FROM booking.booking_requests b
JOIN booking.booking_horses bh ON bh.booking_id = b.booking_id
JOIN fleet.trip_horses th ON th.booking_horse_id = bh.booking_horse_id
JOIN fleet.trips t ON t.trip_id = th.trip_id
JOIN fleet.route_checkpoints rc ON rc.trip_id = t.trip_id AND rc.checkpoint_type = 'Destination' AND rc.is_active
JOIN identity.users d ON d.email = 'driver@racehorsetransport.com'
WHERE b.booking_code = 'BKG-2026-0105'
  AND NOT EXISTS (SELECT 1 FROM acceptance.delivery_failures df WHERE df.booking_id = b.booking_id AND df.trip_id = t.trip_id);

-- Bộ đếm và identity phải đứng sau các ID sinh tự động của dữ liệu demo.
INSERT INTO common.code_counters (prefix, year, last_value) VALUES
    ('BKG', 2026, 105), ('TRP', 2026, 103), ('INC', 2026, 101), ('CLM', 2026, 101)
ON CONFLICT (prefix, year) DO UPDATE SET last_value = GREATEST(common.code_counters.last_value, EXCLUDED.last_value);

COMMIT;
