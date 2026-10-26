# AGENTS.md — Hướng dẫn làm việc với repo Backend

File này dành cho **mọi người trong nhóm và mọi AI agent** (Claude Code, Codex, Cursor…) làm việc trên repo `RacehorseTransportBackend`. Đọc hết trước khi sửa code. Mâu thuẫn giữa file này và yêu cầu của người giao việc → **hỏi lại**, không tự đoán.

---

## 1. Dự án trong 30 giây

- **Hệ thống:** quản lý vận chuyển ngựa đua quốc tế (SWP391, Group 5). Sáu luồng: đặt đơn & báo giá → hồ sơ kiểm dịch → lập chuyến → chạy chuyến → sự cố → bàn giao (e-POD). Thanh toán qua VNPay.
- **Công nghệ:** .NET 8, ASP.NET Core Web API, EF Core + PostgreSQL (Supabase), SignalR, 3 lớp: `RacehorseTransport.API` (Controller, Request/Response) → `RacehorseTransport.Services` (nghiệp vụ) → `RacehorseTransport.Repositories` (Entity, DbContext). Test: `RacehorseTransport.Tests` (xUnit).
- **Deploy:** Railway **tự build và deploy mỗi khi `main` có commit mới**: https://racehorsetransportbackend-production.up.railway.app/swagger. Vì vậy `main` lúc nào cũng phải build được và chạy được.
- **Database thật:** Supabase (dùng chung cho bản deploy và cho máy dev). Mọi người đang dùng chung một DB → cẩn thận với dữ liệu.

## 2. Tài liệu phải đọc trước khi nhận task

| File | Để làm gì |
|---|---|
| `docs/TASKS_R09_R16.md` | **Danh sách task đợt hiện tại**, ai làm gì, thứ tự, mô tả chi tiết, tiêu chí xong |
| `docs/PLAN_NGHIEP_VU_R09.md` | Lý do và quyết định nghiệp vụ (đọc mục "Quyết định đã chốt") |
| `docs/CHINH_SACH_DICH_VU.md` | Chính sách gửi khách. **Code phải khớp mọi con số và quy tắc trong file này** |
| `docs/API.md` | Tài liệu API cho frontend (phải cập nhật khi đổi / thêm endpoint) |
| `README.md` | Kiến trúc, cách chạy, quy ước code chi tiết |

Bản gốc của `PLAN_NGHIEP_VU_R09.md` và `CHINH_SACH_DICH_VU.md` nằm ở repo này. Sửa ở đây rồi chép sang `doc/` của repo frontend.

## 3. Chuẩn bị máy (làm một lần)

```bash
git clone https://github.com/nmhthien58/RacehorseTransportBackend.git
cd RacehorseTransportBackend
dotnet --version        # cần SDK 8.x (xem global.json)
```

1. Copy `RacehorseTransport.API/appsettings.Local.example.json` thành `RacehorseTransport.API/appsettings.Local.json` và điền chuỗi kết nối (xin Thiên). File này **đã nằm trong `.gitignore`, tuyệt đối không commit**.
2. Chạy API:
   ```bash
   dotnet run --project RacehorseTransport.API
   ```
   Mở http://localhost:5080/swagger. Đăng nhập `POST /api/auth/login` bằng tài khoản seed (mật khẩu `Racehorse@123`, ví dụ `admin@racehorsetransport.com`), bấm **Authorize**, dán `accessToken`.
3. Muốn thử thay đổi schema mà không đụng Supabase: dựng PostgreSQL ở máy, chạy `database/01_schema.sql` rồi `database/02_seed.sql`, đặt `"Database": { "Use": "Local" }` (xem README mục 4).

## 4. Quy trình làm MỘT task (bắt buộc làm đúng thứ tự)

### Bước 1 — Nhận task
- Mở `docs/TASKS_R09_R16.md`, tìm task mang mã của mình (ví dụ `BE-07`). Kiểm tra cột **Phụ thuộc**: task phụ thuộc chưa merge thì làm task khác hoặc báo người làm task đó.
- Đổi cột **Trạng thái** trong file Excel theo dõi thành `Đang làm` (file Excel không nằm trong git, xin Thiên).

