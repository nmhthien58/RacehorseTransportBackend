-- =====================================================================================
-- SCRIPT 03: Đồng ý an tử + ủy quyền khẩn cấp (sức khỏe ngựa, giai đoạn b; Chính sách mục 9.6)
-- - trip_incidents: requires_euthanasia_consent, pending_consent_type, consent_requested_at,
--   emergency_authorized_at / _by_user_id / _note
-- - incident_consents.consent_type (Detour | Euthanasia); một đơn có thể có cả hai loại ý kiến trên cùng sự cố
-- - cấu hình Incident.EuthanasiaConsentMinutes (mặc định 60 phút)
-- Idempotent: chạy lại nhiều lần không sao. Database dựng mới bằng 01_schema.sql + 02_seed.sql đã có sẵn.
-- =====================================================================================

ALTER TABLE incident.trip_incidents
    ADD COLUMN IF NOT EXISTS requires_euthanasia_consent boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS pending_consent_type varchar(20),
    ADD COLUMN IF NOT EXISTS consent_requested_at timestamptz,
    ADD COLUMN IF NOT EXISTS emergency_authorized_at timestamptz,
    ADD COLUMN IF NOT EXISTS emergency_authorized_by_user_id integer REFERENCES identity.users (user_id),
    ADD COLUMN IF NOT EXISTS emergency_authorization_note varchar(500);

ALTER TABLE incident.trip_incidents DROP CONSTRAINT IF EXISTS ck_trip_incidents_pending_consent_type;
ALTER TABLE incident.trip_incidents
    ADD CONSTRAINT ck_trip_incidents_pending_consent_type CHECK (pending_consent_type IN ('Detour', 'Euthanasia'));

-- Sự cố đang chờ khách từ trước: là chờ đồng ý đi đường vòng
UPDATE incident.trip_incidents
SET pending_consent_type = 'Detour', consent_requested_at = COALESCE(consent_requested_at, approved_at)
WHERE status = 'AwaitingConsent' AND pending_consent_type IS NULL;

ALTER TABLE incident.incident_consents
    ADD COLUMN IF NOT EXISTS consent_type varchar(20) NOT NULL DEFAULT 'Detour';
ALTER TABLE incident.incident_consents DROP CONSTRAINT IF EXISTS ck_incident_consents_type;
ALTER TABLE incident.incident_consents
    ADD CONSTRAINT ck_incident_consents_type CHECK (consent_type IN ('Detour', 'Euthanasia'));
ALTER TABLE incident.incident_consents DROP CONSTRAINT IF EXISTS uq_incident_consents_incident_booking;
ALTER TABLE incident.incident_consents DROP CONSTRAINT IF EXISTS uq_incident_consents_incident_booking_type;
ALTER TABLE incident.incident_consents
    ADD CONSTRAINT uq_incident_consents_incident_booking_type UNIQUE (incident_id, booking_id, consent_type);

INSERT INTO common.system_settings (setting_key, setting_value, description) VALUES
('Incident.EuthanasiaConsentMinutes', '60', 'Số phút chờ chủ ngựa trả lời khuyến nghị an tử trước khi được áp dụng ủy quyền khẩn cấp')
ON CONFLICT (setting_key) DO NOTHING;
