# Hướng dẫn phần việc của Tú (B-01 → B-09)

Tài liệu này giải thích code đã triển khai cho các task của Tú, cách request đi qua từng tầng, cách chạy thử và những việc cần phối hợp với nhánh `main`.

> Trạng thái khi viết tài liệu (12/10/2026): nhánh hiện tại `feature/direct-file-upload` tại commit `ef66a98`, chậm hơn `main` 8 commit. `main` đã có T-29/T-31, Gmail API, xác thực email và Flow 5/6. Đọc kỹ mục **Bàn giao với main** trước khi merge.

## 1. Kiến trúc cần hiểu trước

Các CRUD B-01, B-02 và B-03 đi theo một đường thống nhất:

```text
HTTP request
  → JWT / [Authorize]
  → [ApiController] kiểm tra DataAnnotations
  → Controller
  → API Mapper: RequestModel → BusinessModel
  → Service: kiểm tra quy tắc nghiệp vụ
  → IRepository<Entity> / Entity Framework
  → PostgreSQL
  → Service Mapper: Entity → BusinessModel
  → API Mapper: BusinessModel → ResponseModel
  → ApiResponse hoặc ApiCollectionResponse
```

Trách nhiệm của từng tầng:

| Tầng                       | Trách nhiệm                                                                                                     |
| -------------------------- | --------------------------------------------------------------------------------------------------------------- |
| `API/RequestModels`        | Kiểm tra hình dạng dữ liệu: bắt buộc, độ dài, khoảng số, biểu thức chính quy.                                   |
| `API/Controllers`          | Khai báo route, HTTP method, role; lấy dữ liệu từ request và gọi service. Không viết nghiệp vụ hay `try/catch`. |
| `API/Mappers`              | `Trim`, viết hoa mã và chuyển Request/Business/Response.                                                        |
| `Services/Implementations` | Kiểm tra trùng, trạng thái, khóa ngoại, quyền trên bản ghi; ném `BusinessException` hoặc `NotFoundException`.   |
| `Services/Mappers`         | Chuyển Entity ↔ BusinessModel.                                                                                  |
| `Repositories`             | Entity, `AppDbContext`, truy vấn và ghi PostgreSQL. Không chứa nghiệp vụ.                                       |

Các service mới được đăng ký trong `ServiceCollectionExtensions.AddBusinessServices`:

```csharp
services.AddScoped<ITransportAssetService, TransportAssetService>();
services.AddScoped<ICountryDocRequirementService, CountryDocRequirementService>();
services.AddScoped<IPriceItemService, PriceItemService>();
services.AddScoped<INotificationReadService, NotificationReadService>();
```

`B-07` không cần service vì chỉ đọc hằng số trong code.

### Khuôn file của một CRUD

Ví dụ với B-01:

```text
RacehorseTransport.API/
  Controllers/TransportAssetsController.cs
  RequestModels/TransportAssetRequests.cs
  ResponseModels/TransportAssetResponse.cs
  Mappers/TransportAssetApiMapper.cs

RacehorseTransport.Services/
  BusinessModels/TransportAssetModels.cs
  Interfaces/ITransportAssetService.cs
  Implementations/TransportAssetService.cs
  Mappers/TransportAssetMapper.cs

RacehorseTransport.Repositories/
  Entities/TransportAsset.cs                 (đã có từ trước)
```

`QueryOptionsRequest`, `BusinessQueryOptions`, `ApplySort` và `ToPagedResultAsync` là phần dùng chung cho tìm kiếm, sắp xếp và phân trang.

---

## 2. B-01 – Quản lý phương tiện

### File chính

- API: `TransportAssetsController.cs`, `TransportAssetRequests.cs`, `TransportAssetResponse.cs`, `TransportAssetApiMapper.cs`.
- Service: `TransportAssetService.cs`, `ITransportAssetService.cs`, `TransportAssetModels.cs`, `TransportAssetMapper.cs`.
- Dữ liệu: entity `TransportAsset`, bảng `fleet.transport_assets`.

### Endpoint và quyền

