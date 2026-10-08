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
- Database đã dựng từ trước và muốn giữ dữ liệu: chạy các file `database/03_update_*.sql` **trước khi** deploy bản code mới. Các file này chỉ thêm cột và bảng, chạy lại nhiều lần không sao. Hiện có: `03_update_incident_rejection_reason.sql`, `03_update_email_verification.sql`.
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
| `Email__Provider` | `Brevo` (xem mục Email bên dưới). Bỏ trống thì hệ thống không gửi mail |
| `Email__FromAddress` | Địa chỉ người gửi, ví dụ Gmail của đồ án (phải là địa chỉ đã xác nhận trong Brevo) |
| `Email__Brevo__ApiKey` | Khóa API của Brevo |
| `Email__FrontendBaseUrl` | Địa chỉ frontend, dùng để tạo link trong email, ví dụ `https://ten-app.vercel.app` |

Railway tự cấp biến `PORT`, không cần thêm. Tạo `Jwt__Key` bằng PowerShell:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

### Email

Hệ thống gửi email xác thực tài khoản, đặt lại mật khẩu, và thông báo cho khách ở các sự kiện chính. Có hai cách gửi, chọn bằng `Email__Provider`:

| `Email__Provider` | Dùng ở đâu | Cần gì |
|---|---|---|
| `Smtp` | Chạy ở máy | Tài khoản Gmail + **app password** (Google Account → Security → 2-Step Verification → App passwords). Điền `Email:Smtp:Username`, `Email:Smtp:Password` trong `appsettings.Local.json` |
| `Brevo` | Bản deploy trên Railway | Tài khoản [Brevo](https://www.brevo.com) miễn phí: xác nhận địa chỉ người gửi (Senders), tạo khóa ở **SMTP & API → API Keys** |
| (bỏ trống) | Khi chưa cần mail | Không gửi gì; link xác thực / đặt lại mật khẩu được ghi ra log của server |

**Vì sao bản deploy không dùng Gmail SMTP:** Railway chặn các cổng SMTP (25, 465, 587) ở gói Free, Trial và Hobby, nên kết nối tới `smtp.gmail.com` sẽ hết thời gian chờ. Brevo gửi qua HTTPS (cổng 443) nên không bị chặn. Nếu nhóm dùng gói Pro của Railway thì đặt `Email__Provider=Smtp` cùng `Email__Smtp__Username`, `Email__Smtp__Password` là được.

Gửi mail lỗi không làm hỏng request: lỗi chỉ được ghi vào log (`Email '...' to ... could not be sent`).

## 3. Kiểm tra sau khi deploy

1. `https://<domain>/health` trả `{"status":"Healthy","database":true,...}`.
2. `https://<domain>/swagger` mở được.
3. `POST /api/auth/login` bằng một tài khoản seed trả về `accessToken`.
4. `POST /api/files` với một file PDF nhỏ trả 201 (xác nhận khóa Supabase và bucket đúng).
5. `POST /api/auth/forgot-password` với email của một tài khoản có thật: thư đặt lại mật khẩu tới hộp thư trong vòng một phút (xác nhận cấu hình email đúng). Không thấy thư thì xem log.

Nếu deploy báo lỗi, mở tab **Deployments** → **View logs**:

| Dấu hiệu trong log | Nguyên nhân |
|---|---|
| `Jwt:Key is missing or shorter than 32 characters` | Chưa đặt `Jwt__Key` |
| `ConnectionStrings:DefaultConnection is empty` | Chưa đặt chuỗi kết nối |
| Health check thất bại, log có `Failed to connect` / `No such host` | Sai host (đang dùng Direct connection) hoặc sai mật khẩu |
| `28P01: password authentication failed` | Sai mật khẩu, hoặc `Username` thiếu phần `.<mã project>` |
| `42703: column u.email_verified_at does not exist` hoặc `42P01: relation "identity.user_tokens" does not exist` | Chưa chạy `database/03_update_email_verification.sql` |
| `Email '...' could not be sent` kèm `timed out` | Đang dùng `Smtp` trên Railway (cổng bị chặn): đổi sang `Brevo` |
| `Email API returned 401` | Sai `Email__Brevo__ApiKey` |

## 4. Việc phải làm trước khi chia sẻ link

- **Đổi mật khẩu các tài khoản seed.** Mật khẩu chung nằm công khai trong `02_seed.sql`, ai đọc repo cũng đăng nhập được tài khoản Admin của bản deploy. Đăng nhập bằng `admin@logistics.com`, gọi `PUT /api/auth/change-password` cho chính nó, rồi `PUT /api/users/{id}/password` cho 5 tài khoản còn lại (hoặc `PATCH /api/users/{id}/active` để khóa tài khoản không dùng).
- **Đặt `Email__FrontendBaseUrl` đúng địa chỉ frontend.** Link trong email xác thực và đặt lại mật khẩu trỏ về địa chỉ này; để mặc định thì link trỏ về `http://localhost:5173`.
- **Project Supabase miễn phí tự tạm dừng** sau khoảng một tuần không có truy cập. Trước buổi demo, mở dashboard và bấm **Restore** nếu project đang ngủ.
