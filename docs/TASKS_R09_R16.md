# Backend — Task đợt nghiệp vụ R-09 → R-16

Cập nhật: 26/10/2026 · Người làm: **Thiên** và **Tú** · File Excel theo dõi: `docs/SWP391_Backend_Tasks_R09_R16.xlsx` (cùng mã task).

> **Đọc trước khi làm (bắt buộc với cả người và AI agent):**
> 1. `docs/PLAN_NGHIEP_VU_R09.md`: lý do và quyết định nghiệp vụ của từng mục R-xx (đọc mục "Quyết định đã chốt" ở đầu file).
> 2. `docs/CHINH_SACH_DICH_VU.md`: chính sách gửi khách. **Code phải khớp từng con số và quy tắc trong file này.** Đổi quy tắc thì sửa cả hai file.
> 3. `docs/API.md`: tài liệu API hiện tại (giọng văn, độ chi tiết phải giữ nguyên khi thêm endpoint mới).
> 4. Code mẫu nên đọc: `IncidentService` (sự cố, nắn tuyến, `RoutePlan`), `PaymentService` (cọc, hoàn tiền, khoản phát sinh), `BookingService` + `PricingService` (đơn, báo giá), `BackgroundJobs/BookingExpiryJob.cs` (job chạy định kỳ).

## Quy ước chung (áp dụng cho mọi task)

**Kiến trúc**
- Ba lớp: Controller → Service → Repository. Quy tắc nghiệp vụ chỉ nằm trong Service. Controller chỉ map request/response.
- Lỗi nghiệp vụ ném `BusinessException` (400), không tìm thấy `NotFoundException` (404), sai quyền `ForbiddenException` (403). Thông báo lỗi viết **tiếng Anh**, rõ ràng, nói được người dùng phải làm gì.
- Comment trong code viết **tiếng Việt**, ngắn, giải thích "vì sao". Tên biến / hàm tiếng Anh.
- Giá trị cấu hình (%, số ngày, số giờ, mức tiền) **không viết cứng**: đọc từ `common.system_settings` (qua `ISystemSettingService`) hoặc `appsettings` (`*Options`). Tên khóa lấy đúng như trong `CHINH_SACH_DICH_VU.md`.
- Thao tác ghi nhiều bảng chạy trong `_unitOfWork.ExecuteInTransactionAsync`. Thông báo / email gửi bên trong transaction (hệ thống đã tự đẩy realtime sau khi commit).
- Mọi thông báo cho khách đi kèm email nếu là sự kiện quan trọng (`emailCustomers: true` hoặc `EmailCustomerAsync` với mẫu riêng trong `EmailTemplates/`).

**Database**
- Mỗi thay đổi schema: sửa `database/01_schema.sql` (cho DB dựng mới) **và** thêm `database/03_update_<tên>.sql` cho DB đang chạy (Supabase). Script phải chạy lại nhiều lần không lỗi (`IF NOT EXISTS`, `DROP CONSTRAINT IF EXISTS`). Mẫu: `03_update_incident_route_plan.sql`.
- Cột JSON dùng `jsonb` + `HasColumnType("jsonb")` trong `AppDbContext`. Tên cột snake_case tự sinh, không cần `HasColumnName`.
- Giá trị trạng thái / loại mới: thêm vào `Repositories/Constants/DomainConstants.cs` (có mảng `All`) **và** vào `CHECK` constraint trong SQL.

**Hoàn thành task khi**
1. `dotnet build` không lỗi, không cảnh báo mới. `dotnet test` pass. Task có quy tắc tính toán (tiền, ngày, giờ) **phải có unit test** trong `RacehorseTransport.Tests`.
2. Endpoint có `[Authorize(Roles = ...)]` đúng role, `/// <summary>` để hiện trên Swagger, `ProducesResponseType`.
3. Cập nhật `docs/API.md` theo đúng kiểu hiện có (mỗi endpoint: ai gọi, tham số, bảng field body, ví dụ request / response, lỗi thường gặp), rồi xuất lại `docs/API_Huong_dan_Frontend.docx` (gửi team frontend).
4. Thêm request mẫu vào `RacehorseTransport.API/RacehorseTransport.API.http`.
5. Mỗi task một nhánh `feature/<mã-task>-<tên-ngắn>` (ví dụ `feature/be-01-geo-service`), một Pull Request vào `main`. Commit không thêm dòng `Co-Authored-By`.
6. Đánh dấu `[x]` trong file này và đổi cột Trạng thái trong Excel khi đã merge.

## Bảng phân công

Điểm = độ lớn tương đối (Cao 5, Trung bình 3, Thấp 2). Thiên giữ các task cốt lõi của luồng nghiệp vụ; Tú làm các phần nền, độc lập hoặc mở rộng.

| Mã | Mục | Task | Người | Mức | Phụ thuộc |
|---|---|---|---|---|---|
| BE-01 | R-11 | Dịch vụ bản đồ: tìm địa chỉ, tọa độ → quốc gia, chỉ đường từng chặng | Thiên | Cao | BE-02 |
| BE-02 | R-11 | Schema tọa độ + script migration nền cho cả đợt | Tú | Thấp | — |
| BE-03 | R-10 | Khoảng giá ước tính khi tạo đơn, bỏ nước quá cảnh do khách nhập | Thiên | TB | BE-01 |
| BE-04 | R-10 | Tuyến dự kiến của đơn (Coordinator lập, Manager duyệt), suy ra nước quá cảnh | Thiên | Cao | BE-01, BE-02 |
| BE-05 | R-10 | Báo giá chốt, hạn chấp nhận, cọc sau khi chấp nhận, đồng ý chính sách | Thiên | Cao | BE-04, BE-17 |
| BE-06 | R-10 | Hồ sơ theo tuyến đã duyệt, nhắc nộp giấy, hủy khi quá hạn, đổi tuyến do công ty | Thiên | Cao | BE-04, BE-05 |
| BE-07 | R-10 / R-14 | Xếp chuyến từ tuyến đã duyệt + quy tắc ≥ 2 tài xế | Tú | TB | BE-04 |
| BE-08 | R-12 | Điều kiện ngựa: tuổi tối thiểu, mang thai | Thiên | TB | BE-02 |
| BE-09 | R-12 / R-13 | Giá trị khai báo theo từng con, chứng từ, giá trị thỏa thuận | Thiên | TB | BE-02 |
| BE-10 | R-12 / R-15 | Kiểm tra trước khi xuất phát (hạn giấy, khám 48h, thanh toán, số tài xế) | Thiên | TB | BE-07 |
| BE-11 | R-12 | Rời chuyến chỉ qua sự cố + hướng xử lý tiếp + trạng thái `Deceased` | Thiên | TB | — |
| BE-12 | R-13 | Bảo hiểm: gói theo con, hồ sơ bồi thường, tự mở, chi trả | Thiên | Cao | BE-09, BE-11 |
| BE-13 | R-09 | Nguyên nhân sự cố (`fault_party`) + bồi thường giao trễ khi đóng chuyến | Thiên | TB | BE-17 |
| BE-14a | R-14 | Hành động đổi xe / đổi người trong sự cố | Tú | TB | BE-13 |
| BE-14b | R-14 | Cửa khẩu từ chối, biên giới đóng, quay đầu, hoàn theo quãng đường chưa đi | Thiên | Cao | BE-13 |
| BE-15 | R-16 | Người nhận: thông tin trên đơn, email người nhận, khách xác nhận có mặt | Thiên | TB | BE-05 |
| BE-16 | R-16 | Không giao được, phí chờ, khách chọn cách xử lý trong 24 giờ | Thiên | Cao | BE-15, BE-13 |
| BE-17 | Chung | Cấu hình mới + API chính sách công khai + phiên bản chính sách | Thiên | TB | BE-02 |
| BE-18 | Chung | Seed dữ liệu demo cho các luồng mới + test tích hợp + tổng hợp API.md | Tú | TB | các task trên |
| BE-19 | Chung | Mẫu email cho các sự kiện mới | Tú | Thấp | — |
| BE-20 | R-17 | Hợp đồng vận chuyển: tạo PDF, xem trước, chấp nhận kèm bằng chứng, bản mới khi đổi điều khoản | Thiên | Cao | BE-05 |

