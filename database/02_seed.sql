-- =====================================================================================
-- SCRIPT 02: SEED DATA (chạy sau script 01, trên database còn trống)
-- System: Cross-Border Racehorse Transport System (SWP391 - Group 5)
--   A. Danh mục: loại giấy tờ, quy định theo quốc gia, bảng giá, phương tiện
--   B. Tài khoản dev (6 role) và 2 con ngựa mẫu
--   C. Một kịch bản đã chạy trọn 6 luồng: BKG-2026-0001 / TRP-2026-0001 (Hà Nội -> Quảng Châu)
-- Mật khẩu chung của mọi tài khoản seed: Racehorse@123
-- CHỈ dùng cho môi trường dev/demo.
-- =====================================================================================

-- ---------- A.1 Loại giấy tờ ----------
INSERT INTO clearance.document_types (doc_type_id, code, name, description, is_mandatory) VALUES
(1, 'HORSE_PASSPORT', 'Hộ chiếu ngựa quốc tế (FEI Passport)', 'Hộ chiếu định danh cá thể do FEI cấp', true),
(2, 'COGGINS_EIA',    'Chứng nhận xét nghiệm Coggins âm tính', 'Xét nghiệm thiếu máu truyền nhiễm ngựa trong 30 ngày', true),
(3, 'HEALTH_CERT',    'Giấy chứng nhận kiểm dịch xuất nhập khẩu', 'Cục Thú Y cấp trước ngày đi', true),
(4, 'VACCINE',        'Chứng nhận tiêm phòng cúm ngựa', 'Lịch tiêm phòng đầy đủ trong 6 tháng', true);

-- ---------- A.2 Quy định giấy tờ theo quốc gia ----------
INSERT INTO clearance.country_doc_requirements (country_code, direction, doc_type_id, validity_days, is_mandatory, regulation_note)
SELECT c.country_code, c.direction, d.doc_type_id, c.validity_days, true, c.note
FROM (VALUES
    ('VN', 'Export', 'HORSE_PASSPORT', NULL, 'Hộ chiếu ngựa còn hiệu lực'),
    ('VN', 'Export', 'HEALTH_CERT',    10,   'Cục Thú y cấp trong 10 ngày trước ngày đi'),
    ('CN', 'Import', 'COGGINS_EIA',    30,   'Xét nghiệm EIA âm tính trong 30 ngày'),
    ('CN', 'Import', 'VACCINE',        180,  'Tiêm phòng cúm ngựa trong 6 tháng'),
    ('FR', 'Import', 'HORSE_PASSPORT', NULL, 'EU Regulation 2020/692'),
    ('FR', 'Import', 'COGGINS_EIA',    30,   'EU Regulation 2020/692'),
    ('FR', 'Import', 'HEALTH_CERT',    2,    'Chứng nhận sức khỏe trong 48 giờ trước khi bay'),
    ('FR', 'Import', 'VACCINE',        180,  'Equine Influenza')
) AS c (country_code, direction, doc_code, validity_days, note)
JOIN clearance.document_types d ON d.code = c.doc_code;

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
-- Hệ số hạng chuồng
('StallClass', 'STALL_SHARED',  'Chuồng ghép (Shared) - 3 ngựa / pallet',   NULL, NULL, 'Multiplier', 1.00, NULL),
('StallClass', 'STALL_COMFORT', 'Chuồng rưỡi (Comfort) - 2 ngựa / pallet',  NULL, NULL, 'Multiplier', 1.40, NULL),
('StallClass', 'STALL_PRIVATE', 'Chuồng riêng (Private) - 1 ngựa / pallet', NULL, NULL, 'Multiplier', 2.20, NULL),
-- Phụ phí trên cước
('Surcharge', 'SUR_FUEL',    'Phụ phí nhiên liệu',                          NULL, NULL, 'Percent', 8,  NULL),
('Surcharge', 'SUR_CLIMATE', 'Phụ phí khoang điều hòa / chăm sóc đặc biệt', NULL, NULL, 'Percent', 10, NULL),
('Surcharge', 'SUR_EXPRESS', 'Phụ phí gấp (khởi hành dưới 7 ngày)',         NULL, NULL, 'Percent', 20, NULL),
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
-- Áp tải (1 groom / 3 ngựa)
('Escort', 'ESCORT', 'Groom bay kèm',                     'Air',    NULL, 'PerGroom',    900, NULL),
('Escort', 'ESCORT', 'Groom đi kèm đường bộ (theo ngày)', 'Ground', NULL, 'PerGroomDay', 120, NULL),
-- Bảo hiểm
('Insurance', 'INS_TRANSIT', 'Bảo hiểm hành trình (% giá trị khai báo)', NULL, NULL, 'Percent', 1, NULL);

