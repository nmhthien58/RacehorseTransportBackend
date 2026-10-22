-- =====================================================================================
-- SCRIPT 03: THÊM 8 TÀI KHOẢN DRIVER ESCORT (driver2 ... driver9)
-- Đủ người để phân công Driver / Escort cho nhiều chuyến cùng lúc.
-- Mật khẩu chung Racehorse@123 (cùng hash với các tài khoản seed khác).
-- Không xóa dữ liệu nào. Chạy lại nhiều lần không sao (email đã có thì bỏ qua).
-- =====================================================================================

-- Đồng bộ sequence với ID lớn nhất hiện có để ID tự sinh không trùng
SELECT setval(pg_get_serial_sequence('identity.users', 'user_id'), (SELECT max(user_id) FROM identity.users));

INSERT INTO identity.users (full_name, email, password_hash, phone_number, role) VALUES
('Trần Văn Hùng',    'driver2@racehorsetransport.com', '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000102', 'DriverEscort'),
('Lê Minh Tuấn',     'driver3@racehorsetransport.com', '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000103', 'DriverEscort'),
('Nguyễn Đức Long',  'driver4@racehorsetransport.com', '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000104', 'DriverEscort'),
('Phạm Quang Huy',   'driver5@racehorsetransport.com', '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000105', 'DriverEscort'),
('Hoàng Văn Nam',    'driver6@racehorsetransport.com', '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000106', 'DriverEscort'),
('Đỗ Thanh Sơn',     'driver7@racehorsetransport.com', '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000107', 'DriverEscort'),
('Bùi Anh Khoa',     'driver8@racehorsetransport.com', '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000108', 'DriverEscort'),
('Vũ Hoàng Phúc',    'driver9@racehorsetransport.com', '$2b$11$TnDrEeOPp7Ni9YMVwIVSq.rPey.BcO0yibgrIxaflXe4t3Q9XmoYK', '+84900000109', 'DriverEscort')
ON CONFLICT (email) DO NOTHING;