| Method | Route                        | Quyền                                        |
| ------ | ---------------------------- | -------------------------------------------- |
| GET    | `/api/transport-assets`      | `UserRoles.Staff`, tức mọi role trừ Customer |
| GET    | `/api/transport-assets/{id}` | Nhân sự                                      |
| POST   | `/api/transport-assets`      | FleetCoordinator, Admin                      |
| PUT    | `/api/transport-assets/{id}` | FleetCoordinator, Admin                      |
| DELETE | `/api/transport-assets/{id}` | FleetCoordinator, Admin                      |

Controller có `[Authorize(Roles = UserRoles.Staff)]`; các action ghi có thêm `ManageRoles`. Hai attribute được áp dụng đồng thời, nhưng FleetCoordinator và Admin đều nằm trong `Staff` nên đúng quyền.

### Luồng xử lý

1. Request model kiểm tra `AssetCode` tối đa 30 ký tự và `CapacityHorses` từ 1 đến 12.
2. API mapper cắt khoảng trắng và viết hoa `AssetCode`.
3. Service kiểm tra `AssetType ∈ AssetTypes.All`, `Status ∈ AssetStatuses.All`, sức chứa 1–12.
4. Khi tạo/sửa, service kiểm tra `AssetCode` chưa trùng; lúc sửa loại chính ID hiện tại khỏi phép kiểm tra.
5. GET tìm `AssetCode` không phân biệt hoa thường, lọc thêm `status`, `assetType`, rồi sort/page.
6. DELETE kiểm tra mọi `Trip.VehicleId`. Xe đã từng xuất hiện ở bất kỳ chuyến nào đều không được xóa, kể cả chuyến đã kết thúc.

Các lỗi nghiệp vụ thành HTTP 400; ID không tồn tại thành 404; Customer gọi GET thành 403.

### Ca kiểm tra quan trọng

- Tạo xe hợp lệ; mã nhập chữ thường phải được lưu viết hoa.
- Mã xe trùng, `AssetType`/`Status` sai, sức chứa 0 hoặc 13 → 400.
- Customer GET → 403; Coordinator POST → 201.
- Xóa xe mới chưa dùng → thành công; xóa asset ID 1 đã gắn chuyến seed → 400.

---

## 3. B-02 – Quy định giấy tờ theo quốc gia

### File chính

- API: `CountryDocRequirementsController.cs`, `CountryDocRequirementRequests.cs`, `CountryDocRequirementResponse.cs`, `CountryDocRequirementApiMapper.cs`.
- Service: `CountryDocRequirementService.cs`, `ICountryDocRequirementService.cs`, `CountryDocRequirementModels.cs`, `CountryDocRequirementMapper.cs`.
- Dữ liệu: entity `CountryDocRequirement`, bảng `clearance.country_doc_requirements`.

### Endpoint và quyền

- GET danh sách/chi tiết: mọi người đã đăng nhập.
- POST/PUT/DELETE: chỉ TransportSpecialist và Admin.

### Luồng xử lý

1. `CountryCode` phải đúng hai chữ cái ASCII và được lưu viết hoa (`vn` → `VN`).
2. `Direction` phải là `Export` hoặc `Import` từ `RequirementDirections.All`.
3. `DocTypeId` phải dương và phải tồn tại trong `clearance.document_types`.
4. `ValidityDays` được để `null`; nếu có phải lớn hơn 0.
5. Bộ ba `(CountryCode, Direction, DocTypeId)` không được trùng.
6. GET dùng `.Include(r => r.DocumentType)` nên response có cả `docTypeCode` và `docTypeName`, không bắt frontend gọi thêm endpoint khác.

### Ca kiểm tra quan trọng

- Tạo bằng `countryCode: "vn"`, sau đó đọc lại phải là `VN`.
- `VNM`, `V1`, direction `Transit`, `ValidityDays = 0` → 400.
- `DocTypeId` không tồn tại → 400.
- Tạo trùng bộ ba → 400.
- Customer GET được nhưng POST → 403.

---

## 4. B-03 – Bảng giá

### File chính

