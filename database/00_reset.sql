-- =====================================================================================
-- SCRIPT 00: RESET (tùy chọn) - XÓA TOÀN BỘ bảng và dữ liệu của hệ thống
-- Chỉ chạy khi muốn dựng lại database từ đầu, sau đó chạy lại 01 và 02.
-- Không đụng tới các schema có sẵn của Supabase (auth, storage, public...).
-- =====================================================================================

DROP SCHEMA IF EXISTS
    billing, pricing, common, acceptance, incident, tracking, fleet, clearance, booking, racehorse, identity
CASCADE;
