-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT cho database đã dựng từ trước ngày 29/10/2026
-- Chính sách mục 7 (phiên bản 1.1): thêm số ngày dự phòng cho hạn giao sớm nhất.
-- Khóa đã có thì giữ nguyên giá trị đang dùng. Code chạy được cả khi chưa chạy file này (mặc định 2).
-- Database dựng mới bằng 02_seed.sql đã có sẵn: không cần chạy file này.
-- Chạy lại nhiều lần không sao.
-- =====================================================================================

INSERT INTO common.system_settings (setting_key, setting_value, description) VALUES
('Delivery.DeadlineBufferDays', '2', 'Số ngày dự phòng cộng vào hạn giao sớm nhất khách được chọn')
ON CONFLICT (setting_key) DO NOTHING;

-- Nội dung chính sách đổi -> tăng phiên bản. Chỉ đổi khi đang là 1.0 (Manager đã tự đặt bản khác thì giữ nguyên).
UPDATE common.system_settings
SET setting_value = '1.1', updated_at = now()
WHERE setting_key = 'Policy.Version' AND setting_value = '1.0';