- API: `PriceItemsController.cs`, `PriceItemRequests.cs`, `PriceItemResponse.cs`, `PriceItemApiMapper.cs`.
- Service quản trị: `PriceItemService.cs`, `IPriceItemService.cs`, `PriceItemModels.cs`, `PriceItemMapper.cs`.
- Service tính giá đã có: `PricingService.cs` / `IPricingService.cs`.
- Cache key: `CacheKeys.PriceItemsAll`.
- Dữ liệu: entity `PriceItem`, bảng `pricing.price_items`.

`PriceItemService` dùng để quản trị bảng giá; `PricingService` dùng bảng giá để tính báo giá. Hai service có mục đích khác nhau, không gộp lại.

### Endpoint và quyền

Toàn bộ `/api/price-items` chỉ dành cho LogisticsManager và Admin:

- GET danh sách với `category`, `transportMode`, `isActive`, `search`, `sort`, `page`, `size`.
- GET `/{id}`.
- POST và PUT.
- PATCH `/{id}/active` với body `{ "isActive": false }`.
- Không có DELETE; giá cũ được vô hiệu hóa bằng `isActive = false`.

### Luồng xử lý

1. `ItemCode` được trim và viết hoa; chuỗi trống của `TransportMode`/`DestCountryCode` đổi thành `null`.
2. Kiểm tra `Category`, `CalcType`, `TransportMode` theo các mảng `All` trong `DomainConstants.cs`.
3. `DestCountryCode` nếu có phải đúng hai chữ cái và được viết hoa; `Value >= 0`.
4. Bộ ba `(ItemCode, TransportMode, DestCountryCode)` không trùng. `null` mang nghĩa dùng chung cho mọi mode/nước.
5. POST mặc định `IsActive = true`; PUT giữ nguyên trạng thái đang có; PATCH là nơi bật/tắt.
6. Sau POST, PUT hoặc PATCH, service gọi:

```csharp
await _cache.RemoveAsync(CacheKeys.PriceItemsAll);
```

Nếu thiếu bước 6, `PricingService` có thể tính theo giá cũ tối đa 30 phút.

### Ca kiểm tra quan trọng

- Role khác Manager/Admin → 403.
- Category, CalcType, TransportMode, mã nước sai → 400.
- Giá âm và bộ ba trùng → 400.
- PATCH thiếu `isActive` → 400.
- Sau sửa giá, gọi `POST /api/bookings/quote-preview` và xác nhận báo giá dùng giá mới.

---

## 5. B-04 – Dữ liệu seed demo

Chỉ sửa `database/02_seed.sql`, không thêm logic C#.

Nội dung bổ sung hiện tại:

- 5 phương tiện đủ `HorseTruck_AirSuspension`, `HorseVan`, `AirStall`; asset ID 4 ở trạng thái `Maintenance`.
- Tổng 13 tài khoản: bộ 6 role ban đầu, thêm nhân sự và 2 Customer mới.
- Customer ID 12 có 2 ngựa; Customer ID 13 có 3 ngựa; tổng dữ liệu seed có 7 ngựa.
- Quy định Import cho các nước có bảng giá: TH, SG, MY, HK, KR, JP, AE, AU, CN, FR, GB, DE, US, KH, LA; VN có quy định Export.
- Kịch bản hoàn tất có `distance_km` cho đơn đường bộ và mỗi hồ sơ đã duyệt đủ bốn loại giấy bắt buộc của tuyến VN → CN.
- Các bảng chèn ID tường minh có `setval(...)` ở cuối file để lần POST tiếp theo không đụng khóa chính.

Tài khoản seed dùng cùng mật khẩu ghi ở đầu `02_seed.sql`. Không sao chép password hash mới bằng tay; mọi tài khoản dùng lại hash chuẩn trong file.

### Kiểm tra B-04

1. Chạy sạch `00_reset.sql` → `01_schema.sql` → `02_seed.sql`.
2. Đăng nhập một tài khoản mới, ví dụ `huy.pham@fleet.com`.
3. Gọi API tạo thêm xe/ngựa để chắc chắn sequence đã tăng đúng, không lỗi duplicate primary key.
4. Kiểm tra asset Maintenance và danh sách quy định từng nước.

