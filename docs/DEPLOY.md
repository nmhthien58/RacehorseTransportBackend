# Deploy: database lên Supabase, API lên Railway

Thứ tự: dựng database trước, rồi mới deploy API (API kiểm tra kết nối database ngay khi khởi động).

## 1. Database trên Supabase

1. Mở project trên [supabase.com/dashboard](https://supabase.com/dashboard) → **SQL Editor** → **New query**.
2. Dán toàn bộ nội dung `database/01_schema.sql`, bấm **Run**. Kết quả mong đợi: `Success. No rows returned`.
3. Mở query mới, dán toàn bộ `database/02_seed.sql`, bấm **Run**.
4. Kiểm tra: **Table Editor** → đổi schema từ `public` sang `identity` → bảng `users` có 6 dòng.

Lưu ý:

- Các bảng nằm trong 10 schema riêng (`identity`, `booking`, `fleet`...), không nằm trong `public`. Supabase không mở các schema này ra Data API, đó là điều mong muốn: chỉ backend được đọc ghi.
- Muốn dựng lại từ đầu: chạy `database/00_reset.sql` rồi lặp lại bước 2 và 3. Lệnh này xóa toàn bộ dữ liệu của hệ thống.
- Database đã dựng từ trước và muốn giữ dữ liệu: chạy các file `database/03_update_*.sql` **trước khi** deploy bản code mới. Các file này chỉ thêm cột, chạy lại nhiều lần không sao.
- `02_seed.sql` tạo 6 tài khoản dùng chung một mật khẩu đã ghi công khai trong repo. Xem mục 4 trước khi đưa link cho người khác.

### Lấy chuỗi kết nối

Bấm **Connect** ở đầu trang project → mục **Session pooler** → lấy `host`, `port`, `user`. Ghép thành:

```
Host=<host của Session pooler>;Port=5432;Database=postgres;Username=postgres.<mã project>;Password=<mật khẩu database>;SSL Mode=Require;Trust Server Certificate=true
```

Không dùng mục *Direct connection*: gói miễn phí chỉ cấp IPv6 cho mục đó, Railway và phần lớn mạng ở nhà không nối được.

## 2. API trên Railway

Repo đã có sẵn `Dockerfile` và `railway.json`, Railway tự nhận.

1. Vào [railway.com](https://railway.com) → **New Project** → **Deploy from GitHub repo** → chọn `RacehorseTransportBackend`, nhánh `main`.
2. Mở service vừa tạo → tab **Variables** → thêm các biến ở bảng dưới.
3. Tab **Settings** → **Networking** → **Generate Domain** để có địa chỉ công khai.
4. Railway build và chạy. Lần deploy được coi là thành công khi `GET /health` trả 200.

### Biến môi trường

Tên biến dùng hai dấu gạch dưới `__` thay cho dấu `:` trong appsettings.

| Biến | Giá trị |
|---|---|
| `ConnectionStrings__DefaultConnection` | Chuỗi kết nối Session pooler ở mục 1 |
| `Jwt__Key` | Chuỗi ngẫu nhiên từ 32 ký tự trở lên. **Không** dùng lại khóa trong `appsettings.Development.json` |
| `Supabase__Url` | `https://<mã project>.supabase.co` |
| `Supabase__ServiceKey` | Khóa `service_role` hoặc secret key của Supabase |
| `Supabase__Bucket` | Tên bucket (để chế độ private) |
| `Cors__AllowedOrigins__0` | Địa chỉ frontend, ví dụ `https://ten-app.vercel.app` (thêm `__1`, `__2` nếu có nhiều) |
| `ConnectionStrings__Redis` | Bỏ trống, trừ khi có Redis |

Railway tự cấp biến `PORT`, không cần thêm. Tạo `Jwt__Key` bằng PowerShell:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

## 3. Kiểm tra sau khi deploy

1. `https://<domain>/health` trả `{"status":"Healthy","database":true,...}`.
2. `https://<domain>/swagger` mở được.
3. `POST /api/auth/login` bằng một tài khoản seed trả về `accessToken`.
4. `POST /api/files` với một file PDF nhỏ trả 201 (xác nhận khóa Supabase và bucket đúng).

Nếu deploy báo lỗi, mở tab **Deployments** → **View logs**:

| Dấu hiệu trong log | Nguyên nhân |
|---|---|
| `Jwt:Key is missing or shorter than 32 characters` | Chưa đặt `Jwt__Key` |
| `ConnectionStrings:DefaultConnection is empty` | Chưa đặt chuỗi kết nối |
| Health check thất bại, log có `Failed to connect` / `No such host` | Sai host (đang dùng Direct connection) hoặc sai mật khẩu |
| `28P01: password authentication failed` | Sai mật khẩu, hoặc `Username` thiếu phần `.<mã project>` |

## 4. Việc phải làm trước khi chia sẻ link

- **Đổi mật khẩu các tài khoản seed.** Mật khẩu chung nằm công khai trong `02_seed.sql`, ai đọc repo cũng đăng nhập được tài khoản Admin của bản deploy. Đăng nhập bằng `admin@logistics.com`, gọi `PUT /api/auth/change-password` cho chính nó, rồi `PUT /api/users/{id}/password` cho 5 tài khoản còn lại (hoặc `PATCH /api/users/{id}/active` để khóa tài khoản không dùng).
- **Refresh token mất mỗi lần Railway khởi động lại** (đang lưu trong bộ nhớ): người dùng phải đăng nhập lại sau mỗi lần deploy. Hết khi task T-30 chuyển token về database, hoặc khi có Redis.
- **Project Supabase miễn phí tự tạm dừng** sau khoảng một tuần không có truy cập. Trước buổi demo, mở dashboard và bấm **Restore** nếu project đang ngủ.
