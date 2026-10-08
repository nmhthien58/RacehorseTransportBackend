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
| `Google__ClientId` | Client ID của OAuth client (mục Google bên dưới). Dùng cho đăng nhập bằng Google và gửi mail |
| `Google__ClientSecret` | Client secret của OAuth client đó |
| `Google__GmailRefreshToken` | Refresh token của tài khoản Gmail đồ án (mục Google, bước 5) |
| `Email__Provider` | `GmailApi`. Bỏ trống thì hệ thống không gửi mail |
| `Email__FromAddress` | Địa chỉ Gmail của đồ án (chính tài khoản đã cấp refresh token) |
| `Email__FrontendBaseUrl` | Địa chỉ frontend, dùng để tạo link trong email, ví dụ `https://ten-app.vercel.app` |

Railway tự cấp biến `PORT`, không cần thêm. Tạo `Jwt__Key` bằng PowerShell:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

### Email

Hệ thống gửi email xác thực tài khoản, đặt lại mật khẩu, và thông báo cho khách ở các sự kiện chính. Cách gửi chọn bằng `Email__Provider`:

| `Email__Provider` | Cách gửi | Cần gì |
|---|---|---|
| `GmailApi` | Gọi Gmail API qua HTTPS. Dùng được ở máy và trên Railway | Ba giá trị `Google__...` ở mục Google bên dưới |
| `Smtp` | SMTP của Gmail (cổng 587). Chỉ dùng được ở máy | App password của Gmail, điền `Email:Smtp:Username`, `Email:Smtp:Password` |
| (bỏ trống) | Không gửi | Link xác thực / đặt lại mật khẩu được ghi ra log của server |

**Vì sao không dùng SMTP trên Railway:** Railway chặn các cổng SMTP (25, 465, 587) ở gói Free, Trial và Hobby, nên kết nối tới `smtp.gmail.com` sẽ hết thời gian chờ. Gmail API đi qua HTTPS (cổng 443) nên không bị chặn, và miễn phí.

Gửi mail lỗi không làm hỏng request: lỗi chỉ được ghi vào log (`Email '...' to ... could not be sent`).

### Google: đăng nhập bằng Google và gửi mail qua Gmail API

Làm một lần, khoảng 20 phút, bằng **tài khoản Gmail của đồ án**. Kết quả là ba giá trị: Client ID, Client secret, Refresh token. Không tốn tiền và không cần nhập thẻ.

**Bước 1. Tạo project**