Thứ tự gợi ý: **Tú** làm BE-02 → BE-19 → BE-07 → BE-14a → BE-18. **Thiên** làm BE-17 → BE-01 → BE-03 → BE-04 → BE-05 → BE-06 → BE-08/09 → BE-10 → BE-11 → BE-13 → BE-12 → BE-14b → BE-15 → BE-16. BE-20 làm ngay sau BE-05 (dùng chung bước chấp nhận báo giá).

---

## Việc của Tú

### [x] BE-02 · Schema tọa độ + script migration nền (R-11)
**Mục tiêu:** chuẩn bị các cột / bảng mà nhiều task khác cần, để Thiên không phải chờ.

Làm:
- `booking.booking_requests`: thêm `pickup_lat`, `pickup_lng`, `dropoff_lat`, `dropoff_lng` (`numeric(9,6)`, nullable vì đơn cũ không có).
- `fleet.route_checkpoints`: thêm `latitude`, `longitude` (`numeric(9,6)`, nullable), `distance_km_from_previous` (`numeric(10,2)`, nullable).
- Entity + `AppDbContext` (precision 9,6 và 10,2). Không đổi logic nào khác.
- Script `database/03_update_geo_coordinates.sql`, cập nhật `01_schema.sql`.

Xong khi: build + test pass; script chạy được hai lần liên tiếp trên DB local mà không lỗi.

### [x] BE-19 · Mẫu email cho các sự kiện mới
**Mục tiêu:** có sẵn mẫu để các task nghiệp vụ chỉ việc gọi.

Tạo file trong `RacehorseTransport.Services/EmailTemplates/` (theo kiểu `BalanceDue.html`: dòng đầu `Subject: ...`, HTML đơn giản, nút dẫn về `{{Link}}`, tiếng Việt), và thêm hằng trong `EmailTemplateNames` kèm comment liệt kê biến:
| Mẫu | Biến | Dùng ở |
|---|---|---|
| `QuoteReady` | BookingCode, PriceUsd, PriceVnd, ExpiresAt | BE-05 |
| `QuoteExpiring` | BookingCode, ExpiresAt | BE-05 |
| `QuoteExpired` | BookingCode | BE-05 |
| `DocumentsReminder` | BookingCode, HorseName, DocumentList, Deadline, ReminderNumber | BE-06 |
| `BookingCancelledMissingDocs` | BookingCode, RefundAmount | BE-06 |
| `RecipientScheduled` | RecipientName, BookingCode, HorseNames, ExpectedDate, ContactPhone | BE-15 (gửi người nhận) |
| `RecipientDeparted` | RecipientName, BookingCode, Eta | BE-15 |
| `RecipientArrivingSoon` | RecipientName, BookingCode, EtaWindow, DriverPhone | BE-15 |
| `ConfirmRecipient` | BookingCode, RecipientName, Eta | BE-15 (gửi khách) |
| `HandoverCompleted` | BookingCode, RecipientName, SignedAt, Condition | BE-15 |
| `DeliveryFailed` | BookingCode, Reason, DecisionDeadline | BE-16 |
| `DelayCompensation` | BookingCode, DelayHours, Amount | BE-13 |
| `ClaimDecision` | ClaimCode, BookingCode, HorseName, Decision, Amount, Reason | BE-12 |

Lưu ý: email gửi **người nhận** không đi qua `EmailCustomerAsync` (người nhận không phải user). Thêm vào `INotificationService` một hàm `EmailAddressAsync(string email, string name, string templateName, IDictionary<string,string> values)` dùng `_emailService.Queue` trực tiếp.

Xong khi: mỗi mẫu render không còn `{{...}}` khi đủ biến (viết một test lặp qua các mẫu).

### [x] BE-07 · Xếp chuyến từ tuyến đã duyệt + quy tắc ≥ 2 tài xế (R-10, R-14)
Phụ thuộc BE-04 (bảng `booking.route_plans`).

Đã làm: chuyến một đơn chưa có mốc tự chép các điểm, tọa độ và quãng đường từ tuyến `Approved`; gợi ý tuyến ghép trả tọa độ điểm đón / trả từ tuyến đã duyệt. `submit` và `approve` cùng kiểm tra các nước thực đi của từng đơn bằng `IGeoService`, chặn nước ngoài tuyến, và dùng `ITransitTimeEstimator` để bắt ít nhất 2 tài xế khi thời gian lái trên `Transit:MaxDrivingHoursPerDaySingleDriver`.

Làm:
- Khi Coordinator thêm ngựa của một đơn vào bản nháp chuyến mà chuyến chưa có mốc: gợi ý / chép các mốc của **tuyến đã duyệt** của đơn (kèm tọa độ, quãng đường từng chặng) sang `route_checkpoints` của chuyến. Ghép nhiều đơn (R-04) thì giữ logic hiện có, chỉ thêm tọa độ cho mốc PickUp / DropOff.
- Chuyến **không được đi qua nước nằm ngoài tuyến đã duyệt** của bất kỳ đơn nào trên chuyến (vì giấy tờ chỉ làm cho các nước đó) → `BusinessException` khi trình duyệt chuyến.
- Quy tắc tài xế: thời gian lái dự kiến (`ITransitTimeEstimator`) > `[Transit:MaxDrivingHoursPerDaySingleDriver]` (9) giờ thì chuyến phải có **≥ 2 thành viên vai trò Driver** → chặn ở bước trình duyệt (`SubmitTripAsync`) và duyệt (`ApproveTripAsync`).

Xong khi: unit test cho quy tắc tài xế (8 giờ: 1 tài xế OK; 10 giờ: 1 tài xế lỗi, 2 tài xế OK) và quy tắc nước ngoài tuyến.

### [x] BE-14a · Đổi xe / đổi người trong sự cố (R-14)
Phụ thuộc BE-13 (đã có `fault_party`).

Đã làm: thêm `ReplaceVehicle` / `ReplaceCrew` và `action_payload`; kiểm tra xe thay thế (trạng thái, loại, lịch, sức chứa, điều hòa), kiểm tra người thay thế (vai trò, hoạt động, trùng lịch), tái kiểm tra khi duyệt; duyệt sẽ đổi trạng thái xe, sắp lại chuồng khi cần, thay `trip_crew`, chuyển ngựa của áp tải cũ và thông báo hai người.

