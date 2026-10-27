-- =====================================================================================
-- SCRIPT 03: CẬP NHẬT cho database đã dựng từ trước ngày 27/10/2026 (BE-08, R-12)
-- Điều kiện ngựa được chở: tình trạng sinh sản của ngựa + cấu hình ngưỡng thai kỳ / sau khi đẻ.
--   - racehorse.horses: is_pregnant, expected_foaling_date, last_foaling_date
--   - common.system_settings: Horse.GestationDays, Horse.LatePregnancyPercent, Horse.PostFoalingDays
-- Cần chạy sau 03_update_policy_settings.sql (BE-17).
-- Database dựng mới bằng 01_schema.sql + 02_seed.sql đã có sẵn các thay đổi này: không cần chạy file này.
-- Chạy lại nhiều lần không sao.
-- =====================================================================================

ALTER TABLE racehorse.horses
    ADD COLUMN IF NOT EXISTS is_pregnant boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS expected_foaling_date date,
    ADD COLUMN IF NOT EXISTS last_foaling_date date;

ALTER TABLE racehorse.horses DROP CONSTRAINT IF EXISTS ck_horses_pregnancy;
ALTER TABLE racehorse.horses
    ADD CONSTRAINT ck_horses_pregnancy
    CHECK (NOT is_pregnant OR (gender = 'Mare' AND expected_foaling_date IS NOT NULL));

INSERT INTO common.system_settings (setting_key, setting_value, description) VALUES
('Horse.GestationDays',        '340', 'Số ngày thai kỳ trung bình của ngựa'),
('Horse.LatePregnancyPercent', '90',  'Không chở ngựa cái đã quá % thai kỳ này (tính tại ngày cuối của khoảng đón)'),
('Horse.PostFoalingDays',      '7',   'Không chở ngựa cái vừa đẻ trong số ngày này')
ON CONFLICT (setting_key) DO NOTHING;
