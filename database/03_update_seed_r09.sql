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