Làm (thêm vào luồng sự cố hiện có, xem `IncidentService`):
- Hành động mới trong `IncidentResponseActions`: `ReplaceVehicle`, `ReplaceCrew` (cập nhật CHECK constraint).
- Phương án `ReplaceVehicle` kèm `replacementVehicleId`: xe phải `Available`, đủ số chuồng cho số ngựa đang trên xe, có điều hòa nếu có đơn yêu cầu. Khi Manager duyệt: `trip.VehicleId` đổi sang xe mới (xe mới `InTransit`), xe cũ `Maintenance`, giữ số chuồng nếu xe mới đủ chỗ, không thì đánh lại số chuồng theo thứ tự.
- Phương án `ReplaceCrew` kèm `removeUserId`, `addUserId`, `crewRole`: người mới phải là `DriverEscort`, không đang ở chuyến khác trùng thời gian. Khi duyệt: cập nhật `trip_crew`, chuyển các ngựa người cũ đang áp tải (`escort_user_id`) sang người mới, báo cả hai người.
- Lưu chi tiết vào một cột `action_payload jsonb` của sự cố (thêm cột + script).

Xong khi: test cho các điều kiện chọn xe / người; báo lỗi rõ khi xe không đủ chỗ.

### [ ] BE-18 · Seed demo + test tích hợp + tổng hợp tài liệu
Làm sau cùng, khi các task kia đã merge.
- `database/02_seed.sql` + một script `03_update_seed_r09.sql`: thêm dữ liệu demo đi hết các luồng mới (một đơn đang chờ báo giá, một đơn báo giá sắp hết hạn, một chuyến có sự cố đổi xe, một hồ sơ bồi thường, một đơn giao trễ có bồi thường, một lần không giao được có phí chờ).
- Test tích hợp với PostgreSQL thật (DB tạm cổng 5433, xem `README.md`) cho luồng: tạo đơn → duyệt sơ bộ → lập tuyến → duyệt tuyến → chấp nhận báo giá + cọc.
- Rà `docs/API.md`: mọi endpoint mới đã có mô tả đúng kiểu; bảng tổng hợp cuối file đủ; xuất lại `.docx`.

---

## Việc của Thiên

### [x] BE-17 · Cấu hình mới + API chính sách công khai (Chung) — làm đầu tiên
Đã làm: `GET /api/policy` (công khai, có `values` theo đúng tên khóa để thay các chỗ `[...]` trong văn bản), `GET/PUT /api/settings/policy` (đọc: nhân sự; sửa: Manager / Admin). Hằng tên khóa trong `SystemSettingService` (`*Key`), đọc qua `ISystemSettingService.GetPolicySettingsAsync()`. Task sau cần khóa mới (vd `Dossier.SubmitDays`, hệ số thai kỳ) thì thêm vào `PolicySettingsModel` + mảng `PolicyKeys` + seed / script.

Làm:
- Thêm các khóa vào `common.system_settings` (seed + script) và vào model cấu hình, có API đọc / sửa cho Admin / Manager như nhóm `Payment.*` hiện có:
  - `Booking.QuoteAcceptDays` = 3
  - `Delay.GraceHours` = 6, `Delay.PercentPer12Hours` = 5, `Delay.MaxPercent` = 25
  - `Insurance.PremiumPercent` = 1, `Insurance.BaseLiabilityUsd` = 5000, `Insurance.BaseVetCapUsd` = 1000, `Insurance.VetCapPercent` = 20, `Insurance.ProofThresholdUsd` = 50000
  - `Horse.MinAgeMonths` = 6
  - `Delivery.FreeWaitingHours` = 2, `Delivery.WaitingFeePerHourUsd` = 40, `Delivery.MaxWaitingHours` = 24, `Delivery.DecisionHours` = 24
  - `Policy.Version` = "1.0"
- `GET /api/policy` **không cần đăng nhập**: trả phiên bản chính sách + toàn bộ giá trị cấu hình mà `CHINH_SACH_DICH_VU.md` nhắc tới (kể cả các khóa `Payment.*` đã có), để frontend dựng trang Chính sách.
- Giá trị không hợp lệ (âm, % > 100…) → 400.

### [x] BE-01 · Dịch vụ bản đồ (R-11)
Đã làm: `IGeoService` điều phối các `IGeocodingProvider` / `IRoutingProvider` (Goong, Nominatim, OSRM; thứ tự đăng ký trong DI = thứ tự ưu tiên, thêm Mapbox chỉ cần một class mới). Dự phòng khi lỗi: đường chim bay × `Geo:FallbackRoadFactor` (1,3), tốc độ `Transit:TruckAverageSpeedKmh`. `CountriesAlongAsync` chia đôi đoạn đường thay vì lấy mẫu đều để ít gọi Nominatim. Goong chỉ chạy khi có `Goong:ApiKey` (máy dev hiện chưa có khóa, nên đã thử thật với Nominatim + OSRM).

Không dùng Google Maps (Việt Nam và Trung Quốc nằm trong danh sách cấm của Google Maps Platform). Dùng **Goong** trong Việt Nam (đã có `Goong:ApiKey`), **Nominatim** (tìm địa chỉ) + **OSRM** (chỉ đường) ngoài Việt Nam. Thiết kế qua interface để thay nhà cung cấp (vd Mapbox) mà không sửa nơi gọi.

Làm:
- `IGeoService`:
  - `SearchAsync(query, countryCode?)` → danh sách `{ displayName, lat, lng, countryCode }` (gợi ý địa chỉ).
  - `ReverseAsync(lat, lng)` → `{ displayName, countryCode }`.
  - `RouteAsync(points[])` → từng chặng `{ distanceKm, durationHours }` + tổng + polyline (đã giải mã thành danh sách tọa độ) để vẽ bản đồ; chọn Goong nếu cả hai đầu chặng ở VN, ngược lại OSRM. Lỗi gọi API → quay về cách tính dự phòng hiện có của `DistanceService` và đánh dấu `isEstimate = true`.
  - `CountriesAlongAsync(polyline)` → mã nước theo thứ tự đi qua (lấy mẫu điểm dọc polyline, reverse geocode, gộp trùng liên tiếp).
- Cache kết quả (`ICacheService` hiện có) theo khóa tọa độ làm tròn 5 chữ số, hạn 7 ngày. Nominatim yêu cầu `User-Agent` riêng và ≤ 1 request/giây: thêm giới hạn tốc độ.
- Endpoint: `GET /api/geo/search?q=&country=`, `GET /api/geo/reverse?lat=&lng=`, `POST /api/geo/route` (body: danh sách điểm). Người đã đăng nhập mới gọi được.
- Cấu hình URL các nhà cung cấp trong `appsettings` (`Geo:NominatimUrl`, `Geo:OsrmUrl`).

Xong khi: unit test với `HttpMessageHandler` giả cho từng nhà cung cấp + trường hợp lỗi rơi về dự phòng.


### [x] BE-03 · Khoảng giá ước tính khi tạo đơn (R-10)
Đã làm: `PricingService.CalculateQuoteRangeAsync` (dùng ở `quote-preview` và tạo đơn). Có tọa độ: quãng đường + nước đi qua từ `IGeoService`; `min` không có phí quá cảnh; `max` = quãng đường × `Booking:QuoteRangeDistanceFactor` (1,25) + phí `FEE_TRANSIT` cho các nước đường A→B đi qua, tối đa `Booking:QuoteMaxTransitCountries` (2). `transitCountryCodes` bỏ khỏi request của khách; nước tìm được ghi vào `booking_transit_countries`. Đơn lưu `quote_min`, `quote_max`; **tạm** `estimated_cost = quote_max` để luồng duyệt / cọc cũ chạy tiếp tới khi có BE-05 (`TODO(BE-05)` trong `BookingService`).