1. Mở [console.cloud.google.com](https://console.cloud.google.com) và đăng nhập bằng Gmail của đồ án.
2. Bấm ô chọn project ở đầu trang → **New Project** → đặt tên (ví dụ `Racehorse Transport`) → **Create**. Chờ tạo xong rồi chọn project đó.

**Bước 2. Bật Gmail API**

1. Menu ☰ → **APIs & Services** → **Library**.
2. Tìm `Gmail API` → bấm vào → **Enable**.

**Bước 3. Màn hình xin quyền (OAuth consent screen)**

1. Menu ☰ → **APIs & Services** → **OAuth consent screen** (giao diện mới ghi là **Google Auth Platform**) → **Get started**.
2. App name: `Racehorse Transport`. User support email: Gmail của đồ án. Audience: **External**. Contact email: Gmail của đồ án. Đồng ý điều khoản → **Create**.
3. Vào mục **Audience** → **Publish app** → **Confirm**, để trạng thái là **In production**.

Bước 3.3 rất quan trọng: nếu để ở trạng thái **Testing**, refresh token hết hạn sau 7 ngày và hệ thống ngừng gửi mail.

**Bước 4. Tạo OAuth client**

1. Mục **Clients** (hoặc **Credentials** → **Create credentials** → **OAuth client ID**) → **Create client**.
2. Application type: **Web application**. Name: `Racehorse Web`.
3. **Authorized JavaScript origins** → thêm `http://localhost:5173`. Khi frontend có địa chỉ deploy thì quay lại thêm địa chỉ đó (ví dụ `https://ten-app.vercel.app`).
4. **Authorized redirect URIs** → thêm `https://developers.google.com/oauthplayground` (chỉ để lấy refresh token ở bước 5).
5. **Create**. Chép lại **Client ID** và **Client secret** (secret chỉ hiện đầy đủ lúc này; có thể tải file JSON về giữ).

**Bước 5. Lấy refresh token cho việc gửi mail**

1. Mở [developers.google.com/oauthplayground](https://developers.google.com/oauthplayground).
2. Bấm biểu tượng bánh răng ở góc phải → tick **Use your own OAuth credentials** → dán Client ID và Client secret → đóng bảng.
3. Ở **Step 1**, ô "Input your own scopes" nhập đúng dòng sau rồi bấm **Authorize APIs**:

```
https://www.googleapis.com/auth/gmail.send
```

4. Chọn tài khoản Gmail của đồ án. Google báo "Google hasn't verified this app": bấm **Advanced** → **Go to Racehorse Transport (unsafe)** → **Continue**. Cảnh báo này xuất hiện vì ứng dụng do chính bạn tạo và chưa gửi Google duyệt; quyền được cấp chỉ là "gửi email thay bạn".
5. Ở **Step 2**, bấm **Exchange authorization code for tokens**. Chép giá trị **Refresh token** (bắt đầu bằng `1//`).

**Bước 6. Điền cấu hình**

Chạy ở máy, trong `RacehorseTransport.API/appsettings.Local.json`:

```json
"Email": {
  "Provider": "GmailApi",
  "FromAddress": "<gmail của đồ án>",
  "FrontendBaseUrl": "http://localhost:5173"
},
"Google": {
  "ClientId": "<Client ID>",
  "ClientSecret": "<Client secret>",
  "GmailRefreshToken": "<Refresh token>"
}
```

Trên Railway: thêm các biến `Google__ClientId`, `Google__ClientSecret`, `Google__GmailRefreshToken`, `Email__Provider=GmailApi`, `Email__FromAddress`, `Email__FrontendBaseUrl` (bảng ở trên).

Frontend: đặt `VITE_GOOGLE_CLIENT_ID=<Client ID>` trong file `.env` của frontend. Client ID không phải bí mật; Client secret và Refresh token thì **không được** đưa cho frontend hay commit lên git.

**Khi nào phải làm lại bước 5:** đổi mật khẩu tài khoản Gmail của đồ án, thu hồi quyền của ứng dụng trong Google Account, hoặc không dùng refresh token quá 6 tháng. Dấu hiệu: log có `Gmail API token failed with 400` kèm `invalid_grant`.

## 3. Kiểm tra sau khi deploy

1. `https://<domain>/health` trả `{"status":"Healthy","database":true,...}`.
2. `https://<domain>/swagger` mở được.
3. `POST /api/auth/login` bằng một tài khoản seed trả về `accessToken`.
4. `POST /api/files` với một file PDF nhỏ trả 201 (xác nhận khóa Supabase và bucket đúng).
5. `POST /api/auth/forgot-password` với email của một tài khoản có thật: thư đặt lại mật khẩu tới hộp thư trong vòng một phút (xác nhận cấu hình email đúng). Không thấy thư thì xem log.
6. Bấm nút "Đăng nhập bằng Google" trên frontend và đăng nhập được (xác nhận Client ID khớp ở cả hai phía).

Nếu deploy báo lỗi, mở tab **Deployments** → **View logs**:

| Dấu hiệu trong log | Nguyên nhân |
|---|---|
| `Jwt:Key is missing or shorter than 32 characters` | Chưa đặt `Jwt__Key` |
| `ConnectionStrings:DefaultConnection is empty` | Chưa đặt chuỗi kết nối |
| Health check thất bại, log có `Failed to connect` / `No such host` | Sai host (đang dùng Direct connection) hoặc sai mật khẩu |
| `28P01: password authentication failed` | Sai mật khẩu, hoặc `Username` thiếu phần `.<mã project>` |
| `42703: column u.email_verified_at does not exist` hoặc `42P01: relation "identity.user_tokens" does not exist` | Chưa chạy `database/03_update_email_verification.sql` |
| `Email '...' could not be sent` kèm `timed out` | Đang dùng `Smtp` trên Railway (cổng bị chặn): đổi sang `GmailApi` |
| `Gmail API token failed with 400` kèm `invalid_grant` | Refresh token hết hạn hoặc bị thu hồi: làm lại bước 5 của mục Google. Nếu lặp lại sau 7 ngày: ứng dụng còn ở trạng thái Testing (bước 3.3) |
| `Gmail API token failed with 401` kèm `invalid_client` | Sai `Google__ClientId` hoặc `Google__ClientSecret` |
| `Gmail API send failed with 403` | Chưa bật Gmail API (bước 2), hoặc refresh token được cấp thiếu quyền `gmail.send` |
| `Gmail API is not configured` | Thiếu một trong ba biến `Google__...` |
| Đăng nhập Google luôn trả 401 | `Google__ClientId` ở backend khác với Client ID frontend đang dùng |
| Nút Google ở frontend báo `origin_mismatch` / không hiện | Địa chỉ frontend chưa có trong **Authorized JavaScript origins** (bước 4.3) |

## 4. Việc phải làm trước khi chia sẻ link

- **Đổi mật khẩu các tài khoản seed.** Mật khẩu chung nằm công khai trong `02_seed.sql`, ai đọc repo cũng đăng nhập được tài khoản Admin của bản deploy. Đăng nhập bằng `admin@logistics.com`, gọi `PUT /api/auth/change-password` cho chính nó, rồi `PUT /api/users/{id}/password` cho 5 tài khoản còn lại (hoặc `PATCH /api/users/{id}/active` để khóa tài khoản không dùng).
- **Đặt `Email__FrontendBaseUrl` đúng địa chỉ frontend.** Link trong email xác thực và đặt lại mật khẩu trỏ về địa chỉ này; để mặc định thì link trỏ về `http://localhost:5173`.
- **Project Supabase miễn phí tự tạm dừng** sau khoảng một tuần không có truy cập. Trước buổi demo, mở dashboard và bấm **Restore** nếu project đang ngủ.