-- ---------- A.4 Phương tiện ----------
INSERT INTO fleet.transport_assets (asset_id, asset_code, asset_type, capacity_horses, has_climate_control, has_gps_tracker, status, last_sanitization_date) VALUES
(1, '29B-888.99', 'HorseTruck_AirSuspension', 6, true, true, 'Available', now());

-- ---------- B.1 Tài khoản dev (cùng một mật khẩu, đã băm bằng BCrypt) ----------
INSERT INTO identity.users (user_id, full_name, email, password_hash, phone_number, role) VALUES
(1, 'Trần Quốc Bảo',     'bao.tran@logistics.com',        '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84912000001', 'LogisticsManager'),
(2, 'Nguyễn Thị Mai',    'mai.nguyen@clearance.com',      '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84912000002', 'TransportSpecialist'),
(3, 'Lê Hoàng Nam',      'nam.le@fleet.com',              '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84912000003', 'FleetCoordinator'),
(4, 'Phạm Văn Đức',      'duc.pham@transport.com',        '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84912000004', 'DriverEscort'),
(5, 'Jane Smith',        'jane.smith@racehorseowner.com', '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84988111222', 'Customer'),
(6, 'Quản trị hệ thống', 'admin@logistics.com',           '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84912000000', 'Admin');

-- ---------- B.2 Ngựa của Jane Smith ----------
INSERT INTO racehorse.horses (horse_id, owner_user_id, name, breed, gender, date_of_birth, color, microchip_number, passport_number, special_care_requirements) VALUES
(1, 5, 'Red Flash (Tia Chớp Đỏ)',  'Thoroughbred', 'Stallion', '2020-04-15', 'Hồng sắc', '982000412345671', 'FEI-VN-2023-01', 'Cần lót đệm rơm dày, nhiệt độ cabin 16-19°C'),
(2, 5, 'Golden Pegasus (Kim Mã)', 'Thoroughbred', 'Mare',     '2021-03-10', 'Bạch sắc', '982000412345672', 'FEI-VN-2023-02', 'Dễ say xe nhẹ, cần treo bao cỏ tươi');

-- =====================================================================================
-- C. KỊCH BẢN ĐÃ HOÀN TẤT 6 LUỒNG
-- =====================================================================================

-- Flow 1: Jane Smith đặt chuyến 2 ngựa sang Quảng Châu; Manager Bảo duyệt, giao Mai (hồ sơ) và Nam (lộ trình)
INSERT INTO booking.booking_requests (booking_id, booking_code, customer_user_id, pickup_address, pickup_country_code, dropoff_address, dropoff_country_code,
                                      departure_date, delivery_date, total_horses, estimated_cost, status,
                                      reviewed_by_user_id, reviewed_at, assigned_specialist_id, assigned_coordinator_id, created_at) VALUES
(1, 'BKG-2026-0001', 5, 'Trang trại Yên Bài, Ba Vì, Hà Nội', 'VN', 'Trường đua Tùng Hóa, TP. Quảng Châu', 'CN',
 '2026-10-01 06:00:00+07', '2026-10-02 18:00:00+08', 2, 7500.00, 'Completed',
 1, '2026-09-20 09:00:00+07', 2, 3, '2026-09-19 14:00:00+07');

INSERT INTO booking.booking_horses (booking_horse_id, booking_id, horse_id, notes, status) VALUES
(1, 1, 1, 'Chuồng 1 bên trái', 'Approved'),
(2, 1, 2, 'Chuồng 2 ở giữa',   'Approved');

-- Flow 2: Mai lập hồ sơ, khách nộp giấy tờ, Mai duyệt, cơ quan chức năng thông quan
INSERT INTO clearance.digital_dossiers (dossier_id, dossier_code, booking_horse_id, specialist_user_id, status, clearance_number, cleared_at, created_at) VALUES
(1, 'DOS-2026-0001', 1, 2, 'Cleared', 'VET-CERT-VN-CN-2026-001', '2026-09-28 16:00:00+07', '2026-09-20 10:00:00+07'),
(2, 'DOS-2026-0002', 2, 2, 'Cleared', 'VET-CERT-VN-CN-2026-001', '2026-09-28 16:00:00+07', '2026-09-20 10:00:00+07');