Lần kiểm chứng ngày 12/10/2026 đã rà tĩnh toàn bộ FK, unique key và `setval`, nhưng máy chạy kiểm chứng không có `psql` và Docker daemon chưa hoạt động nên chưa thể thực thi ba script trên PostgreSQL thật. Đây là bước bắt buộc phải chạy lại ở máy dev/CI trước khi merge.

---

## 6. B-05 – Chuông thông báo, phần đọc

### File chính

- API: `NotificationsController.cs`, `NotificationRequests.cs`, `NotificationResponses.cs`, `NotificationApiMapper.cs`.
- Service: `NotificationReadService.cs`, `INotificationReadService.cs`, `NotificationModels.cs`, `NotificationMapper.cs`.
- Phần tạo thông báo vẫn là `NotificationService` / `INotificationService` của T-03.

Tách `NotificationReadService` khỏi `NotificationService` giúp phần hộp thư của Tú không làm thay đổi hợp đồng tạo thông báo mà các flow khác đang dùng.

### Endpoint

| Method | Route                                    | Ý nghĩa                                     |
| ------ | ---------------------------------------- | ------------------------------------------- |
| GET    | `/api/notifications?isRead=&page=&size=` | Danh sách của user hiện tại, mới nhất trước |
| GET    | `/api/notifications/unread-count`        | Trả `{ count }` trong `ApiResponse`         |
| PATCH  | `/api/notifications/{id}/read`           | Đánh dấu một thông báo đã đọc               |
| PATCH  | `/api/notifications/read-all`            | Đọc tất cả thông báo của user hiện tại      |

Tất cả endpoint cần đăng nhập, không giới hạn role.

### Quy tắc bảo mật

- `currentUserId` lấy từ JWT bằng `User.GetUserId()`, không nhận từ body/query.
- Mọi truy vấn đều có `RecipientUserId == currentUserId`.
- Khi mark-read, ID tồn tại nhưng thuộc người khác vẫn trả 404, tránh tiết lộ thông báo của người khác.
- `NotificationId` dùng `long` từ controller đến service.
- `read-all` dùng `ExecuteUpdateAsync`, cập nhật bằng một câu SQL thay vì vòng lặp nhiều lần.

### Ca kiểm tra quan trọng

- Lọc `isRead=true/false`, kiểm tra `createdAt` giảm dần.
- Unread count giảm sau khi đọc.
- User A đọc ID của user B → 404 và dữ liệu B không đổi.
- Read-all của A không làm thay đổi thông báo B.
- Gọi mark-read hai lần vẫn an toàn (idempotent).

---

## 7. B-06 – Nội dung email

Tám file nằm trong `RacehorseTransport.Services/EmailTemplates/`:

| File                      | Placeholder chính                                                                                |
| ------------------------- | ------------------------------------------------------------------------------------------------ |
| `VerifyEmail.html`        | `FullName`, `Link`, `ExpiresInHours`                                                             |
| `ResetPassword.html`      | `FullName`, `Link`, `ExpiresInMinutes`                                                           |
| `BookingApproved.html`    | `FullName`, `BookingCode`, `Link`                                                                |
| `BookingRejected.html`    | `FullName`, `BookingCode`, `RejectionReason`, `Link`                                             |
| `DocumentsRequested.html` | `FullName`, `BookingCode`, `HorseName`, `DocumentRequest`, `Link`                                |
| `DossierCleared.html`     | `FullName`, `BookingCode`, `HorseName`, `ClearanceNumber`, `Link`                                |
| `TripIncident.html`       | `FullName`, `BookingCode`, `TripCode`, `IncidentCode`, `Severity`, `IncidentDescription`, `Link` |
| `HorseArrived.html`       | `FullName`, `BookingCode`, `HorseName`, `Destination`, `Link`                                    |

Quy ước của renderer trên `main`:

- Dòng đầu tiên phải là `Subject: ...`.
- Phần còn lại là HTML body.
- Placeholder có dạng `{{Name}}`.
- Các file được nhúng vào assembly nhờ `EmbeddedResource Include="EmailTemplates\*.html"` trong `.csproj`.
- Không dùng JavaScript; CSS inline để tương thích Gmail.