Làm:
- Request tạo đơn / báo giá nhận tọa độ điểm đón, trả (BE-02); **bỏ** `transitCountryCodes` khỏi request của khách (giữ cột trong DB vì tuyến sẽ ghi vào).
- `PricingService` trả **khoảng giá**: `min` = quãng đường thực (BE-01, A→B) × đơn giá + hạng chuồng + bảo hiểm (nếu chọn); `max` = quãng đường × 1.25 + phí thủ tục cho 1–2 nước quá cảnh nếu đường A→B cắt qua nước khác (dùng `CountriesAlongAsync`). Hệ số và số nước là cấu hình.
- Đơn mới: lưu `quote_min`, `quote_max`; `estimated_cost` để trống tới khi có báo giá chốt (BE-05). Trạng thái đầu là `Submitted` như cũ.
- Cập nhật các test hiện có của `PricingService` cho đúng hành vi mới.

### [x] BE-04 · Tuyến dự kiến của đơn (R-10)
Đã làm: bảng `route_plans` (+ `Superseded` cho bản duyệt cũ, chỉ mục duy nhất "một bản Approved / đơn") và `route_plan_stops`; `approve` của đơn thành **duyệt sơ bộ** (`PreApproved`, chưa thu cọc, báo Coordinator); `RoutePlanService` (preview / tạo / sửa / trình / duyệt / từ chối). Quy tắc cửa khẩu: một chặng được qua tối đa 1 + (số đầu chặng là `BorderGate`) nước (`RoutePlanRules`). Duyệt tuyến: đơn `RoutePlanned`, nước quá cảnh / quãng đường / giờ vận chuyển theo tuyến; `TODO(BE-05)` tính báo giá chốt. Manager từ chối được đơn ở `PreApproved` / `RoutePlanned`; chốt giá trị ngựa mở cho cả hai trạng thái này. **Phải merge cùng BE-05** (một mình BE-04 thì đơn dừng ở `RoutePlanned`).

Làm:
- Bảng `booking.route_plans` (một đơn có nhiều phiên bản tuyến; chỉ một bản `Approved` hiệu lực): `route_plan_id`, `booking_id`, `status` (`Draft` / `Submitted` / `Approved` / `Rejected`), `transit_country_codes`, `total_distance_km`, `estimated_hours`, `planned_by_user_id`, `reviewed_by_user_id`, `reviewed_at`, `rejection_reason`, `created_at`. Bảng con `booking.route_plan_stops`: thứ tự, tên, loại (Origin / RestStop / BorderGate / Destination), địa chỉ, tọa độ, `distance_km_from_previous`, `rest_minutes`.
- Trạng thái đơn mới: `PreApproved` (Manager duyệt sơ bộ), `RoutePlanned` (Manager đã duyệt tuyến). Luồng: `Submitted → PreApproved → RoutePlanned → Quoted …` (BE-05). Endpoint duyệt đơn hiện tại đổi thành "duyệt sơ bộ".
- Coordinator: tạo / sửa bản nháp tuyến (gửi danh sách điểm), hệ thống gọi BE-01 để tính từng chặng, suy ra nước quá cảnh, **cảnh báo** (trả về danh sách) khi đổi nước mà không có mốc `BorderGate` giữa hai điểm; trình duyệt thì chặn nếu còn cảnh báo đó.
- Manager: duyệt / từ chối tuyến (kèm lý do; từ chối thì Coordinator sửa và trình lại).
- Endpoint gợi ý: `GET/POST/PUT /api/bookings/{id}/route-plans`, `POST /api/route-plans/{id}/submit|approve|reject`.
- Thông báo: Coordinator khi có đơn `PreApproved` cần lập tuyến; Manager khi có tuyến chờ duyệt.

### [x] BE-05 · Báo giá chốt, hạn chấp nhận, cọc, đồng ý chính sách (R-10)
Đã làm: `BookingQuoteService` + `QuoteRules`. Duyệt tuyến gọi `IssueFinalQuoteAsync` trong cùng transaction: giá theo `total_distance_km` + nước quá cảnh của tuyến, giữ bảng giá / Express lúc gửi đơn (`QuotedAt = created_at`, bỏ kiểm tra lịch), bảo hiểm thay bằng dòng `INS_FULL` = `Insurance.PremiumPercent`% giá trị thỏa thuận từng con gói `Full`. Đơn mới: `estimated_cost = 0` tới khi `Quoted` (bỏ cách tạm của BE-03). Cột mới `quoted_at`, `quote_expires_at`, `quote_reminder_sent_at`, `quote_accepted_at`, `policy_version`, `policy_accepted_at`; trạng thái `Quoted`, `QuoteExpired`; giai đoạn khách `QuoteReady`. Tạo đơn và `accept-quote` bắt `acceptPolicy` + `policyVersion` = `Policy.Version` hiện hành. Hạn cọc = hạn báo giá (không gia hạn khi chấp nhận); `POST .../payments` chặn cọc sau hạn. Luật cũ "quá `DepositDueHours`" chỉ áp cho đơn không có `quote_expires_at`. Đơn `Quoted` vẫn lập lại tuyến được, duyệt bản mới thì báo giá tính lại. `TODO(BE-20)` lưu bằng chứng hợp đồng ở `AcceptQuoteAsync`. **Phải merge cùng BE-04.**
Làm:
- Manager duyệt tuyến (BE-04) → tính **báo giá chốt** theo quãng đường thật của tuyến + phí thủ tục từng nước quá cảnh + bảo hiểm theo từng con (BE-09/BE-12) + Express; lưu `estimated_cost`, `quote_breakdown`, `quoted_at`, `quote_expires_at` = now + `Booking.QuoteAcceptDays`; đơn → `Quoted`; email `QuoteReady`.
- `POST /api/bookings/{id}/accept-quote` (Customer, body có `policyVersion` và `acceptPolicy = true`): lưu `policy_version`, `policy_accepted_at`; đơn → `AwaitingDeposit` (dùng lại luồng cọc hiện có của `PaymentService`, cọc tính trên báo giá chốt). Từ chối báo giá: `POST .../decline-quote` → `Cancelled`, không phí.
- Job định kỳ (mở rộng `BookingExpiryJob`): còn 1 ngày → email `QuoteExpiring`; quá hạn mà chưa chấp nhận **hoặc** đã chấp nhận nhưng chưa cọc → `QuoteExpired`, email `QuoteExpired`.
- Lúc tạo đơn cũng bắt `acceptPolicy = true` + `policyVersion` (lưu lần đồng ý đầu).
- Bỏ luồng cũ "Manager duyệt → AwaitingDeposit ngay". Đơn cũ đang `AwaitingDeposit` vẫn chạy như cũ.
- Test: hết hạn đúng giờ, chấp nhận sau hạn bị chặn, cọc tính đúng trên báo giá chốt.

