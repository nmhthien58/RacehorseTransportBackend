-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT cho database đã dựng từ trước ngày 27/10/2026 (BE-17)
-- Thêm các khóa cấu hình của Chính sách dịch vụ vào common.system_settings, với giá trị ghi trong
-- docs/CHINH_SACH_DICH_VU.md. Khóa đã có thì giữ nguyên giá trị đang dùng.
-- Database dựng mới bằng 02_seed.sql đã có sẵn các khóa này: không cần chạy file này.
-- Chạy lại nhiều lần không sao.
-- =====================================================================================

INSERT INTO common.system_settings (setting_key, setting_value, description) VALUES
('Policy.Version',                       '1.0',   'Phiên bản Chính sách dịch vụ hiện hành (lưu vào đơn khi khách đồng ý)'),
('Booking.QuoteAcceptDays',              '3',     'Số ngày khách được xem và chấp nhận báo giá chốt + đặt cọc'),
('Delay.GraceHours',                     '6',     'Số giờ trễ được miễn bồi thường'),
('Delay.PercentPer12Hours',              '5',     '% tiền cước bồi thường cho mỗi 12 giờ trễ sau ân hạn'),
('Delay.MaxPercent',                     '25',    '% tiền cước bồi thường giao trễ tối đa'),
('Insurance.PremiumPercent',             '1',     '% phí bảo hiểm toàn giá trị trên giá trị thỏa thuận mỗi con'),
('Insurance.BaseLiabilityUsd',           '5000',  'Trách nhiệm tối đa (USD / con) khi ngựa chết / mất mà không mua bảo hiểm'),
('Insurance.BaseVetCapUsd',              '1000',  'Chi phí thú y tối đa (USD / con) khi ngựa bị thương mà không mua bảo hiểm'),
('Insurance.VetCapPercent',              '20',    '% giá trị thỏa thuận tối đa cho chi phí thú y khi có bảo hiểm'),
('Insurance.ProofThresholdUsd',          '50000', 'Giá trị khai báo (USD / con) từ mức này phải kèm chứng từ'),
('Horse.MinAgeMonths',                   '6',     'Tuổi tối thiểu (tháng) của ngựa, tính tới ngày cuối của khoảng đón'),
('Delivery.FreeWaitingHours',            '2',     'Số giờ xe chờ miễn phí tại điểm giao'),
('Delivery.WaitingFeePerHourUsd',        '40',    'Phí chờ (USD / giờ) sau thời gian chờ miễn phí'),
('Delivery.MaxWaitingHours',             '24',    'Số giờ chờ tính phí tối đa'),
('Delivery.DecisionHours',               '24',    'Số giờ khách có để chọn cách xử lý khi không giao được')
ON CONFLICT (setting_key) DO NOTHING;
