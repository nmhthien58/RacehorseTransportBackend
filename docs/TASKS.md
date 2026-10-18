# Phân chia công việc Backend

Cập nhật: 08/10/2026 · Người làm: **Thiên** (mã `T-xx`) và **Tú** (mã `B-xx`).

- Task của Tú là các phần độc lập với 6 luồng chính (danh mục quản trị, đọc thông báo, dữ liệu seed, nội dung email, test), nên hai người làm song song mà không phải chờ nhau.
- Đánh dấu `[x]` khi task đã merge vào `main`.

## Tiêu chí hoàn thành (áp dụng cho mọi task)

1. Build không lỗi, không cảnh báo mới.
2. Đủ ba lớp: Controller → Service → Repository; quy tắc nghiệp vụ nằm trong Service.
3. Endpoint có `[Authorize]` đúng role và có `/// <summary>` để hiện trên Swagger.
4. RequestModel có DataAnnotations; thông báo lỗi rõ ràng.
5. Đã thử trên Swagger cả trường hợp đúng lẫn sai (404, 400, 403) và thêm request mẫu vào file `.http`.

---

## M0 – Khung dự án (đã xong)

- [x] **T-00** Solution 3 lớp, 18 entity + `AppDbContext`, `IRepository<T>`, `IUnitOfWork`, khuôn `ApiResponse`, middleware bắt lỗi, Swagger, CORS, cache (Redis / bộ nhớ).
- [x] **T-00a** Auth: đăng ký Customer, đăng nhập, refresh token, đăng xuất, `GET /api/auth/me`, phân quyền theo role.
- [x] **T-00b** CRUD mẫu `DocumentTypes` (có tìm kiếm, sắp xếp, phân trang, cache).

---

## Việc của Tú

Làm theo thứ tự từ trên xuống: task sau khó hơn task trước một chút. Mỗi task một nhánh, một Pull Request.

### [x] B-01 · Quản lý phương tiện
Bảng `fleet.transport_assets` · entity `TransportAsset`.

| Method | Đường dẫn | Ai được gọi |
|---|---|---|
| GET | `/api/transport-assets?search=&status=&assetType=&sort=&page=&size=` | Nhân sự (mọi role trừ Customer) |
| GET | `/api/transport-assets/{id}` | Nhân sự |
| POST | `/api/transport-assets` | FleetCoordinator, Admin |
| PUT | `/api/transport-assets/{id}` | FleetCoordinator, Admin |
| DELETE | `/api/transport-assets/{id}` | FleetCoordinator, Admin |

Quy tắc:
- `AssetCode` (biển số) không trùng.
- `AssetType` thuộc `AssetTypes.All`, `Status` thuộc `AssetStatuses.All`, `CapacityHorses` từ 1 đến 12.
- Không xóa được xe đã từng gắn với chuyến (`Trips.VehicleId`) → `BusinessException`.
- Tìm kiếm theo `AssetCode` (không phân biệt hoa thường, xem cách làm trong `DocumentTypeService`); lọc thêm theo `status`, `assetType` (thêm 2 tham số vào controller).

### [x] B-02 · Quy định giấy tờ theo quốc gia
Bảng `clearance.country_doc_requirements` · entity `CountryDocRequirement`.

| Method | Đường dẫn | Ai được gọi |
|---|---|---|
| GET | `/api/country-doc-requirements?countryCode=&direction=&page=&size=` | Mọi người đã đăng nhập |
| GET | `/api/country-doc-requirements/{id}` | Mọi người đã đăng nhập |
| POST / PUT / DELETE | `/api/country-doc-requirements[/{id}]` | TransportSpecialist, Admin |

Quy tắc:
- `CountryCode` đúng 2 chữ cái, lưu viết hoa. `Direction` thuộc `RequirementDirections.All`.
- `DocTypeId` phải tồn tại. Bộ ba (`CountryCode`, `Direction`, `DocTypeId`) không trùng.
- `ValidityDays` để trống hoặc > 0.
- Response trả kèm `docTypeCode`, `docTypeName` (dùng `.Include(r => r.DocumentType)`).