### [x] BE-06 · Hồ sơ theo tuyến, nhắc giấy, hủy khi quá hạn, đổi tuyến do công ty (R-10)
Đã làm: danh sách giấy của hồ sơ vốn tính động theo `booking_transit_countries` (BE-04 ghi theo tuyến đã duyệt) nên không phải đổi. Cấu hình mới `Dossier.SubmitDays` (7, nhóm chính sách, thêm vào văn bản chính sách mục 6). `booking_requests` thêm `docs_due_at`, `docs_reminders_sent`, `route_change_cancel_until`; `BookingActivator` đặt hạn khi cọc. `DossierDeadlineService` (job): nhắc khi còn ≤ 3 ngày / ≤ 1 ngày (`DocumentsReminder`, mỗi ngựa còn thiếu), quá hạn → hủy + `RefundForCancellationAsync` (thêm tham số `fullRefund`), `BookingCancelledMissingDocs`; ngựa đã lên chuyến thì báo Manager, không tự hủy. "Thiếu" = giấy bắt buộc chưa nộp hoặc bị từ chối (đang chờ duyệt coi như đã nộp). Quá hạn trong lúc đang có đổi tuyến do công ty → hoàn 100% (khách không có lỗi). Đổi tuyến do công ty: `RoutePlanService` cho lập / duyệt tuyến ở `AwaitingDeposit` / `Approved` / `Assigned` (ngựa chưa rời điểm đón), giữ giá, nước mới → gia hạn giấy + `route_change_cancel_until` + email; `POST /api/bookings/{id}/cancel-route-change` hoàn 100%. Khách đổi điểm đón / trả: `PUT /api/bookings/{id}/locations` (`BookingChangeService`), **chỉ khi chưa trả tiền** — đơn về `PreApproved`, báo giá / hợp đồng nháp rút lại, lập tuyến + báo giá lại. Đã cọc thì liên hệ công ty (chưa làm bản hợp đồng sửa đổi cho trường hợp này: cần quyết định cách xử lý tiền cọc đã trả).
Làm:
- Khi đơn được cọc: tạo hồ sơ cho từng ngựa với **danh sách giấy theo nước đi, các nước quá cảnh của tuyến đã duyệt, nước đến** (dùng `country_doc_requirements` hiện có).
- Hạn nộp giấy cho khách (cấu hình `Dossier.SubmitDays`, mặc định 7 ngày từ lúc cọc). Job: nhắc lần 1 khi còn 3 ngày, lần 2 khi còn 1 ngày (email `DocumentsReminder`); quá hạn còn thiếu → hủy đơn theo **chính sách khách hủy sau cọc** (`RefundForCancellationAsync`), email `BookingCancelledMissingDocs`.
- Đổi tuyến **do công ty** sau khi khách đã chấp nhận báo giá (Coordinator tạo phiên bản tuyến mới, Manager duyệt): giá chốt **giữ nguyên**; nước mới → thêm giấy cần nộp + đặt hạn mới + email; khách chọn hủy vì không lo được giấy → **hoàn 100%** (`POST /api/bookings/{id}/cancel-route-change`).
- Đổi điểm đón / trả **do khách**: tạo lại báo giá (về `RoutePlanned` → `Quoted`).

### [x] BE-08 · Điều kiện ngựa: tuổi, mang thai (R-12)
Đã làm: `racehorse.horses` thêm `is_pregnant`, `expected_foaling_date`, `last_foaling_date` (chỉ ngựa cái); cấu hình mới `Horse.GestationDays` (340), `Horse.LatePregnancyPercent` (90), `Horse.PostFoalingDays` (7) trong nhóm chính sách của BE-17. Kiểm tra lúc tạo đơn (`HorseEligibilityRules`), một lỗi liệt kê mọi con. Vừa đẻ: tính tới **ngày đầu** khoảng đón (task không nói rõ; chọn ngày đầu để ngày đón nào cũng đã qua 7 ngày). Chưa có API thêm ngựa vào đơn đã tạo nên chỉ kiểm tra ở bước tạo đơn.

Làm:
- Bảng ngựa: thêm `sex` nếu chưa có, `is_pregnant`, `expected_foaling_date` (nullable).
- Khi thêm ngựa vào đơn (và khi tạo đơn): chặn nếu tuổi tại **ngày cuối của khoảng đón** < `Horse.MinAgeMonths`; ngựa cái mang thai mà ngày cuối của khoảng đón ≥ ngày dự sinh − 34 ngày (≈ 90% của 340 ngày, đưa 340 và 90% vào cấu hình); vừa đẻ trong 7 ngày (thêm `last_foaling_date` nếu cần). Thông báo lỗi ghi rõ tên ngựa và lý do.
- Test cho từng ngưỡng (đúng ngưỡng, dưới, trên).

### [x] BE-09 · Giá trị khai báo theo từng con, chứng từ, giá trị thỏa thuận (R-12, R-13)
Đã làm: `booking_horses` thêm `declared_value`, `agreed_value`, `value_proof_path`, `insurance_plan` (`None` / `Full`); tạo đơn nhận giá trị từng con (khai một con thì phải khai mọi con; tổng ghi vào `booking_requests.declared_value`), từ `Insurance.ProofThresholdUsd` bắt buộc chứng từ; `PUT /api/bookings/{id}/horse-values` cho Manager chốt giá trị thỏa thuận (mặc định = khai báo) khi đơn còn `Submitted` (`TODO(BE-04)`: mở thêm `PreApproved` / `RoutePlanned`). Báo giá vẫn tính bảo hiểm trên tổng khai báo như cũ; chuyển sang giá trị thỏa thuận từng con ở BE-12.

Làm:
- `booking.booking_horses`: thêm `declared_value`, `agreed_value`, `value_proof_url`, `insurance_plan` (`None` / `Full`). Giữ `booking_requests.declared_value` cũ = tổng (để đơn cũ không vỡ), đơn mới tính từ các con.
- Giá trị ≥ `Insurance.ProofThresholdUsd` bắt buộc có tệp chứng từ (upload qua storage hiện có).
- Manager chốt `agreed_value` từng con khi duyệt tuyến / trước khi gửi báo giá (mặc định = giá trị khai báo); báo giá dùng `agreed_value`.

### [x] BE-10 · Kiểm tra trước khi xuất phát (R-12, R-15)
Đã làm: loại giấy `FIT_EXAM` (khách nộp vào hồ sơ thú y của ngựa, `is_mandatory = false` để không lọt vào checklist hồ sơ; kiểm lúc xuất phát, hạn `Booking.FitExamValidHours = 48`). `DepartureCheckRules` (có unit test) dùng chung cho `GET /api/trips/{id}/departure-check` và `StartTripAsync`: hồ sơ `Cleared`, giấy đã duyệt hết hạn tại giờ xuất phát thực tế (cảnh báo nếu hết hạn trước ngày tới), `FIT_EXAM` trong 48 giờ, đơn trả đủ, số tài xế BE-07 (đường bộ, km chặng đã lưu hoặc tuyến đã duyệt). DB chưa có `FIT_EXAM` thì chỉ cảnh báo `FitExamTypeNotConfigured`. Script `03_update_fit_exam.sql`.
Làm:
- Loại giấy mới "Khám lâm sàng đủ sức khỏe" (`FIT_EXAM`), hạn 2 ngày, bắt buộc cho **mọi** ngựa ở mọi tuyến.
- `StartTripAsync` kiểm tra và trả **một lỗi liệt kê tất cả vấn đề**: hồ sơ chưa `Cleared`; giấy hết hạn tính tại **giờ xuất phát thực tế**; thiếu `FIT_EXAM` trong 48 giờ; đơn chưa trả đủ phần còn lại; thiếu tài xế theo quy tắc BE-07.
- `GET /api/trips/{id}/departure-check` (đoàn xe, Coordinator) trả cùng danh sách đó để app hiện trước khi bấm xuất phát.

### [x] BE-11 · Rời chuyến chỉ qua sự cố + `Deceased` (R-12)
Đã làm (quyết định chốt 26/10/2026):
- `HandOverHere`: ký **biên bản e-POD riêng** cho con ngựa tại nơi dỡ (`POST /api/trips/{id}/handover` + `tripHorseId`); bỏ ràng buộc một biên bản / đơn / chuyến.
- `ReturnToOrigin`: con ngựa **kết thúc** với đơn, không về hàng chờ; chở về do Coordinator sắp xếp ngoài hệ thống. Chỉ `WaitRecover` về hàng chờ.
- Biên bản thú y nộp **lúc đề xuất** phương án (`vetReportPath`, tải lên qua `POST /api/files`); `Deceased` chỉ ở sự cố `Critical`.
- `TODO(BE-12)` (mở hồ sơ bồi thường) và `TODO(BE-14b)` (hoàn cước chưa đi) trong `IncidentService.ApplyOffloadAsync` đã làm ở hai task đó.

