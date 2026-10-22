-- =====================================================================================
-- SCRIPT 03: THÊM 8 PHƯƠNG TIỆN (xe tải ngựa, xe van, khoang bay)
-- Đủ xe trống để lập nhiều chuyến cùng lúc. Tất cả để Available.
-- Không xóa dữ liệu nào. Chạy lại nhiều lần không sao (asset_code đã có thì bỏ qua).
-- =====================================================================================

-- Đồng bộ sequence với ID lớn nhất hiện có để ID tự sinh không trùng
SELECT setval(pg_get_serial_sequence('fleet.transport_assets', 'asset_id'), (SELECT max(asset_id) FROM fleet.transport_assets));

INSERT INTO fleet.transport_assets (asset_code, asset_type, capacity_horses, has_climate_control, has_gps_tracker, status, last_sanitization_date) VALUES
('29C-123.45',       'HorseTruck_AirSuspension', 6, true,  true, 'Available', now()),
('51D-678.90',       'HorseTruck_AirSuspension', 8, true,  true, 'Available', now()),
('30K-246.13',       'HorseTruck_AirSuspension', 4, true,  true, 'Available', now()),
('43C-357.91',       'HorseTruck_AirSuspension', 6, true,  true, 'Available', now()),
('51F-112.34',       'HorseVan',                 2, true,  true, 'Available', now()),
('29H-998.76',       'HorseVan',                 3, false, true, 'Available', now()),
('AIR-STALL-SGN-03', 'AirStall',                 3, true,  true, 'Available', now()),
('AIR-STALL-HAN-04', 'AirStall',                 2, true,  true, 'Available', now())
ON CONFLICT (asset_code) DO NOTHING;