### Bước 2 — Tạo nhánh từ `main` mới nhất
```bash
git checkout main
git pull origin main
git checkout -b feature/be-07-trip-from-route-plan
```
Tên nhánh: `feature/<mã-task-viết-thường>-<mô-tả-ngắn-tiếng-anh>`. Sửa lỗi: `fix/<mô-tả>`.

### Bước 3 — Code
- Làm đúng phạm vi task. **Không sửa / "dọn dẹp" file ngoài phạm vi.** Thấy lỗi khác thì ghi lại, báo người giao việc.
- Theo quy ước ở mục 5.

### Bước 4 — Tự kiểm tra
```bash
dotnet build
dotnet test
```
- Build không lỗi, **không thêm cảnh báo mới**; test pass hết.
- Task có công thức (tiền, ngày, giờ, phần trăm) **phải có unit test mới**.
- Chạy API, thử endpoint trên Swagger cả trường hợp đúng lẫn sai (400, 403, 404). Thêm request mẫu vào `RacehorseTransport.API/RacehorseTransport.API.http`.

### Bước 5 — Database (nếu task đổi schema)
- Sửa `database/01_schema.sql` **và** tạo `database/03_update_<tên>.sql`. Script chạy hai lần liên tiếp không lỗi (`ADD COLUMN IF NOT EXISTS`, `DROP CONSTRAINT IF EXISTS` rồi `ADD CONSTRAINT`…). Mẫu: `database/03_update_incident_route_plan.sql`.
- **Không tự chạy script lên Supabase.** Gửi script cho Thiên trong mô tả PR. Thiên chạy script **trước khi merge**, vì Railway deploy ngay khi merge: code mới chạy trên DB thiếu cột sẽ làm hỏng bản đang chạy.

### Bước 6 — Tài liệu
- Đổi / thêm endpoint → cập nhật `docs/API.md` đúng kiểu hiện có: ai được gọi, tham số, bảng field của body (kiểu, bắt buộc, ý nghĩa), ví dụ request, ví dụ response, lỗi thường gặp. Thêm dòng vào bảng tổng hợp cuối file.
- Xuất lại bản Word cho frontend (`docs/API_Huong_dan_Frontend.docx`, file này không commit, gửi qua nhóm chat). Không có công cụ xuất thì báo Thiên.
- Đánh dấu `[x]` cho task trong `docs/TASKS_R09_R16.md`.

### Bước 7 — Commit
- Commit nhỏ, mỗi commit một ý. Message theo Conventional Commits, tiếng Anh:
  ```
  feat(trips): copy approved route plan into trip checkpoints (BE-07)
  fix(payments): build vnp_ReturnUrl from the API origin only
  docs(api): document route plan endpoints
  test(pricing): cover delay compensation cap
  ```
- **Không thêm dòng `Co-Authored-By`** hay chữ ký AI vào commit / PR.
- Kiểm tra trước khi commit: `git status` không có `appsettings.Local.json`, `.env`, file `bin/`, `obj/`, khóa bí mật.

### Bước 8 — Push và mở Pull Request
```bash
git push -u origin feature/be-07-trip-from-route-plan
gh pr create --base main --title "feat(trips): trip from approved route plan (BE-07)" --body-file pr.md
```
(Không có `gh` thì mở PR trên trang GitHub.)

Mô tả PR gồm:
1. Mã task + một đoạn ngắn đã làm gì.
2. Endpoint thêm / đổi.
3. **Script DB cần chạy** (tên file), hoặc ghi "Không đổi DB".
4. Cách kiểm tra (2–3 bước trên Swagger).
5. Kết quả `dotnet test` (dán dòng cuối).

### Bước 9 — Review và merge
- Thiên review. Sửa theo góp ý bằng commit mới trên cùng nhánh rồi `git push` (không `--force` khi PR đang được review).
- Nhánh bị cũ so với `main`:
  ```bash
  git fetch origin
  git merge origin/main      # giải quyết conflict, build + test lại, rồi push
  ```
- Merge kiểu **Squash and merge**. Script DB phải được chạy trên Supabase trước bước này. Sau khi merge, xem tab Deployments trên Railway đã `Success`, rồi đổi Trạng thái trong Excel thành `Xong`.