Làm:
- Bỏ endpoint `POST /api/trips/{id}/horses/{tripHorseId}/offload` (hoặc trả 410 kèm hướng dẫn), cập nhật API.md.
- Phương án `OffloadHorse` (đã có) bắt buộc `affectedTripHorseId` + `offloadOutcome`: `WaitRecover` (về hàng chờ xếp chuyến), `ReturnToOrigin`, `HandOverHere` (khách nhận tại chỗ, cho phép ký e-POD tại mốc đó), `Deceased`. Duyệt phương án mới đổi trạng thái ngựa. Trường hợp khẩn cấp: tài xế đánh dấu `emergencyOffload` khi báo sự cố → ngựa đổi trạng thái ngay, phương án vẫn phải duyệt sau.
- Trạng thái mới `Deceased` cho `trip_horses` và `booking_horses`; bắt buộc có tệp biên bản thú y; thông báo + email khách; mở hồ sơ bồi thường (gọi BE-12).
- Hoàn phần cước chưa đi của con ngựa đó theo `fault_party` (dùng hàm tính của BE-14b).


### [x] BE-12 · Bảo hiểm và hồ sơ bồi thường (R-13)
Đã làm: phí `Full` theo từng con đã có từ BE-04 (dòng `INS_FULL`). Bảng `billing.insurance_claims` (chốt gói + giá trị thỏa thuận lúc mở, thêm `fault_party`, `delivery_failure_id`, `paid_reference`); `ClaimService` + `ClaimRules` (trần chi trả, cửa sổ 24 giờ, có unit test). Tự mở ở: sự cố `Critical` có chọn ngựa (`Injury`), rời chuyến `Deceased` (`Death`; hồ sơ `Injury` chưa quyết định của cùng sự cố được nâng thành `Death`), rời chuyến vì `EquineHealthIssue`, e-POD `Injured`, từ chối nhận `RefusedInjured` / `RefusedWrongHorse` (`Injury` / `Loss`); mỗi sự cố / lần không giao được chỉ một hồ sơ mỗi con. Khách mở trong 24 giờ sau `delivered_at` của con ngựa. Duyệt: `faultParty` bỏ trống thì lấy theo sự cố. Chi trả qua `IPaymentService.PayCompensationAsync`: phần ≤ số còn hoàn được → hoàn qua cổng (dòng `Compensation` gắn khoản gốc, dùng lại retry / mark-refunded), phần vượt → dòng `Compensation` `Manual` kèm mã chuyển khoản. `payments.payment_type` nới lên `varchar(15)`; `ck_payments_refund_link` cho phép `Compensation` có hoặc không có khoản gốc. 8 endpoint mới (API.md mục 19c). Script `03_update_insurance_claims.sql`.
Làm:
- Phí: `Full` = `Insurance.PremiumPercent` × `agreed_value` từng con, cộng vào báo giá (thay dòng `INS_TRANSIT` hiện tại).
- Bảng `billing.insurance_claims`: `claim_code`, `booking_id`, `booking_horse_id`, `incident_id?`, `claim_type` (`Death` / `Injury` / `Loss`), `plan` (lúc xảy ra), `requested_amount`, `approved_amount`, `status` (`Open` / `UnderReview` / `Approved` / `Rejected` / `Paid`), `decision_reason`, `evidence` (danh sách tệp), `opened_by`, `reviewed_by`, các mốc thời gian.
- Tự mở khi: ngựa `Deceased` (BE-11); phương án `OffloadHorse` vì sức khỏe; sự cố Critical liên quan một con ngựa; e-POD `Injured` hoặc từ chối nhận vì thương tích (BE-16). Khách tự mở trong **24 giờ** sau khi ký e-POD.
- Trần chi trả theo gói: `None` → chỉ khi `fault_party = Company`, chết / mất ≤ `BaseLiabilityUsd`, thương ≤ `BaseVetCapUsd`; `Full` → chết / mất ≤ 100% `agreed_value`, thương ≤ `VetCapPercent`% `agreed_value`. Manager duyệt vượt trần → 400.
- Chi trả: ghi một dòng thanh toán loại mới `Compensation` (thêm vào `PaymentTypes` + CHECK); hoàn qua VNPay nếu ≤ số tiền khách đã trả cho đơn, phần còn lại ghi nhận chuyển khoản thủ công kèm mã tham chiếu (giống `MarkRefundedManuallyAsync`).
- Email `ClaimDecision`. Endpoint cho khách (xem, mở, nộp chứng từ) và Manager (danh sách, duyệt, từ chối, đánh dấu đã trả).
- Test các trần chi trả và điều kiện "chỉ khi lỗi công ty".

### [x] BE-13 · `fault_party` + bồi thường giao trễ (R-09)
Đã làm: `resolve` bắt buộc `faultParty` (+ `faultNote`); đóng chuyến bị chặn khi còn sự cố `Approved` chưa đóng. "Lỗi công ty" = không có sự cố được chấp nhận hoặc có ít nhất một sự cố `Company` (dùng cho cả KPI và bồi thường). Sự cố `Company` không cộng chi phí vào phát sinh của khách (Chính sách 9.1). Bồi thường lưu ở bảng riêng `billing.delay_compensations` (chờ duyệt → duyệt / từ chối); duyệt thì tạo các dòng `Refund` qua `IPaymentService.RefundAmountAsync` (dùng lại cho BE-12 / BE-14b). Phí thủ tục = `FEE_DOCS`, `FEE_CLEARANCE`, `FEE_QUARANTINE`, `FEE_TRANSIT`; `FEE_PICKUP` tính là cước.

Làm:
- `incident.trip_incidents`: thêm `fault_party` (`Company` / `Customer` / `ForceMajeure`), bắt buộc khi đóng sự cố (`ResolveIncidentAsync` nhận thêm tham số).
- KPI: `DelayedForceMajeure` chỉ khi các sự cố đã duyệt có `fault_party = ForceMajeure` (thay cách hiện tại "có sự cố là bất khả kháng").
- Khi đóng chuyến (`TripCompletionService`): với **từng đơn**, giờ trễ = giờ ký e-POD − `delivery_deadline`. Nếu > `Delay.GraceHours` **và** chuyến trễ do lỗi công ty: tiền bù = tiền cước × min(ceil((giờ trễ − ân hạn) / 12) × `PercentPer12Hours`, `MaxPercent`)%; tiền cước = báo giá chốt − bảo hiểm − phí thủ tục (lấy từ `quote_breakdown`). Tạo dòng hoàn tiền **chờ Manager duyệt** (loại `DelayCompensation`), Manager có thể sửa số tiền kèm lý do; duyệt → hoàn qua VNPay, email `DelayCompensation`.
- Test công thức: đúng 6h, 6h01, 18h, trần 25%, bất khả kháng = 0.

