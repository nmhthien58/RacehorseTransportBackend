-- =====================================================================================
-- SCRIPT 03 (TÙY CHỌN): DỌN DẸP cho database đã chạy bản thử nghiệm "xác thực email" ngày 08/10/2026
-- Tính năng xác thực email đã được bỏ. File này xóa cột và các token không còn dùng.
-- KHÔNG bắt buộc: bản code mới chạy bình thường khi cột còn đó.
-- Chỉ chạy SAU KHI bản code mới đã deploy xong (bản cũ còn đọc cột này). Chạy lại nhiều lần không sao.
-- =====================================================================================

DELETE FROM identity.user_tokens WHERE token_type = 'EmailVerification';

ALTER TABLE identity.users
    DROP COLUMN IF EXISTS email_verified_at;

ALTER TABLE identity.user_tokens
    DROP CONSTRAINT IF EXISTS ck_user_tokens_type;
ALTER TABLE identity.user_tokens
    ADD CONSTRAINT ck_user_tokens_type CHECK (token_type IN ('Refresh', 'PasswordReset'));