INSERT INTO clearance.dossier_documents (document_id, dossier_id, doc_type_id, document_number, file_url, uploaded_by_user_id, uploaded_at, status, reviewed_by_user_id, reviewed_at) VALUES
(1, 1, 1, 'FEI-PASSPORT-982000412345671', 'https://storage.racehorse.vn/docs/rf_passport.pdf', 5, '2026-09-22 08:00:00+07', 'Approved', 2, '2026-09-23 10:00:00+07'),
(2, 1, 2, 'COGGINS-LAB-2026-888',         'https://storage.racehorse.vn/docs/rf_coggins.pdf',  5, '2026-09-22 08:05:00+07', 'Approved', 2, '2026-09-23 10:05:00+07'),
(3, 2, 1, 'FEI-PASSPORT-982000412345672', 'https://storage.racehorse.vn/docs/gp_passport.pdf', 5, '2026-09-22 08:10:00+07', 'Approved', 2, '2026-09-23 10:10:00+07');

-- Flow 3: Nam lập kế hoạch chuyến, Bảo duyệt. Flow 6: Bảo đóng chuyến
-- (trễ 30 phút, chưa quá ngưỡng 30 nên vẫn OnTime; chi phí thực tế = kế hoạch 4200 + phát sinh sự cố 400)
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

INSERT INTO fleet.trip_horses (trip_horse_id, trip_id, booking_horse_id, stall_slot_number, notes) VALUES
(1, 1, 1, 1, 'Red Flash - Chuồng số 1'),
(2, 1, 2, 2, 'Golden Pegasus - Chuồng số 2');

-- Flow 4 + 5: mốc Hữu Nghị (2) bị hủy do tắc biên, thay bằng mốc Tân Thanh (5)
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

-- Flow 6: khách ký biên bản e-POD
INSERT INTO acceptance.handover_acceptances (handover_id, trip_id, booking_id, handover_date_time, recipient_name, recipient_phone, recipient_id_card, digital_signature_url, overall_condition, remarks, driver_user_id) VALUES
(1, 1, 1, '2026-10-02 19:00:00+08', 'Jane Smith', '+84988111222', 'PASSPORT-US-991283', 'https://storage.racehorse.vn/signatures/epod_trip1.png', 'Excellent', '2 cá thể ngựa đến nơi an toàn tuyệt đối, không trầy xước.', 4);

INSERT INTO common.system_notifications (recipient_user_id, title, message, reference_type, reference_id, is_read, created_at) VALUES
(5, 'Đơn BKG-2026-0001 đã được phê duyệt', 'Yêu cầu vận chuyển của bạn đã được phê duyệt và phân công nhân sự phụ trách.', 'Booking', 1, true,  '2026-09-20 09:00:00+07'),
(5, 'Ngựa đã tới điểm bàn giao',           'Chuyến TRP-2026-0001 đã tới Trường đua Tùng Hóa. Vui lòng kiểm tra và ký biên bản bàn giao.', 'Trip', 1, false, '2026-10-02 18:30:00+08');

-- Bộ đếm sinh mã: khớp với các mã đã dùng ở trên (BKG-2026-0001, DOS-2026-0002, TRP-2026-0001, INC-2026-0001)
INSERT INTO common.code_counters (prefix, year, last_value) VALUES
('BKG', 2026, 1),
('DOS', 2026, 2),
('TRP', 2026, 1),
('INC', 2026, 1);

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
SELECT setval(pg_get_serial_sequence('fleet.transport_assets', 'asset_id'),            (SELECT max(asset_id) FROM fleet.transport_assets));
SELECT setval(pg_get_serial_sequence('fleet.trips', 'trip_id'),                        (SELECT max(trip_id) FROM fleet.trips));
SELECT setval(pg_get_serial_sequence('fleet.trip_horses', 'trip_horse_id'),            (SELECT max(trip_horse_id) FROM fleet.trip_horses));
SELECT setval(pg_get_serial_sequence('fleet.route_checkpoints', 'checkpoint_id'),      (SELECT max(checkpoint_id) FROM fleet.route_checkpoints));
SELECT setval(pg_get_serial_sequence('tracking.horse_welfare_logs', 'log_id'),         (SELECT max(log_id) FROM tracking.horse_welfare_logs));
SELECT setval(pg_get_serial_sequence('incident.trip_incidents', 'incident_id'),        (SELECT max(incident_id) FROM incident.trip_incidents));
SELECT setval(pg_get_serial_sequence('acceptance.handover_acceptances', 'handover_id'), (SELECT max(handover_id) FROM acceptance.handover_acceptances));