### [x] BE-14b · Cửa khẩu từ chối, biên giới đóng, quay đầu, hoàn theo quãng đường (R-14)
Đã làm: loại sự cố `BorderRejected`, `BorderClosed`, `CrewUnavailable`, `RecipientUnavailable`; hành động `ReturnToOrigin`, `HoldAtStable`; trạng thái `AwaitingConsent`. `ReturnRouteRules`: tuyến về = các mốc đã đi theo chiều ngược, mốc cuối là điểm xuất phát loại `Destination` (e-POD ký ở nơi đi), giờ theo khoảng cách giờ chiều đi; mọi mốc chưa tới bị bỏ, ngựa chưa đón → `Cancelled`. **Không thêm trạng thái ngựa "đang trả về"**: "đang trả về" = chuyến có sự cố `ReturnToOrigin` đã duyệt (thêm trạng thái mới sẽ phải sửa mọi danh sách trạng thái của BE-07). Hàm hoàn tiền `RefundAmount` (Company 100% đã trả; Customer 0; ForceMajeure tiền cước × tỉ lệ chưa đi, tiền cước như BE-13 nên phí thủ tục không hoàn) chạy khi đóng sự cố quay về; tỉ lệ chưa đi lưu trong `route_change.ReturnUnusedRatios`. `HoldAtStable`: phí chuồng = ngày × ngựa × `Stable.FeePerHorsePerDayUsd` (khóa cấu hình mới, 50) cộng vào `additional_cost` khi đóng. Đi vòng tốn thêm: `extra_cost_for_customer`, bảng `incident.incident_consents`, `POST /api/incidents/{id}/customer-consent`; đủ đồng ý → áp tuyến + cộng tiền vào phát sinh + hợp đồng `DetourAccepted`; một người từ chối → `ReturnToOrigin`. `extraCostForCustomer` là **tổng**, chia theo số ngựa như chi phí phát sinh khác. Làm luôn `TODO(BE-14b)` của BE-11: đóng sự cố `OffloadHorse` (trừ `WaitRecover`) → hoàn phần cước chưa đi của con ngựa (`OffloadRefundAmount`: cước đơn / số ngựa × tỉ lệ chưa đi; lỗi công ty và bất khả kháng như nhau vì phí thủ tục không nằm trong cước, lỗi khách 0).
Làm:
- Loại sự cố mới: `BorderRejected`, `BorderClosed`, `CrewUnavailable`, `RecipientUnavailable` (CHECK + constant). Hành động mới: `ReturnToOrigin`, `HoldAtStable`.
- `ReturnToOrigin` khi duyệt: tạo tuyến về (đảo các mốc đã đi, dùng `RoutePlan` hiện có), ngựa của các đơn bị ảnh hưởng chuyển trạng thái "đang trả về", e-POD cuối cùng ký ở nơi đi.
- Hàm tính hoàn tiền theo bảng lỗi (dùng chung cho BE-11, BE-14b, BE-16):
  - `Company`: hoàn 100% đã trả.
  - `Customer`: không hoàn cước; chi phí quay đầu / chuồng tạm thành khoản phát sinh.
  - `ForceMajeure`: hoàn tiền cước × (quãng đường chưa đi / tổng quãng đường) − phí thủ tục đã nộp; chi phí chuồng tạm + thú y thành khoản phát sinh của khách.
- Đi đường vòng tốn thêm tiền (`BorderClosed` + `Reroute`): phương án kèm `extraCostForCustomer`; Manager duyệt xong thì **khách phải đồng ý** trên app (`POST /api/incidents/{id}/customer-consent`) trước khi áp tuyến; khách từ chối → phương án chuyển `ReturnToOrigin`.
- Test hàm tính hoàn cho 3 nguyên nhân.

### [x] BE-15 · Người nhận và thông báo người nhận (R-16)
Đã làm: cột người nhận + `recipient_confirm_requested_at`, `alternate_recipient_*` trên `booking_requests`; logic gom ở `RecipientService` (luồng chuyến chỉ thêm một dòng gọi ở duyệt chuyến, xuất phát, rời mốc, e-POD). Tạo đơn bắt tên + SĐT hoặc `recipientIsCustomer = true` (lấy theo hồ sơ khách). Khung giờ đã báo = ETA (tính bằng `LiveEtaCalculator`) ± 1 giờ, ghi lúc xuất phát và cập nhật lúc "xe sắp tới". Email người nhận chỉ gửi khi có email. So tên người ký e-POD bỏ dấu / hoa thường (`RecipientRules`). Thông tin người nhận cho đoàn xe ở API riêng `GET /api/trips/{id}/recipients` (không sửa chi tiết chuyến để tránh đụng BE-07). Đổi người nhận sau khi giao kết hợp đồng → `CreateAmendmentAsync(RecipientChanged)`, người nhận mới có hiệu lực ngay (bản hợp đồng mới để ghi nhận). Hợp đồng PDF in người nhận.
Làm:
- `booking_requests`: `recipient_name`, `recipient_phone`, `recipient_email`, `recipient_id_number`, `recipient_is_customer`, `recipient_confirmed_at`, `recipient_notified_window_start`, `recipient_notified_window_end`. Bắt buộc tên + SĐT khi tạo đơn mới. Khách sửa được tới khi xe check-in điểm trả (`PUT /api/bookings/{id}/recipient`).
- Email người nhận (BE-19, hàm `EmailAddressAsync`): chuyến được duyệt (`RecipientScheduled`); xe xuất phát (`RecipientDeparted`, ghi ETA làm khung giờ đã báo); xe rời mốc ngay trước điểm trả của đơn (`RecipientArrivingSoon`).
- Job: 24 giờ trước ETA tới điểm trả → thông báo + email `ConfirmRecipient` cho khách; `POST /api/bookings/{id}/confirm-recipient` lưu `recipient_confirmed_at`.
- e-POD: API chi tiết chuyến cho đoàn xe trả thông tin người nhận của từng đơn; e-POD điền sẵn, nếu tên người ký khác người nhận trong đơn → bắt buộc khách xác nhận trên app trước (`POST /api/bookings/{id}/approve-alternate-recipient`).
- Ký e-POD xong → thông báo + email `HandoverCompleted` cho khách.

### [x] BE-16 · Không giao được, phí chờ, quyết định 24 giờ (R-16)
Đã làm: bảng `acceptance.delivery_failures`, `handover_acceptances.waiting_hours / waiting_fee`; `DeliveryService` + `DeliveryRules` (phí chờ tính theo phút đầy đủ, giờ lẻ làm tròn lên). Ghi không giao được: lưu lý do / ảnh, tự mở sự cố (`RecipientUnavailable`, hoặc `EquineHealthIssue` / `Other` khi từ chối vì bị thương / nhầm ngựa) **trực tiếp** vì chuyến có thể đã `ArrivedDestination`; cho xe rời mốc `DropOff`. Theo lựa chọn của anh, **chi phí tự tính**: khóa mới `Delivery.ExtraKmFeeUsd` (2 USD/km) cho người nhận / địa chỉ khác (km theo bản đồ), chuồng tạm theo `Stable.FeePerHorsePerDayUsd` (tính khi đóng chuyến), chở về = báo giá chiều ngược (bảng giá chưa có chiều ngược thì lấy cước cùng tuyến chiều đi). Quá hạn → `HoldAtStable` (job). Phí chờ + chi phí lựa chọn cộng vào phát sinh khi đóng chuyến (`GetChargesForTripAsync`). Điều phối thực hiện lựa chọn qua sự cố đã mở. Hồ sơ bồi thường khi từ chối vì bị thương / nhầm ngựa: làm ở BE-12.
Làm:
- `POST /api/trips/{id}/bookings/{bookingId}/delivery-failed` (đoàn xe): lý do `RecipientAbsent` / `RecipientRefused` / `RefusedInjured` / `RefusedWrongHorse`, ghi chú, ảnh.
- Phí chờ: giờ chờ = (lúc ký e-POD hoặc lúc ghi không giao được) − lúc check-in điểm trả − `Delivery.FreeWaitingHours`; chỉ tính nếu lúc check-in ≥ `recipient_notified_window_start`; làm tròn lên giờ; tối đa `MaxWaitingHours`; × `WaitingFeePerHourUsd` → cộng vào **khoản phát sinh** của đơn (`ExtraCharges`, luồng thanh toán Extra hiện có).
- `RecipientAbsent` / `RecipientRefused`: đơn sang trạng thái chờ khách quyết định, email `DeliveryFailed`; khách chọn trong `Delivery.DecisionHours` (`POST /api/bookings/{id}/delivery-decision`: `AlternateRecipient` (kèm người nhận mới + địa chỉ, tạo mốc giao mới), `HoldAtStable`, `ReturnToOrigin`); job quá hạn → `HoldAtStable`. Chi phí của các lựa chọn là khoản phát sinh của khách.
- `RefusedInjured` / `RefusedWrongHorse`: tạo sự cố + hồ sơ bồi thường (BE-12), `fault_party = Company`, chi phí chuồng tạm do công ty chịu (không cộng phát sinh cho khách).
- Test công thức phí chờ (xe tới sớm hơn khung đã báo = 0; 2h = 0; 3h10 = 2 giờ tính phí; trần 24 giờ).

