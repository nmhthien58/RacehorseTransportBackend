-- BE-10 (R-12, R-15): loại giấy "Khám lâm sàng đủ sức khỏe" (FIT_EXAM), khách cung cấp, cấp trong 48 giờ trước giờ xuất phát.
-- Bắt buộc cho mọi ngựa nhưng kiểm lúc bấm xuất phát (giấy có sau khi hồ sơ đã thông quan), nên is_mandatory = false
-- để không lọt vào checklist hồ sơ của các tuyến chưa có quy định riêng.
-- Code đã chạy được khi chưa có dòng này (chỉ cảnh báo FitExamTypeNotConfigured); chạy script để bật kiểm tra.
-- Chạy được nhiều lần.

-- Đồng bộ sequence để dòng mới không trùng ID đã có
SELECT setval(pg_get_serial_sequence('clearance.document_types', 'doc_type_id'), (SELECT max(doc_type_id) FROM clearance.document_types));

INSERT INTO clearance.document_types (code, name, description, is_mandatory, provided_by)
SELECT 'FIT_EXAM',
       'Khám lâm sàng đủ sức khỏe',
       'Bác sĩ thú y khám lâm sàng, xác nhận ngựa đủ sức khỏe để vận chuyển; cấp trong 48 giờ trước giờ xuất phát',
       false,
       'Customer'
WHERE NOT EXISTS (SELECT 1 FROM clearance.document_types WHERE code = 'FIT_EXAM');