### Điều cấm
- **Không push thẳng lên `main`**, không `git push --force` lên `main`. (Chỉ Thiên được làm khi cần sửa gấp.)
- Không commit bí mật: chuỗi kết nối, khóa JWT, `HashSecret` VNPay, khóa Google, khóa Supabase.
- Không xóa / sửa dữ liệu thật trên Supabase bằng tay khi chưa hỏi Thiên.
- Không đổi phiên bản package, framework, cấu trúc solution nếu task không yêu cầu.

## 5. Quy ước code (tóm tắt, chi tiết ở README mục 5)

- **Ba lớp:** Controller chỉ nhận request, gọi Service, map response. Mọi quy tắc nghiệp vụ nằm trong Service. Repository dùng `IRepository<T>` + `IUnitOfWork`.
- **Lỗi:** `BusinessException` (400), `NotFoundException` (404), `ForbiddenException` (403). Thông báo lỗi bằng **tiếng Anh**, nói rõ người dùng phải làm gì.
- **Ngôn ngữ:** comment tiếng Việt, ngắn, giải thích "vì sao"; tên biến / hàm / class tiếng Anh; email gửi khách tiếng Việt (mẫu trong `Services/EmailTemplates/`).
- **Endpoint:** `[Authorize(Roles = ...)]` đúng role; `/// <summary>` cho Swagger; `[ProducesResponseType]`; trả `ApiResponse<T>`.
- **Request:** DataAnnotations (`[Required]`, `[MaxLength]`, `[Range]`) với thông báo lỗi rõ.
- **Cấu hình:** không viết cứng số %, ngày, giờ, tiền. Dùng `common.system_settings` (qua `ISystemSettingService`) hoặc `*Options` trong `appsettings.json`. Tên khóa lấy đúng như trong `docs/CHINH_SACH_DICH_VU.md`.
- **Hằng số:** trạng thái, loại mới thêm vào `Repositories/Constants/DomainConstants.cs` (kèm mảng `All`) và vào `CHECK` constraint trong SQL.
- **Ghi nhiều bảng:** bọc trong `_unitOfWork.ExecuteInTransactionAsync(...)`. Thông báo / email gửi trong transaction (hệ thống tự đẩy realtime sau khi commit).
- **JSON trong DB:** cột `jsonb`, cấu hình `HasColumnType("jsonb")`.
- **Thời gian:** lưu `DateTimeOffset` (UTC); so ngày theo múi giờ khách gửi lên như `PricingService` đang làm.
- **Thông báo cho khách:** sự kiện quan trọng có kèm email (`emailCustomers: true` hoặc `EmailCustomerAsync` với mẫu riêng).

## 6. Hướng dẫn riêng cho AI agent

1. **Đọc trước, sửa sau:** đọc mục task trong `docs/TASKS_R09_R16.md`, mục R-xx tương ứng trong `docs/PLAN_NGHIEP_VU_R09.md`, các file code được nhắc tên, và một đoạn code mẫu cùng loại trước khi viết.
2. **Bám đúng phạm vi task.** Không refactor "tiện thể", không đổi tên, không định dạng lại file không liên quan.
3. **Không chắc về nghiệp vụ → dừng và hỏi** người đang giao việc, trích đoạn chính sách liên quan. Không tự chế quy tắc.
4. Trước khi báo xong: chạy `dotnet build` và `dotnet test`, **dán kết quả thật** (dòng cuối) vào câu trả lời. Không tự khẳng định "đã chạy" khi chưa chạy.
5. Không chạy lệnh ghi vào Supabase, không đọc / in ra nội dung `appsettings.Local.json`, `.env`.
6. Không tự `git push` lên `main`, không tự merge PR. Được phép tạo nhánh, commit, push nhánh và mở PR nếu người giao việc yêu cầu.
7. Kết thúc bằng bảng: **File | Thay đổi | Lý do**, kèm danh sách script DB cần chạy và endpoint mới.

## 7. Liên hệ

- **Thiên:** trưởng nhóm backend, review PR, chạy script DB, quản lý Railway / Supabase / VNPay.
- **Tú:** backend.
- **Vũ, Thắng:** frontend (repo `RacehorseTransportFrontend`). Đổi API thì báo họ và gửi bản Word mới.