Việc có file HTML chưa có nghĩa tất cả mẫu đã được gửi. Xem mục **Bàn giao với main** ở cuối tài liệu.

---

## 8. B-07 – Enum lookup cho dropdown

### File chính

- `API/Controllers/LookupsController.cs`.
- `API/ResponseModels/EnumLookupsResponse.cs`.

Endpoint `GET /api/lookups/enums` yêu cầu đăng nhập, không gọi service và không truy cập DB. Controller trả bản sao (`ToArray`) của các mảng `All` trong `DomainConstants.cs`, gồm role, giới tính ngựa, trạng thái booking/hồ sơ/chuyến, mode vận chuyển, hạng chuồng, loại xe, checkpoint, welfare, incident, handover và bảng giá.

`OnTimeStatuses` chưa có mảng `All`, nên controller tạo mảng từ ba constant `OnTime`, `DelayedForceMajeure`, `DelayedOperationalFault`.

Kiểm tra:

- Có token → 200 và các key JSON ở camelCase, ví dụ `bookingStatuses`, `transportModes`.
- Không token → 401.
- Khi thêm enum mới vào domain, nhớ cập nhật `All` và response lookup tương ứng.

---

## 9. B-08 – Bộ request thử nghiệm

Toàn bộ request nằm trong `RacehorseTransport.API/RacehorseTransport.API.http`.

Phần đầu file có các named request đăng nhập theo role:

```text
loginAdmin       → adminToken
loginManager     → managerToken
loginCoordinator → coordinatorToken
loginCustomer    → customerToken
login            → accessToken (TransportSpecialist)
```

### Cách chạy

1. Cài extension **REST Client** trong VS Code hoặc dùng HTTP Client của IDE tương thích.
2. Đổi `@baseUrl` nếu API không chạy ở `http://localhost:5080`.
3. Điền `@password` bằng mật khẩu seed trên máy cá nhân; không commit mật khẩu vào file.
4. Chạy các request login trước để sinh token. REST Client lấy dữ liệu bằng JSONPath, ví dụ `{{loginAdmin.response.body.$.data.accessToken}}`; ký hiệu `$` là bắt buộc.
5. Trong từng khối B-01/B-02/B-03, chạy request `# @name create...` trước. ID trả về được lưu thành `createdAssetId`, `createdRequirementId`, `createdPriceItemId` cho PUT/PATCH/DELETE.
6. Mỗi endpoint có request đúng và request sai (400/401/403/404 tùy trường hợp).

Lưu ý dữ liệu có trạng thái:

- B-01/B-02 tạo rồi xóa record demo; nếu dừng giữa chừng, lần tạo tiếp theo có thể báo trùng. Hãy xóa record cũ hoặc reset DB.
- B-03 cố ý không có DELETE. Sau lần chạy đầu, `DEMO_HANDLING_TU` còn lại ở trạng thái inactive; lần chạy tiếp theo cần đổi `itemCode` hoặc reset DB.
- B-05 thay đổi `is_read` của notification seed. Muốn thấy lại unread count ban đầu phải chạy lại seed.
- Không bấm chạy các request theo thứ tự ngẫu nhiên khi chúng dùng `{{created...Id}}`.

Khi phát hiện lỗi, issue cần ghi endpoint, role, request body, expected, actual và các bước tái hiện.

---

## 10. B-09 – Unit test báo giá và KPI

### File chính

- Project `RacehorseTransport.Tests`, đã được thêm vào `RacehorseTransport.sln`.
- `PricingServiceTests.cs`.
- `TripKpiCalculatorTests.cs`.
- Hàm thuần được test: `Services/Implementations/TripKpiCalculator.cs`.

### Pricing tests

`PricingServiceTests` cấp sẵn bảng giá qua `SeededPriceCache`; repository giả sẽ ném lỗi nếu bị gọi. Vì vậy đây là unit test thực sự, không cần PostgreSQL/Redis.

Các nhóm đã bao phủ:

- Shared/Comfort/Private với hệ số 1.0/1.4/2.2.
- 1, 2, 4, 7 ngựa với đúng bậc chiết khấu cao nhất.
- Bốn tổ hợp có/không Express và Climate Control; Fuel luôn có.
- Có/không `DeclaredValue`; bảo hiểm chỉ xuất hiện khi có giá trị khai báo.

### KPI tests

- Không bất khả kháng: trễ 0, 30, 31, 120, 600 phút.
- Có bất khả kháng: các mốc tương tự; trễ trên 30 phút vẫn KPI 100 và trạng thái acceptable force majeure.
- Kết thúc sớm không tạo delay âm.
- Công thức dùng bậc 30 phút và KPI thấp nhất 50; 31 phút đã tính một bậc phạt 5 điểm.

### Lệnh chạy

```powershell
dotnet restore RacehorseTransport.sln
dotnet build RacehorseTransport.sln --no-restore
dotnet test RacehorseTransport.Tests/RacehorseTransport.Tests.csproj --no-build
```

Hoặc chạy test kèm tên chi tiết:

```powershell
dotnet test RacehorseTransport.Tests/RacehorseTransport.Tests.csproj --logger "console;verbosity=detailed"
```

Máy cần .NET SDK tương thích `global.json` (hiện yêu cầu 8.0.420; `latestFeature` cho phép chọn feature band tương thích mới nhất trong cùng major/minor). Ngày 12/10/2026, solution đã được restore và build bằng SDK 8.0.420 với kết quả **0 lỗi, 0 cảnh báo**; bộ test B-09 chạy thành công **24/24 test**. Trước khi merge vẫn nên chạy lại ba lệnh trên ở máy dev/CI sau khi đồng bộ `main`.

---

## 11. Chạy database và API từ đầu

### Cách 1: PostgreSQL cài trên máy

Các lệnh sau xóa và dựng lại toàn bộ database dev; không chạy trên database có dữ liệu cần giữ:

```powershell
psql -h localhost -p 5432 -U postgres -d racehorse_transport -f database/00_reset.sql
psql -h localhost -p 5432 -U postgres -d racehorse_transport -f database/01_schema.sql
psql -h localhost -p 5432 -U postgres -d racehorse_transport -f database/02_seed.sql
```

### Cách 2: Docker

```powershell
docker compose up -d postgres
docker compose up -d redis
```

PostgreSQL Docker dùng port máy `5433`; Redis dùng `6379`. Script schema/seed trong `docker-compose.yml` chỉ tự chạy khi volume PostgreSQL được khởi tạo lần đầu. Nếu volume đã tồn tại, hãy chạy ba file SQL thủ công theo đúng thứ tự.

### Cấu hình và chạy API

1. Copy `RacehorseTransport.API/appsettings.Local.example.json` thành `appsettings.Local.json`.
2. Chọn `Database.Use` và điền connection string. Với Docker:

```text
Host=localhost;Port=5433;Database=racehorse_transport;Username=postgres;Password=postgres
```

3. Chạy:

```powershell
dotnet run --project RacehorseTransport.API
```

4. Mở `http://localhost:5080/swagger` hoặc gọi `GET http://localhost:5080/health`.
5. Login, copy access token, bấm **Authorize** trên Swagger, sau đó thử cả request đúng và sai.

---

## 12. Checklist trước khi tạo Pull Request

- [ ] Đã đồng bộ `main` theo hướng dẫn bên dưới và xử lý conflict có chủ đích.
- [ ] `dotnet build RacehorseTransport.sln` không lỗi và không có warning mới.
- [ ] `dotnet test` pass.
- [ ] Chạy `00_reset` → `01_schema` → `02_seed` không lỗi trên PostgreSQL sạch.
- [ ] Đăng nhập được tài khoản seed mới.
- [ ] Swagger đã thử 200/201, 400, 401/403 và 404.
- [ ] B-03 sửa/bật/tắt giá làm quote-preview nhận giá mới.
- [ ] B-05 không thể đọc hoặc sửa notification của người khác.
- [ ] `.http` chạy đúng thứ tự, không còn record demo làm request sau bị trùng.
- [ ] Không commit `appsettings.Local.json`, mật khẩu, service key hoặc token.

