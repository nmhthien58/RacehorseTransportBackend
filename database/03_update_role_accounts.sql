-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT cho database đã dựng trước khi 02_seed.sql có 6 tài khoản theo vai trò
-- Thêm mỗi vai trò một tài khoản dễ nhớ (tên = vai trò), mật khẩu chung Racehorse@123.
-- Database dựng mới bằng 02_seed.sql đã có sẵn: không cần chạy file này.
-- Không xóa dữ liệu nào. Chạy lại nhiều lần không sao (email đã có thì bỏ qua).
-- =====================================================================================

-- Đồng bộ sequence với ID lớn nhất hiện có để ID tự sinh không trùng
SELECT setval(pg_get_serial_sequence('identity.users', 'user_id'), (SELECT max(user_id) FROM identity.users));

INSERT INTO identity.users (full_name, email, password_hash, phone_number, role) VALUES
('Customer',             'customer@racehorsetransport.com',    '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000001', 'Customer'),
('Logistics Manager',    'manager@racehorsetransport.com',     '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000002', 'LogisticsManager'),
('Transport Specialist', 'specialist@racehorsetransport.com',  '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000003', 'TransportSpecialist'),
('Fleet Coordinator',    'coordinator@racehorsetransport.com', '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000004', 'FleetCoordinator'),
('Driver Escort',        'driver@racehorsetransport.com',      '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000005', 'DriverEscort'),
('Admin',                'admin@racehorsetransport.com',       '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000006', 'Admin')
ON CONFLICT (email) DO NOTHING;
