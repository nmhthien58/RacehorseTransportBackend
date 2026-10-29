# Tổng hợp phần việc của Tú trong dự án

Tài liệu này tổng hợp phần backend do **Tú** phụ trách trong đợt nghiệp vụ R-09 đến R-16, dựa trên `TASKS_R09_R16.md`, code hiện tại và lịch sử Pull Request. Tài liệu chỉ ghi nhận phần được phân công cho Tú, không nhận các task do Thiên thực hiện.

## 1. Tổng quan kết quả

Tú phụ trách 5 task:

| Task | Nội dung | Kết quả Git |
|---|---|---|
| BE-02 | Schema tọa độ và khoảng cách từng chặng | Đã merge qua PR #10 |
| BE-19 | Bộ mẫu email cho các nghiệp vụ mới | Đã merge qua PR #11 |
| BE-07 | Xếp chuyến từ tuyến đã duyệt và quy tắc hai tài xế | Đã merge qua PR #28 |
| BE-14a | Đổi xe hoặc đổi nhân sự thông qua sự cố | Đã merge qua PR #29 |
| BE-18 | Seed demo, test tích hợp PostgreSQL và rà soát API | Đang review tại PR #42 |

Các phần này trải trên cả ba tầng của hệ thống:

- `RacehorseTransport.API`: request/response, mapper, ví dụ gọi API.
- `RacehorseTransport.Services`: quy tắc nghiệp vụ, kiểm tra điều kiện, giao dịch và thông báo.
- `RacehorseTransport.Repositories`: entity, hằng nghiệp vụ, ánh xạ EF Core.
- `database`: schema dựng mới và script cập nhật DB hiện có.
- `RacehorseTransport.Tests`: unit test và integration test với PostgreSQL thật.
- `docs`: tài liệu API, trạng thái task và bản Word gửi frontend.

---

## 2. BE-02 — Nền tảng tọa độ và khoảng cách

### Mục tiêu

Chuẩn bị dữ liệu bản đồ để các task lập tuyến, tính quãng đường, theo dõi chuyến và xử lý sự cố có thể phát triển tiếp.

### Phần đã làm

- Thêm tọa độ điểm đón và điểm giao vào `booking.booking_requests`:
  - `pickup_lat`, `pickup_lng`.
  - `dropoff_lat`, `dropoff_lng`.
- Thêm dữ liệu bản đồ vào `fleet.route_checkpoints`:
  - `latitude`, `longitude`.
  - `distance_km_from_previous`.
- Cập nhật entity `BookingRequest`, `RouteCheckpoint` và precision trong `AppDbContext`.
- Cập nhật schema dựng mới `database/01_schema.sql`.
- Tạo migration idempotent `database/03_update_geo_coordinates.sql` cho DB đang chạy.

### Giá trị mang lại

- Mỗi đơn có thể lưu đúng vị trí đón và giao do khách chọn trên bản đồ.
- Mỗi checkpoint biết tọa độ và quãng đường từ mốc trước.
- Là nền cho dịch vụ bản đồ, tuyến dự kiến, ETA, ghép chuyến và tính phần quãng đường chưa đi.
- Giữ tương thích với dữ liệu cũ vì các cột mới cho phép `NULL`.

---

## 3. BE-19 — Bộ email cho nghiệp vụ mới

### Mục tiêu

Chuẩn bị sẵn hệ thống email để các task báo giá, hồ sơ, người nhận, giao hàng, bồi thường và bảo hiểm chỉ cần gọi đúng template.

### Phần đã làm

Tạo 13 template email tiếng Việt:

| Template | Sự kiện |
|---|---|
| `QuoteReady` | Báo giá chốt đã sẵn sàng |
| `QuoteExpiring` | Báo giá sắp hết hạn |
| `QuoteExpired` | Báo giá đã hết hạn |
| `DocumentsReminder` | Nhắc khách bổ sung giấy tờ |
| `BookingCancelledMissingDocs` | Hủy đơn vì thiếu giấy tờ |
| `RecipientScheduled` | Báo người nhận khi chuyến đã được lên lịch |
| `RecipientDeparted` | Báo người nhận khi xe xuất phát |
| `RecipientArrivingSoon` | Báo xe sắp tới điểm giao |
| `ConfirmRecipient` | Nhắc khách kiểm tra thông tin người nhận |
| `HandoverCompleted` | Xác nhận đã bàn giao |
| `DeliveryFailed` | Báo không giao được và hạn chọn cách xử lý |
| `DelayCompensation` | Báo khoản bồi thường giao trễ |
| `ClaimDecision` | Báo kết quả hồ sơ bồi thường bảo hiểm |

Ngoài template, Tú còn:

- Bổ sung hằng tên template trong `EmailTemplateNames`.
- Thêm `INotificationService.EmailAddressAsync(...)` và implementation tương ứng.
- Cho phép gửi email trực tiếp tới người nhận ngựa dù họ không có tài khoản hệ thống.
- Viết `EmailTemplateRendererTests` chạy qua toàn bộ template, kiểm tra khi truyền đủ biến thì không còn placeholder `{{...}}`.

### Giá trị mang lại

- Thống nhất nội dung email giữa các luồng.
- Tách template khỏi logic nghiệp vụ.
- Hỗ trợ cả khách hàng có tài khoản và người nhận bên ngoài hệ thống.
- Giảm nguy cơ gửi email còn placeholder hoặc thiếu dữ liệu.

---

## 4. BE-07 — Xếp chuyến từ tuyến đã duyệt

### Mục tiêu

Đảm bảo kế hoạch chuyến thực tế bám đúng tuyến đã được Manager duyệt cho đơn và đủ tài xế cho tuyến dài.

### Phần đã làm

#### 4.1. Chép tuyến đã duyệt sang chuyến

- Khi Coordinator thêm ngựa vào một chuyến chưa có checkpoint, hệ thống lấy `booking.route_plans` đang `Approved`.
- Chép các điểm tuyến sang `fleet.route_checkpoints`, bao gồm:
  - Loại và thứ tự mốc.
  - Địa chỉ.
  - Tọa độ.
  - Quãng đường của từng chặng.
- Với chuyến ghép nhiều đơn, giữ cơ chế `PickUp`/`DropOff` hiện có và bổ sung tọa độ tương ứng.
- API gợi ý tuyến trả tọa độ điểm đón và điểm giao để frontend vẽ đúng tuyến.

#### 4.2. Không cho chuyến đi sai phạm vi giấy tờ

- Thu thập các quốc gia thực tế mà chuyến đi qua.
- So sánh với tuyến đã duyệt và danh sách quốc gia đã chuẩn bị giấy tờ của từng đơn.
- Chặn `SubmitTripAsync` và `ApproveTripAsync` nếu chuyến đi qua nước nằm ngoài tuyến hợp lệ.
- Trả `BusinessException` rõ ràng để Coordinator sửa kế hoạch.

#### 4.3. Quy tắc tài xế cho tuyến dài

- Dùng `ITransitTimeEstimator` để tính thời gian lái dự kiến.
- Đọc giới hạn từ `Transit:MaxDrivingHoursPerDaySingleDriver`, không viết cứng trong service.
- Nếu thời gian lái vượt giới hạn thì bắt buộc có ít nhất hai thành viên `Driver`.
- Kiểm tra cả lúc trình duyệt lẫn lúc Manager phê duyệt, tránh dữ liệu thay đổi sau bước submit.

### Kiểm thử

`TripAssignmentRulesTests` bao phủ:

- Tuyến 8 giờ với một tài xế: hợp lệ.
- Tuyến 10 giờ với một tài xế: bị chặn.
- Tuyến 10 giờ với hai tài xế: hợp lệ.
- Chuyến đi qua nước ngoài tuyến đã duyệt: bị chặn.

### Giá trị mang lại