### [x] BE-20 · Hợp đồng vận chuyển (R-17)
Đã làm: bảng `booking_contracts` (+ chỉ mục "một Draft / một Accepted mỗi đơn"), mã `HD-yyyy-nnnn` mỗi phiên bản. `ContractService` + `ContractPdfRenderer` (QuestPDF 2026.2.3, giấy phép Community; dùng font **Lato đi kèm gói QuestPDF**, đủ dấu tiếng Việt, không phải tải font riêng). Metadata PDF cố định theo ngày báo giá → sinh lại cùng dữ liệu ra **đúng từng byte**, nên `file_sha256` là SHA-256 của file. Phụ lục: `docs/CHINH_SACH_DICH_VU.md` nhúng vào assembly (`.dockerignore` có ngoại lệ cho file này), bỏ khối ghi chú `>`, thay `[Khóa = ...]` bằng cấu hình hiện hành (`SystemSettingService.ConfigValues`). Duyệt tuyến → báo giá → bản nháp; lập lại tuyến khi `Quoted` → bản nháp mới, bản cũ `Superseded`. `accept-quote` bắt `contractId` + `contractSha256`, lưu IP (`X-Forwarded-For`), User-Agent, phiên bản chính sách; email `ContractAccepted` gửi **link** tới trang đơn (không đính kèm file: hệ thống email hiện chưa hỗ trợ đính kèm). `IContractService.CreateAmendmentAsync` + `POST /api/contracts/{id}/accept` sẵn cho BE-06 / BE-14b / BE-15 (`TODO` áp dụng thay đổi khi chấp nhận). Vận đơn hàng không: cột `air_waybill_number`, `air_waybill_file_path` trên `fleet.trips`, API riêng `GET/PUT /api/trips/{id}/air-waybill` (không sửa chi tiết chuyến để tránh đụng BE-07). Người nhận trên hợp đồng: `TODO(BE-15)`.
Đọc `docs/PLAN_NGHIEP_VU_R09.md` mục R-17 và `docs/CHINH_SACH_DICH_VU.md` mục 12. **Không có bước ký riêng, không OTP:** cú bấm "Chấp nhận & đặt cọc" sau khi xem hợp đồng là đồng ý giao kết.

Làm:
- Bảng `booking.booking_contracts`: `contract_id`, `contract_code` (mã `HD-2026-0001`, thêm tiền tố vào `ICodeGenerator`), `booking_id`, `version` (1, 2, …), `status` (`Draft` / `Accepted` / `Superseded`), `reason` (`Initial` / `PickupDropoffChanged` / `DetourAccepted` / `RecipientChanged`), `file_path` (Supabase Storage), `file_sha256`, `policy_version`, `accepted_by_user_id`, `accepted_at`, `accepted_ip`, `accepted_user_agent`, `created_at`. Một đơn chỉ có tối đa một bản `Draft` và một bản `Accepted` hiệu lực. Script `03_update_booking_contracts.sql`.
- **Sinh PDF** bằng QuestPDF (giấy phép Community miễn phí cho tổ chức nhỏ; nhúng font hỗ trợ tiếng Việt, ví dụ Noto Sans, vào `Services/Assets/Fonts`). Nội dung: số hợp đồng + phiên bản; công ty, khách, người nhận; bảng ngựa (tên, vi mạch, giá trị thỏa thuận, gói bảo hiểm); tuyến đã duyệt (điểm đón / trả, các nước quá cảnh, tổng km); khoảng ngày đón, hạn giao; bảng giá chốt theo `quote_breakdown`, tiền cọc, phần còn lại, hạn thanh toán; Manager duyệt + ngày; phụ lục toàn văn chính sách (render từ `docs/CHINH_SACH_DICH_VU.md` nhúng làm resource, thay các giá trị `[...]` bằng cấu hình hiện hành, bỏ khối ghi chú cho nhóm phát triển). Sinh lại cùng dữ liệu phải ra cùng nội dung chữ (không đưa thời điểm sinh file vào nội dung) để mã băm ổn định.
- **Tạo bản nháp** khi đơn chuyển `Quoted` (gọi từ BE-05) và mỗi khi báo giá chốt thay đổi; bản nháp cũ bị thay.
- **Endpoint:**
  - `GET /api/bookings/{id}/contract` (Customer của đơn, nhân sự): thông tin bản hiện hành (nháp hoặc đã chấp nhận) + `fileSha256` + link tải có hạn.
  - `GET /api/bookings/{id}/contracts`: lịch sử các phiên bản.
  - `GET /api/contracts/{contractId}/file`: tải PDF (kiểm tra quyền xem đơn).
- **Chấp nhận** (mở rộng `accept-quote` của BE-05 và luồng chấp nhận bản mới): body có `contractId`, `contractSha256`, `acceptPolicy`, `policyVersion`. Server kiểm tra bản đó đang là `Draft` hiện hành và mã băm khớp (lệch → 400 `The contract has changed since you opened it. Please review the latest version.`), rồi chuyển `Accepted`, lưu người bấm / thời điểm / IP (`X-Forwarded-For` khi chạy sau proxy Railway) / User-Agent / phiên bản chính sách; bản `Accepted` cũ (nếu có) → `Superseded`. Email PDF cho khách (mẫu `ContractAccepted`: BookingCode, ContractCode, Version, link tải).
- **Bản mới khi đổi điều khoản:** BE-06 (khách đổi điểm đón / trả → báo giá lại), BE-14b (khách đồng ý trả thêm để đi đường vòng), BE-15 (đổi người nhận) gọi `IContractService.CreateAmendmentAsync(bookingId, reason)` → bản nháp `version + 1`. Thay đổi chỉ có hiệu lực khi khách chấp nhận bản mới (`POST /api/contracts/{contractId}/accept`, cùng kiểm tra như trên). Task nào làm trước thì để `// TODO(BE-20)` ở nơi gọi.
- **Đơn hàng không:** cột `air_waybill_number`, `air_waybill_file_path` trên chuyến (Coordinator nhập / tải lên), hiện trong chi tiết chuyến.
- Test: mã băm ổn định khi sinh lại; chấp nhận với mã băm cũ bị chặn; bản mới thay thế đúng bản cũ; khách khác không tải được hợp đồng.