---

## 13. Bàn giao với `main`, T-29 và T-31

Đây là phần bắt buộc trước khi coi B-06/B-09 đã tích hợp hoàn chỉnh.

### Vì sao phải đồng bộ

Nhánh hiện tại là tổ tiên của `main` và chậm 8 commit. `main` đã có:

- T-29: `IEmailService`, hàng đợi nền, `EmailTemplateRenderer`, SMTP/Gmail API và DI.
- T-30: xác thực email, quên/đặt lại mật khẩu, token DB.
- T-31: `NotificationService` có thể gửi email cho Customer.
- Flow 5/6 và bản chính của `CompletionModels.cs`, `TripKpiCalculator.cs`.

`TASKS.md` quy định mỗi task một nhánh và một Pull Request. Lần triển khai này đang tập hợp B-01 → B-09 trong cùng worktree để kiểm tra tích hợp, vì vậy trước khi mở PR cần tách commit theo từng task rồi cherry-pick sang các nhánh `B-01`, `B-02`, ... hoặc xin nhóm chấp thuận một PR gộp. Chỉ đánh dấu `[x]` sau khi PR tương ứng đã merge vào `main`.

Hãy commit phần của từng nhánh trước rồi mới đồng bộ; không rebase khi worktree còn thay đổi chưa commit:

```powershell
git fetch origin
git rebase origin/main
```

Nhóm có thể dùng merge thay cho rebase nếu quy ước Git của nhóm yêu cầu như vậy.

### Cách xử lý conflict

1. Giữ hạ tầng email/Gmail API, queue, options và DI từ `main`.
2. Giữ đủ 8 file HTML của B-06. `VerifyEmail` và `ResetPassword` hiện tương thích placeholder mà `main` truyền vào.
3. Giữ cấu hình `EmbeddedResource` đúng một lần trong `RacehorseTransport.Services.csproj`.
4. `CompletionModels.cs` và `TripKpiCalculator.cs` đã có trên `main`: không tạo hai bản; dùng bản của `main` và giữ project/test B-09 trỏ tới nó.
5. Gộp bốn dòng DI B-01/B-02/B-03/B-05 vào `ServiceCollectionExtensions` mới của `main`, không làm mất các đăng ký email/incident/completion.
6. Gộp seed với schema mới của `main`; đặt `UPDATE identity.users SET email_verified_at = created_at` sau khi đã chèn đủ 13 user để các tài khoản demo mới cũng được xác thực.
7. Chạy lại toàn bộ SQL trên database sạch.

### Handoff template giữa Tú và Thiên

T-31 trên `main` hiện gửi mọi sự kiện khách hàng qua mẫu chung `Notification.html` với `FullName`, `Title`, `Message`, `Link`. Sáu mẫu chuyên biệt của B-06 (`BookingApproved`, `BookingRejected`, `DocumentsRequested`, `DossierCleared`, `TripIncident`, `HorseArrived`) chưa được gọi tự động.

Để hoàn tất wiring, Thiên cần:

1. Thêm tên sáu mẫu vào `EmailTemplateNames`.
2. Tại đúng sự kiện nghiệp vụ, gọi `IEmailService.Queue` với đúng template và cung cấp đủ placeholder ở bảng B-06.
3. Chỉ gửi email chuyên biệt cho Customer theo quy tắc T-31; nhân sự vẫn chỉ nhận chuông.
4. Không chỉ thay `Notification` bằng một tên template trong `NotificationService`, vì service chung hiện không có đủ dữ liệu như `HorseName`, `IncidentCode`, `ClearanceNumber` hay `RejectionReason`.
5. Render thử cả tám mẫu sau build để phát hiện placeholder bị thiếu, rồi gửi thử qua Gmail API ở môi trường dev.

Nếu nhóm quyết định tiếp tục dùng `Notification.html` chung, sáu file sự kiện chỉ là nội dung dự phòng và B-06 chưa được sử dụng ở runtime; điều này phải được ghi rõ trong PR thay vì đánh dấu là đã wiring.