- Kế hoạch chuyến và hồ sơ kiểm dịch không bị lệch nhau.
- Giảm rủi ro thiếu tài xế trên hành trình dài.
- Tận dụng lại tuyến đã duyệt thay vì Coordinator nhập lại checkpoint thủ công.

---

## 5. BE-14a — Đổi xe và đổi nhân sự qua sự cố

### Mục tiêu

Cho phép đoàn xe tiếp tục hành trình an toàn khi xe hỏng hoặc tài xế/áp tải không thể tiếp tục, nhưng mọi thay đổi phải đi qua quy trình báo sự cố và Manager duyệt.

### Phần đã làm

#### 5.1. Mở rộng mô hình sự cố

- Thêm hai hành động:
  - `ReplaceVehicle`.
  - `ReplaceCrew`.
- Thêm cột `action_payload jsonb` vào `incident.trip_incidents`.
- Cập nhật hằng nghiệp vụ, entity, `AppDbContext` và CHECK constraint.
- Cập nhật `database/01_schema.sql`.
- Tạo script idempotent `database/03_update_incident_replacements.sql`.

#### 5.2. Thay xe

Khi Coordinator đề xuất xe thay thế, hệ thống kiểm tra:

- Xe tồn tại và đang `Available`.
- Đúng loại phương tiện cần dùng.
- Không trùng lịch với chuyến khác.
- Đủ sức chứa cho số ngựa còn trên xe.
- Có điều hòa nếu bất kỳ đơn nào yêu cầu kiểm soát nhiệt độ.

Khi Manager duyệt:

- Chuyến chuyển sang xe mới.
- Xe mới chuyển `InTransit`.
- Xe cũ chuyển `Maintenance`.
- Giữ số chuồng hiện tại nếu phù hợp; nếu không thì đánh lại số chuồng tuần tự.

#### 5.3. Thay tài xế hoặc nhân viên áp tải

Khi Coordinator đề xuất người thay thế, hệ thống kiểm tra:

- Người mới có role `DriverEscort` và tài khoản còn hoạt động.
- Không trùng lịch với chuyến khác.
- `crewRole` cần thay là `Driver` hoặc `Escort` hợp lệ.
- Người bị thay đang thuộc crew của chuyến.

Khi Manager duyệt:

- Cập nhật `fleet.trip_crew`.
- Nếu thay Escort, chuyển các ngựa do người cũ phụ trách sang người mới.
- Gửi thông báo cho cả người bị thay và người thay thế.

#### 5.4. An toàn khi duyệt

- Điều kiện xe/người được kiểm tra lúc đề xuất.
- Hệ thống kiểm tra lại ngay lúc Manager duyệt, vì trạng thái xe, lịch hoặc nhân sự có thể thay đổi trong thời gian chờ.
- Các thay đổi liên quan được thực hiện trong transaction.

### Kiểm thử

`IncidentReplacementRulesTests` bao phủ các trường hợp xe không đủ chỗ, thiếu điều hòa, sai trạng thái, nhân sự sai role và trùng lịch.

### Giá trị mang lại

- Thay xe/người có lịch sử phê duyệt rõ ràng thay vì sửa trực tiếp chuyến.
- Giữ nhất quán giữa xe, crew, người áp tải từng con và trạng thái tài sản.
- Hạn chế chọn nhầm xe hoặc nhân sự không đủ điều kiện.

---

## 6. BE-18 — Dữ liệu demo, test tích hợp và tài liệu

### Mục tiêu

Hoàn thiện dữ liệu và kiểm thử xuyên suốt sau khi các task R-09 đến R-16 đã được tích hợp.

### Phần đã làm

#### 6.1. Dữ liệu demo

Bổ sung vào `database/02_seed.sql` và script idempotent `database/03_update_seed_r09.sql`:

| Mã demo | Tình huống |
|---|---|
| `BKG-2026-0101` | Đơn đã duyệt sơ bộ, đang chờ tuyến và báo giá chốt |
| `BKG-2026-0102` | Đơn có báo giá hết hạn trong 12 giờ |
| `BKG-2026-0103` / `TRP-2026-0101` | Chuyến có sự cố đổi xe và hồ sơ claim bảo hiểm |
| `BKG-2026-0104` / `TRP-2026-0102` | Đơn giao trễ do lỗi công ty, có bồi thường |
| `BKG-2026-0105` / `TRP-2026-0103` | Không giao được vì người nhận vắng mặt, có phí chờ |

