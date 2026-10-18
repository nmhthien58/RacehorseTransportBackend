-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT cho database đã dựng trước ngày 18/10/2026 (R-01)
-- Đơn không còn "ngày khởi hành / ngày giao" do khách chọn, mà là:
--   pickup_window_start .. pickup_window_end : khoảng ngày ngựa sẵn sàng được đón
--   delivery_deadline                        : hạn giao trễ nhất
--   estimated_transit_hours                  : thời gian vận chuyển hệ thống ước tính lúc báo giá
-- Đơn cũ: departure_date -> pickup_window_start = pickup_window_end, delivery_date -> delivery_deadline.
-- Database dựng mới bằng 01_schema.sql đã có sẵn các cột này: không cần chạy file này.
-- PHẢI chạy NGAY TRƯỚC khi deploy bản backend có R-01 (bản cũ đọc cột departure_date sẽ lỗi sau khi chạy).
-- Chạy lại nhiều lần không sao.
-- =====================================================================================

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_schema = 'booking' AND table_name = 'booking_requests' AND column_name = 'departure_date') THEN
        ALTER TABLE booking.booking_requests RENAME COLUMN departure_date TO pickup_window_start;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_schema = 'booking' AND table_name = 'booking_requests' AND column_name = 'delivery_date') THEN
        ALTER TABLE booking.booking_requests RENAME COLUMN delivery_date TO delivery_deadline;
    END IF;
END $$;

ALTER TABLE booking.booking_requests ADD COLUMN IF NOT EXISTS pickup_window_end timestamptz;
UPDATE booking.booking_requests SET pickup_window_end = pickup_window_start WHERE pickup_window_end IS NULL;
ALTER TABLE booking.booking_requests ALTER COLUMN pickup_window_end SET NOT NULL;

ALTER TABLE booking.booking_requests ADD COLUMN IF NOT EXISTS estimated_transit_hours numeric(7,1);

ALTER TABLE booking.booking_requests DROP CONSTRAINT IF EXISTS ck_booking_requests_dates;
ALTER TABLE booking.booking_requests ADD CONSTRAINT ck_booking_requests_dates
    CHECK (pickup_window_end >= pickup_window_start AND delivery_deadline > pickup_window_start);