### [x] B-03 · Bảng giá
Bảng `pricing.price_items` · entity `PriceItem`.

| Method | Đường dẫn | Ai được gọi |
|---|---|---|
| GET | `/api/price-items?category=&transportMode=&isActive=&search=&page=&size=` | LogisticsManager, Admin |
| GET | `/api/price-items/{id}` | LogisticsManager, Admin |
| POST / PUT | `/api/price-items[/{id}]` | LogisticsManager, Admin |
| PATCH | `/api/price-items/{id}/active` (body `{ "isActive": false }`) | LogisticsManager, Admin |

Quy tắc:
- `Category` thuộc `PriceCategories.All`, `CalcType` thuộc `PriceCalcTypes.All`.
- `TransportMode` để trống hoặc thuộc `TransportModes.All`; `DestCountryCode` để trống hoặc 2 chữ cái.
- `Value` ≥ 0. Bộ ba (`ItemCode`, `TransportMode`, `DestCountryCode`) không trùng.
- Không có DELETE: ngừng dùng một dòng giá bằng `isActive = false`.

### [x] B-04 · Bổ sung dữ liệu seed cho demo
Sửa file `database/02_seed.sql` (không đụng tới code C#). Thêm dữ liệu để demo phong phú hơn:

- 4–5 phương tiện đủ ba loại `AssetType`, có xe đang `Maintenance`.
- Mỗi role nhân sự thêm 1–2 tài khoản (dùng lại đúng chuỗi `password_hash` của các tài khoản có sẵn); thêm 2 khách hàng, mỗi người 2–3 con ngựa.
- Quy định giấy tờ cho các nước đã có trong bảng giá (TH, SG, JP, AU, KH, LA...).
- Cuối file có đoạn `setval(...)`: bảng nào chèn ID tường minh thì phải có dòng `setval` tương ứng.

Kiểm tra: chạy `00_reset.sql` → `01_schema.sql` → `02_seed.sql` không lỗi, đăng nhập được bằng tài khoản mới.

### [x] B-05 · Chuông thông báo – phần đọc
Bảng `common.system_notifications` · entity `SystemNotification`. Phần **tạo** thông báo do Thiên làm (T-03).

| Method | Đường dẫn | Mô tả |
|---|---|---|
| GET | `/api/notifications?isRead=&page=&size=` | Thông báo của người đang đăng nhập, mới nhất trước |
| GET | `/api/notifications/unread-count` | Trả `{ "count": 3 }` |
| PATCH | `/api/notifications/{id}/read` | Đánh dấu đã đọc |
| PATCH | `/api/notifications/read-all` | Đánh dấu đã đọc tất cả |

Quy tắc: chỉ thao tác trên thông báo có `RecipientUserId` = người đang đăng nhập (của người khác → `NotFoundException`). Lưu ý `NotificationId` kiểu `long`.

### [x] B-06 · Nội dung email
Viết nội dung (tiêu đề + thân HTML đơn giản, tiếng Việt) cho các email hệ thống gửi đi, mỗi mẫu một file trong `RacehorseTransport.Services/EmailTemplates/`. Chỗ cần điền dữ liệu viết dạng `{{FullName}}`, `{{BookingCode}}`, `{{Link}}`. Danh sách mẫu: đặt lại mật khẩu, đơn được duyệt, đơn bị từ chối, cần bổ sung giấy tờ, hồ sơ đã thông quan, có sự cố trên chuyến, ngựa đã tới điểm giao. Phần gửi mail do Thiên làm (T-29).

### [x] B-07 · Danh mục giá trị cho dropdown
`GET /api/lookups/enums` (không cần service, không cần DB) — trả về các mảng `All` trong `DomainConstants.cs` để FE đổ dropdown:

```json
{ "bookingStatuses": ["Submitted", "..."], "transportModes": ["Ground", "Air"], "stallClasses": ["..."], "...": [] }
```

### [x] B-08 · Bộ request thử nghiệm
Hoàn thiện file `RacehorseTransport.API.http`: mỗi endpoint của B-01 → B-07 có ít nhất một request đúng và một request sai. Ghi lại lỗi phát hiện được thành issue.

### [x] B-09 · Unit test công thức giá và KPI
Làm sau khi T-05 và T-26 xong. Tạo project `RacehorseTransport.Tests` (xUnit). Thiên viết sẵn 2 test mẫu; Tú viết thêm các trường hợp theo bảng trong phụ lục cuối file: mỗi hạng chuồng, từng bậc chiết khấu (1, 2, 4, 7 ngựa), có/không phụ phí gấp và điều hòa, có/không bảo hiểm; KPI khi trễ 0, 30, 31, 120, 600 phút, có và không có sự cố.

---

## Việc của Thiên

### M1 – Nền tảng dùng chung

- [x] **T-01 · Quản trị người dùng & hồ sơ cá nhân** — `PUT /api/auth/me` (sửa họ tên, số điện thoại). Admin tạo tài khoản nhân sự (`POST /api/users`), danh sách có lọc theo role (`GET /api/users?role=`), khóa/mở (`PATCH /api/users/{id}/active`), đặt lại mật khẩu. `PUT /api/auth/change-password` cho mọi người. `GET /api/users/staff?role=` cho Manager chọn người khi phân công.
- [x] **T-02 · Lưu file** — `IFileStorageService` đẩy file lên **Supabase Storage** (bucket riêng tư, trả link có chữ ký hết hạn vì giấy tờ là dữ liệu nhạy cảm; đổi nhà cung cấp không phải sửa nghiệp vụ); `POST /api/files` (multipart) trả về URL. Giới hạn dung lượng và đuôi file (pdf, jpg, png). Dùng cho giấy tờ, ảnh ngựa, ảnh nhật ký, chữ ký e-POD.
- [x] **T-03 · Gửi thông báo** — `INotificationService.SendAsync(userId, title, message, referenceType, referenceId)` và `SendToRoleAsync(role, ...)`. Các luồng bên dưới đều gọi tới đây. *(B-05 phụ thuộc task này để có dữ liệu thử.)*
- [x] **T-04 · Sinh mã** — `ICodeGenerator`: `BKG-2026-0001`, `DOS-…`, `TRP-…`, `INC-…`; không trùng khi hai request tới cùng lúc.
- [x] **T-28 · Hồ sơ ngựa của khách** — `GET/POST/PUT/DELETE /api/horses`. Khách chỉ thấy và sửa ngựa của mình (`OwnerUserId` lấy từ token, không nhận từ client; của người khác → 403); nhân sự xem được tất cả. `MicrochipNumber`, `PassportNumber` không trùng. Xóa mềm (`IsActive = false`), không xóa được ngựa đang nằm trong đơn chưa kết thúc.
- [x] **T-28b · Hồ sơ thú y theo ngựa** — `GET/POST/DELETE /api/horses/{id}/documents`, bảng `racehorse.horse_documents`. Chủ ngựa bổ sung giấy tờ bất cứ lúc nào (không bắt buộc khi tạo ngựa, không qua duyệt), nhân sự xem được; `validity` tính từ ngày hết hạn. Danh mục giấy tờ mở rộng thêm `IMPORT_PERMIT`, `GLANDERS_TEST`, `PIROPLASMOSIS_TEST`, `EVA_TEST`; DB cũ chạy `database/03_update_horse_documents.sql`.
- [x] **T-29 · Gửi email** — `IEmailService` gửi qua SMTP của một tài khoản Gmail lập riêng cho đồ án (cấu hình trong `appsettings.Local.json`), dùng mẫu của B-06. Gửi ở nền: mail lỗi thì ghi log, không làm hỏng request.
- [x] **T-30 · Quên mật khẩu & token trong database** — `POST /api/auth/forgot-password` gửi link đặt lại mật khẩu, `POST /api/auth/reset-password` đặt mật khẩu mới (token dùng một lần, hạn 60 phút, đăng xuất mọi phiên cũ). Refresh token chuyển từ cache về bảng `identity.user_tokens` nên không còn mất khi server khởi động lại. Phần xác thực email khi đăng ký đã bỏ (quyết định 08/10/2026).
- [x] **T-31 · Thông báo qua email** — `INotificationService` gửi kèm email cho **khách hàng** ở các sự kiện chính (duyệt/từ chối đơn, cần bổ sung giấy tờ, giấy bị từ chối, đã thông quan, hồ sơ có vấn đề, sự cố, đã tới điểm giao). Nhân sự chỉ nhận chuông trong ứng dụng.
- [x] **T-32 · Đăng nhập bằng Google** — `POST /api/auth/google` nhận ID token từ nút Google của frontend, kiểm tra chữ ký và Client ID ngay tại server, rồi cấp token của hệ thống. Email chưa có thì tạo tài khoản Customer đã xác thực; email đã có thì đăng nhập vào tài khoản đó.

### M2 – Flow 1: Tạo & duyệt yêu cầu vận chuyển · REQUIRED

- [x] **T-05 · Báo giá** — `IPricingService` tính giá bằng C# từ bảng `pricing.price_items` (công thức ở phụ lục cuối file); `POST /api/bookings/quote-preview` cho khách xem giá trước khi gửi (không ghi DB); lưu các dòng báo giá vào `QuoteBreakdown` (jsonb). Cache bảng giá, xóa cache khi B-03 sửa giá.
- [x] **T-06 · Tạo đơn** — `POST /api/bookings` (Customer): kiểm tra ngựa thuộc về khách, ngựa chưa nằm trong đơn khác trùng thời gian, khoảng pickup đủ xa (thời gian đặt trước tối thiểu) và hạn giao đủ thời gian vận chuyển, đơn đường bộ phải có `DistanceKm` (hoặc server tự tính), `IsExpress` theo quy tắc ở phụ lục "Báo giá". Tạo `BookingHorses`, tính giá, báo cho Manager. *(Sửa ngày 18/10/2026: khoảng pickup + hạn giao thay cho ngày đi / ngày giao.)*
- [x] **T-07 · Xem đơn** — `GET /api/bookings` (khách: đơn của mình; Specialist: đơn mình phụ trách; Manager/Coordinator: tất cả; lọc theo trạng thái, ngày), `GET /api/bookings/{id}` trả đủ ngựa, báo giá, Specialist phụ trách, tiến độ hồ sơ, và **từng con ngựa đang ở chuyến nào** (một đơn có thể đi nhiều chuyến).
- [x] **T-08 · Duyệt đơn** — `POST /api/bookings/{id}/approve` (Manager, **bắt buộc chọn Specialist phụ trách** ngay trong thao tác duyệt → `Approved`, tự tạo `DigitalDossier` cho từng ngựa), `/reject` (bắt buộc lý do), `/reassign-specialist` (Manager đổi người), `/cancel` (khách, chỉ khi `Submitted`). Không phân công Coordinator theo đơn. Mỗi bước gửi thông báo.
- [x] **T-09 · Tự hết hạn** — `BackgroundService` chuyển đơn `Submitted` quá 48 giờ sang `Expired`.

### M3 – Flow 2: Hồ sơ pháp lý & kiểm dịch · REQUIRED

- [x] **T-10 · Xem hồ sơ** — `GET /api/dossiers` (Specialist: hồ sơ được giao; khách: hồ sơ ngựa của mình), `GET /api/dossiers/{id}` kèm **checklist** giấy tờ bắt buộc (Export của nước đi + Import của nước đến, lấy từ `CountryDocRequirements`) đối chiếu với giấy đã nộp.
- [x] **T-11 · Nộp giấy tờ** — Specialist `POST /api/dossiers/{id}/request-documents` (→ `AwaitingDocs`, báo khách). Khách `POST /api/dossiers/{id}/documents` (file + loại + số + ngày hết hạn), nộp lại giấy bị từ chối.
- [x] **T-12 · Thẩm định giấy tờ** — `POST /api/documents/{id}/approve`, `/reject` (kèm `CorrectionNote`). Kiểm tra hạn hiệu lực theo `ValidityDays`. Bị từ chối lần thứ 3 → hồ sơ sang `Issue`, báo Manager.
- [x] **T-13 · Nộp cơ quan & thông quan** — `POST /api/dossiers/{id}/submit-to-authorities` (chỉ khi đủ giấy bắt buộc đã duyệt), `/clear` (nhập `ClearanceNumber`), `/issue` (nhập lý do). Khi mọi hồ sơ của đơn đã `Cleared` → báo khách và các Coordinator.

### M4 – Flow 3: Lộ trình & điều phối phương tiện · REQUIRED

- [x] **T-14 · Hàng chờ xếp chuyến & tài nguyên rảnh** — `GET /api/trips/unplanned-horses`: ngựa thuộc đơn `Approved` chưa nằm trên chuyến nào, mọi Coordinator đều thấy. `GET /api/trips/available-vehicles?from=&to=&mode=`, `/available-crew?from=&to=`: loại xe/người đã có chuyến trùng thời gian; đơn đường bay chỉ dùng `AirStall`.
- [x] **T-15 · Dự thảo chuyến** — `POST /api/trips` (Coordinator): xe, nhân sự (`TripCrew`, ít nhất 1 Driver, 1 Escort cho mỗi 3 ngựa), ngựa (`TripHorses`, lấy từ **một hoặc nhiều** đơn `Approved` cùng phương thức vận chuyển; ngược lại ngựa của một đơn có thể chia ra nhiều chuyến, không vượt sức chứa, không trùng ô chuồng), danh sách mốc. `PUT /api/trips/{id}` khi còn `Draft`.
- [x] **T-16 · Mốc lộ trình** — Thêm/sửa/xóa/đổi thứ tự `RouteCheckpoints` khi chuyến còn `Draft`; mốc đầu là `Origin`, mốc cuối là `Destination`, thời gian tăng dần.
- [x] **T-17 · Trình & duyệt kế hoạch** — `POST /api/trips/{id}/submit` (→ `PendingApproval`), `/approve` (Manager → `Scheduled`, báo tài xế/áp tải), `/reject` (lý do, tăng `PlanRejectionCount`; lần thứ 3 → `Cancelled`).
- [x] **T-18 · Màn hình tài xế** — `GET /api/trips` (tài xế / áp tải chỉ thấy chuyến mình có tên trong `TripCrew`; nhân sự khác thấy tất cả; lọc theo trạng thái, ngày), `GET /api/trips/{id}` đủ lịch trình, đoàn xe, ngựa phụ trách kèm yêu cầu chăm sóc, các mốc, và các đơn trên chuyến.

### M5 – Flow 4: Trạng thái & nhật ký lộ trình · REQUIRED

- [x] **T-19 · Xuất phát** — `POST /api/trips/{id}/start`: chỉ khi `Scheduled`, người gọi thuộc đoàn, mọi hồ sơ của ngựa trên chuyến đã `Cleared`. Chuyến → `InTransit`, xe → `InTransit`, mốc `Origin` → `Departed`.
- [x] **T-20 · Check-in mốc** — `POST /api/checkpoints/{id}/arrive`, `/clear` (cửa khẩu), `/depart`: phải theo đúng thứ tự, ghi giờ thực tế, chưa nghỉ đủ `MandatoryRestMinutes` thì không cho rời. Mỗi lần cập nhật báo cho khách.
- [x] **T-21 · Nhật ký thể trạng ngựa** — `POST /api/trips/{id}/welfare-logs` (nhiệt độ, nước, ăn uống, mức căng thẳng, ghi chú, ảnh); `GET` theo chuyến / theo ngựa. Ngựa `Refused` hoặc `Agitated` → cảnh báo Coordinator.
- [x] **T-22 · Theo dõi** — Coordinator: `GET /api/tracking/trips` (dự kiến so với thực tế, số phút trễ từng mốc). Khách: `GET /api/tracking/bookings/{id}` (dòng thời gian các mốc + nhật ký của ngựa mình). Cache Redis TTL ngắn, xóa cache khi có check-in mới.

### M6 – Flow 5: Sự cố & nắn tuyến · OPTIONAL

- [x] **T-23 · Báo sự cố** — `POST /api/trips/{id}/incidents` (loại, mức độ, vị trí, mô tả, ngựa bị ảnh hưởng); `GET` danh sách/chi tiết. Báo ngay cho Coordinator, Manager và khách.
- [x] **T-24 · Phương án & phê duyệt** — Coordinator `/propose` (phương án, ghi chú tuyến mới, chi phí phát sinh → `PlanProposed`); Manager `/approve` hoặc `/reject`; `/apply-reroute` (tắt mốc cũ `IsActive = false`, thêm mốc mới, chuyến → `EmergencyRerouting`); `/resolve` (chuyến → `InTransit`).

### M7 – Flow 6: Bàn giao & nghiệm thu · OPTIONAL

- [x] **T-25 · Đến đích & e-POD** — Việc "đã tới điểm giao" đã có ở T-20 (check-in mốc `Destination` chuyển chuyến sang `ArrivedDestination`), nên task này chỉ còn `POST /api/trips/{id}/handover` (người nhận, ảnh chữ ký vẽ tay trên màn hình, tình trạng, đối chiếu mã microchip từng ngựa). Tài xế tạo biên bản trên ứng dụng mobile. Chuyến ghép nhiều đơn thì **mỗi đơn một biên bản**; chuyến chỉ đóng được khi mọi đơn trên chuyến đã ký.
- [x] **T-26 · Đóng chuyến** — `POST /api/trips/{id}/close` (Manager): chốt KPI bằng C# trong một transaction (quy tắc ở phụ lục cuối file); trả về KPI, số phút trễ, chi phí thực tế.
- [x] **T-27 · Báo cáo** — `GET /api/reports/summary?from=&to=`: doanh thu, chi phí vận hành, tỉ lệ đúng giờ, KPI trung bình, số sự cố theo loại.

---

## Thứ tự làm và phụ thuộc

```
Tuần 1   Thiên: T-01 → T-04, T-28, T-05, T-06          Tú: B-01, B-02
Tuần 2   Thiên: T-29 → T-31, T-07 → T-09, T-10 → T-13  Tú: B-03, B-06, B-04
Tuần 3   Thiên: T-14 → T-18                            Tú: B-05, B-07
Tuần 4   Thiên: T-19 → T-22                            Tú: B-08 + sửa lỗi review
Tuần 5   Thiên: T-23 → T-27 (optional), dọn dẹp        Tú: B-09, hỗ trợ test tích hợp với FE
```

Không task nào của Tú chặn việc của Thiên. Chiều ngược lại:

- **B-05 cần T-03** để có thông báo mà đọc.
- **B-06 nên xong trước T-29**; nếu chậm, T-29 dùng nội dung tạm.
- **B-09 cần T-05 và T-26.**
- **B-03**: khi sửa giá phải gọi `ICacheService.RemoveAsync(CacheKeys.PriceItemsAll)` (Thiên thêm sẵn khóa này ở T-05), nếu quên thì báo giá dùng giá cũ tối đa 30 phút.

## Quyết định đã chốt (Thiên, 08/10/2026)

**Mô hình đơn – chuyến**

1. **Đơn và chuyến là nhiều – nhiều.** Một chuyến ghép được ngựa của nhiều đơn; một đơn nhiều ngựa có thể chia ra nhiều chuyến. Không tự tách đơn. Mọi chỗ cần "đơn của chuyến" hay "chuyến của đơn" đều đi qua `trip_horses`.
2. **Biên bản e-POD theo từng đơn trên chuyến** (`handover_acceptances.booking_id`, duy nhất theo cặp chuyến + đơn). Chuyến chỉ đóng được khi mọi đơn trên chuyến đã ký.

**Phân công**

3. **Specialist: có người phụ trách cụ thể**, Manager chọn ngay khi duyệt đơn và đổi được sau đó.
4. **Coordinator: không phân công theo đơn.** Đơn đã duyệt vào hàng chờ xếp chuyến chung; ai tạo chuyến thì phụ trách chuyến đó (`trips.planned_by_user_id`). Cột `assigned_coordinator_id` và trạng thái đơn `Assigned` không còn dùng. *Cần sửa câu "phân công song song" trong tài liệu Business Process – Flow 1.*
5. **Role `DriverEscort` chỉ để FE chia màn hình.** Vai trò thật trên từng chuyến (`Driver` / `Escort`) được phân riêng ở `trip_crew.crew_role` khi lập kế hoạch.

**Vận hành chuyến**

6. **Chặn cứng việc xuất phát khi còn hồ sơ chưa `Cleared`** (T-19).
7. **Theo dõi "real-time"**: client gọi lại API định kỳ; SignalR để sau nếu còn thời gian.
8. **Tài xế dùng ứng dụng mobile.** Chữ ký bàn giao là ảnh vẽ tay trên màn hình, lưu như các nền tảng giao hàng. Backend dùng chung một bộ API cho web và mobile.

**Tài khoản và hạ tầng**

9. **Không xác thực email khi đăng ký**: đăng ký xong là dùng được ngay. (Từng làm thử rồi bỏ ngày 08/10/2026 để giảm số email phải gửi.)
10. **Có làm**: quên mật khẩu, thông báo qua email cho khách, đăng nhập bằng Google. Email gửi từ Gmail lập riêng cho đồ án, qua Gmail API vì Railway chặn cổng SMTP. **Chưa làm**: thanh toán / đặt cọc.
11. **File lưu trên Supabase Storage**, cùng project Supabase mới của nhóm với database.
12. **Thông báo của API bằng tiếng Anh.**

**Lịch của đơn (Thiên, 18/10/2026)**

13. **Khách không chọn ngày khởi hành / ngày giao.** Khách nhập khoảng ngày ngựa sẵn sàng được đón (`pickup_window_start` → `pickup_window_end`, dài tối đa 30 ngày) và hạn giao trễ nhất (`delivery_deadline`). Ngày chạy thật do Coordinator chốt khi lập chuyến; chuyến phải khởi hành trong khoảng pickup và kết thúc trước hạn giao của mọi đơn trên chuyến.
14. **Thời gian đặt trước tối thiểu:** ngày pickup sớm nhất = hôm nay + 2 ngày duyệt + 2 ngày đặt cọc + 14 ngày làm hồ sơ + 3 ngày xếp chuyến (= 21 ngày, chỉnh trong section `Booking`). Manager không duyệt được đơn mà khoảng pickup không còn đủ 2 + 14 + 3 ngày.
15. **Thời gian vận chuyển do hệ thống ước tính** từ quãng đường và phương thức (`TransitTimeEstimator`, tham số ở section `Transit`), dùng cho phí áp tải theo ngày và kiểm tra hạn giao.

---

## Phụ lục: công thức nghiệp vụ (chuyển từ stored procedure của bản SQL Server)

### Báo giá (T-05)

Mọi mức giá đọc từ `pricing.price_items` (`is_active = true`). Với mỗi `item_code`, lấy **một** dòng khớp phương thức vận chuyển và nước đến của đơn; dòng ghi rõ nước đến được ưu tiên hơn dòng dùng chung (`dest_country_code` trống), rồi tới dòng ghi rõ phương thức.

| Bước | Cách tính |
|---|---|
| Cước một ngựa | Đường bay: giá `FREIGHT` · Đường bộ: `DistanceKm` × giá `FREIGHT` |
| 1. Cước gốc | Tổng từng ngựa chưa hủy: cước một ngựa × hệ số hạng chuồng (`STALL_SHARED` / `STALL_COMFORT` / `STALL_PRIVATE`). Mỗi hạng chuồng một dòng báo giá |
| 2. Chiết khấu số lượng | Bậc `Discount` cao nhất có `min_horses` ≤ số ngựa: cước gốc × % (dòng âm) |
| 3. Phụ phí | Tính trên cước sau chiết khấu: `SUR_FUEL` luôn có; `SUR_CLIMATE` khi `RequiresClimateControl`; `SUR_EXPRESS` khi `IsExpress` = đầu khoảng pickup trước (ngày pickup sớm nhất + 7 ngày) **hoặc** hạn giao dư dưới 24 giờ so với (đầu khoảng pickup + thời gian vận chuyển chậm nhất) |
| 4. Phí thủ tục | Các dòng `Clearance` có giá > 0: `PerHorse` × số ngựa, `PerBooking` × 1 |
| 5. Phí áp tải | Số groom = làm tròn lên (số ngựa ÷ 3). `PerGroom`: × đơn giá · `PerGroomDay`: × đơn giá × số ngày vận chuyển **do hệ thống ước tính** (làm tròn lên tổng giờ ÷ 24) |
| 6. Bảo hiểm | Chỉ khi có `DeclaredValue`: giá trị khai báo × % `INS_TRANSIT` |

`EstimatedCost` = tổng các dòng. Mỗi dòng lưu `{ code, name, qty, unitPrice, amount }`, làm tròn 2 chữ số. Báo lỗi nếu tuyến chưa có giá `FREIGHT`, hoặc đơn đường bộ thiếu `DistanceKm`.

### Ước tính thời gian vận chuyển (R-08, `TransitTimeEstimator`)

| Phương thức | Cách tính (tham số mặc định, section `Transit`) |
|---|---|
| Đường bộ | Giờ lái = km ÷ 55. Cứ lái 4,5 giờ nghỉ 45 phút; 1 tài xế lái tối đa 9 giờ/ngày (≥ 2 tài xế: 18 giờ), hết giờ thì nghỉ đêm 10 giờ. + 6 giờ mỗi cửa khẩu (khác nước = 1 cửa khẩu) + 1 giờ xếp / dỡ ở mỗi điểm đón, trả |
| Đường bay | 2 × 4 giờ xe ra / vào sân bay + có mặt trước 6 giờ + giờ bay (km ÷ 800, không có km thì 8 giờ) + kiểm tra ở nơi đến 4 giờ + xếp / dỡ |
| Kết quả | Tổng giờ dự kiến; mốc chậm nhất = tổng × 1,15; số ngày = làm tròn lên tổng ÷ 24 |

### Đóng chuyến và chốt KPI (T-26)

1. Chưa có biên bản e-POD thì không đóng được.
2. Số phút trễ = `ActualEndDate` − `PlannedEndDate` (âm thì tính 0; chưa có `ActualEndDate` thì lấy giờ hiện tại).
3. Chuyến có sự cố ở trạng thái `Approved` hoặc `Resolved` được coi là bất khả kháng.
4. Trễ ≤ 30 phút → `OnTime`, KPI 100. Trễ > 30 phút và bất khả kháng → `Delayed_AcceptableForceMajeure`, KPI 100. Trễ > 30 phút, không bất khả kháng → `Delayed_OperationalFault`, KPI = 100 − (phút trễ ÷ 30) × 5, thấp nhất 50.
5. `ActualCost` = `PlannedCost` + tổng `AdditionalCost` của các sự cố `Approved` / `Resolved`.
6. Chuyến → `Completed`, ghi người đóng và thời điểm đóng; xe → `Available`.
7. Đơn (`Approved` / `Assigned`) có ngựa trên chuyến → `Completed` nếu **mọi** ngựa chưa hủy của đơn đã nằm trên ít nhất một chuyến `Completed` và không còn chuyến nào dang dở.