Script đã được kiểm tra chạy lặp hai lần trên PostgreSQL thật mà không tạo bản ghi trùng.

#### 6.2. Integration test PostgreSQL

Tạo `BookingQuoteDepositFlowIntegrationTests` chạy trên database tạm ở cổng 5433:

1. Tạo database tạm.
2. Chạy `01_schema.sql` và `02_seed.sql`.
3. Khách tạo đơn.
4. Manager duyệt sơ bộ.
5. Coordinator lập và trình tuyến.
6. Manager duyệt tuyến, hệ thống phát hành báo giá chốt và hợp đồng nháp.
7. Khách chấp nhận báo giá và hợp đồng.
8. Khách tạo và hoàn tất khoản cọc bằng mock gateway.
9. Xác nhận đơn chuyển sang `Approved` và tiền cọc đã ghi nhận.
10. Xóa database tạm sau khi test.

Test được bật bằng `RUN_POSTGRES_INTEGRATION_TESTS=1`; khi chạy bộ unit test thông thường, test PostgreSQL được đánh dấu skip rõ ràng.

#### 6.3. Rà soát tài liệu API

- Đối chiếu tự động controller với bảng endpoint cuối `docs/API.md`.
- Kết quả: **181 endpoint trong code và 181 endpoint trong tài liệu, không thiếu/không dư**.
- Bổ sung mô tả đầy đủ cho `POST /api/files` và `GET /api/files/url`.
- Sửa phần SignalR đã lỗi thời: hệ thống đã có `/hubs/notifications`.
- Tạo `scripts/export-api-docx.ps1` để xuất lại `API_Huong_dan_Frontend.docx`.

### Kết quả kiểm tra BE-18

- Build: 0 warning, 0 error.
- Unit test: 436 pass, 1 integration test skip khi không bật PostgreSQL.
- Integration test PostgreSQL: 1 pass, 0 fail.
- Không ghi dữ liệu lên Supabase trong quá trình kiểm thử.

---

## 7. Các script database do Tú cung cấp

| Script | Mục đích | Trạng thái |
|---|---|---|
| `database/03_update_geo_coordinates.sql` | Thêm tọa độ và khoảng cách từng chặng | Đã merge |
| `database/03_update_incident_replacements.sql` | Thêm `action_payload`, `ReplaceVehicle`, `ReplaceCrew` | Đã merge |
| `database/03_update_seed_r09.sql` | Thêm dữ liệu demo R-09 đến R-16 | Chờ review tại PR #42 |

Các script được viết theo hướng idempotent. Việc chạy trên Supabase do Thiên thực hiện trước khi merge/deploy.

---

## 8. Phạm vi trách nhiệm của Tú trong toàn hệ thống

Tóm lại, phần Tú đảm nhiệm tập trung vào bốn nhóm giá trị:

1. **Nền dữ liệu:** tọa độ, quãng đường và migration để các chức năng bản đồ/lộ trình hoạt động.
2. **Khả năng vận hành:** xếp chuyến từ tuyến đã duyệt, kiểm tra quốc gia, đủ tài xế, thay xe/người khi có sự cố.
3. **Giao tiếp nghiệp vụ:** bộ email dùng chung cho báo giá, hồ sơ, người nhận, giao nhận và bồi thường.
4. **Chất lượng bàn giao:** seed demo đầy đủ, integration test PostgreSQL, kiểm kê API và xuất tài liệu Word.

Những task nghiệp vụ khác như phát hành báo giá chốt, kiểm tra trước xuất phát, bảo hiểm, giao thất bại và hợp đồng do Thiên phụ trách; phần Tú tích hợp với chúng thông qua interface, trạng thái và dữ liệu chung của dự án.
