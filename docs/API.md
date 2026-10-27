# Tài liệu API cho Frontend

Tài liệu này hướng dẫn gọi API của hệ thống vận chuyển ngựa đua. Mỗi API (endpoint) có một mục riêng, ghi rõ: dùng để làm gì, ai được gọi, gửi lên những gì, nhận về những gì, và các lỗi hay gặp. Các ví dụ JSON trong tài liệu được chép từ kết quả chạy thật.

| | |
|---|---|
| Địa chỉ server (bản deploy) | `https://racehorsetransportbackend-production.up.railway.app` |
| Swagger (trang thử API trên trình duyệt) | `https://racehorsetransportbackend-production.up.railway.app/swagger` |
| Server khi backend chạy ở máy | `http://localhost:5080` |

**Mục lục**

1. [Bắt đầu nhanh](#1-bắt-đầu-nhanh)
2. [Quy ước chung](#2-quy-ước-chung)
3. [Vai trò và các giá trị cố định](#3-vai-trò-và-các-giá-trị-cố-định)
4. [Đăng nhập và tài khoản của tôi](#4-đăng-nhập-và-tài-khoản-của-tôi)
5. [Quản lý người dùng](#5-quản-lý-người-dùng)
6. [Ngựa](#6-ngựa)
6b. [Bản đồ: tìm địa chỉ, chỉ đường](#6b-bản-đồ-tìm-địa-chỉ-chỉ-đường)
7. [Flow 1 – Đơn vận chuyển](#7-flow-1--đơn-vận-chuyển)
7b. [Tuyến dự kiến của đơn](#7b-tuyến-dự-kiến-của-đơn)
7c. [Hợp đồng vận chuyển](#7c-hợp-đồng-vận-chuyển)
8. [Flow 2 – Hồ sơ kiểm dịch](#8-flow-2--hồ-sơ-kiểm-dịch)
9. [Flow 3 – Lập kế hoạch chuyến](#9-flow-3--lập-kế-hoạch-chuyến)
10. [Flow 4 – Chạy chuyến và theo dõi](#10-flow-4--chạy-chuyến-và-theo-dõi)
11. [Flow 5 – Sự cố và nắn tuyến](#11-flow-5--sự-cố-và-nắn-tuyến)
12. [Flow 6 – Bàn giao, đóng chuyến và báo cáo](#12-flow-6--bàn-giao-đóng-chuyến-và-báo-cáo)
13. [Danh mục loại giấy tờ](#13-danh-mục-loại-giấy-tờ)
14. [Thông báo trong ứng dụng (chuông)](#14-thông-báo-trong-ứng-dụng-chuông)
15. [Quản lý phương tiện](#15-quản-lý-phương-tiện)
16. [Quy định giấy tờ theo quốc gia](#16-quy-định-giấy-tờ-theo-quốc-gia)
17. [Bảng giá](#17-bảng-giá)
18. [Danh mục giá trị cho dropdown](#18-danh-mục-giá-trị-cho-dropdown)
19. [Thanh toán (đặt cọc, phần còn lại, phát sinh, hoàn tiền)](#19-thanh-toán-đặt-cọc-phần-còn-lại-phát-sinh-hoàn-tiền)
19b. [Bồi thường giao trễ](#19b-bồi-thường-giao-trễ)
20. [Chính sách dịch vụ và cấu hình](#20-chính-sách-dịch-vụ-và-cấu-hình)
21. [Những phần chưa có API](#21-những-phần-chưa-có-api)
22. [Bảng tra nhanh mọi endpoint](#22-bảng-tra-nhanh-mọi-endpoint)

---

## 1. Bắt đầu nhanh

### API là gì trong dự án này

Frontend không đọc database trực tiếp. Mọi dữ liệu đều lấy và gửi qua server bằng các request HTTP. Mỗi request gồm:

- **Method**: `GET` (lấy dữ liệu), `POST` (tạo mới hoặc thực hiện một hành động), `PUT` (sửa toàn bộ), `PATCH` (sửa một phần), `DELETE` (xóa).
- **Đường dẫn**: ví dụ `/api/horses`. Địa chỉ đầy đủ = địa chỉ server + đường dẫn, ví dụ `https://racehorsetransportbackend-production.up.railway.app/api/horses`.
- **Header**: thông tin kèm theo, quan trọng nhất là `Authorization` chứa token đăng nhập.
- **Body**: dữ liệu gửi lên (chỉ có ở `POST`, `PUT`, `PATCH`).

Server trả về một **mã trạng thái** (200 là thành công, 4xx là lỗi phía người gọi, 5xx là lỗi server) và một **body JSON**.

### Thử API bằng Swagger trước khi viết code

1. Mở trang Swagger ở bảng trên.
2. Tìm `POST /api/auth/login`, bấm **Try it out**, nhập email và mật khẩu của một tài khoản mẫu, bấm **Execute**.
3. Trong kết quả, chép giá trị `data.accessToken`.
4. Bấm nút **Authorize** ở đầu trang, dán token vào, bấm **Authorize**.
5. Từ giờ mọi API khác trên trang đều gọi được với tài khoản đó.

### Tài khoản mẫu

| Email | Vai trò (`role`) | Dùng để thử |
|---|---|---|
| `jane.smith@racehorseowner.com` | `Customer` | Khách hàng: ngựa, đặt chuyến, nộp giấy tờ, theo dõi |
| `bao.tran@logistics.com` | `LogisticsManager` | Duyệt đơn, duyệt kế hoạch chuyến, duyệt phương án sự cố |
| `mai.nguyen@clearance.com` | `TransportSpecialist` | Hồ sơ kiểm dịch |
| `nam.le@fleet.com` | `FleetCoordinator` | Xếp chuyến, bảng theo dõi, xử lý sự cố |
| `duc.pham@transport.com` | `DriverEscort` | Tài xế: chạy chuyến, nhật ký ngựa, báo sự cố |
| `admin@logistics.com` | `Admin` | Quản lý tài khoản |

Ngoài ra mỗi vai trò có một tài khoản dễ nhớ, tên tài khoản chính là tên vai trò:

| Email | Vai trò (`role`) |
|---|---|
| `customer@racehorsetransport.com` | `Customer` |
| `manager@racehorsetransport.com` | `LogisticsManager` |
| `specialist@racehorsetransport.com` | `TransportSpecialist` |
| `coordinator@racehorsetransport.com` | `FleetCoordinator` |
| `driver@racehorsetransport.com` | `DriverEscort` |
| `admin@racehorsetransport.com` | `Admin` |

Mọi tài khoản mẫu dùng chung một mật khẩu, ghi ở dòng đầu file `database/02_seed.sql` trong repo backend. Trong các ví dụ bên dưới mật khẩu được viết là `<mật khẩu>`.

### Lời gọi đầu tiên bằng JavaScript

Ví dụ dưới dùng `fetch` có sẵn của trình duyệt: đăng nhập, rồi lấy danh sách ngựa.

```js
const BASE_URL = "https://racehorsetransportbackend-production.up.railway.app";

// 1. Đăng nhập
const loginRes = await fetch(`${BASE_URL}/api/auth/login`, {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ email: "jane.smith@racehorseowner.com", password: "<mật khẩu>" }),
});
const loginJson = await loginRes.json();

if (!loginJson.success) {
  alert(loginJson.message);          // ví dụ: "Email or password is incorrect."
} else {
  const token = loginJson.data.accessToken;

  // 2. Gọi một API cần đăng nhập: gửi token trong header Authorization
  const horsesRes = await fetch(`${BASE_URL}/api/horses?page=1&size=10`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  const horsesJson = await horsesRes.json();

  console.log(horsesJson.data);        // mảng các con ngựa
  console.log(horsesJson.pagination);  // { page, pageSize, totalItems, totalPages }
}
```

### Nên làm: tạo một file `api.js` dùng chung (axios)

Viết `fetch` ở từng màn hình sẽ phải lặp lại việc gắn token và xử lý hết hạn. Nên cài `axios` (`npm install axios`) và tạo một file dùng chung như sau. File này tự gắn token vào mọi request, và khi token hết hạn thì tự xin token mới rồi gọi lại.

```js
// src/api.js
import axios from "axios";

const api = axios.create({ baseURL: import.meta.env.VITE_API_URL });

// Trước mỗi request: gắn access token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem("accessToken");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

let refreshing = null; // nhiều request cùng hết hạn thì chỉ xin token mới một lần

// Sau mỗi response lỗi 401: thử xin token mới rồi gọi lại request cũ
api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config;
    const refreshToken = localStorage.getItem("refreshToken");

    if (error.response?.status !== 401 || original._retried || !refreshToken) {
      return Promise.reject(error);
    }
    original._retried = true;

    try {
      refreshing ??= axios.post(`${api.defaults.baseURL}/api/auth/refresh`, { refreshToken });
      const { data } = (await refreshing).data;
      localStorage.setItem("accessToken", data.accessToken);
      localStorage.setItem("refreshToken", data.refreshToken);
      return api(original);
    } catch {
      localStorage.clear();
      window.location.href = "/login";
      return Promise.reject(error);
    } finally {
      refreshing = null;
    }
  }
);

export default api;
```

Khai báo địa chỉ server trong file `.env` của frontend: `VITE_API_URL=https://racehorsetransportbackend-production.up.railway.app`.

Cách dùng ở màn hình:

```js
import api from "./api";

// Lấy dữ liệu
const res = await api.get("/api/horses", { params: { page: 1, size: 10 } });
const horses = res.data.data;            // res.data là body JSON của server; .data bên trong là dữ liệu
const pagination = res.data.pagination;

// Gửi dữ liệu và bắt lỗi
try {
  const res = await api.post("/api/bookings/1/cancel");
  alert(res.data.message);               // "Booking cancelled successfully"
} catch (error) {
  alert(error.response?.data?.message ?? "Không kết nối được server");
}
```

---

## 2. Quy ước chung

### 2.1. Khuôn response

Mọi API, kể cả khi lỗi, đều trả về cùng một khuôn:

```json
{
  "success": true,
  "message": "Horse created successfully",
  "data": { },
  "errors": null
}
```

| Trường | Ý nghĩa |
|---|---|
| `success` | `true` nếu thành công, `false` nếu lỗi |
| `message` | Câu thông báo bằng tiếng Anh, có thể hiện thẳng cho người dùng |
| `data` | Dữ liệu trả về: một object, một mảng, hoặc `null` |
| `errors` | `null` khi thành công; khi lỗi là mảng các câu báo lỗi |

API danh sách có phân trang trả thêm `pagination`:

```json
{
  "success": true,
  "message": "Horses retrieved successfully",
  "data": [ ],
  "errors": null,
  "pagination": { "page": 1, "pageSize": 10, "totalItems": 37, "totalPages": 4 }
}
```

- Tên trường viết kiểu **camelCase** (`horseId`, `createdAt`).
- Trong tài liệu này, mục "Response — phần `data`" chỉ in nội dung của `data`; phần vỏ `success`, `message`, `errors` luôn giống trên.
- Tên dòng báo giá và tên loại giấy tờ là tiếng Việt vì lấy từ dữ liệu.

### 2.2. Lỗi và cách xử lý

| HTTP | Nghĩa là gì | Frontend nên làm gì |
|---|---|---|
| 400 | Dữ liệu gửi lên sai, hoặc vi phạm quy tắc nghiệp vụ | Hiện `message`. Nếu `message` là `Validation failed` thì hiện từng dòng trong `errors` |
| 401 | Chưa đăng nhập, token sai hoặc đã hết hạn | Xin token mới (mục 4); không được thì chuyển về trang đăng nhập. File `api.js` ở mục 1 đã làm việc này |
| 403 | Đã đăng nhập nhưng không có quyền | Hiện `message`, không thử lại |
| 404 | Không tìm thấy dữ liệu theo id đã gửi | Hiện `message` |
| 415 | Gửi sai kiểu dữ liệu (ví dụ gửi JSON vào API nhận file) | Sửa code: xem mục 2.5 |
| 500 | Lỗi phía server | Báo lỗi chung, thử lại sau, báo cho backend |

Ví dụ thật của từng loại:

Lỗi 400 do dữ liệu sai định dạng (nhiều lỗi cùng lúc, nằm trong `errors`):

```json
{
  "success": false,
  "message": "Validation failed",
  "data": null,
  "errors": [
    "Email format is invalid.",
    "Password is required."
  ]
}
```

Lỗi 400 do vi phạm quy tắc nghiệp vụ (đọc `message`):

```json
{
  "success": false,
  "message": "Microchip number '982000412345699' is already registered.",
  "data": null,
  "errors": [
    "Microchip number '982000412345699' is already registered."
  ]
}
```

Lỗi 401 (chưa gửi token hoặc token hết hạn):

```json
{
  "success": false,
  "message": "Authentication is required. Access token is missing, invalid or expired.",
  "data": null,
  "errors": [
    "Authentication is required. Access token is missing, invalid or expired."
  ]
}
```

Lỗi 403 (không đủ quyền):

```json
{
  "success": false,
  "message": "You do not have permission to perform this action.",
  "data": null,
  "errors": [
    "You do not have permission to perform this action."
  ]
}
```

Lỗi 404:

```json
{
  "success": false,
  "message": "Horse with ID 9999 was not found.",
  "data": null,
  "errors": [
    "Horse with ID 9999 was not found."
  ]
}
```

Một hàm hiển thị lỗi dùng chung cho mọi màn hình:

```js
export function getErrorMessage(error) {
  const body = error.response?.data;
  if (!body) return "Không kết nối được server.";
  if (body.message === "Validation failed" && body.errors?.length) return body.errors.join("\n");
  return body.message;
}
```

### 2.3. API danh sách: tìm kiếm, sắp xếp, phân trang

Các API danh sách (`GET /api/horses`, `GET /api/bookings`, ...) nhận chung 4 tham số query:

| Tham số | Ý nghĩa | Ví dụ |
|---|---|---|
| `search` | Tìm theo từ khóa, không phân biệt hoa thường | `search=golden` |
| `sort` | Tên trường cần sắp xếp; thêm `-` phía trước để giảm dần; nhiều trường cách nhau bằng dấu phẩy | `sort=-createdAt,name` |
| `page` | Trang muốn lấy, bắt đầu từ 1 | `page=2` |
| `size` | Số dòng mỗi trang, từ 1 đến 100, mặc định 10 | `size=20` |

Ví dụ: `GET /api/horses?search=golden&sort=-createdAt&page=1&size=20`.

Đọc `pagination.totalPages` để biết có bao nhiêu trang, `pagination.totalItems` để hiện tổng số dòng. Mỗi danh sách còn có bộ lọc riêng, ghi ở từng API.

### 2.4. Ngày giờ

- **Gửi lên** theo chuẩn ISO 8601 **kèm múi giờ**: `2026-11-07T06:00:00+07:00` (6 giờ sáng giờ Việt Nam). Trong JavaScript: `new Date(value).toISOString()` cho ra chuỗi hợp lệ.
- **Server trả về** giờ UTC: `2026-11-06T23:00:00+00:00`. Khi hiển thị, dùng `new Date(value).toLocaleString("vi-VN")` để đổi sang giờ của máy người dùng.
- Trường **chỉ có ngày** (ngày sinh của ngựa, ngày hết hạn giấy tờ) viết dạng `2027-05-20`.

### 2.5. Gửi JSON và gửi file

Phần lớn API nhận **JSON**: gửi object bình thường, axios tự đặt header.

Bốn nhóm API có kèm file thì nhận **`multipart/form-data`** (kiểu dữ liệu của form có file), không nhận JSON:

| API | Trường chứa file | Bắt buộc | Loại file |
|---|---|---|---|
| `POST /api/horses`, `PUT /api/horses/{id}` | `photo` | Không | `jpg`, `jpeg`, `png` |
| `POST /api/dossiers/{id}/documents` | `file` | Có | `pdf`, `jpg`, `jpeg`, `png` |
| `POST /api/trips/{id}/welfare-logs` | `photo` | Không | `jpg`, `jpeg`, `png` |
| `POST /api/trips/{tripId}/handover` | `signature` | Có | `jpg`, `jpeg`, `png` |

Cách gửi: tạo `FormData`, thêm từng trường bằng `append`, thêm file lấy từ ô `<input type="file">`.

```js
const form = new FormData();
form.append("name", "Silver Wind");
form.append("microchipNumber", "982000412345699");
form.append("passportNumber", "704001000000009");
form.append("gender", "Gelding");
form.append("dateOfBirth", "2019-05-20");
form.append("color", "Xám");
form.append("photo", fileInput.files[0]);   // bỏ dòng này nếu không có ảnh

const res = await api.post("/api/horses", form);
console.log(res.data.data.photoUrl);        // link ảnh, dùng được ngay
```

Lưu ý khi gửi file:

- **Không tự đặt header `Content-Type`**. Trình duyệt và axios sẽ tự đặt đúng.
- Mỗi file tối đa **10 MB**. Nội dung file phải đúng với đuôi file (đổi tên `.exe` thành `.png` sẽ bị từ chối).
- Mọi giá trị trong `FormData` đều là chuỗi: số viết `"17.5"` (dấu chấm), ngày viết `"2027-01-15"`, đúng/sai viết `"true"` / `"false"`.
- Gửi JSON vào các API này sẽ nhận lỗi `415`.

**Hiển thị file:** các trường `photoUrl` và `fileUrl` trong response là **link xem được ngay**. Gắn thẳng vào `<img src={horse.photoUrl} />` hoặc mở trong tab mới để xem PDF; không cần gọi thêm API nào.

- Link có thời hạn (dùng được ít nhất 30 phút kể từ lúc nhận response). Không lưu link vào `localStorage`; khi cần hiển thị lại thì gọi lại API để lấy link mới.
- `photoUrl` là `null` khi không có ảnh. Nếu file không còn trên kho lưu trữ: `photoUrl` là `null`, `fileUrl` là chuỗi rỗng. Nên có ảnh mặc định cho trường hợp này.

### 2.6. Cách đọc từng mục API

- `{id}`, `{tripId}` trong đường dẫn là chỗ thay bằng số thật: `/api/horses/{id}` thành `/api/horses/3`.
- **Ai gọi được**: tài khoản có vai trò khác gọi vào sẽ nhận `403`.
- Cột **Bắt buộc**: "Có" nghĩa là thiếu sẽ nhận lỗi 400.
- Sau khi thực hiện một hành động (duyệt, từ chối, check-in...), server trả luôn dữ liệu mới nhất của đối tượng. Dùng `data` đó cập nhật màn hình, không cần gọi lại API lấy chi tiết.

### 2.7. CORS

Server chỉ nhận request từ các địa chỉ frontend đã khai báo; hiện cho phép `http://localhost:5173`. Nếu trình duyệt báo lỗi CORS, hoặc khi frontend có địa chỉ deploy, báo backend để thêm địa chỉ đó.

---

## 3. Vai trò và các giá trị cố định

### Vai trò (`role`)

Sau khi đăng nhập, đọc `data.user.role` để quyết định hiện màn hình nào.

| `role` | Là ai | Màn hình chính |
|---|---|---|
| `Customer` | Chủ ngựa / câu lạc bộ | Ngựa của tôi, đặt chuyến, nộp giấy tờ, theo dõi đơn |
| `LogisticsManager` | Quản lý điều hành | Duyệt đơn, duyệt kế hoạch chuyến, duyệt phương án sự cố |
| `TransportSpecialist` | Chuyên viên thủ tục | Các hồ sơ kiểm dịch được giao |
| `FleetCoordinator` | Điều phối đội xe | Xếp chuyến, bảng theo dõi, xử lý sự cố |
| `DriverEscort` | Tài xế / nhân viên áp tải | Chuyến của tôi, check-in, nhật ký ngựa, báo sự cố (ứng dụng mobile) |
| `Admin` | Quản trị hệ thống | Tài khoản, danh mục |

`DriverEscort` là vai trò của tài khoản. Trên từng chuyến, người đó là lái xe hay áp tải thì xem ở `crew[].crewRole` của chuyến.

### Giá trị cố định (enum)

Các trường dưới đây chỉ nhận đúng những giá trị liệt kê, **phân biệt hoa thường**. Dùng cho dropdown và để hiện nhãn tiếng Việt.

| Trường | Giá trị và ý nghĩa |
|---|---|
| Trạng thái đơn (`status`) | `Submitted` chờ duyệt · `PreApproved` đã duyệt sơ bộ, công ty đang lập tuyến · `RoutePlanned` đã duyệt tuyến, đang tính báo giá chốt · `Quoted` đã gửi báo giá chốt, chờ khách chấp nhận · `AwaitingDeposit` chờ khách đặt cọc · `Approved` đã cọc, đang làm hồ sơ · `Assigned` đã xếp chuyến · `AwaitingFinalPayment` giao xong nhưng còn chi phí phát sinh chưa trả · `Completed` hoàn tất · `Rejected` bị từ chối · `Expired` quá hạn duyệt hoặc quá hạn đặt cọc (đơn cũ) · `QuoteExpired` quá hạn chấp nhận / đặt cọc báo giá chốt · `Cancelled` khách hủy hoặc từ chối báo giá |
| Trạng thái tuyến dự kiến (`routePlan.status`) | `Draft` bản nháp · `Submitted` chờ duyệt · `Approved` tuyến hiệu lực của đơn · `Rejected` bị từ chối, sửa rồi trình lại · `Superseded` bản đã duyệt nhưng bị thay bằng bản mới |
| Loại mốc của tuyến dự kiến (`stopType`) | `Origin` điểm đón (lấy từ đơn) · `RestStop` trạm nghỉ · `BorderGate` cửa khẩu · `Destination` điểm trả (lấy từ đơn) |
| `paymentType` (mục 19) | `Deposit` đặt cọc · `Balance` phần còn lại · `Extra` phát sinh · `Refund` hoàn tiền |
| Trạng thái giao dịch (mục 19) | `Pending` đang chờ cổng · `Succeeded` thành công · `Failed` thất bại · `Expired` link bị thay |
| `provider` (mục 19) | `VnPay` · `Mock` cổng giả lập · `Manual` ghi nhận tay |
| Trạng thái ngựa trong đơn | `Pending` · `Approved` · `Cancelled` |
| `transportMode` | `Ground` đường bộ · `Air` đường hàng không |
| `stallClass` (hạng chuồng) | `Shared` chuồng ghép · `Comfort` chuồng rưỡi · `Private` chuồng riêng |
| Giới tính ngựa (`gender`) | `Stallion` ngựa đực · `Mare` ngựa cái · `Gelding` ngựa thiến |
| Trạng thái hồ sơ | `Draft` mới mở · `AwaitingDocs` chờ khách nộp giấy · `Reviewing` đang thẩm định · `SubmittedToAuthorities` đã nộp cơ quan chức năng · `Cleared` đã thông quan · `Issue` có vấn đề |
| Trạng thái giấy tờ | `Pending` chờ duyệt · `Approved` đã duyệt · `Rejected` bị từ chối |
| Trạng thái dòng checklist (`state`) | `Missing` chưa nộp · `Pending` · `Approved` · `Rejected` |
| Trạng thái chuyến (`overallStatus`) | `Draft` bản nháp · `PendingApproval` chờ duyệt · `Scheduled` đã lên lịch · `InTransit` đang đi · `EmergencyRerouting` đang nắn tuyến vì sự cố · `ArrivedDestination` đã tới đích · `Completed` đã đóng · `Cancelled` đã hủy |
| `crewRole` | `Driver` lái xe · `Escort` áp tải |
| `checkpointType` (loại mốc) | `Origin` điểm đi · `PickUp` điểm đón ngựa của một đơn · `RestStop` trạm nghỉ · `BorderGate` cửa khẩu · `DropOff` điểm trả ngựa của một đơn · `Destination` điểm giao (mốc cuối) |
| Trạng thái mốc | `Pending` chưa tới · `Arrived` đã tới · `Cleared` đã thông quan (chỉ cửa khẩu) · `Departed` đã rời · `Cancelled` bị bỏ khi nắn tuyến |
| Trạng thái ngựa trên chuyến (`horses[].status`) | `Planned` đã xếp chuyến, chưa lên xe · `InTransit` đang trên xe · `Delivered` đã ký bàn giao (ở điểm trả, hoặc tại chỗ khi rời chuyến) · `Offloaded` đã rời chuyến giữa đường qua sự cố, đi tiếp thế nào xem `offloadOutcome` · `Deceased` chết trên đường · `Cancelled` chuyến bị hủy |
| Hướng xử lý khi ngựa rời chuyến (`offloadOutcome`) | `WaitRecover` chờ hồi phục rồi về hàng chờ xếp chuyến khác · `ReturnToOrigin` đưa về nơi đi (con ngựa kết thúc với đơn) · `HandOverHere` khách nhận ngựa tại nơi dỡ, ký e-POD tại chỗ · `Deceased` ngựa chết. `null` khi tài xế vừa dỡ khẩn cấp và phương án chưa được duyệt |
| Trạng thái ngựa trong đơn | `Pending` chờ duyệt · `Approved` đã duyệt · `Cancelled` đã hủy · `Deceased` chết trên đường |
| `feedStatus` (ăn uống) | `Normal` bình thường · `Reduced` ăn ít · `Refused` bỏ ăn |
| `stressLevel` | `Calm` bình tĩnh · `MildStress` hơi căng thẳng · `Agitated` kích động |
| `incidentType` (loại sự cố) | `MechanicalBreakdown` hỏng xe · `BorderCongestion` tắc biên · `EquineHealthIssue` sức khỏe ngựa · `Weather` thời tiết · `ClimateControlFailure` hỏng điều hòa · `CustomsHold` bị giữ ở hải quan · `Other` khác |
| `severity` (mức độ sự cố) | `Minor` nhẹ · `Moderate` vừa · `Major` nặng · `Critical` nghiêm trọng |
| Nguyên nhân sự cố (`faultParty`) | `Company` lỗi công ty · `Customer` lỗi khách · `ForceMajeure` bất khả kháng. Chọn khi đóng sự cố; quyết định ai chịu chi phí và có bồi thường giao trễ không (Chính sách mục 9.1) |
| Trạng thái sự cố | `Reported` mới báo · `PlanProposed` đã có phương án chờ duyệt · `Approved` phương án được duyệt · `Rejected` phương án bị từ chối · `Resolved` đã xử lý xong |
| `overallCondition` (tình trạng khi bàn giao) | `Excellent` tốt · `NormalFatigue` mệt bình thường · `Injured` bị thương |
| `onTimeStatus` (kết quả đúng giờ của chuyến) | `OnTime` đúng giờ · `Delayed_AcceptableForceMajeure` trễ do bất khả kháng · `Delayed_OperationalFault` trễ do lỗi vận hành |
| Loại phương tiện (`assetType`) | `HorseTruck_AirSuspension` xe tải giảm xóc khí · `HorseVan` xe van · `AirStall` chuồng hàng không |
| Tình trạng phương tiện (`status`, mục 15) | `Available` sẵn sàng · `InTransit` đang chạy chuyến · `Maintenance` đang bảo dưỡng |
| `direction` (quy định giấy tờ, mục 16) | `Export` khi rời nước · `Import` khi vào nước · `Transit` khi chỉ đi qua nước đó |
| `category` (nhóm giá, mục 17) | `Freight` cước · `StallClass` hệ số hạng chuồng · `Surcharge` phụ phí · `Discount` chiết khấu · `Clearance` phí thủ tục · `Escort` người chăm ngựa · `Insurance` bảo hiểm |
| `calcType` (cách tính giá, mục 17) | `PerHorse` · `PerKmPerHorse` · `Multiplier` · `Percent` · `PerBooking` · `PerGroom` · `PerGroomDay` |
| `referenceType` (thông báo, mục 14) | `Booking` · `Dossier` · `Trip` · `Incident` |

---

## 4. Đăng nhập và tài khoản của tôi

### Token hoạt động thế nào

Khi đăng nhập thành công, server trả về hai chuỗi:

- **`accessToken`**: gửi kèm trong header `Authorization: Bearer <accessToken>` của mọi request cần đăng nhập. Sống **60 phút**.
- **`refreshToken`**: dùng để xin `accessToken` mới khi cái cũ hết hạn, không bắt người dùng đăng nhập lại. Sống **7 ngày** và **chỉ dùng được một lần**: mỗi lần xin token mới, server trả luôn `refreshToken` mới, phải lưu đè cái cũ.

Lưu cả hai vào `localStorage` sau khi đăng nhập. File `api.js` ở mục 1 đã xử lý việc gắn token và tự xin token mới.

### Email hệ thống gửi cho người dùng

Hệ thống tự gửi email trong hai trường hợp; frontend không phải gọi gì thêm:

- **Quên mật khẩu:** email chứa link `<địa chỉ frontend>/reset-password?token=...`. Frontend cần có trang `/reset-password`: đọc `token` trên URL bằng `new URLSearchParams(window.location.search).get("token")`, cho người dùng nhập mật khẩu mới, rồi gọi `POST /api/auth/reset-password`.
- **Thông báo cho khách hàng** ở các sự kiện chính: đơn được duyệt / bị từ chối, cần bổ sung giấy tờ, giấy bị từ chối, chuyến được duyệt (nhắc trả phần còn lại), ngựa đã thông quan, hồ sơ có vấn đề, có sự cố trên chuyến, ngựa tới điểm giao. Nhân sự không nhận email, chỉ nhận thông báo trong ứng dụng.

Đăng ký tài khoản **không** cần xác thực email: đăng ký xong là dùng được ngay.

### POST /api/auth/register — Đăng ký tài khoản khách hàng

Tạo tài khoản mới với vai trò `Customer` và đăng nhập luôn (response có sẵn token). Tài khoản nhân sự không đăng ký ở đây mà do Admin tạo (mục 5).

- **Ai gọi được:** Ai cũng gọi được, không cần đăng nhập
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `fullName` | chuỗi | Có | Họ tên, tối đa 100 ký tự |
| `email` | chuỗi | Có | Email đúng định dạng, chưa ai dùng |
| `password` | chuỗi | Có | Mật khẩu, từ 8 đến 64 ký tự |
| `phoneNumber` | chuỗi | Không | Số điện thoại, 6–30 ký tự gồm số, khoảng trắng, `+ ( ) . -` |

Ví dụ request:

```json
{
  "fullName": "Lê Minh Anh",
  "email": "minhanh@example.com",
  "password": "MinhAnh@2026",
  "phoneNumber": "+84901234567"
}
```

Response `201` — phần `data`:

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "accessTokenExpiresAt": "2026-10-08T08:24:28+00:00",
  "refreshToken": "NCWPGUvt...",
  "user": {
    "userId": 7,
    "fullName": "Lê Minh Anh",
    "email": "minhanh@example.com",
    "phoneNumber": "+84901234567",
    "role": "Customer",
    "isActive": true,
    "createdAt": "2026-10-08T07:24:28+00:00"
  }
}
```

`data` có cùng dạng với đăng nhập. Lưu `accessToken`, `refreshToken` và `user` như sau khi đăng nhập.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Email '...' is already registered.` | Email đã có người dùng |
| 400 | `Validation failed` | Thiếu trường, email sai định dạng, mật khẩu ngắn hơn 8 ký tự |

### POST /api/auth/login — Đăng nhập

Đổi email và mật khẩu lấy token.

- **Ai gọi được:** ai cũng gọi được, không cần đăng nhập
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| email | chuỗi | Có | Email đã đăng ký |
| password | chuỗi | Có | Mật khẩu |

Ví dụ request:

```json
{
  "email": "jane.smith@racehorseowner.com",
  "password": "<mật khẩu>"
}
```

Response `200` — phần `data`:

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "accessTokenExpiresAt": "2026-10-08T08:24:28+00:00",
  "refreshToken": "c5tbHtK4...",
  "user": {
    "userId": 5,
    "fullName": "Jane Smith",
    "email": "jane.smith@racehorseowner.com",
    "phoneNumber": "+84988111222",
    "role": "Customer",
    "isActive": true,
    "createdAt": "2026-10-08T07:24:28+00:00"
  }
}
```

| Trường | Ý nghĩa |
|---|---|
| `accessToken` | Token gửi kèm mọi request |
| `accessTokenExpiresAt` | Thời điểm `accessToken` hết hạn |
| `refreshToken` | Dùng để xin token mới |
| `user` | Thông tin người đăng nhập; `user.role` quyết định giao diện |

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 401 | `Email or password is incorrect.` | Sai email hoặc mật khẩu |
| 403 | `This account has been deactivated.` | Tài khoản đã bị Admin khóa |
| 400 | `Validation failed` | Thiếu email / mật khẩu, email sai định dạng |

### POST /api/auth/google — Đăng nhập bằng Google

Đăng nhập bằng tài khoản Google thay cho email và mật khẩu. Luồng hoạt động:

1. Frontend hiện nút "Đăng nhập bằng Google" (thư viện của Google). Người dùng bấm và chọn tài khoản.
2. Google trả cho frontend một chuỗi gọi là **ID token** (trong code là `credential`).
3. Frontend gửi chuỗi đó tới API này. Server kiểm tra với Google rồi trả về `accessToken`, `refreshToken`, `user` **giống hệt đăng nhập thường**; từ đây mọi thứ như cũ.

Server xử lý theo email của tài khoản Google:

- Email **chưa có** tài khoản: tạo tài khoản `Customer` mới.
- Email **đã có** tài khoản (khách hoặc nhân sự): đăng nhập vào đúng tài khoản đó.

- **Ai gọi được:** ai cũng gọi được, không cần đăng nhập
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `idToken` | chuỗi | Có | Chuỗi `credential` Google trả về cho frontend |

Ví dụ request:

```json
{
  "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6Ij..."
}
```

Response `200` — phần `data` (cùng dạng với đăng nhập thường):

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "accessTokenExpiresAt": "2026-10-08T08:24:28+00:00",
  "refreshToken": "fTQhHEzK...",
  "user": {
    "userId": 8,
    "fullName": "Phạm Anh Thư",
    "email": "thu.pham@gmail.com",
    "phoneNumber": null,
    "role": "Customer",
    "isActive": true,
    "createdAt": "2026-10-08T07:24:28+00:00"
  }
}
```

Cách làm ở frontend (React + Vite). Cài thư viện: `npm install @react-oauth/google`. Thêm vào file `.env` của frontend: `VITE_GOOGLE_CLIENT_ID=<Client ID do backend cung cấp>`.

```js
// main.jsx: bọc ứng dụng một lần
import { GoogleOAuthProvider } from "@react-oauth/google";

<GoogleOAuthProvider clientId={import.meta.env.VITE_GOOGLE_CLIENT_ID}>
  <App />
</GoogleOAuthProvider>

// Trang đăng nhập
import { GoogleLogin } from "@react-oauth/google";
import api from "./api";

<GoogleLogin
  onSuccess={async ({ credential }) => {
    const res = await api.post("/api/auth/google", { idToken: credential });
    const { accessToken, refreshToken, user } = res.data.data;
    localStorage.setItem("accessToken", accessToken);
    localStorage.setItem("refreshToken", refreshToken);
    // chuyển trang theo user.role, giống sau khi đăng nhập thường
  }}
  onError={() => alert("Đăng nhập Google không thành công")}
/>
```

Lưu ý:

- Nút Google chỉ hoạt động trên các địa chỉ frontend đã được khai báo trong Google Cloud (hiện có `http://localhost:5173`). Khi frontend có địa chỉ deploy, báo backend để thêm vào.
- Tài khoản tạo qua Google **không có mật khẩu**. Muốn đăng nhập bằng mật khẩu thì dùng "Quên mật khẩu" để đặt một mật khẩu.
- Đăng xuất vẫn gọi `POST /api/auth/logout` như thường.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 401 | `Google sign-in failed: the Google token is invalid or has expired.` | Chuỗi gửi lên không phải ID token hợp lệ của Google, đã quá hạn (khoảng 1 giờ), hoặc được cấp cho một ứng dụng khác (sai Client ID) |
| 401 | `Google sign-in failed: this Google account has no verified email address.` | Tài khoản Google chưa xác nhận email |
| 403 | `This account has been deactivated.` | Tài khoản tương ứng đã bị Admin khóa |
| 400 | `Google sign-in is not configured on this server.` | Backend chưa cấu hình Client ID |

### POST /api/auth/refresh — Xin token mới

Gọi khi một request trả về `401` vì `accessToken` hết hạn. Gửi `refreshToken` đang lưu, nhận về cặp token mới. `refreshToken` cũ hết hiệu lực ngay sau lời gọi này.

- **Ai gọi được:** Ai cũng gọi được, không cần `accessToken`
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `refreshToken` | chuỗi | Có | `refreshToken` nhận được lúc đăng nhập hoặc lần refresh gần nhất |

Ví dụ request:

```json
{
  "refreshToken": "VIhRZAGZ..."
}
```

Response `200`: `data` giống hệt đăng nhập (có `accessToken`, `refreshToken` mới và `user`). Lưu đè cả hai token.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 401 | `Refresh token is invalid or has expired.` | Token đã dùng rồi, đã hết hạn, đã đăng xuất, hoặc mật khẩu vừa được đặt lại. Chuyển về trang đăng nhập |

### POST /api/auth/logout — Đăng xuất

Hủy `refreshToken` trên server. Sau khi gọi, xóa token trong `localStorage` và chuyển về trang đăng nhập.

- **Ai gọi được:** Mọi người đã đăng nhập
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `refreshToken` | chuỗi | Có | `refreshToken` đang lưu |

Ví dụ request:

```json
{
  "refreshToken": "9J7MXtzE..."
}
```

Response `200`: `data` là `null`, `message` là `Logged out successfully`.

### GET /api/auth/me — Xem hồ sơ của tôi

Lấy thông tin của người đang đăng nhập. Dùng khi tải lại trang để biết người dùng là ai mà không cần đăng nhập lại.

- **Ai gọi được:** Mọi người đã đăng nhập

Response `200` — phần `data`:

```json
{
  "userId": 5,
  "fullName": "Jane Smith",
  "email": "jane.smith@racehorseowner.com",
  "phoneNumber": "+84988111222",
  "role": "Customer",
  "isActive": true,
  "createdAt": "2026-10-08T07:24:28+00:00"
}
```

### PUT /api/auth/me — Sửa hồ sơ của tôi

Sửa họ tên và số điện thoại của chính mình. Email và vai trò không tự sửa được.

- **Ai gọi được:** Mọi người đã đăng nhập
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `fullName` | chuỗi | Có | Họ tên, tối đa 100 ký tự |
| `phoneNumber` | chuỗi | Không | Số điện thoại; gửi chuỗi rỗng để xóa |

Ví dụ request:

```json
{
  "fullName": "Lê Minh Anh",
  "phoneNumber": "+84909999888"
}
```

Response `200` — phần `data`:

```json
{
  "userId": 7,
  "fullName": "Lê Minh Anh",
  "email": "minhanh@example.com",
  "phoneNumber": "+84909999888",
  "role": "Customer",
  "isActive": true,
  "createdAt": "2026-10-08T07:24:28+00:00"
}
```

### PUT /api/auth/change-password — Đổi mật khẩu

Đổi mật khẩu của chính mình; phải nhập đúng mật khẩu hiện tại.

- **Ai gọi được:** Mọi người đã đăng nhập
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `currentPassword` | chuỗi | Có | Mật khẩu hiện tại |
| `newPassword` | chuỗi | Có | Mật khẩu mới, 8–64 ký tự, khác mật khẩu hiện tại |

Ví dụ request:

```json
{
  "currentPassword": "<mật khẩu hiện tại>",
  "newPassword": "<mật khẩu mới>"
}
```

Response `200`: `data` là `null`, `message` là `Password changed successfully`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Current password is incorrect.` | Nhập sai mật khẩu hiện tại |
| 400 | `New password must be different from the current password.` | Mật khẩu mới trùng mật khẩu cũ |

### POST /api/auth/forgot-password — Quên mật khẩu: xin link đặt lại

Gửi email chứa link đặt lại mật khẩu (hạn 60 phút, mỗi phút chỉ gửi một lần cho cùng một tài khoản). API **luôn trả 200**, kể cả khi email chưa đăng ký, để người lạ không dò được email nào có tài khoản. Vì vậy luôn hiện cùng một câu: "Nếu email đã đăng ký, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu."

- **Ai gọi được:** Ai cũng gọi được, không cần đăng nhập
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `email` | chuỗi | Có | Email của tài khoản |

Ví dụ request:

```json
{
  "email": "minhanh@example.com"
}
```

Response `200`: `data` là `null`, `message` là `If this email is registered, a password reset link has been sent.`

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Validation failed` | Email sai định dạng |

### POST /api/auth/reset-password — Đặt mật khẩu mới bằng token trong link

Trang `/reset-password` của frontend gọi API này với `token` lấy trên URL và mật khẩu mới. Sau khi thành công, mọi phiên đăng nhập cũ của tài khoản bị đăng xuất; chuyển người dùng về trang đăng nhập.

- **Ai gọi được:** Ai cũng gọi được, không cần đăng nhập
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `token` | chuỗi | Có | Giá trị `token` trên URL của link trong email |
| `newPassword` | chuỗi | Có | Mật khẩu mới, 8–64 ký tự |

Ví dụ request:

```json
{
  "token": "Zp4mT9v...",
  "newPassword": "<mật khẩu mới>"
}
```

Response `200`: `data` là `null`, `message` là `Password reset successfully`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `The password reset link is invalid or has expired. Please request a new one.` | Token sai, đã dùng rồi, hoặc quá 60 phút |
| 400 | `Validation failed` | Mật khẩu mới ngắn hơn 8 ký tự |

---

## 5. Quản lý người dùng

Màn hình quản trị tài khoản của `Admin`, cộng một API lấy danh sách nhân sự để đổ vào dropdown "chọn người phụ trách".

Một tài khoản (`user`) có dạng:

```json
{
  "userId": 9,
  "fullName": "Trần Thị Hoa",
  "email": "hoa.tran@transport.com",
  "phoneNumber": "+84912000007",
  "role": "DriverEscort",
  "isActive": true,
  "createdAt": "2026-10-08T07:24:29+00:00"
}
```

| Trường | Ý nghĩa |
|---|---|
| `userId` | Mã tài khoản |
| `fullName`, `email`, `phoneNumber` | Thông tin liên hệ |
| `role` | Vai trò (mục 3) |
| `isActive` | `false` là tài khoản đã bị khóa, không đăng nhập được |
| `createdAt` | Thời điểm tạo |

### GET /api/users — Danh sách tài khoản

Danh sách mọi tài khoản, có phân trang.

- **Ai gọi được:** Admin

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `role` | chuỗi | Không | Lọc theo vai trò, ví dụ `DriverEscort` |
| `isActive` | true / false | Không | Lọc tài khoản đang hoạt động / đã khóa |
| `search` | chuỗi | Không | Từ khóa tìm kiếm |
| `sort` | chuỗi | Không | Trường sắp xếp, thêm `-` phía trước để giảm dần |
| `page` | số | Không | Trang, bắt đầu từ 1 (mặc định 1) |
| `size` | số | Không | Số dòng mỗi trang, 1–100 (mặc định 10) |

Ví dụ: `GET /api/users?role=DriverEscort&page=1&size=10`

Response `200` — phần `data`:

```json
[
  {
    "userId": 4,
    "fullName": "Phạm Văn Đức",
    "email": "duc.pham@transport.com",
    "phoneNumber": "+84912000004",
    "role": "DriverEscort",
    "isActive": true,
    "createdAt": "2026-10-08T07:24:28+00:00"
  }
]
```

`data` là mảng tài khoản (ví dụ trên chỉ in 1 phần tử), kèm `pagination`. `search` tìm theo họ tên và email.

### GET /api/users/{id} — Chi tiết một tài khoản

Lấy một tài khoản theo mã.

- **Ai gọi được:** Admin

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã tài khoản (`userId`) |

Response `200`: `data` là một tài khoản, dạng như trên.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 404 | `User with ID 99 was not found.` | Không có tài khoản với mã đó |

### POST /api/users — Tạo tài khoản nhân sự

Tạo tài khoản cho nhân viên. Không tạo được tài khoản `Customer` ở đây (khách tự đăng ký).

- **Ai gọi được:** Admin
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `fullName` | chuỗi | Có | Họ tên |
| `email` | chuỗi | Có | Email, chưa ai dùng |
| `password` | chuỗi | Có | Mật khẩu ban đầu, 8–64 ký tự |
| `phoneNumber` | chuỗi | Không | Số điện thoại |
| `role` | chuỗi | Có | Một trong: `LogisticsManager`, `TransportSpecialist`, `FleetCoordinator`, `DriverEscort`, `Admin` |

Ví dụ request:

```json
{
  "fullName": "Trần Thị Hoa",
  "email": "hoa.tran@transport.com",
  "password": "<mật khẩu>",
  "phoneNumber": "+84912000007",
  "role": "DriverEscort"
}
```

Response `201`: `data` là tài khoản vừa tạo.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Email '...' is already registered.` | Email đã tồn tại |
| 400 | `Customer accounts are created through self-registration, not by an administrator.` | Gửi `role` là `Customer` |
| 400 | `Role '...' is invalid. Allowed roles: ...` | Viết sai tên vai trò |

### PUT /api/users/{id} — Sửa tài khoản

Sửa họ tên, số điện thoại và vai trò của một tài khoản.

- **Ai gọi được:** Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã tài khoản |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `fullName` | chuỗi | Có | Họ tên |
| `phoneNumber` | chuỗi | Không | Số điện thoại |
| `role` | chuỗi | Có | Vai trò mới |

Ví dụ request:

```json
{
  "fullName": "Trần Thị Hoa",
  "phoneNumber": "+84912000077",
  "role": "DriverEscort"
}
```

Response `200`: `data` là tài khoản sau khi sửa.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `You cannot change your own role.` | Admin tự đổi vai trò của mình |
| 400 | `An account cannot be switched between Customer and a staff role.` | Đổi khách thành nhân sự hoặc ngược lại |

### PATCH /api/users/{id}/active — Khóa hoặc mở tài khoản

Khóa tài khoản (không đăng nhập được nữa) hoặc mở lại.

- **Ai gọi được:** Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã tài khoản |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `isActive` | true / false | Có | `false` để khóa, `true` để mở |

Ví dụ request:

```json
{
  "isActive": false
}
```

Response `200`: `data` là tài khoản với `isActive` mới. `message` là `User deactivated successfully` hoặc `User activated successfully`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `You cannot deactivate your own account.` | Admin tự khóa mình |

### PUT /api/users/{id}/password — Đặt lại mật khẩu cho người khác

Đặt mật khẩu mới cho một tài khoản khi người đó quên mật khẩu.

- **Ai gọi được:** Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã tài khoản |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `newPassword` | chuỗi | Có | Mật khẩu mới, 8–64 ký tự |

Ví dụ request:

```json
{
  "newPassword": "<mật khẩu mới>"
}
```

Response `200`: `data` là `null`, `message` là `Password reset successfully`.

### GET /api/users/staff — Danh sách nhân sự cho dropdown

Trả về các nhân sự **đang hoạt động**, không phân trang. Dùng để đổ dropdown, ví dụ chọn chuyên viên khi duyệt đơn.

- **Ai gọi được:** LogisticsManager, TransportSpecialist, FleetCoordinator, Admin

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `role` | chuỗi | Không | Chỉ lấy một vai trò, ví dụ `TransportSpecialist` |

Ví dụ: `GET /api/users/staff?role=TransportSpecialist`

Response `200` — phần `data`:

```json
[
  {
    "userId": 2,
    "fullName": "Nguyễn Thị Mai",
    "email": "mai.nguyen@clearance.com",
    "phoneNumber": "+84912000002",
    "role": "TransportSpecialist",
    "isActive": true,
    "createdAt": "2026-10-08T07:24:28+00:00"
  },
  {
    "userId": 10,
    "fullName": "Vũ Đức Long",
    "email": "long.vu@clearance.com",
    "phoneNumber": "+84912000008",
    "role": "TransportSpecialist",
    "isActive": true,
    "createdAt": "2026-10-08T07:24:29+00:00"
  }
]
```

---

## 6. Ngựa

Khách hàng khai báo ngựa của mình trước khi đặt chuyến. Khách chỉ thấy và sửa được ngựa của chính mình; nhân sự xem được tất cả nhưng không sửa.

Một con ngựa (`horse`) có dạng:

```json
{
  "horseId": 4,
  "ownerUserId": 5,
  "ownerName": "Jane Smith",
  "name": "Silver Wind",
  "microchipNumber": "982000412345699",
  "passportNumber": "704001000000009",
  "breed": "Thoroughbred",
  "gender": "Gelding",
  "dateOfBirth": "2019-05-20",
  "color": "Xám",
  "specialCareRequirements": "Dễ say xe, cần cỏ tươi",
  "photoUrl": "https://abcd.supabase.co/storage/v1/object/sign/files/horses/2026/10/6f316c....png?token=eyJ...",
  "isActive": true,
  "isPregnant": false,
  "expectedFoalingDate": null,
  "lastFoalingDate": null,
  "createdAt": "2026-10-08T07:24:30+00:00"
}
```

| Trường | Ý nghĩa |
|---|---|
| `horseId` | Mã ngựa, dùng khi đặt chuyến |
| `ownerUserId`, `ownerName` | Chủ ngựa |
| `microchipNumber` | Mã vi mạch gắn trên ngựa (15 chữ số), không trùng trong toàn hệ thống |
| `passportNumber` | Số hộ chiếu ngựa dạng UELN (15 ký tự, chữ hoa), không trùng trong toàn hệ thống |
| `breed` | Giống ngựa |
| `gender` | `Stallion`, `Mare` hoặc `Gelding` |
| `dateOfBirth` | Ngày sinh, dạng `2019-05-20` |
| `color` | Màu lông |
| `specialCareRequirements` | Yêu cầu chăm sóc đặc biệt, tài xế sẽ đọc được |
| `photoUrl` | Link ảnh, dùng được ngay; `null` nếu không có ảnh |
| `isActive` | `false` là ngựa đã bị xóa |
| `isPregnant`, `expectedFoalingDate` | Ngựa cái đang mang thai và ngày dự sinh (dạng `2026-12-20`) |
| `lastFoalingDate` | Lần đẻ gần nhất của ngựa cái; `null` nếu chưa đẻ / không khai |

### GET /api/horses — Danh sách ngựa

Khách hàng nhận về ngựa của mình. Nhân sự nhận về tất cả.

- **Ai gọi được:** Mọi người đã đăng nhập

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `ownerUserId` | số | Không | Chỉ có tác dụng với nhân sự: lọc theo chủ ngựa |
| `includeInactive` | true / false | Không | `true` để lấy cả ngựa đã xóa (mặc định `false`) |
| `search` | chuỗi | Không | Từ khóa tìm kiếm |
| `sort` | chuỗi | Không | Trường sắp xếp, thêm `-` phía trước để giảm dần |
| `page` | số | Không | Trang, bắt đầu từ 1 (mặc định 1) |
| `size` | số | Không | Số dòng mỗi trang, 1–100 (mặc định 10) |

Ví dụ: `GET /api/horses?search=silver&page=1&size=10`. `search` tìm theo tên, mã vi mạch, số hộ chiếu.

Response `200`: `data` là mảng ngựa (dạng như trên), kèm `pagination`.

### GET /api/horses/{id} — Chi tiết một con ngựa

Lấy một con ngựa theo mã.

- **Ai gọi được:** Chủ ngựa, hoặc nhân sự

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã ngựa (`horseId`) |

Response `200`: `data` là một con ngựa.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 403 | `You can only view your own horses.` | Khách xem ngựa của người khác |
| 404 | `Horse with ID 9999 was not found.` | Không có ngựa với mã đó |

### POST /api/horses — Thêm ngựa

Khai báo một con ngựa mới của khách đang đăng nhập. Chủ ngựa luôn là người đang đăng nhập.

- **Ai gọi được:** Customer
- **Dữ liệu gửi lên:** `multipart/form-data` (xem mục 2.5)

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `name` | chuỗi | Có | Tên ngựa, tối đa 100 ký tự |
| `microchipNumber` | chuỗi | Có | Mã vi mạch theo chuẩn ISO 11784/11785: **đúng 15 chữ số**, ví dụ `982000412345699`. Không trùng |
| `passportNumber` | chuỗi | Có | Số hộ chiếu dạng UELN: **đúng 15 ký tự** = 3 số mã quốc gia + 3 số mã cơ quan cấp + 9 chữ/số định danh, ví dụ `704001000000009` hoặc `250001A12345678`. Không trùng; chữ thường tự đổi thành chữ hoa |
| `breed` | chuỗi | Không | Giống ngựa; bỏ trống thì mặc định `Thoroughbred` |
| `gender` | chuỗi | Có | `Stallion`, `Mare` hoặc `Gelding` |
| `dateOfBirth` | ngày | Có | Dạng `2019-05-20`, phải ở quá khứ |
| `color` | chuỗi | Có | Màu lông, tối đa 30 ký tự |
| `specialCareRequirements` | chuỗi | Không | Yêu cầu chăm sóc đặc biệt |
| `isPregnant` | true / false | Không | Chỉ ngựa cái (`Mare`). Mặc định `false` |
| `expectedFoalingDate` | ngày | Có khi `isPregnant` là `true` | Ngày dự sinh, dạng `2026-12-20` |
| `lastFoalingDate` | ngày | Không | Chỉ ngựa cái: lần đẻ gần nhất, sau ngày sinh và không ở tương lai |
| `photo` | file | Không | Ảnh ngựa: `jpg`, `jpeg`, `png`, tối đa 10 MB |

Ví dụ code: xem mục 2.5.

Response `201`: `data` là con ngựa vừa tạo, `photoUrl` là link ảnh vừa tải lên.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Validation failed`, trong `errors` có `MicrochipNumber must be exactly 15 digits (ISO 11784/11785).` | Mã vi mạch không đủ/thừa số hoặc có chữ |
| 400 | `Validation failed`, trong `errors` có `PassportNumber must be a 15-character UELN: 6 digits followed by 9 letters or digits.` | Số hộ chiếu sai định dạng UELN |
| 400 | `Microchip number '...' is already registered.` | Mã vi mạch đã có trong hệ thống |
| 400 | `Passport number '...' is already registered.` | Số hộ chiếu đã có trong hệ thống |
| 400 | `DateOfBirth must be in the past.` | Ngày sinh ở tương lai |
| 400 | `Only a mare can be pregnant or have a foaling date.` | Khai mang thai / ngày đẻ cho ngựa đực hoặc ngựa thiến |
| 400 | `ExpectedFoalingDate is required when the mare is pregnant.` | Khai mang thai mà không có ngày dự sinh |
| 400 | `LastFoalingDate must be after DateOfBirth and not in the future.` | Ngày đẻ gần nhất không hợp lệ |
| 400 | `Files in category 'horses' must be images (.jpg, .jpeg, .png).` | Gửi PDF làm ảnh |
| 400 | `The file content does not match its extension '.png'.` | File không phải ảnh thật |

### PUT /api/horses/{id} — Sửa ngựa

Sửa thông tin một con ngựa. Phải gửi lại **đủ các trường bắt buộc**, kể cả trường không đổi.

- **Ai gọi được:** Chủ ngựa (Customer)
- **Dữ liệu gửi lên:** `multipart/form-data` (xem mục 2.5)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã ngựa |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `name` | chuỗi | Có | Tên ngựa, tối đa 100 ký tự |
| `microchipNumber` | chuỗi | Có | Mã vi mạch theo chuẩn ISO 11784/11785: **đúng 15 chữ số**, ví dụ `982000412345699`. Không trùng |
| `passportNumber` | chuỗi | Có | Số hộ chiếu dạng UELN: **đúng 15 ký tự** = 3 số mã quốc gia + 3 số mã cơ quan cấp + 9 chữ/số định danh, ví dụ `704001000000009` hoặc `250001A12345678`. Không trùng; chữ thường tự đổi thành chữ hoa |
| `breed` | chuỗi | Không | Giống ngựa; bỏ trống thì mặc định `Thoroughbred` |
| `gender` | chuỗi | Có | `Stallion`, `Mare` hoặc `Gelding` |
| `dateOfBirth` | ngày | Có | Dạng `2019-05-20`, phải ở quá khứ |
| `color` | chuỗi | Có | Màu lông, tối đa 30 ký tự |
| `specialCareRequirements` | chuỗi | Không | Yêu cầu chăm sóc đặc biệt |
| `isPregnant` | true / false | Không | Chỉ ngựa cái (`Mare`). Mặc định `false` |
| `expectedFoalingDate` | ngày | Có khi `isPregnant` là `true` | Ngày dự sinh, dạng `2026-12-20` |
| `lastFoalingDate` | ngày | Không | Chỉ ngựa cái: lần đẻ gần nhất, sau ngày sinh và không ở tương lai |
| `photo` | file | Không | Ảnh ngựa: `jpg`, `jpeg`, `png`, tối đa 10 MB |
| `removePhoto` | true / false | Không | `true` để xóa ảnh đang có |

Response `200`: `data` là con ngựa sau khi sửa.

Quy tắc về ảnh khi sửa:

- Gửi `photo`: ảnh mới thay ảnh cũ.
- Không gửi `photo`: giữ nguyên ảnh đang có.
- Gửi `removePhoto=true` (và không gửi `photo`): xóa ảnh.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 403 | `You can only modify your own horses.` | Sửa ngựa của người khác |
| 400 | `Validation failed`, trong `errors` có `MicrochipNumber must be exactly 15 digits (ISO 11784/11785).` | Mã vi mạch không đủ/thừa số hoặc có chữ |
| 400 | `Validation failed`, trong `errors` có `PassportNumber must be a 15-character UELN: 6 digits followed by 9 letters or digits.` | Số hộ chiếu sai định dạng UELN |
| 400 | `Microchip number '...' is already registered.` | Mã vi mạch đã có trong hệ thống |
| 400 | `Passport number '...' is already registered.` | Số hộ chiếu đã có trong hệ thống |
| 400 | `DateOfBirth must be in the past.` | Ngày sinh ở tương lai |

### DELETE /api/horses/{id} — Xóa ngựa

Xóa một con ngựa khỏi danh sách. Dữ liệu cũ (đơn, chuyến đã đi) vẫn được giữ.

- **Ai gọi được:** Chủ ngựa (Customer)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã ngựa |

Response `200`: `data` là `null`, `message` là `Horse deleted successfully`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `This horse is part of a booking that is still in progress and cannot be deleted.` | Ngựa đang nằm trong một đơn chưa kết thúc |
| 403 | `You can only modify your own horses.` | Xóa ngựa của người khác |

### Hồ sơ thú y của ngựa

Mỗi con ngựa có một "tủ hồ sơ" riêng: hộ chiếu, kết quả xét nghiệm, giấy tiêm phòng... Chủ ngựa bổ sung bất cứ lúc nào, **không bắt buộc khi thêm ngựa** và không cần ai duyệt.

Đừng nhầm với giấy tờ trong **hồ sơ kiểm dịch** (mục 8): đó là giấy tờ nộp cho từng đơn vận chuyển và được chuyên viên duyệt. Hồ sơ thú y của ngựa chỉ là nơi khách lưu sẵn giấy tờ của mình.

Một hồ sơ có dạng:

```json
{
  "documentId": 5,
  "horseId": 1,
  "docTypeId": 6,
  "docTypeCode": "GLANDERS_TEST",
  "docTypeName": "Xét nghiệm bệnh tỵ (Glanders)",
  "documentNumber": "GL-2026-77",
  "issuedBy": "Phòng xét nghiệm NAVETCO",
  "issueDate": "2026-10-10",
  "expiryDate": "2026-11-04",
  "validity": "ExpiringSoon",
  "fileUrl": "https://abcd.supabase.co/storage/v1/object/sign/files/documents/2026/10/9a1c....pdf?token=eyJ...",
  "notes": "CFT âm tính",
  "uploadedByUserId": 5,
  "uploadedByName": "Jane Smith",
  "uploadedAt": "2026-10-15T08:30:00+00:00"
}
```

| Trường | Ý nghĩa |
|---|---|
| `documentId` | Mã hồ sơ, dùng khi xóa |
| `docTypeId`, `docTypeCode`, `docTypeName` | Loại giấy tờ (xem danh mục ở mục 13) |
| `documentNumber` | Số giấy tờ, có thể trống |
| `issuedBy` | Bác sĩ thú y / phòng xét nghiệm / cơ quan cấp, có thể trống |
| `issueDate`, `expiryDate` | Ngày cấp, ngày hết hạn (dạng `2026-10-10`), có thể trống |
| `validity` | Tình trạng hiệu lực, server tự tính từ `expiryDate`: `Valid` (còn hạn), `ExpiringSoon` (hết hạn trong 30 ngày tới), `Expired` (đã hết hạn), `NoExpiry` (không có ngày hết hạn) |
| `fileUrl` | Link xem file, có hạn khoảng 60 phút. Hết hạn thì gọi lại API danh sách để lấy link mới |
| `notes` | Ghi chú, có thể trống |

### GET /api/horses/{id}/documents — Danh sách hồ sơ thú y của ngựa

Lấy toàn bộ hồ sơ của một con ngựa, mới nhất trước. Không phân trang.

- **Ai gọi được:** Chủ ngựa, hoặc nhân sự

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã ngựa (`horseId`) |

Response `200`: `data` là một mảng hồ sơ (dạng ở trên). Ngựa chưa có hồ sơ nào thì là `[]`.

Ví dụ code:

```js
const res = await api.get(`/horses/${horseId}/documents`);
const documents = res.data.data;
```

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 403 | `You can only view your own horses.` | Khách xem hồ sơ ngựa của người khác |
| 404 | `Horse with ID 9999 was not found.` | Không có ngựa với mã đó |

### POST /api/horses/{id}/documents — Thêm một hồ sơ thú y

Thêm **một** hồ sơ cho ngựa. Muốn thêm nhiều hồ sơ một lúc thì gọi API này nhiều lần, mỗi lần một hồ sơ.

- **Ai gọi được:** Chủ ngựa (Customer)
- **Dữ liệu gửi lên:** `multipart/form-data` (xem mục 2.5)

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `docTypeId` | số | Có | Loại giấy tờ, lấy từ `GET /api/document-types/lookup` |
| `file` | file | Có | `pdf`, `jpg`, `jpeg`, `png`, tối đa 10 MB |
| `documentNumber` | chuỗi | Không | Số giấy tờ, tối đa 100 ký tự |
| `issuedBy` | chuỗi | Không | Nơi cấp, tối đa 200 ký tự |
| `issueDate` | ngày | Không | Dạng `2026-10-10`, không được ở tương lai |
| `expiryDate` | ngày | Không | Dạng `2026-11-04`, không được trước `issueDate` |
| `notes` | chuỗi | Không | Ghi chú, tối đa 500 ký tự |

Ví dụ code:

```js
const form = new FormData();
form.append("docTypeId", 6);
form.append("documentNumber", "GL-2026-77");
form.append("issuedBy", "Phòng xét nghiệm NAVETCO");
form.append("issueDate", "2026-10-10");
form.append("expiryDate", "2026-11-04");
form.append("file", fileInput.files[0]);

const res = await api.post(`/horses/${horseId}/documents`, form, {
  headers: { "Content-Type": "multipart/form-data" },
});
const created = res.data.data;
```

Response `201`: `data` là hồ sơ vừa thêm.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Validation failed`, trong `errors` có `File is required.` | Không gửi file |
| 400 | `Document type with ID 99 does not exist.` | `docTypeId` sai |
| 400 | `IssueDate cannot be in the future.` | Ngày cấp ở tương lai |
| 400 | `ExpiryDate must be on or after IssueDate.` | Ngày hết hạn trước ngày cấp |
| 400 | `File type '.docx' is not allowed. Allowed types: ...` | Sai loại file |
| 400 | `The file is too large. Maximum size is 10 MB.` | File quá 10 MB |
| 403 | `You can only modify your own horses.` | Thêm hồ sơ cho ngựa của người khác |
| 404 | `Horse with ID 9999 was not found.` | Không có ngựa với mã đó, hoặc ngựa đã bị xóa |

### DELETE /api/horses/{id}/documents/{documentId} — Xóa một hồ sơ thú y

- **Ai gọi được:** Chủ ngựa (Customer)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã ngựa |
| `documentId` | Mã hồ sơ cần xóa |

Response `200`: `data` là `null`, `message` là `Horse document deleted successfully`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 403 | `You can only modify your own horses.` | Xóa hồ sơ ngựa của người khác |
| 404 | `Document with ID 12 was not found for horse 1.` | Hồ sơ không có hoặc không thuộc con ngựa này |

---

## 6b. Bản đồ: tìm địa chỉ, chỉ đường

Dùng cho component bản đồ (FE-01): ô tìm địa chỉ có gợi ý, ghim điểm trên bản đồ, vẽ tuyến. **Không dùng Google Maps** (Việt Nam và Trung Quốc nằm trong danh sách cấm của Google Maps Platform). Backend tự chọn nhà cung cấp:

| Việc | Trong Việt Nam | Ngoài Việt Nam |
|---|---|---|
| Tìm địa chỉ, tọa độ → địa chỉ | Goong | Nominatim (OpenStreetMap) |
| Chỉ đường | Goong (xe tải), khi **cả hai đầu chặng** ở Việt Nam | OSRM (OpenStreetMap) |

Frontend chỉ gọi 3 API dưới đây, không gọi thẳng nhà cung cấp nào và không cần khóa API. Bản đồ nền dùng **Leaflet + OpenStreetMap**.

Vài điều nên biết:

- Kết quả được lưu đệm 7 ngày, nên lần gọi sau với cùng địa chỉ / tọa độ trả về ngay.
- Nominatim chỉ cho **1 request / giây**: chờ 400 ms sau khi người dùng ngừng gõ rồi mới gọi `search` (debounce), đừng gọi theo từng phím.
- Nhà cung cấp chỉ đường bị lỗi thì chặng đó được **ước tính** theo đường chim bay × 1,3, thời gian theo tốc độ xe chở ngựa trung bình; chặng đó có `isEstimate: true` và đường vẽ là đoạn thẳng. Nên hiện chú thích "khoảng cách ước tính".
- Tọa độ luôn là số thập phân: `lat` (vĩ độ, −90 … 90), `lng` (kinh độ, −180 … 180). Mã nước là mã ISO 2 chữ in hoa (`VN`, `CN`, `KH`...).

### GET /api/geo/search — Gợi ý địa chỉ

- **Ai gọi được:** Mọi người đã đăng nhập

Tham số query:

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `q` | chuỗi | Có | Chữ người dùng gõ, 2–200 ký tự |
| `country` | chuỗi | Không | Mã nước 2 chữ để thu hẹp tìm kiếm. `VN` dùng Goong; bỏ trống = tìm khắp nơi |

Ví dụ: `GET /api/geo/search?q=Nam%20Ninh`

Response `200`: `data` là mảng gợi ý (tối đa 5), có thể rỗng:

```json
{
  "success": true,
  "message": "Request processed successfully",
  "data": [
    {
      "displayName": "Nam Ninh, Quảng Tây, Trung Quốc",
      "lat": 22.8158,
      "lng": 108.318,
      "countryCode": "CN",
      "provider": "Nominatim"
    }
  ],
  "errors": null
}
```

| Trường | Ý nghĩa |
|---|---|
| `displayName` | Địa chỉ đầy đủ để hiện trong danh sách gợi ý và điền vào ô địa chỉ |
| `lat`, `lng` | Tọa độ để ghim lên bản đồ và gửi khi tạo đơn |
| `countryCode` | Nước của địa điểm |
| `provider` | Nhà cung cấp trả kết quả (`Goong`, `Nominatim`), chỉ để tham khảo |

Ví dụ ô tìm có debounce:

```js
let timer;
function onAddressInput(text) {
  clearTimeout(timer);
  if (text.trim().length < 2) return;
  timer = setTimeout(async () => {
    const res = await api.get("/api/geo/search", { params: { q: text } });
    setSuggestions(res.data.data);
  }, 400);
}
```

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Validation failed` (`q must have at least 2 characters.`) | `q` quá ngắn / quá dài |
| 400 | `Validation failed` (`country must be a 2-letter ISO code such as VN.`) | `country` sai dạng |
| 401 | | Chưa đăng nhập |

### GET /api/geo/reverse — Tọa độ → địa chỉ

Khi người dùng bấm lên bản đồ hoặc kéo ghim: lấy địa chỉ và nước của điểm đó để điền vào ô địa chỉ.

- **Ai gọi được:** Mọi người đã đăng nhập

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `lat` | số | Có | Vĩ độ, −90 … 90 |
| `lng` | số | Có | Kinh độ, −180 … 180 |

Ví dụ: `GET /api/geo/reverse?lat=10.7769&lng=106.7009`

Response `200`: `data` là một địa điểm (dạng như `search`). `lat`, `lng` giữ đúng điểm người dùng đã chọn.

```json
{
  "displayName": "Khu phố 3, Phường Sài Gòn, Thành phố Hồ Chí Minh, 71016, Việt Nam",
  "lat": 10.7769,
  "lng": 106.7009,
  "countryCode": "VN",
  "provider": "Nominatim"
}
```

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Validation failed` (`lat must be between -90 and 90.`) | Tọa độ ngoài khoảng |
| 404 | `No address was found at this location. Pick a point on land or search for the address.` | Bấm giữa biển, hoặc nhà cung cấp tạm lỗi |

### POST /api/geo/route — Chỉ đường qua nhiều điểm

Tính đường bộ đi qua các điểm **theo đúng thứ tự gửi lên**: quãng đường, thời gian lái từng chặng, tổng, và đường để vẽ. Dùng để hiện quãng đường A → B khi tạo đơn, và để vẽ tuyến có nhiều mốc khi lập tuyến.

- **Ai gọi được:** Mọi người đã đăng nhập
- **Dữ liệu gửi lên:** JSON

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `points` | mảng | Có | 2–25 điểm theo thứ tự đi |
| `points[].lat`, `points[].lng` | số | Có | Tọa độ |
| `points[].countryCode` | chuỗi | Không | Nước của điểm (lấy từ kết quả `search` / `reverse`). Gửi kèm thì backend khỏi tra lại, nhanh hơn |
| `includeCountries` | true/false | Không (mặc định `false`) | `true` = trả thêm các nước tuyến đi qua theo thứ tự. Lần đầu có thể mất vài giây đến vài chục giây với tuyến dài (mỗi điểm phải tra nước, 1 lần / giây); các lần sau lấy từ bộ nhớ đệm |

Ví dụ request:

```json
{
  "points": [
    { "lat": 21.0285, "lng": 105.8542, "countryCode": "VN" },
    { "lat": 22.817, "lng": 108.3665, "countryCode": "CN" }
  ],
  "includeCountries": true
}
```

Response `200`:

```json
{
  "success": true,
  "message": "Request processed successfully",
  "data": {
    "legs": [
      {
        "fromIndex": 0,
        "toIndex": 1,
        "distanceKm": 385.14,
        "durationHours": 4.92,
        "provider": "Osrm",
        "isEstimate": false,
        "polyline": [[21.0285, 105.8542], [21.0312, 105.8601], [22.817, 108.3665]]
      }
    ],
    "totalDistanceKm": 385.14,
    "totalDurationHours": 4.92,
    "isEstimate": false,
    "polyline": [[21.0285, 105.8542], [21.0312, 105.8601], [22.817, 108.3665]],
    "countryCodes": ["VN", "CN"]
  },
  "errors": null
}
```

(Mảng `polyline` thật có hàng trăm đến vài nghìn điểm, ở đây rút gọn.)

| Trường | Ý nghĩa |
|---|---|
| `legs[]` | Từng chặng giữa hai điểm liên tiếp: `fromIndex` → `toIndex` là vị trí trong mảng `points` gửi lên |
| `legs[].distanceKm`, `durationHours` | Quãng đường (km) và thời gian lái (giờ) của chặng, chưa tính nghỉ / qua cửa khẩu. Dùng để hiện nhãn trên từng đoạn đường |
| `legs[].provider` | `Goong`, `Osrm`, hoặc `Estimate` (ước tính khi nhà cung cấp lỗi) |
| `legs[].isEstimate` | `true` = chặng ước tính theo đường chim bay |
| `legs[].polyline` | Đường vẽ của chặng: mảng `[vĩ độ, kinh độ]` |
| `totalDistanceKm`, `totalDurationHours` | Tổng các chặng |
| `isEstimate` | `true` nếu có ít nhất một chặng ước tính |
| `polyline` | Đường vẽ cả tuyến, tối đa 2000 điểm |
| `countryCodes` | Các nước đi qua theo thứ tự (chỉ có khi `includeCountries: true`, ngược lại `null`) |

Vẽ bằng react-leaflet:

```jsx
import { MapContainer, TileLayer, Polyline } from "react-leaflet";

<MapContainer center={[16, 106]} zoom={5} style={{ height: 320 }}>
  <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
             attribution="&copy; OpenStreetMap contributors" />
  <Polyline positions={route.polyline} />
</MapContainer>
```

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Validation failed` (`points must have at least 2 points.`) | Ít hơn 2 điểm hoặc quá 25 điểm |
| 400 | `Validation failed` (`lat must be between -90 and 90.`) | Tọa độ sai |
| 401 | | Chưa đăng nhập |

---

## 7. Flow 1 – Đơn vận chuyển

Khách hàng gửi một **đơn vận chuyển** (booking) cho một hoặc nhiều con ngựa, kèm **khoảng giá ước tính**. Quản lý **duyệt sơ bộ** (đơn khả thi) và chọn chuyên viên thủ tục phụ trách; điều phối viên **lập tuyến dự kiến** trên bản đồ (mục 7b); quản lý **duyệt tuyến**; sau đó khách nhận **báo giá chốt**, chấp nhận và **đặt cọc** (mục 19). Khi khách cọc xong, hệ thống tự mở một hồ sơ kiểm dịch cho mỗi con ngựa (Flow 2).

Vòng đời của một đơn:

```
Submitted ──(quản lý duyệt sơ bộ)──> PreApproved ──(điều phối lập tuyến, quản lý duyệt tuyến)──> RoutePlanned ──(server tính ngay)──> Quoted
    ──(khách chấp nhận báo giá)──> AwaitingDeposit ──(khách cọc)──> Approved ──(xếp chuyến)──> Assigned ──(giao xong)──> Completed
                                                                                                                       │
                                                                                                                       └─(còn phát sinh)─> AwaitingFinalPayment ─(trả xong)─> Completed
Quoted / AwaitingDeposit ──(quá hạn báo giá mà chưa chấp nhận hoặc chưa cọc)──> QuoteExpired   (không mất phí)
Quoted ──(khách từ chối báo giá)──> Cancelled   (không mất phí)
Submitted / PreApproved / RoutePlanned ──(quản lý từ chối)──> Rejected
Mọi trạng thái trước khi lên chuyến ──(khách hủy)──> Cancelled   (đã trả tiền thì tự hoàn, xem mục 19)
Submitted ──(quá 48 giờ không ai duyệt)──> Expired
```

**Báo giá chốt (R-10) và hợp đồng (R-17).** Ngay khi quản lý duyệt tuyến, server tính **giá chốt** theo quãng đường và các nước quá cảnh của tuyến (giữ nguyên bảng giá và mức hỏa tốc lúc khách gửi đơn), cộng **phí bảo hiểm toàn giá trị** cho từng con chọn gói `Full` (`Insurance.PremiumPercent`% giá trị thỏa thuận, dòng `INS_FULL`). Giá chốt nằm ở `estimatedCost` + `quoteLines`; server tạo kèm **hợp đồng PDF** bản nháp (mục 7c); đơn chuyển `Quoted` và khách nhận thông báo + email. Khách có `Booking.QuoteAcceptDays` ngày (mặc định 3, xem `GET /api/policy`) tính tới `quoteExpiresAt` để **chấp nhận và đặt cọc**; còn 1 ngày thì server gửi email nhắc một lần. Quá hạn (chưa chấp nhận, hoặc chấp nhận mà chưa cọc) thì đơn tự đóng `QuoteExpired`, không mất phí. Trong lúc đơn `Quoted`, công ty vẫn có thể lập lại tuyến; duyệt bản mới thì báo giá được tính lại với hạn mới.

Đơn tạo trước R-10 (không có `quoteExpiresAt`) vẫn theo luật cũ: quá `Payment.DepositDueHours` giờ sau khi duyệt mà chưa cọc thì `Expired`.

**Hạn nộp giấy (BE-06, Chính sách mục 6).** Khi khách cọc xong (đơn `Approved`), mỗi con ngựa có một hồ sơ kiểm dịch; danh sách giấy cần nộp tự tính theo **nước đi + các nước quá cảnh của tuyến đã duyệt + nước đến** (mục 8). Khách có `Dossier.SubmitDays` ngày (mặc định 7) tính từ lúc cọc, hạn ở `docsDueAt`. Server gửi email nhắc khi còn 3 ngày và khi còn 1 ngày (chỉ cho ngựa còn thiếu giấy bắt buộc chưa nộp hoặc bị từ chối; giấy đang chờ duyệt coi như đã nộp). Quá hạn mà vẫn thiếu: đơn bị hủy, hoàn tiền **như khách tự hủy sau khi cọc** (`Payment.CancelRefundPercent`% tiền cọc + 100% phần còn lại đã trả), email cho khách. Nếu ngựa đã được xếp lên chuyến thì không tự hủy mà báo quản lý xử lý.

**Đổi tuyến do công ty (BE-06, Chính sách mục 2).** Sau khi khách đã chấp nhận báo giá (`AwaitingDeposit`, `Approved`, `Assigned` và ngựa chưa rời điểm đón), điều phối vẫn lập được phiên bản tuyến mới và quản lý duyệt như thường; **giá chốt giữ nguyên**, đơn giữ trạng thái. Tuyến mới đi qua nước mới (khi đơn đang làm giấy) thì danh sách giấy tự thêm giấy quá cảnh của nước đó, hạn nộp giấy được gia hạn thêm `Dossier.SubmitDays` ngày, khách nhận thông báo + email, và tới `routeChangeCancelUntil` khách được **hủy và hoàn 100%** (`POST /api/bookings/{id}/cancel-route-change`).

**Khách đổi điểm đón / trả (BE-06).** Trước khi trả bất kỳ khoản nào, khách đổi được bằng `PUT /api/bookings/{id}/locations`: khoảng giá tính lại; nếu đã có tuyến / báo giá thì bị rút lại, đơn về `PreApproved` để lập tuyến và báo giá mới. Đã trả tiền thì khách liên hệ công ty.

Thứ tự gọi thông thường:

1. Khách: `GET /api/horses` để chọn ngựa.
2. Khách: `POST /api/bookings/quote-preview` để xem giá (gọi lại mỗi khi khách đổi lựa chọn).
3. Khách: `POST /api/bookings` để gửi đơn.
4. Quản lý: `GET /api/bookings?status=Submitted` để xem đơn chờ duyệt, `GET /api/users/staff?role=TransportSpecialist` để lấy danh sách chuyên viên.
5. Quản lý: `POST /api/bookings/{id}/approve` (duyệt sơ bộ) hoặc `POST /api/bookings/{id}/reject`.
6. Điều phối: `GET /api/bookings?status=PreApproved` rồi lập tuyến (mục 7b) và trình; quản lý duyệt tuyến.
7. Khách: `GET /api/bookings/{id}` xem báo giá chốt (`customerStage = QuoteReady`), rồi `POST /api/bookings/{id}/accept-quote` (hoặc `decline-quote`).
8. Khách: `GET /api/bookings/{id}/payments` và `POST /api/bookings/{id}/payments` để đặt cọc qua VNPay (mục 19) trước `quoteExpiresAt`.

Ai thấy đơn nào: khách thấy đơn của mình; chuyên viên thấy đơn mình phụ trách; `LogisticsManager`, `FleetCoordinator`, `Admin` thấy tất cả; tài xế không xem đơn (gọi vào nhận 403).

### POST /api/bookings/estimate-distance — Ước tính quãng đường đường bộ

Backend tự động tính toán quãng đường (km) và thời gian di chuyển (giờ) dựa trên địa điểm đón và giao (hỗ trợ Goong API, OSRM và bảng cự ly chuẩn của hệ thống).

- **Ai gọi được:** Mọi người đã đăng nhập
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `pickupCountryCode` | chuỗi | Không | Mã quốc gia đón (mặc định `VN`) |
| `pickupCity` | chuỗi | Không | Tỉnh / Thành phố đón (ví dụ: `TP. Hồ Chí Minh`, `Hà Nội`) |
| `pickupAddress` | chuỗi | Không | Địa chỉ đón chi tiết |
| `dropoffCountryCode` | chuỗi | Có | Mã quốc gia giao (2 chữ cái, ví dụ: `KH`, `TH`, `LA`, `CN`) |
| `dropoffCity` | chuỗi | Không | Tỉnh / Thành phố giao (ví dụ: `Phnom Penh`, `Bangkok`) |
| `dropoffAddress` | chuỗi | Không | Địa chỉ giao chi tiết |

Response `200` — phần `data`:
```json
{
  "distanceKm": 250,
  "estimatedHours": 4.5,
  "routeSummary": "250 km (TP.HCM/Miền Nam → Cửa khẩu Mộc Bài/Bavet → Phnom Penh)",
  "source": "RouteMatrix"
}
```

### POST /api/bookings/quote-preview — Xem giá trước khi gửi đơn

Tính **khoảng giá ước tính** cho một đơn dự kiến. Không lưu gì vào hệ thống, gọi bao nhiêu lần cũng được.

**Khoảng giá (R-10).** Lúc khách gửi yêu cầu, công ty chưa lập tuyến nên chỉ báo **khoảng giá** `quoteMin` → `quoteMax`; **giá chốt** gửi sau khi công ty lập tuyến (khách chưa phải thanh toán gì). Form nên hiện: "Giá ước tính: {quoteMin} – {quoteMax} USD. Giá chốt gửi sau khi công ty lập tuyến; chưa phải thanh toán."

- Đường bộ, có tọa độ điểm đón / trả (`pickupLat`, `pickupLng`, `dropoffLat`, `dropoffLng` từ component bản đồ): quãng đường thật A → B và **các nước đường đi qua** lấy từ bản đồ (mục 6b). `quoteMin` = giá theo quãng đường thật, **chưa có** phí quá cảnh; `quoteMax` = quãng đường × 1,25 (đi vòng, mốc nghỉ) + phí thủ tục cho các nước đường A → B đi qua (tối đa 2 nước).
- **Khách không còn nhập nước quá cảnh** (`transitCountryCodes` bị bỏ khỏi request; gửi lên cũng bị bỏ qua). Không có tọa độ (app cũ) thì quãng đường tính như trước (`distanceKm` hoặc theo tên thành phố) và không tính nước quá cảnh.
- Lần đầu tính một tuyến mới có thể mất **10–40 giây** (phải tra nước theo từng đoạn đường, nhà cung cấp chỉ cho 1 lượt / giây); các lần sau trả về ngay. Nên hiện vòng xoay "Đang tính giá..." và tránh gọi lại khi người dùng chưa đổi điểm.
- Đường hàng không: `quoteMin` = `quoteMax`.

**Khách không chọn ngày khởi hành.** Khách chỉ cho biết ngựa sẵn sàng được đón trong khoảng ngày nào (`pickupWindowStart` → `pickupWindowEnd`) và hạn giao trễ nhất (`deliveryDeadline`). Ngày chạy thật do điều phối viên chốt khi lập chuyến.

- **Ngày pickup sớm nhất:** hôm nay + `leadTimeDays` ngày = 2 ngày duyệt + 2 ngày đặt cọc + **số ngày làm hồ sơ của tuyến** (`clearanceDays`) + 3 ngày xếp chuyến. `clearanceDays` = giấy lâu nhất trong các quy định của nước đi (Export), các nước quá cảnh (Transit) và nước đến (Import), ví dụ VN → Campuchia 7 ngày, VN → Trung Quốc 21 ngày; tuyến chưa có số liệu thì 14 ngày. Server trả về `earliestPickupDate`; form nên khóa các ngày sớm hơn.
- **Nước quá cảnh** (`transitCountryCodes` trong response): server tự xác định từ bản đồ. Mỗi nước thêm một dòng phí `FEE_TRANSIT`, thêm một lần qua cửa khẩu trong thời gian vận chuyển, và có thể làm `clearanceDays` dài hơn.
- **Thời gian vận chuyển** do server ước tính từ quãng đường và phương thức (`transit`), không lấy từ ngày khách nhập. Hạn giao phải từ `earliestDeliveryDeadline` trở đi (= đầu khoảng pickup + thời gian vận chuyển chậm nhất).
- **Phụ phí gấp (Express):** khi đầu khoảng pickup trước `expressUntilDate` (sát ngày sớm nhất, phải làm hồ sơ gấp) hoặc hạn giao dư ít hơn 24 giờ so với `earliestDeliveryDeadline`.
- Chưa chọn ngày thì cứ bỏ trống 3 trường ngày: server vẫn trả về `earliestPickupDate`, `expressUntilDate` và thời gian vận chuyển để form hiển thị gợi ý.

- **Ai gọi được:** Mọi người đã đăng nhập
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `transportMode` | chuỗi | Có | `Ground` (đường bộ) hoặc `Air` (hàng không) |
| `dropoffCountryCode` | chuỗi | Có | Mã nước đến, 2 chữ cái, ví dụ `CN`, `TH` |
| `pickupCity` | chuỗi | Không | Tỉnh/thành phố đón (dùng để tự động tính quãng đường đường bộ) |
| `pickupAddress` | chuỗi | Không | Địa chỉ đón chi tiết |
| `dropoffCity` | chuỗi | Không | Tỉnh/thành phố giao (dùng để tự động tính quãng đường đường bộ) |
| `dropoffAddress` | chuỗi | Không | Địa chỉ giao chi tiết |
| `distanceKm` | số nguyên | Không | Quãng đường (km). Nếu bỏ trống với `Ground`, server sẽ tự động tính |
| `requiresClimateControl` | true / false | Không | Cần khoang điều hòa (có phụ phí) |
| `declaredValue` | số | Không | Giá trị khai báo để mua bảo hiểm. Bỏ trống hoặc `null` là không mua |
| `pickupWindowStart` | ngày giờ | Không | Ngày đầu của khoảng ngựa sẵn sàng được đón; không sớm hơn `earliestPickupDate` |
| `pickupWindowEnd` | ngày giờ | Không | Ngày cuối của khoảng đón; không trước `pickupWindowStart`, khoảng dài tối đa 30 ngày. Bỏ trống thì bằng `pickupWindowStart` |
| `deliveryDeadline` | ngày giờ | Không | Hạn giao trễ nhất; không sớm hơn `earliestDeliveryDeadline` |
| `pickupLat`, `pickupLng` | số | Không (nên gửi) | Tọa độ điểm đón chọn trên bản đồ (`GET /api/geo/search` / `reverse`). Gửi đủ 4 tọa độ thì quãng đường và các nước đi qua lấy từ bản đồ |
| `dropoffLat`, `dropoffLng` | số | Không (nên gửi) | Tọa độ điểm trả |
| `horses` | mảng | Có | Mỗi phần tử là một con ngựa, chỉ cần `stallClass`: `Shared`, `Comfort` hoặc `Private` |

Gửi `pickupWindowEnd` hoặc `deliveryDeadline` thì phải gửi kèm `pickupWindowStart`.

Ví dụ request:

```json
{
  "transportMode": "Ground",
  "dropoffCountryCode": "CN",
  "distanceKm": 900,
  "requiresClimateControl": true,
  "declaredValue": 50000,
  "pickupWindowStart": "2026-11-25T00:00:00+07:00",
  "pickupWindowEnd": "2026-11-30T00:00:00+07:00",
  "deliveryDeadline": "2026-12-06T00:00:00+07:00",
  "horses": [
    {
      "stallClass": "Shared"
    },
    {
      "stallClass": "Private"
    }
  ]
}
```

Response `200` — phần `data`:

```json
{
  "lines": [
    {
      "code": "FREIGHT_SHARED",
      "name": "Cước vận chuyển - Chuồng ghép (Shared) - 3 ngựa / pallet",
      "quantity": 1,
      "unitPrice": 900,
      "amount": 900
    },
    {
      "code": "FREIGHT_PRIVATE",
      "name": "Cước vận chuyển - Chuồng riêng (Private) - 1 ngựa / pallet",
      "quantity": 1,
      "unitPrice": 1980,
      "amount": 1980
    },
    {
      "code": "DISC_VOL_2",
      "name": "Chiết khấu 2-3 ngựa",
      "quantity": 1,
      "unitPrice": -144,
      "amount": -144
    },
    {
      "code": "SUR_FUEL",
      "name": "Phụ phí nhiên liệu",
      "quantity": 1,
      "unitPrice": 218.88,
      "amount": 218.88
    }
  ],
  "total": 6788.48,
  "quoteMin": 6788.48,
  "quoteMax": 8120.6,
  "routeIsEstimate": false,
  "currencyCode": "USD",
  "horseCount": 2,
  "groomCount": 1,
  "transitDays": 2,
  "isExpress": false,
  "distanceKm": 900,
  "estimatedHours": 35.9,
  "routeSummary": "1354 km (VN → LA → TH)",
  "leadTimeDays": 28,
  "clearanceDays": 21,
  "transitCountryCodes": [],
  "earliestPickupDate": "2026-11-15",
  "expressUntilDate": "2026-11-22",
  "earliestDeliveryDeadline": "2026-11-26T10:18:00+00:00",
  "transit": {
    "drivingHours": 16.4,
    "restHours": 11.5,
    "overnightStops": 1,
    "borderHours": 6,
    "handlingHours": 2,
    "flightHours": 0,
    "totalHours": 35.9,
    "maxHours": 41.3,
    "transitDays": 2
  }
}
```

(`lines` trong ví dụ đã rút gọn còn 4 dòng; thực tế có đủ cước, chiết khấu, phụ phí, phí thủ tục, áp tải, bảo hiểm.)

| Trường | Ý nghĩa |
|---|---|
| `lines[]` | Các dòng của bảng giá: `name` (tên hiển thị), `quantity`, `unitPrice`, `amount` (thành tiền). Dòng có `amount` âm là chiết khấu |
| `total` | Tổng tiền theo quãng đường thật, chưa có phí quá cảnh (bằng `quoteMin`) |
| `quoteMin`, `quoteMax` | **Khoảng giá ước tính** để hiện cho khách (xem đầu mục) |
| `routeIsEstimate` | `true` khi nhà cung cấp bản đồ lỗi và quãng đường là ước tính theo đường chim bay; nên ghi chú "quãng đường ước tính" |
| `currencyCode` | Đơn vị tiền (`USD`) |
| `horseCount`, `groomCount` | Số ngựa và số người áp tải được tính |
| `transitDays` | Số ngày vận chuyển ước tính (dùng tính phí áp tải theo ngày) |
| `isExpress` | `true` nếu có phụ phí gấp (xem đầu mục). Server tự xác định |
| `distanceKm`, `routeSummary` | Quãng đường đường bộ và mô tả tuyến, ví dụ `1354 km (VN → LA → TH)` |
| `transitCountryCodes` | Các nước quá cảnh server xác định từ bản đồ (mảng rỗng khi đi thẳng hoặc không gửi tọa độ) |
| `estimatedHours` | Tổng thời gian vận chuyển dự kiến (giờ), bằng `transit.totalHours` |
| `leadTimeDays` | Số ngày cần từ hôm nay tới ngày pickup sớm nhất của tuyến này |
| `earliestPickupDate` | Ngày pickup sớm nhất được chọn (dạng `yyyy-MM-dd`) |
| `expressUntilDate` | Pickup trước ngày này thì có phụ phí gấp |
| `earliestDeliveryDeadline` | Hạn giao sớm nhất được chọn, tính từ `pickupWindowStart` (hoặc từ `earliestPickupDate` khi chưa chọn ngày) |
| `transit` | Thời gian vận chuyển tách theo thành phần (giờ): `drivingHours` lái xe, `restHours` nghỉ giữa chặng + nghỉ đêm (`overnightStops` số đêm), `borderHours` qua cửa khẩu, `handlingHours` xếp / dỡ ngựa, `flightHours` phần bay (có mặt trước giờ bay + bay + kiểm tra ở nơi đến), `totalHours` dự kiến, `maxHours` chậm nhất (đã cộng 15% dự phòng), `transitDays` số ngày |

Cách server ước tính thời gian (tham số chỉnh trong `appsettings.json`, section `Transit`):

- **Đường bộ:** giờ lái = km ÷ 55 km/h; cứ lái 4,5 giờ nghỉ 45 phút; một tài xế lái tối đa 9 giờ/ngày, hết giờ thì nghỉ đêm 10 giờ; mỗi cửa khẩu 6 giờ; xếp và dỡ ngựa 1 giờ mỗi đầu.
- **Đường bay:** 4 giờ xe ra sân bay + có mặt trước giờ bay 6 giờ + giờ bay (km ÷ 800 km/h, không có khoảng cách thì 8 giờ) + kiểm tra thú y / hải quan 4 giờ + 4 giờ xe tới nơi nhận + xếp / dỡ 2 giờ.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `No freight rate is configured ...` | Tuyến này chưa có trong bảng giá |
| 400 | `Validation failed` | Thiếu trường, mã nước không phải 2 chữ cái, không có con ngựa nào |
| 400 | `PickupWindowStart must be on or after 2026-11-15: a booking on this route needs about 28 days ...` | Chọn ngày pickup quá gần |
| 400 | `PickupWindowEnd must be on or after PickupWindowStart.` | Ngày cuối khoảng trước ngày đầu |
| 400 | `The pickup window cannot be longer than 30 days.` | Khoảng pickup quá dài |
| 400 | `DeliveryDeadline must be on or after ...: this route is estimated to take up to 41.3 hours ...` | Hạn giao không đủ thời gian vận chuyển |
| 400 | `PickupWindowStart is required when PickupWindowEnd or DeliveryDeadline is given.` | Gửi ngày cuối / hạn giao mà thiếu ngày đầu |

### POST /api/bookings — Gửi đơn vận chuyển

Tạo đơn mới ở trạng thái `Submitted`; các quản lý nhận thông báo có đơn mới.

R-10: server tính **khoảng giá ước tính** như `quote-preview` (gửi tọa độ để lấy quãng đường và các nước đi qua từ bản đồ) và lưu `quoteMin`, `quoteMax` vào đơn. Khách không nhập nước quá cảnh nữa; server ghi các nước đi qua tìm được vào `transitCountryCodes` của đơn (Manager vẫn sửa được khi duyệt). **Giá chốt chỉ có sau khi công ty duyệt tuyến**: đơn mới có `estimatedCost = 0` và `quoteLines` là các dòng của khoảng giá (mức thấp), tới khi đơn `Quoted`. Hiển thị cho khách "Giá ước tính: `quoteMin` – `quoteMax`".

**Bắt buộc đồng ý chính sách:** khách phải tích ô "Tôi đã đọc và đồng ý với Chính sách dịch vụ" và gửi `acceptPolicy = true` cùng `policyVersion` lấy từ `GET /api/policy` (`version`). Server lưu phiên bản và thời điểm đồng ý vào đơn (`policyVersion`, `policyAcceptedAt`).

- **Ai gọi được:** Customer
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `pickupAddress` | chuỗi | Có | Địa chỉ nhận ngựa chi tiết (số nhà, tên đường, trang trại), tối đa 255 ký tự |
| `pickupCity` | chuỗi | Không | Tỉnh / Thành phố đón (ví dụ `TP. Hồ Chí Minh`, `Hà Nội`) |
| `pickupCountryCode` | chuỗi | Có | Mã nước đi, 2 chữ cái, ví dụ `VN` |
| `dropoffAddress` | chuỗi | Có | Địa chỉ giao ngựa chi tiết (trường đua, trang trại đích) |
| `dropoffCity` | chuỗi | Không | Tỉnh / Thành phố giao (ví dụ `Phnom Penh`, `Bangkok`) |
| `dropoffCountryCode` | chuỗi | Có | Mã nước đến, 2 chữ cái (ví dụ `KH`, `TH`, `LA`, `CN`) |
| `transportMode` | chuỗi | Có | `Ground` (đường bộ) hoặc `Air` (hàng không) |
| `distanceKm` | số nguyên | Không | Quãng đường (km). Bỏ trống thì server tự động tính toán |
| `requiresClimateControl` | true / false | Không | Cần khoang điều hòa (có phụ phí) |
| `declaredValue` | số | Không | Tổng giá trị khai báo của cả đơn (cách cũ). **Nên khai theo từng con** bằng `horses[].declaredValue` (bên dưới); khi đó trường này bị bỏ qua và server lấy tổng các con |
| `pickupWindowStart` | ngày giờ | Có | Ngày đầu của khoảng ngựa sẵn sàng được đón (không phải ngày khởi hành); không sớm hơn `earliestPickupDate` của xem giá |
| `pickupWindowEnd` | ngày giờ | Có | Ngày cuối của khoảng đón; từ `pickupWindowStart` trở đi, khoảng dài tối đa 30 ngày |
| `deliveryDeadline` | ngày giờ | Có | Hạn giao trễ nhất; không sớm hơn `earliestDeliveryDeadline` của xem giá |
| `specialInstructions` | chuỗi | Không | Ghi chú cho công ty, tối đa 2000 ký tự |
| `pickupLat`, `pickupLng` | số | Không (nên gửi) | Tọa độ điểm đón chọn trên bản đồ (`GET /api/geo/search` / `reverse`). Gửi đủ 4 tọa độ thì quãng đường và các nước đi qua lấy từ bản đồ |
| `dropoffLat`, `dropoffLng` | số | Không (nên gửi) | Tọa độ điểm trả |
| `acceptPolicy` | true / false | Có (phải là `true`) | Khách đã tích đồng ý Chính sách dịch vụ |
| `policyVersion` | chuỗi | Có | Phiên bản chính sách khách đang xem, lấy từ `GET /api/policy` → `version` (ví dụ `1.0`) |
| `horses` | mảng | Có | Ít nhất 1 phần tử, xem bảng dưới |

Mỗi phần tử của `horses`:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `horseId` | số | Có | Mã ngựa của khách (lấy từ `GET /api/horses`) |
| `stallClass` | chuỗi | Có | `Shared`, `Comfort` hoặc `Private` |
| `notes` | chuỗi | Không | Ghi chú riêng cho con ngựa này, tối đa 300 ký tự |
| `declaredValue` | số | Không | Giá trị khai báo của con ngựa (USD). Khai cho một con thì **phải khai cho mọi con** trong đơn. Bảo hiểm và bồi thường tính trên giá trị này (sau khi Manager chốt) |
| `valueProofPath` | chuỗi | Có khi `declaredValue` ≥ `Insurance.ProofThresholdUsd` (50.000 USD) | Chứng từ giá trị: hóa đơn mua, giấy định giá của bác sĩ thú y / chuyên gia, hoặc kết quả đấu giá. Tải lên trước bằng `POST /api/files` với `category = documents`, rồi gửi `path` nhận được |
| `insurancePlan` | chuỗi | Không | Gói bảo hiểm của con ngựa: `None` (không mua, mặc định) hoặc `Full` (toàn giá trị). So sánh hai gói xem `GET /api/policy` (`insurance`) và Chính sách mục 8 |

Ví dụ request:

```json
{
  "pickupAddress": "Trang trại Yên Bài, Ba Vì, Hà Nội",
  "pickupCountryCode": "VN",
  "dropoffAddress": "Trường đua Tùng Hóa, Quảng Châu",
  "dropoffCountryCode": "CN",
  "pickupLat": 21.1415,
  "pickupLng": 105.4183,
  "dropoffLat": 23.1585,
  "dropoffLng": 113.3214,
  "acceptPolicy": true,
  "policyVersion": "1.0",
  "pickupWindowStart": "2026-11-25T00:00:00+07:00",
  "pickupWindowEnd": "2026-11-30T00:00:00+07:00",
  "deliveryDeadline": "2026-12-06T00:00:00+07:00",
  "transportMode": "Ground",
  "distanceKm": 900,
  "requiresClimateControl": true,
  "declaredValue": 50000,
  "specialInstructions": "Giữ cabin 17 độ",
  "horses": [
    {
      "horseId": 1,
      "stallClass": "Shared",
      "notes": "Chuồng bên trái"
    },
    {
      "horseId": 2,
      "stallClass": "Private"
    }
  ]
}
```

Response `201` — phần `data`:

```json
{
  "bookingId": 2,
  "bookingCode": "BKG-2026-0002",
  "customerUserId": 5,
  "customerName": "Jane Smith",
  "pickupAddress": "Trang trại Yên Bài, Ba Vì, Hà Nội",
  "pickupCountryCode": "VN",
  "dropoffAddress": "Trường đua Tùng Hóa, Quảng Châu",
  "dropoffCountryCode": "CN",
  "transitCountryCodes": [],
  "pickupWindowStart": "2026-11-24T17:00:00+00:00",
  "pickupWindowEnd": "2026-11-29T17:00:00+00:00",
  "deliveryDeadline": "2026-12-05T17:00:00+00:00",
  "transportMode": "Ground",
  "totalHorses": 2,
  "estimatedCost": 6788.48,
  "currencyCode": "USD",
  "status": "Submitted",
  "assignedSpecialistId": null,
  "assignedSpecialistName": null,
  "createdAt": "2026-10-08T07:24:30+00:00",
  "customerEmail": "jane.smith@racehorseowner.com",
  "customerPhone": "+84988111222",
  "specialInstructions": "Giữ cabin 17 độ",
  "distanceKm": 900,
  "estimatedTransitHours": 35.9,
  "isExpress": false,
  "requiresClimateControl": true,
  "declaredValue": 50000,
  "rejectionReason": null,
  "reviewedByUserId": null,
  "reviewedByName": null,
  "reviewedAt": null,
  "quoteLines": [
    {
      "code": "FREIGHT_SHARED",
      "name": "Cước vận chuyển - Chuồng ghép (Shared) - 3 ngựa / pallet",
      "quantity": 1,
      "unitPrice": 900,
      "amount": 900
    }
  ],
  "horses": [
    {
      "bookingHorseId": 3,
      "horseId": 1,
      "horseName": "Red Flash (Tia Chớp Đỏ)",
      "microchipNumber": "982000412345671",
      "stallClass": "Shared",
      "status": "Pending",
      "notes": "Chuồng bên trái",
      "dossierId": null,
      "dossierCode": null,
      "dossierStatus": null,
      "trips": []
    }
  ]
}
```

(`quoteLines` và `horses` trong ví dụ response đã rút gọn còn 1 phần tử.) Ý nghĩa các trường xem ở `GET /api/bookings/{id}`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `You must accept the service policy to send a request.` | Không gửi `acceptPolicy = true` |
| 400 | `The service policy has changed (current version 1.1). Please read the latest policy and accept it again.` | `policyVersion` thiếu hoặc khác phiên bản hiện hành: tải lại `GET /api/policy`, cho khách đọc và tích lại |
| 400 | `PickupWindowStart must be on or after ...` | Chọn ngày pickup quá gần (xem mục xem giá) |
| 400 | `DeliveryDeadline must be on or after ...` | Hạn giao không đủ thời gian vận chuyển |
| 400 | `The pickup window cannot be longer than 30 days.` | Khoảng pickup quá dài |
| 400 | `Horse(s) with ID 9 were not found among your active horses.` | Ngựa không phải của khách, hoặc đã xóa |
| 400 | `These horses are already in another booking for an overlapping period: ...` | Ngựa đang nằm trong một đơn khác trùng thời gian |
| 400 | `Declare the value of every horse or of none. Missing: Sakura Wind.` | Khai giá trị cho một số con mà bỏ trống con khác |
| 400 | `Midnight Crown: a value from 50,000 USD needs a proof document (...)` | Giá trị từ ngưỡng chứng từ mà không gửi `valueProofPath` |
| 400 | `Midnight Crown: ValueProofPath is not a valid uploaded file path.` | `valueProofPath` không phải đường dẫn do `POST /api/files` (category `documents`) trả về |
| 400 | `Midnight Crown: InsurancePlan 'Gold' is invalid. Allowed values: None, Full.` | Sai tên gói bảo hiểm |
| 400 | `These horses cannot be transported in this pickup window: Little Star: it will be younger than 6 months on 2026-11-30 (born 2026-07-01). Lady Due: it is due to foal on 2026-12-20; a pregnant mare can only travel if the pickup window ends before 2026-11-16.` | Ngựa không đủ điều kiện chở (Chính sách mục 5): chưa đủ `Horse.MinAgeMonths` tháng tuổi tính tới **ngày cuối** khoảng đón; ngựa cái mang thai mà ngày cuối khoảng đón không trước ngày dự sinh − 34 ngày (340 × 10%); ngựa cái vừa đẻ chưa đủ `Horse.PostFoalingDays` ngày tính tới **ngày đầu** khoảng đón. `message` liệt kê mọi con không đạt và lý do |
| 400 | `The same horse appears more than once in this booking.` | Chọn trùng một con ngựa |
| 403 | `You do not have permission to perform this action.` | Người gọi không phải `Customer` |

### GET /api/bookings — Danh sách đơn

Danh sách đơn có phân trang. Mỗi người chỉ thấy các đơn mình được phép xem (xem đầu mục).

- **Ai gọi được:** Customer, LogisticsManager, TransportSpecialist, FleetCoordinator, Admin

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `status` | chuỗi | Không | Lọc theo trạng thái, ví dụ `Submitted` |
| `stage` | chuỗi | Không | Lọc theo giai đoạn theo góc nhìn khách (`customerStage`, xem bảng bên dưới), ví dụ `InTransit` |
| `pickupFrom` | ngày giờ | Không | Chỉ lấy đơn có `pickupWindowStart` từ thời điểm này |
| `pickupTo` | ngày giờ | Không | Chỉ lấy đơn có `pickupWindowStart` tới thời điểm này |
| `customerUserId` | số | Không | Chỉ có tác dụng với nhân sự: lọc theo khách hàng |
| `search` | chuỗi | Không | Từ khóa tìm kiếm |
| `sort` | chuỗi | Không | Trường sắp xếp, thêm `-` phía trước để giảm dần |
| `page` | số | Không | Trang, bắt đầu từ 1 (mặc định 1) |
| `size` | số | Không | Số dòng mỗi trang, 1–100 (mặc định 10) |

Ví dụ: `GET /api/bookings?status=Submitted&sort=-createdAt&page=1&size=10`. `search` tìm theo mã đơn và địa chỉ. Màn "Đơn vận chuyển" của khách: `GET /api/bookings?stage=AwaitingPayment` để mở tab "Chờ thanh toán".

Response `200` — phần `data`:

```json
[
  {
    "bookingId": 2,
    "bookingCode": "BKG-2026-0002",
    "customerUserId": 5,
    "customerName": "Jane Smith",
    "pickupAddress": "Trang trại Yên Bài, Ba Vì, Hà Nội",
    "pickupCountryCode": "VN",
    "dropoffAddress": "Trường đua Tùng Hóa, Quảng Châu",
    "dropoffCountryCode": "CN",
    "transitCountryCodes": [],
    "pickupWindowStart": "2026-11-24T17:00:00+00:00",
    "pickupWindowEnd": "2026-11-29T17:00:00+00:00",
    "deliveryDeadline": "2026-12-05T17:00:00+00:00",
    "transportMode": "Ground",
    "totalHorses": 2,
    "estimatedCost": 6788.48,
    "currencyCode": "USD",
    "status": "Submitted",
    "customerStage": "PendingReview",
    "assignedSpecialistId": null,
    "assignedSpecialistName": null,
    "createdAt": "2026-10-08T07:24:30+00:00"
  }
]
```

| Trường | Ý nghĩa |
|---|---|
| `bookingId`, `bookingCode` | Mã đơn (số) và mã hiển thị (`BKG-2026-0002`) |
| `customerUserId`, `customerName` | Khách đặt đơn |
| `pickupAddress`, `dropoffAddress` | Điểm nhận và điểm giao |
| `transitCountryCodes` | Nước quá cảnh theo thứ tự đi qua; mảng rỗng là đi thẳng |
| `pickupWindowStart`, `pickupWindowEnd` | Khoảng ngày ngựa sẵn sàng được đón (giờ UTC). Ngày chạy thật xem ở `horses[].trips[].plannedStartDate` của chi tiết đơn |
| `deliveryDeadline` | Hạn giao trễ nhất (giờ UTC) |
| `totalHorses` | Số ngựa trong đơn |
| `estimatedCost`, `currencyCode` | **Giá chốt** (R-10); `0` khi chưa có báo giá chốt (đơn `Submitted` / `PreApproved` / `RoutePlanned`), lúc đó hiển thị khoảng `quoteMin` – `quoteMax` |
| `status` | Trạng thái đơn (mục 3) |
| `customerStage` | Giai đoạn của đơn theo góc nhìn khách, server tự suy ra (xem bảng dưới). Dùng làm tab và nhãn trên màn "Đơn vận chuyển" của khách thay cho `status` |
| `assignedSpecialistId`, `assignedSpecialistName` | Chuyên viên phụ trách; `null` khi đơn chưa duyệt |

**Giai đoạn theo góc nhìn khách (`customerStage`).** Server gộp trạng thái đơn, thanh toán, hồ sơ và trạng thái từng con ngựa trên chuyến thành **một** giai đoạn, giống "đơn hàng" trên sàn thương mại điện tử. Không lưu trong DB nên không sửa trực tiếp được.

| Giá trị | Tên gợi ý trên giao diện | Khi nào |
|---|---|---|
| `PendingReview` | Chờ duyệt | Đơn `Submitted`, `PreApproved` hoặc `RoutePlanned` (công ty đang xem xét và lập tuyến) |
| `QuoteReady` | Có báo giá | Đơn `Quoted`: hiện giá chốt, hạn `quoteExpiresAt`, nút "Chấp nhận" / "Từ chối" |
| `AwaitingDeposit` | Chờ đặt cọc | Đơn `AwaitingDeposit` |
| `PreparingDossier` | Đang làm hồ sơ | Đơn đã cọc, còn hồ sơ kiểm dịch chưa `Cleared` |
| `AwaitingTrip` | Chờ xếp chuyến | Hồ sơ xong, ngựa chưa nằm trên chuyến nào đã được duyệt |
| `Scheduled` | Đã xếp chuyến | Ngựa nằm trên chuyến đã duyệt, đã trả đủ phần còn lại, chờ ngày chạy |
| `AwaitingPayment` | Chờ thanh toán | Ngựa đã có chuyến nhưng chưa trả đủ phần còn lại; hoặc sau khi giao còn chi phí phát sinh chưa trả (`AwaitingFinalPayment`) |
| `InTransit` | Đang vận chuyển | Có con ngựa của đơn đang trên xe |
| `AwaitingHandover` | Chờ ký nhận | Xe đã tới điểm trả của đơn, chưa ký biên bản bàn giao |
| `Completed` | Hoàn thành | Đơn `Completed`, không còn nợ |
| `Closed` | Đã hủy / Từ chối | Đơn `Rejected`, `Cancelled`, `Expired` hoặc `QuoteExpired` |

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Stage 'Nope' is invalid. Allowed values: PendingReview, AwaitingDeposit, ...` | `stage` viết sai |

### GET /api/bookings/{id} — Chi tiết đơn

Toàn bộ thông tin của một đơn: bảng giá, kết quả duyệt, và với từng con ngựa: hồ sơ kiểm dịch và các chuyến nó được xếp lên.

- **Ai gọi được:** Như danh sách đơn

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã đơn (`bookingId`) |

Response `200` — phần `data`:

```json
{
  "bookingId": 2,
  "bookingCode": "BKG-2026-0002",
  "customerUserId": 5,
  "customerName": "Jane Smith",
  "pickupAddress": "Trang trại Yên Bài, Ba Vì, Hà Nội",
  "pickupCountryCode": "VN",
  "dropoffAddress": "Trường đua Tùng Hóa, Quảng Châu",
  "dropoffCountryCode": "CN",
  "transitCountryCodes": [],
  "pickupWindowStart": "2026-11-24T17:00:00+00:00",
  "pickupWindowEnd": "2026-11-29T17:00:00+00:00",
  "deliveryDeadline": "2026-12-05T17:00:00+00:00",
  "transportMode": "Ground",
  "totalHorses": 2,
  "estimatedCost": 6788.48,
  "currencyCode": "USD",
  "status": "Approved",
  "customerStage": "Scheduled",
  "assignedSpecialistId": 2,
  "assignedSpecialistName": "Nguyễn Thị Mai",
  "createdAt": "2026-10-08T07:24:30+00:00",
  "customerEmail": "jane.smith@racehorseowner.com",
  "customerPhone": "+84988111222",
  "specialInstructions": "Giữ cabin 17 độ",
  "distanceKm": 900,
  "estimatedTransitHours": 35.9,
  "isExpress": false,
  "requiresClimateControl": true,
  "declaredValue": 50000,
  "rejectionReason": null,
  "reviewedByUserId": 1,
  "reviewedByName": "Trần Quốc Bảo",
  "reviewedAt": "2026-10-08T07:24:30+00:00",
  "quoteLines": [
    {
      "code": "FREIGHT_SHARED",
      "name": "Cước vận chuyển - Chuồng ghép (Shared) - 3 ngựa / pallet",
      "quantity": 1,
      "unitPrice": 900,
      "amount": 900
    }
  ],
  "horses": [
    {
      "bookingHorseId": 3,
      "horseId": 1,
      "horseName": "Red Flash (Tia Chớp Đỏ)",
      "microchipNumber": "982000412345671",
      "stallClass": "Shared",
      "status": "Approved",
      "notes": "Chuồng bên trái",
      "declaredValue": 120000.00,
      "agreedValue": 100000.00,
      "valueProofUrl": "https://abcd.supabase.co/storage/v1/object/sign/files/documents/2026/10/9c1e....pdf?token=eyJ...",
      "insurancePlan": "Full",
      "dossierId": 3,
      "dossierCode": "DOS-2026-0003",
      "dossierStatus": "Cleared",
      "trips": [
        {
          "tripId": 2,
          "tripCode": "TRP-2026-0002",
          "tripStatus": "Scheduled",
          "horseStatus": "Planned",
          "stallSlotNumber": 1,
          "plannedStartDate": "2026-11-06T23:00:00+00:00",
          "plannedEndDate": "2026-11-09T11:00:00+00:00",
          "pickedUpAt": null,
          "deliveredAt": null
        }
      ]
    }
  ]
}
```

Ngoài các trường của danh sách, chi tiết có thêm:

| Trường | Ý nghĩa |
|---|---|
| `customerEmail`, `customerPhone` | Liên hệ của khách |
| `specialInstructions`, `distanceKm`, `requiresClimateControl`, `declaredValue`, `isExpress` | Thông tin khách đã khai |
| `estimatedTransitHours` | Thời gian vận chuyển server ước tính lúc gửi đơn (giờ); `null` với đơn tạo trước khi có tính năng này |
| `rejectionReason` | Lý do bị từ chối (chỉ có khi `status` là `Rejected`) |
| `reviewedByName`, `reviewedAt` | Ai duyệt / từ chối và lúc nào |
| `quoteLines[]` | Các dòng báo giá đã lưu, dạng giống `lines` của xem giá |
| `horses[]` | Từng con ngựa trong đơn |
| `quoteMin`, `quoteMax` | Khoảng giá ước tính lúc khách gửi đơn (R-10); `null` với đơn cũ |
| `quotedAt`, `quoteExpiresAt` | Lúc gửi báo giá chốt và hạn chấp nhận + đặt cọc (R-10); `null` khi chưa có báo giá chốt hoặc đơn cũ |
| `quoteAcceptedAt` | Lúc khách chấp nhận báo giá chốt |
| `docsDueAt` | Hạn khách nộp đủ giấy tờ (BE-06); có sau khi cọc. Hiện đếm ngược trên trang đơn |
| `routeChangeCancelUntil` | Có giá trị khi công ty đổi tuyến thêm nước mới: tới lúc này khách được hủy và hoàn 100% (hiện nút "Hủy và hoàn 100%") |
| `policyVersion`, `policyAcceptedAt` | Phiên bản Chính sách dịch vụ khách đã đồng ý gần nhất (lúc gửi đơn, cập nhật lại khi chấp nhận báo giá) và thời điểm đồng ý |
| `horses[].bookingHorseId` | Mã "ngựa trong đơn", dùng khi xếp chuyến và khi Manager chốt giá trị |
| `horses[].declaredValue` | Giá trị khách khai báo (USD); `null` với đơn cũ chỉ khai một giá trị cho cả đơn |
| `horses[].agreedValue` | Giá trị thỏa thuận Manager chốt (mặc định bằng giá trị khai báo). **Bảo hiểm và bồi thường tính trên giá trị này** |
| `horses[].valueProofUrl` | Link xem chứng từ giá trị (có hạn); `null` nếu không có |
| `horses[].insurancePlan` | Gói bảo hiểm của con ngựa: `None` hoặc `Full` |
| `horses[].dossierId`, `dossierCode`, `dossierStatus` | Hồ sơ kiểm dịch của con ngựa; có sau khi đơn được duyệt |
| `horses[].trips[]` | Các chuyến con ngựa được xếp lên, kèm `tripStatus` (trạng thái xe) và `horseStatus` (trạng thái con ngựa trên chuyến) |
| `horses[].trips[].pickedUpAt`, `deliveredAt` | Lúc con ngựa lên xe (xe rời điểm đón) và lúc ký bàn giao; dùng để vẽ timeline tiến trình đơn |
| `customerStage` | Giai đoạn theo góc nhìn khách, xem bảng ở `GET /api/bookings` |
| `payment` | Tóm tắt thanh toán của đơn, cùng dạng `data` của `GET /api/bookings/{id}/payments` (mục 19) |

Một đơn có thể đi nhiều chuyến và một chuyến có thể chở ngựa của nhiều đơn, nên luôn đọc chuyến theo từng con ngựa.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 403 | `You do not have access to this booking.` | Đơn của khách khác, hoặc chuyên viên không phụ trách đơn này |
| 404 | `Booking with ID 99 was not found.` | Không có đơn với mã đó |

### POST /api/bookings/{id}/cancel — Khách hủy đơn

Hủy đơn của mình khi đơn đang `Submitted`, `PreApproved`, `RoutePlanned`, `Quoted`, `AwaitingDeposit`, `Approved` hoặc `Assigned` **và chưa con ngựa nào nằm trên một chuyến** (chưa bị hủy). Nếu khách đã trả tiền, server tự tạo dòng hoàn tiền: `cancelRefundPercent`% tiền cọc (cấu hình, mặc định 50%) + 100% phần còn lại đã trả, gọi API hoàn tiền của VNPay, và báo khách qua thông báo + email. Khách xem kết quả hoàn tiền trong `GET /api/bookings/{id}/payments`.

- **Ai gọi được:** Customer (chủ đơn)
- **Dữ liệu gửi lên:** không có (không cần body)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã đơn |

Response `200`: `data` là chi tiết đơn với `status` là `Cancelled`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `A booking can only be cancelled before departure (current status: Completed).` | Đơn đã kết thúc hoặc không còn hủy được |
| 400 | `Horses of this booking are already planned on trip TRP-2026-0003. Please contact the logistics team to cancel.` | Ngựa đã được xếp lên chuyến |
| 403 | `You can only cancel your own bookings.` | Hủy đơn của người khác |

### POST /api/bookings/{id}/accept-quote — Khách chấp nhận báo giá chốt

Khách đồng ý giá chốt (`estimatedCost`, `quoteLines`), **Hợp đồng vận chuyển** bản nháp đang xem (mục 7c) và Chính sách dịch vụ: đây là nút **"Chấp nhận & đặt cọc"**, cú bấm đó là giao kết hợp đồng (không ký riêng, không OTP). Hợp đồng chuyển `Accepted`, server lưu bằng chứng (tài khoản, thời điểm, IP, User-Agent, phiên bản chính sách) và email cho khách. Đơn chuyển `AwaitingDeposit`, tiền cọc = `Payment.DepositPercent`% giá chốt; khách đặt cọc bằng `POST /api/bookings/{id}/payments` trước `quoteExpiresAt` (hạn không đổi khi chấp nhận). Quản lý nhận thông báo. Nếu cấu hình cọc 0% thì đơn chuyển thẳng `Approved` và mở hồ sơ.

- **Ai gọi được:** Customer (chủ đơn)
- **Khi nào:** đơn đang `Quoted` và chưa quá `quoteExpiresAt`
- **Dữ liệu gửi lên:** JSON

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `contractId` | số | Có | `contractId` của bản hợp đồng khách đang xem (`GET /api/bookings/{id}/contract`) |
| `contractSha256` | chuỗi | Có | `fileSha256` của bản đó (gửi nguyên giá trị nhận được) |
| `acceptPolicy` | true / false | Có (phải là `true`) | Khách tích ô "Tôi đã đọc hợp đồng và Chính sách dịch vụ" |
| `policyVersion` | chuỗi | Có | Phiên bản lấy từ `GET /api/policy` → `version` |

Ví dụ request:

```json
{
  "contractId": 12,
  "contractSha256": "5fbcfef0a93c6d1e0b4c7f3a2d9e8b1c6a5f4e3d2c1b0a9f8e7d6c5b4a392817",
  "acceptPolicy": true,
  "policyVersion": "1.0"
}
```

Response `200`: `data` là chi tiết đơn với `status = AwaitingDeposit`, `quoteAcceptedAt` đã có, `payment.nextPaymentType = Deposit`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Only a quoted booking can be accepted (current status: PreApproved).` | Chưa có báo giá chốt, hoặc đã chấp nhận rồi |
| 400 | `The contract has changed since you opened it. Please review the latest version.` | `contractId` không phải bản nháp hiện hành (công ty vừa lập lại tuyến nên có bản mới) hoặc `contractSha256` không khớp: tải lại `GET /api/bookings/{id}/contract`, cho khách xem lại rồi bấm lại |
| 400 | `ContractSha256 is required.` / `ContractId is required.` | Thiếu thông tin bản hợp đồng |
| 400 | `This quote expired at 30/10/2026 15:11 (Vietnam time). Please submit a new request.` | Quá hạn báo giá |
| 400 | `You must accept the service policy to continue.` | `acceptPolicy` không phải `true` |
| 400 | `The service policy has changed (current version 1.1). ...` | `policyVersion` khác phiên bản hiện hành |
| 403 | `You can only answer the quote of your own booking.` | Đơn của khách khác (người gọi không phải Customer cũng nhận 403) |

### POST /api/bookings/{id}/decline-quote — Khách từ chối báo giá chốt

Đơn chuyển `Cancelled`, không mất phí (chưa trả đồng nào); ngựa trong đơn được giải phóng. Quản lý nhận thông báo kèm lý do.

- **Ai gọi được:** Customer (chủ đơn)
- **Khi nào:** đơn đang `Quoted`
- **Dữ liệu gửi lên:** JSON `{ "reason": "Giá cao hơn dự kiến" }` (`reason` không bắt buộc, tối đa 500 ký tự)

Response `200`: `data` là chi tiết đơn với `status = Cancelled`.

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Only a quoted booking can be declined (current status: AwaitingDeposit).` | Đã chấp nhận rồi (khi đó dùng `POST /api/bookings/{id}/cancel`, có thể mất một phần cọc) |
| 403 | `You can only answer the quote of your own booking.` | Đơn của khách khác |

**Lưu ý thanh toán:** `POST /api/bookings/{id}/payments` trả `400 The quote for booking ... has expired. Please submit a new request.` khi đơn `AwaitingDeposit` đã quá `quoteExpiresAt` (job đóng đơn chạy vài phút một lần).

### PUT /api/bookings/{id}/locations — Khách đổi điểm đón / trả

Khách chọn lại điểm đón / trả trên bản đồ khi **chưa trả khoản nào**. Server tính lại khoảng giá (`quoteMin`, `quoteMax`), quãng đường và các nước đi qua. Nếu đơn đã có tuyến hoặc báo giá (`RoutePlanned`, `Quoted`, `AwaitingDeposit` chưa cọc): báo giá chốt và hợp đồng nháp bị rút lại, tuyến đã duyệt thành `Superseded`, tuyến đang soạn / chờ duyệt thành `Rejected`; đơn về `PreApproved`, điều phối và quản lý nhận thông báo lập tuyến lại; duyệt tuyến mới thì khách nhận báo giá chốt mới.

- **Ai gọi được:** Customer (chủ đơn)
- **Khi nào:** đơn `Submitted`, `PreApproved`, `RoutePlanned`, `Quoted`, hoặc `AwaitingDeposit` chưa trả đồng nào
- **Dữ liệu gửi lên:** JSON

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `pickupAddress` | chuỗi | Có | Địa chỉ đón mới, tối đa 255 ký tự |
| `pickupCountryCode` | chuỗi | Có | Mã nước đón, 2 chữ cái |
| `pickupLat`, `pickupLng` | số | Không (nên gửi) | Tọa độ điểm đón (`GET /api/geo/search` / `reverse`) |
| `dropoffAddress` | chuỗi | Có | Địa chỉ giao mới |
| `dropoffCountryCode` | chuỗi | Có | Mã nước giao |
| `dropoffLat`, `dropoffLng` | số | Không (nên gửi) | Tọa độ điểm giao |

Ví dụ request:

```json
{
  "pickupAddress": "Sơn Tây, Hà Nội",
  "pickupCountryCode": "VN",
  "pickupLat": 21.1382,
  "pickupLng": 105.5051,
  "dropoffAddress": "Trường đua Nam Ninh",
  "dropoffCountryCode": "CN",
  "dropoffLat": 22.817,
  "dropoffLng": 108.3665
}
```

Response `200`: `data` là chi tiết đơn với địa chỉ, khoảng giá mới; nếu báo giá bị rút lại thì `status = PreApproved`, `estimatedCost = 0`, `quotedAt = null`.

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `The pickup / drop-off can no longer be changed online (current status: Approved). Please contact the logistics team.` | Đơn đã cọc / đang chạy / đã đóng |
| 400 | `A payment was already made for this booking. Please contact the logistics team to change the pickup / drop-off.` | `AwaitingDeposit` nhưng đã trả một phần |
| 400 | `PickupCountryCode must be a 2-letter country code.` | Sai mã nước |
| 403 | `You can only change your own bookings.` | Đơn của khách khác |

### POST /api/bookings/{id}/cancel-route-change — Khách hủy vì công ty đổi tuyến (hoàn 100%)

Dùng khi chi tiết đơn có `routeChangeCancelUntil` (công ty đổi tuyến sang nước mới mà khách không lo được giấy) và chưa quá thời điểm đó. Đơn chuyển `Cancelled`, server hoàn **100%** mọi khoản đã trả (kể cả tiền cọc) qua VNPay, quản lý nhận thông báo.

- **Ai gọi được:** Customer (chủ đơn)
- **Dữ liệu gửi lên:** không có

Response `200`: `data` là chi tiết đơn với `status = Cancelled`; xem khoản hoàn trong `GET /api/bookings/{id}/payments`.

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `This booking has no company route change that allows a free cancellation. Use POST /api/bookings/{id}/cancel instead.` | Không có thay đổi tuyến do công ty, hoặc đã quá `routeChangeCancelUntil` |
| 400 | `Horses of this booking are already planned on trip TRP-2026-0003. Please contact the logistics team to cancel; the refund is still 100%.` | Ngựa đã được xếp lên chuyến |
| 403 | `You can only change your own bookings.` | Đơn của khách khác |

### POST /api/bookings/{id}/approve — Duyệt sơ bộ đơn

**Đổi từ R-10:** đây là bước **duyệt sơ bộ** (đơn khả thi), chưa thu cọc. Duyệt một đơn đang `Submitted` và chọn chuyên viên phụ trách. Đơn chuyển sang `PreApproved`; khách nhận thông báo "đang lập tuyến, chưa phải thanh toán"; các điều phối viên nhận thông báo cần lập tuyến (mục 7b). Cọc chỉ thu sau khi khách chấp nhận báo giá chốt.

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã đơn |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `specialistUserId` | số | Có | `userId` của một `TransportSpecialist` đang hoạt động (lấy từ `GET /api/users/staff?role=TransportSpecialist`) |

Ví dụ request:

```json
{
  "specialistUserId": 2
}
```

Response `200`: `data` là chi tiết đơn với `status` là `PreApproved` và `assignedSpecialistName` là chuyên viên vừa chọn.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Only a Submitted booking can be approved (current status: ...).` | Đơn không còn ở trạng thái chờ duyệt |
| 400 | `User with ID 9 is not an active transport specialist.` | Người được chọn không phải chuyên viên, hoặc đã bị khóa |
| 400 | `This booking can no longer be approved: its pickup window ends on ... on this route need about 26 days ...` | Duyệt quá muộn: khoảng pickup không còn đủ thời gian lập tuyến, đặt cọc, làm hồ sơ của tuyến và xếp chuyến. Từ chối và nhờ khách đặt lại khoảng ngày muộn hơn |

### PUT /api/bookings/{id}/horse-values — Chốt giá trị thỏa thuận từng con

Trước khi gửi báo giá, Manager xem giá trị khách khai báo và chứng từ (`horses[].valueProofUrl`), rồi **chốt giá trị thỏa thuận** của từng con. Bảo hiểm và bồi thường về sau tính trên giá trị thỏa thuận, không định giá lại (kiểu "agreed value" của bảo hiểm ngựa). Mặc định giá trị thỏa thuận bằng giá trị khai báo, nên chỉ cần gọi khi muốn sửa.

Khách nhận thông báo liệt kê các con có giá trị thay đổi (kèm ghi chú nếu có).

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON
- **Khi nào:** đơn đang `Submitted`, `PreApproved` hoặc `RoutePlanned` (trước khi gửi báo giá)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã đơn |

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `horses` | mảng | Có | Các con cần chốt (ít nhất 1); con không gửi giữ nguyên |
| `horses[].bookingHorseId` | số | Có | Lấy từ `horses[].bookingHorseId` của chi tiết đơn |
| `horses[].agreedValue` | số | Có | Giá trị thỏa thuận (USD), không âm |
| `note` | chuỗi | Không | Ghi chú gửi khách, ví dụ lý do chốt khác giá trị khai báo, tối đa 500 ký tự |

Ví dụ request:

```json
{
  "horses": [
    { "bookingHorseId": 3, "agreedValue": 95000 },
    { "bookingHorseId": 4, "agreedValue": 30000 }
  ],
  "note": "Theo giấy định giá của bác sĩ thú y"
}
```

Response `200`: `data` là chi tiết đơn sau khi lưu; `horses[].agreedValue` là giá trị mới.

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Agreed values can only be set before the quote is sent (current status: Approved).` | Đơn đã qua bước báo giá |
| 400 | `Booking horse(s) with ID 999 are not in booking BKG-2026-0002.` | `bookingHorseId` không thuộc đơn |
| 400 | `AgreedValue cannot be negative.` / `Validation failed` | Giá trị âm hoặc thiếu trường |
| 403 | | Không phải quản lý / Admin |
| 404 | `Booking with ID 99 was not found.` | Không có đơn |

### PUT /api/bookings/{id}/transit-countries — Xác nhận / sửa nước quá cảnh

Khi xét duyệt đơn, Manager xác nhận các nước ngựa sẽ đi qua (khách có thể đã gợi ý lúc gửi đơn). Ví dụ VN → Nga đường bộ thì phải đi qua Trung Quốc: gửi `["CN"]`. Danh sách mới **thay toàn bộ** danh sách cũ; gửi mảng rỗng nghĩa là đi thẳng.

Sau khi đổi, server **tính lại báo giá** (phí quá cảnh, thời gian qua cửa khẩu, thời gian làm hồ sơ) và báo cho khách. Ngày "hôm nay" khi tính lại là ngày khách tạo đơn, nên khách không bị bắt đổi lịch chỉ vì đơn chờ duyệt vài ngày; nhưng nếu tuyến mới cần làm hồ sơ lâu hơn khiến khoảng pickup không còn hợp lệ thì server báo lỗi, khi đó Manager nên từ chối đơn.

Hồ sơ kiểm dịch của đơn sẽ có thêm các giấy tờ hướng `Transit` của từng nước này (xem `GET /api/dossiers/{id}`).

- **Ai gọi được:** LogisticsManager, Admin
- **Khi nào gọi được:** đơn còn ở trạng thái `Submitted` (trước khi duyệt / từ chối)
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã đơn |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `transitCountryCodes` | mảng chuỗi | Có | Nước đi qua theo thứ tự, mã 2 chữ cái; tối đa 5 nước, không trùng nhau, không trùng nước đi / nước đến. `[]` = đi thẳng |

Ví dụ request:

```json
{
  "transitCountryCodes": ["CN"]
}
```

Response `200`: `data` là chi tiết đơn (như `GET /api/bookings/{id}`) với `transitCountryCodes`, `estimatedCost`, `quoteLines`, `isExpress`, `estimatedTransitHours` đã cập nhật.

Ví dụ JavaScript:

```js
await api.put(`/bookings/${bookingId}/transit-countries`, { transitCountryCodes: ['CN'] });
```

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Transit countries can only be changed before the booking is reviewed (current status: Approved).` | Đơn đã được duyệt / từ chối |
| 400 | `Transit countries cannot include the origin or the destination country.` | Có nước trùng nước đi / nước đến |
| 400 | `The same transit country is listed more than once.` | Trùng nước |
| 400 | `PickupWindowStart must be on or after ...` | Tuyến mới cần làm hồ sơ lâu hơn, khoảng pickup của khách không còn đủ |
| 403 | `You do not have permission to perform this action.` | Người gọi không phải Manager / Admin |
| 404 | `Booking with ID 99 was not found.` | Không có đơn |

### POST /api/bookings/{id}/reject — Từ chối đơn

Từ chối một đơn chưa gửi báo giá (`Submitted`, `PreApproved` hoặc `RoutePlanned`, ví dụ không lập được tuyến khả thi), bắt buộc ghi lý do. Khách nhận thông báo kèm lý do.

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã đơn |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `reason` | chuỗi | Có | Lý do từ chối, tối đa 500 ký tự |

Ví dụ request:

```json
{
  "reason": "Tuyến này tạm ngừng nhận trong tháng tới."
}
```

Response `200`: `data` là chi tiết đơn với `status` là `Rejected` và `rejectionReason` vừa nhập.

### POST /api/bookings/{id}/reassign-specialist — Đổi chuyên viên phụ trách

Đổi chuyên viên của một đơn đã `Approved`. Các hồ sơ chưa xong của đơn chuyển sang chuyên viên mới.

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã đơn |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `specialistUserId` | số | Có | `userId` của chuyên viên mới |

Ví dụ request:

```json
{
  "specialistUserId": 10
}
```

Response `200`: `data` là chi tiết đơn với `assignedSpecialistId` mới.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `The specialist can only be changed on an approved booking (current status: ...).` | Đơn chưa duyệt hoặc đã kết thúc |
| 400 | `... is already the specialist of this booking.` | Chọn lại đúng người đang phụ trách |

---

## 7b. Tuyến dự kiến của đơn

Sau khi đơn được **duyệt sơ bộ** (`PreApproved`), điều phối viên **lập tuyến dự kiến** trên bản đồ: điểm đón và điểm trả lấy từ đơn (không sửa được), điều phối thêm các **trạm nghỉ** và **cửa khẩu** theo thứ tự đi. Hệ thống tính quãng đường và thời gian từng chặng, tự suy ra **các nước quá cảnh**, và **cảnh báo** chặng nào đổi nước mà thiếu mốc cửa khẩu. Quản lý **duyệt** hoặc **từ chối** (kèm lý do). Duyệt xong, tuyến thành tuyến hiệu lực của đơn: nước quá cảnh, quãng đường, thời gian vận chuyển của đơn lấy theo tuyến; đơn chuyển `RoutePlanned`. Khi xếp chuyến, các mốc của tuyến này được chép sang lộ trình của chuyến (BE-07).

Một đơn có thể có nhiều **phiên bản** tuyến (`version` 1, 2, ...) nhưng chỉ một bản `Approved` hiệu lực; khi duyệt bản mới, bản cũ thành `Superseded`.

Vòng đời:

```
Draft ──(trình)──> Submitted ──(quản lý duyệt)──> Approved ──(có bản mới được duyệt)──> Superseded
  ▲                    │
  │                    └──(quản lý từ chối)──> Rejected
  └────────(sửa)────────────────────────────────┘
```

**Quy tắc cửa khẩu.** Mỗi lần qua biên giới phải có một mốc `BorderGate`. Hệ thống tra các nước dọc từng chặng: chặng thường chỉ được ở trong 1 nước; chặng có một đầu là cửa khẩu được qua 2 nước; chặng nối hai cửa khẩu được qua 3 nước. Vượt quá thì có cảnh báo (`warnings`) và **không trình được**. Hiện cảnh báo ngay tại đoạn đường liên quan (`fromIndex` → `toIndex` là vị trí trong `stops`).

**Thời gian chờ.** Lần đầu tính một tuyến mới mất vài giây đến vài chục giây (tra nước dọc đường, 1 lượt / giây); sửa lại các mốc gần đó thì nhanh vì có bộ nhớ đệm. Nên gọi `preview` khi người dùng **thả** ghim / bấm "Tính lại", không gọi theo từng lần kéo.

Thứ tự gọi thông thường:

1. Điều phối: `GET /api/bookings?status=PreApproved` để xem đơn cần lập tuyến.
2. Điều phối: `POST /api/bookings/{bookingId}/route-plans/preview` mỗi khi thêm / kéo / xóa mốc để vẽ lại và xem cảnh báo.
3. Điều phối: `POST /api/bookings/{bookingId}/route-plans` để lưu bản nháp (hoặc `PUT /api/route-plans/{id}` để sửa), rồi `POST /api/route-plans/{id}/submit`.
4. Quản lý: `POST /api/route-plans/{id}/approve` hoặc `/reject`.
5. Bị từ chối: điều phối xem `rejectionReason`, sửa bằng `PUT` rồi trình lại.

Một tuyến (`routePlan`) có dạng:

```json
{
  "routePlanId": 3,
  "bookingId": 4,
  "bookingCode": "BKG-2026-0004",
  "version": 1,
  "status": "Approved",
  "stops": [
    { "sequenceOrder": 1, "name": "Pickup", "stopType": "Origin", "address": "Ba Vì, Hà Nội", "lat": 21.0285, "lng": 105.8542, "countryCode": "VN", "distanceKmFromPrevious": null, "durationHoursFromPrevious": null, "restMinutes": 0 },
    { "sequenceOrder": 2, "name": "Trạm nghỉ Lạng Sơn", "stopType": "RestStop", "address": "TP Lạng Sơn", "lat": 21.8537, "lng": 106.7615, "countryCode": "VN", "distanceKmFromPrevious": 153.73, "durationHoursFromPrevious": 2.31, "restMinutes": 60 },
    { "sequenceOrder": 3, "name": "Cửa khẩu Hữu Nghị", "stopType": "BorderGate", "address": "Đồng Đăng, Cao Lộc, Lạng Sơn", "lat": 21.9749, "lng": 106.7064, "countryCode": "CN", "distanceKmFromPrevious": 21.76, "durationHoursFromPrevious": 0.42, "restMinutes": 120 },
    { "sequenceOrder": 4, "name": "Drop-off", "stopType": "Destination", "address": "Nanning", "lat": 22.817, "lng": 108.3665, "countryCode": "CN", "distanceKmFromPrevious": 215.47, "durationHoursFromPrevious": 2.95, "restMinutes": 0 }
  ],
  "transitCountryCodes": [],
  "totalDistanceKm": 390.96,
  "estimatedHours": 15.8,
  "isEstimate": false,
  "polyline": [[21.0285, 105.8542], [21.0301, 105.8577], [22.817, 108.3665]],
  "warnings": [],
  "notes": "Qua Hữu Nghị",
  "plannedByUserId": 17,
  "plannedByName": "Fleet Coordinator",
  "reviewedByName": "Logistics Manager",
  "reviewedAt": "2026-10-27T08:30:12.415+00:00",
  "rejectionReason": null,
  "createdAt": "2026-10-27T08:28:02.110+00:00",
  "updatedAt": "2026-10-27T08:30:12.415+00:00"
}
```

| Trường | Ý nghĩa |
|---|---|
| `version`, `status` | Phiên bản và trạng thái tuyến (mục 3) |
| `stops[]` | Các mốc theo thứ tự đi: mốc đầu `Origin` và mốc cuối `Destination` lấy từ đơn |
| `stops[].countryCode` | Nước của mốc (mốc cửa khẩu có thể tra ra nước bên kia biên giới) |
| `stops[].distanceKmFromPrevious`, `durationHoursFromPrevious` | Quãng đường (km) và thời gian lái (giờ) của chặng từ mốc trước tới mốc này; dùng làm nhãn trên đoạn đường |
| `stops[].restMinutes` | Thời gian nghỉ bắt buộc tại mốc (phút) |
| `transitCountryCodes` | Nước quá cảnh suy ra từ tuyến (bỏ nước đi và nước đến), theo thứ tự |
| `totalDistanceKm`, `estimatedHours` | Tổng quãng đường và thời gian vận chuyển dự kiến (đã gồm nghỉ, qua cửa khẩu) |
| `isEstimate` | `true` khi có chặng tính theo đường chim bay (nhà cung cấp bản đồ lỗi) |
| `polyline` | Đường vẽ cả tuyến: mảng `[vĩ độ, kinh độ]` |
| `warnings[]` | Cảnh báo cửa khẩu (chỉ có trong kết quả `preview`, tạo, sửa): `fromIndex`, `toIndex`, `countryCodes`, `message`. Còn cảnh báo thì không trình được |
| `rejectionReason` | Lý do lần từ chối gần nhất |

### GET /api/bookings/{bookingId}/route-plans — Các phiên bản tuyến của đơn

- **Ai gọi được:** nhân sự xem được đơn (mọi phiên bản); khách của đơn (chỉ tuyến `Approved`, để vẽ trên trang báo giá)

Response `200`: `data` là mảng tuyến, phiên bản mới nhất trước.

### GET /api/route-plans/{id} — Chi tiết một tuyến

- **Ai gọi được:** như trên (khách chỉ xem được tuyến `Approved`, bản khác trả 403)

### POST /api/bookings/{bookingId}/route-plans/preview — Tính thử tuyến (không lưu)

- **Ai gọi được:** FleetCoordinator, LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON, giống body của tạo tuyến bên dưới

Response `200`: `data` là tuyến đã tính (`routePlanId = 0`, `status = Draft`), có `warnings`.

### POST /api/bookings/{bookingId}/route-plans — Tạo bản nháp tuyến

Đơn phải đang `PreApproved` (hoặc `RoutePlanned` / `Quoted` khi cần phiên bản mới: duyệt bản mới thì báo giá tính lại). Sau khi khách đã chấp nhận báo giá (`AwaitingDeposit`, `Approved`, `Assigned` mà ngựa chưa rời điểm đón) vẫn lập được bản mới: đó là **đổi tuyến do công ty**, giá giữ nguyên (xem mục 7). Mỗi lúc một đơn chỉ có một bản đang soạn / chờ duyệt.

- **Ai gọi được:** FleetCoordinator, Admin
- **Dữ liệu gửi lên:** JSON

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `stops` | mảng | Không (rỗng = đi thẳng) | Các mốc **ở giữa** theo thứ tự đi, tối đa 23. Điểm đón / trả không gửi |
| `stops[].name` | chuỗi | Có | Tên mốc, tối đa 100 ký tự |
| `stops[].stopType` | chuỗi | Có | `RestStop` hoặc `BorderGate` |
| `stops[].address` | chuỗi | Không | Địa chỉ (lấy từ `GET /api/geo/search` / `reverse`), tối đa 255 ký tự |
| `stops[].lat`, `stops[].lng` | số | Có | Tọa độ |
| `stops[].countryCode` | chuỗi | Không | Nước của mốc (từ kết quả tìm địa chỉ); gửi kèm thì khỏi tra lại |
| `stops[].restMinutes` | số nguyên | Không | Nghỉ bắt buộc tại mốc, 0–1440 phút |
| `notes` | chuỗi | Không | Ghi chú cho quản lý, tối đa 1000 ký tự |
| `originLat`, `originLng`, `destinationLat`, `destinationLng` | số | Chỉ với đơn cũ chưa có tọa độ | Ghim điểm đón / trả trên bản đồ |

Ví dụ request:

```json
{
  "stops": [
    { "name": "Trạm nghỉ Lạng Sơn", "stopType": "RestStop", "address": "TP Lạng Sơn", "lat": 21.8537, "lng": 106.7615, "countryCode": "VN", "restMinutes": 60 },
    { "name": "Cửa khẩu Hữu Nghị", "stopType": "BorderGate", "address": "Đồng Đăng, Cao Lộc, Lạng Sơn", "lat": 21.9749, "lng": 106.7064, "restMinutes": 120 }
  ],
  "notes": "Qua Hữu Nghị"
}
```

Response `201`: `data` là tuyến vừa tạo (`status = Draft`), kèm `warnings`.

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `A route can only be planned after the booking passes the first review (current status: Submitted).` | Đơn chưa duyệt sơ bộ |
| 400 | `Booking BKG-2026-0004 already has route version 1 (Draft). Edit or review that version instead of starting a new one.` | Đã có bản đang soạn / chờ duyệt |
| 400 | `StopType 'Destination' is invalid for a stop. Allowed values: RestStop, BorderGate.` | Sai loại mốc |
| 400 | `Booking BKG-2026-0001 has no map coordinates for its pickup / drop-off. ...` | Đơn cũ chưa có tọa độ mà không gửi `originLat` ... |
| 400 | `Route plans are only made for ground transport.` | Đơn đường hàng không |

### PUT /api/route-plans/{id} — Sửa tuyến

Thay toàn bộ danh sách mốc của bản `Draft` hoặc `Rejected` (bản bị từ chối quay về `Draft`). Body giống tạo tuyến.

- **Ai gọi được:** FleetCoordinator, Admin

Response `200`: tuyến sau khi sửa, kèm `warnings`.

### POST /api/route-plans/{id}/submit — Trình duyệt

- **Ai gọi được:** FleetCoordinator, Admin

Hệ thống kiểm tra lại cảnh báo cửa khẩu; còn cảnh báo thì 400. Trình xong, `status = Submitted` và các quản lý nhận thông báo.

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `The route cannot be submitted yet: The leg Pickup → Drop-off crosses VN → CN without a BorderGate stop at each border; ...` | Còn chặng đổi nước thiếu cửa khẩu |
| 400 | `Only a draft or rejected route can be submitted (current status: Submitted).` | Đã trình rồi |
| 400 | `The pickup / drop-off of this booking changed after the route was saved. Save the route again (PUT) before submitting.` | Khách vừa đổi điểm đón / trả: gọi `PUT /api/route-plans/{id}` (cùng danh sách mốc) để tính lại rồi trình |

### POST /api/route-plans/{id}/approve — Duyệt tuyến

- **Ai gọi được:** LogisticsManager, Admin

Tuyến thành `Approved` (bản `Approved` cũ, nếu có, thành `Superseded`); đơn chuyển `RoutePlanned`, nước quá cảnh / quãng đường / thời gian vận chuyển của đơn lấy theo tuyến. Người lập tuyến được báo. Ngay sau đó server tính **báo giá chốt** theo tuyến: đơn chuyển `Quoted`, khách nhận thông báo + email (xem "Báo giá chốt" ở mục 7). Response vẫn là tuyến vừa duyệt; muốn xem giá chốt thì gọi `GET /api/bookings/{id}`.

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Only a submitted route can be reviewed (current status: Draft).` | Tuyến chưa trình |
| 403 | | Không phải quản lý / Admin |

### POST /api/route-plans/{id}/reject — Từ chối tuyến

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON `{ "reason": "Thêm trạm nghỉ trước cửa khẩu" }` (bắt buộc, tối đa 500 ký tự)

Tuyến thành `Rejected`, người lập nhận thông báo kèm lý do; sửa bằng `PUT` rồi trình lại.

## 7c. Hợp đồng vận chuyển

Cùng lúc gửi báo giá chốt (đơn `Quoted`), server tự tạo **Hợp đồng vận chuyển** dạng PDF (Chính sách mục 12). Hợp đồng có: số hợp đồng `HD-2026-0001` và phiên bản; công ty, khách, người nhận; bảng ngựa (tên, số vi mạch, hạng chuồng, giá trị thỏa thuận, gói bảo hiểm); tuyến đã duyệt (điểm đón / giao, các mốc, nước quá cảnh, tổng km); khoảng ngày đón, hạn giao; bảng giá chốt, tiền cọc, phần còn lại, các hạn thanh toán; người duyệt tuyến; và **phụ lục** là toàn văn Chính sách dịch vụ đúng phiên bản hiện hành (các con số cấu hình đã thay bằng giá trị thật).

**Không có bước ký riêng, không OTP.** Khách xem PDF trên ứng dụng, tích "Tôi đã đọc hợp đồng và Chính sách dịch vụ" rồi bấm **Chấp nhận & đặt cọc** (`POST /api/bookings/{id}/accept-quote` kèm `contractId` + `contractSha256`). Server chỉ nhận nếu đó đúng là bản nháp hiện hành và mã băm khớp, nên khách không thể vô tình chấp nhận một bản cũ.

Vòng đời một phiên bản:

```
Draft ──(khách chấp nhận)──> Accepted ──(có bản mới được chấp nhận)──> Superseded
  │
  └──(công ty báo giá lại / có bản nháp mới)──> Superseded
```

- Mỗi đơn có tối đa **một** bản `Draft` (chờ khách chấp nhận) và **một** bản `Accepted` (hợp đồng đang hiệu lực). Bản đã `Accepted` không bao giờ bị sửa.
- Công ty lập lại tuyến khi đơn còn `Quoted` → báo giá tính lại → **bản nháp mới** (`version` + 1), bản nháp cũ thành `Superseded`.
- Sau khi giao kết, khi điều khoản đổi theo yêu cầu của khách (đổi điểm đón / giao, đồng ý đi đường vòng, đổi người nhận — các task BE-06, BE-14b, BE-15) server tạo bản nháp mới với `reason` tương ứng; khách xem và chấp nhận bằng `POST /api/contracts/{contractId}/accept`. Thay đổi chỉ có hiệu lực khi khách chấp nhận.
- `fileSha256` là mã SHA-256 (64 ký tự hex thường) của đúng file PDF; cùng dữ liệu sinh lại vẫn ra cùng file.

Các giá trị cố định:

| Trường | Giá trị |
|---|---|
| `status` | `Draft` chờ khách chấp nhận · `Accepted` đã giao kết, đang hiệu lực · `Superseded` bị thay bằng bản mới |
| `reason` | `Initial` bản đầu theo báo giá chốt · `PickupDropoffChanged` khách đổi điểm đón / giao · `DetourAccepted` khách đồng ý trả thêm đi đường vòng · `RecipientChanged` đổi người nhận |

Gợi ý màn hình của khách (trang chi tiết đơn khi `customerStage = QuoteReady`): hiện giá chốt + hạn `quoteExpiresAt`; nút "Xem hợp đồng" mở `fileUrl` (PDF) trong tab mới; ô tích "Tôi đã đọc hợp đồng và Chính sách dịch vụ" (link tới trang Chính sách); nút "Chấp nhận & đặt cọc" gọi `accept-quote` rồi chuyển sang thanh toán cọc. Nếu nhận lỗi "The contract has changed..." thì tải lại hợp đồng và báo khách xem lại bản mới.

### GET /api/bookings/{id}/contract — Hợp đồng hiện hành của đơn

Bản đang chờ khách chấp nhận (`Draft`) nếu có, nếu không là bản đã giao kết (`Accepted`). Kèm link tải PDF có hạn (`fileUrl`, hết hạn lúc `fileUrlExpiresAt`, khoảng vài chục phút: hết hạn thì gọi lại API này).

- **Ai gọi được:** Customer (chủ đơn); nhân sự xem được đơn (chuyên viên chỉ đơn mình phụ trách)
- **Dữ liệu gửi lên:** không có

Response `200` — phần `data`:

```json
{
  "contractId": 12,
  "contractCode": "HD-2026-0001",
  "bookingId": 2,
  "bookingCode": "BKG-2026-0002",
  "version": 1,
  "status": "Draft",
  "reason": "Initial",
  "fileSha256": "5fbcfef0a93c6d1e0b4c7f3a2d9e8b1c6a5f4e3d2c1b0a9f8e7d6c5b4a392817",
  "policyVersion": "1.0",
  "fileUrl": "https://xyz.supabase.co/storage/v1/object/sign/racehorse-files/documents/2026/10/...pdf?token=...",
  "fileUrlExpiresAt": "2026-10-27T09:10:00+00:00",
  "acceptedByUserId": null,
  "acceptedByName": null,
  "acceptedAt": null,
  "acceptedIp": null,
  "acceptedUserAgent": null,
  "acceptedPolicyVersion": null,
  "createdAt": "2026-10-27T08:40:00+00:00"
}
```

| Trường | Ý nghĩa |
|---|---|
| `contractId`, `contractCode`, `version` | Mã hợp đồng (mỗi phiên bản một mã) và số phiên bản |
| `status`, `reason` | Xem bảng giá trị cố định ở trên |
| `fileSha256` | Mã băm file PDF; gửi lại nguyên giá trị này khi chấp nhận |
| `policyVersion` | Phiên bản Chính sách dịch vụ in trong phụ lục |
| `fileUrl`, `fileUrlExpiresAt` | Link tải PDF có hạn |
| `acceptedByName`, `acceptedAt`, `acceptedIp`, `acceptedUserAgent`, `acceptedPolicyVersion` | Bằng chứng giao kết (chỉ có khi `Accepted`) |

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 403 | `You do not have access to this booking.` | Đơn của khách khác / chuyên viên không phụ trách |
| 404 | `Booking BKG-2026-0002 has no contract yet. It is created together with the final quote.` | Đơn chưa có báo giá chốt |

### GET /api/bookings/{id}/contracts — Lịch sử các phiên bản

Mọi phiên bản của đơn, mới nhất trước (không có `fileUrl`; tải từng bản bằng API dưới). Quyền như API trên.

### GET /api/contracts/{contractId}/file — Tải PDF một phiên bản

Trả `302` chuyển hướng tới link tải có hạn của file. Gọi kèm token như mọi API (ví dụ `axios.get(url, { responseType: 'blob' })`, trình duyệt tự theo chuyển hướng). Quyền như xem đơn; khách khác nhận `403`.

### POST /api/contracts/{contractId}/accept — Khách chấp nhận bản hợp đồng sửa đổi

Dùng cho các bản **sau** khi hợp đồng đầu đã giao kết (bản đầu luôn chấp nhận cùng báo giá bằng `accept-quote`). Bản cũ đang `Accepted` thành `Superseded`; khách nhận email.

- **Ai gọi được:** Customer (chủ đơn)
- **Dữ liệu gửi lên:** JSON

```json
{
  "contractSha256": "9a1c...e4",
  "acceptPolicy": true,
  "policyVersion": "1.0"
}
```

Response `200`: `data` là hợp đồng (dạng như trên) với `status = Accepted`.

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `The first contract is accepted together with the quote: use POST /api/bookings/{id}/accept-quote.` | Đơn chưa có hợp đồng nào được giao kết |
| 400 | `The contract has changed since you opened it. Please review the latest version.` | Không phải bản nháp hiện hành, hoặc mã băm không khớp |
| 400 | `You must accept the service policy to continue.` / `The service policy has changed ...` | Chưa tích chính sách / sai phiên bản |
| 403 | `You can only accept the contract of your own booking.` | Hợp đồng của khách khác |
| 404 | `Contract with ID 99 was not found.` | Không có bản hợp đồng đó |

### Vận đơn hàng không của chuyến

Chuyến chở ngựa của đơn **hàng không** (`transportMode = Air`) có thêm **vận đơn** (air waybill): điều phối nhập số vận đơn và tải bản scan lên (`POST /api/files`, `category = documents`, rồi gửi `path`).

`GET /api/trips/{id}/air-waybill` — nhân sự (trừ tài xế) hoặc khách có ngựa trên chuyến. `PUT /api/trips/{id}/air-waybill` — FleetCoordinator, Admin:

```json
{
  "airWaybillNumber": "880-12345675",
  "airWaybillFilePath": "documents/2026/10/0f8e7d6c5b4a39281706f5e4d3c2b1a0.pdf"
}
```

`airWaybillFilePath` không bắt buộc (bỏ trống thì giữ file cũ). Response `200` — phần `data`: `{ "tripId": 1, "tripCode": "TRP-2026-0001", "airWaybillNumber": "880-12345675", "airWaybillFilePath": "documents/...pdf", "airWaybillFileUrl": "https://...signed..." }`.

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Trip TRP-2026-0003 carries no horse of an Air booking, so it has no air waybill.` | Chuyến chỉ chở đơn đường bộ |
| 400 | `AirWaybillFilePath is not a valid uploaded file path (...)` | Đường dẫn file không do `POST /api/files` trả về |
| 400 | `Trip TRP-2026-0001 is already Completed.` | Chuyến đã xong / đã hủy |
| 403 | `You do not have access to this trip.` | Khách không có ngựa trên chuyến |

---

## 8. Flow 2 – Hồ sơ kiểm dịch

Mỗi con ngựa trong một đơn đã duyệt có một **hồ sơ kiểm dịch** (dossier). Hồ sơ gồm các giấy tờ mà nước đi và nước đến yêu cầu. Khách nộp từng giấy, chuyên viên duyệt hoặc từ chối từng giấy; đủ giấy thì chuyên viên nộp cho cơ quan chức năng và ghi nhận kết quả thông quan. Ngựa chỉ được lên đường khi hồ sơ đã `Cleared`.

Vòng đời của một hồ sơ:

```
Draft ──(chuyên viên yêu cầu nộp)──> AwaitingDocs ──(khách nộp giấy)──> Reviewing
                                          ▲                                 │
                                          └───(một giấy bị từ chối)─────────┤
                                                                            │ (mọi giấy bắt buộc đã duyệt)
                                                                            ▼
                              Cleared <──(thông quan)── SubmittedToAuthorities

Issue: một giấy bị từ chối 3 lần, hoặc cơ quan chức năng không chấp nhận.
       Chuyên viên bấm "yêu cầu nộp" để mở lại hồ sơ (về AwaitingDocs).
```

Thứ tự gọi thông thường:

1. Chuyên viên: `GET /api/dossiers` rồi `GET /api/dossiers/{id}` để xem checklist.
2. Chuyên viên: `POST /api/dossiers/{id}/request-documents` để yêu cầu khách nộp.
3. Khách: `GET /api/dossiers/{id}`, rồi với mỗi dòng checklist gọi `POST /api/dossiers/{id}/documents`.
4. Chuyên viên: `POST /api/documents/{id}/approve` hoặc `/reject` cho từng giấy.
5. Chuyên viên: khi `isReadyToSubmit` là `true`, gọi `POST /api/dossiers/{id}/submit-to-authorities`.
6. Chuyên viên: `POST /api/dossiers/{id}/clear` khi có kết quả thông quan.

Mọi API ghi trong mục này trả về **chi tiết hồ sơ** mới nhất (dạng của `GET /api/dossiers/{id}`).

Quyền: chỉ chuyên viên **đang phụ trách** hồ sơ (và `Admin`) mới thao tác được; chuyên viên khác nhận 403 `Only the specialist in charge of this dossier can do this.`

### GET /api/dossiers — Danh sách hồ sơ

Khách thấy hồ sơ của ngựa mình; chuyên viên thấy hồ sơ mình phụ trách; quản lý, điều phối, Admin thấy tất cả.

- **Ai gọi được:** Customer, LogisticsManager, TransportSpecialist, FleetCoordinator, Admin

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `status` | chuỗi | Không | Lọc theo trạng thái hồ sơ, ví dụ `Reviewing` |
| `bookingId` | số | Không | Chỉ lấy hồ sơ của một đơn |
| `search` | chuỗi | Không | Từ khóa tìm kiếm |
| `sort` | chuỗi | Không | Trường sắp xếp, thêm `-` phía trước để giảm dần |
| `page` | số | Không | Trang, bắt đầu từ 1 (mặc định 1) |
| `size` | số | Không | Số dòng mỗi trang, 1–100 (mặc định 10) |

Ví dụ: `GET /api/dossiers?bookingId=2`. `search` tìm theo mã hồ sơ, tên ngựa, mã vi mạch, mã đơn.

Response `200` — phần `data`:

```json
[
  {
    "dossierId": 4,
    "dossierCode": "DOS-2026-0004",
    "status": "Draft",
    "bookingHorseId": 4,
    "bookingId": 2,
    "bookingCode": "BKG-2026-0002",
    "customerName": "Jane Smith",
    "horseId": 2,
    "horseName": "Golden Pegasus (Kim Mã)",
    "microchipNumber": "982000412345672",
    "pickupCountryCode": "VN",
    "dropoffCountryCode": "CN",
    "transitCountryCodes": [],
    "pickupWindowStart": "2026-11-24T17:00:00+00:00",
  "pickupWindowEnd": "2026-11-29T17:00:00+00:00",
    "specialistUserId": 2,
    "specialistName": "Nguyễn Thị Mai",
    "clearanceNumber": null,
    "clearedAt": null,
    "createdAt": "2026-10-08T07:24:30+00:00"
  }
]
```

### GET /api/dossiers/{id} — Chi tiết hồ sơ và checklist giấy tờ

Trả về hồ sơ kèm **checklist**: danh sách giấy tờ phải có, đã ghép sẵn với giấy khách đã nộp. Dựng màn hình nộp giấy tờ và màn hình thẩm định từ `checklist`.

- **Ai gọi được:** Như danh sách hồ sơ

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã hồ sơ (`dossierId`) |

Response `200` — phần `data`:

```json
{
  "dossierId": 4,
  "dossierCode": "DOS-2026-0004",
  "status": "Reviewing",
  "bookingHorseId": 4,
  "bookingId": 2,
  "bookingCode": "BKG-2026-0002",
  "customerName": "Jane Smith",
  "horseId": 2,
  "horseName": "Golden Pegasus (Kim Mã)",
  "microchipNumber": "982000412345672",
  "pickupCountryCode": "VN",
  "dropoffCountryCode": "CN",
  "transitCountryCodes": [],
  "pickupWindowStart": "2026-11-24T17:00:00+00:00",
  "pickupWindowEnd": "2026-11-29T17:00:00+00:00",
  "specialistUserId": 2,
  "specialistName": "Nguyễn Thị Mai",
  "clearanceNumber": null,
  "clearedAt": null,
  "createdAt": "2026-10-08T07:24:30+00:00",
  "passportNumber": "704001000000002",
  "issueReason": null,
  "checklist": [
    {
      "docTypeId": 2,
      "docTypeCode": "COGGINS_EIA",
      "docTypeName": "Xét nghiệm thiếu máu truyền nhiễm (EIA/Coggins)",
      "isMandatory": true,
      "validityDays": 30,
      "regulationNote": "Xét nghiệm EIA âm tính trong 30 ngày",
      "requiredBy": [
        "CN Import"
      ],
      "state": "Pending",
      "latestDocument": {
        "documentId": 4,
        "docTypeId": 2,
        "docTypeCode": "COGGINS_EIA",
        "docTypeName": "Xét nghiệm thiếu máu truyền nhiễm (EIA/Coggins)",
        "documentNumber": "DOC-4-2",
        "fileUrl": "https://abcd.supabase.co/storage/v1/object/sign/files/documents/2026/10/398ccb....pdf?token=eyJ...",
        "status": "Pending",
        "correctionNote": null,
        "expiryDate": "2027-12-31",
        "rejectionCount": 0,
        "uploadedByUserId": 5,
        "uploadedByName": "Jane Smith",
        "uploadedAt": "2026-10-08T07:24:30+00:00",
        "reviewedByName": null,
        "reviewedAt": null
      }
    },
    {
      "docTypeId": 3,
      "docTypeCode": "HEALTH_CERT",
      "docTypeName": "Giấy chứng nhận kiểm dịch xuất khẩu",
      "isMandatory": true,
      "validityDays": 10,
      "regulationNote": "Cục Thú y cấp trong 10 ngày trước ngày đi",
      "requiredBy": [
        "VN Export"
      ],
      "state": "Missing",
      "latestDocument": null
    }
  ],
  "requiredCount": 4,
  "approvedCount": 0,
  "isReadyToSubmit": false,
  "documents": [
    {
      "documentId": 4,
      "docTypeId": 2,
      "docTypeCode": "COGGINS_EIA",
      "docTypeName": "Xét nghiệm thiếu máu truyền nhiễm (EIA/Coggins)",
      "documentNumber": "DOC-4-2",
      "fileUrl": "https://abcd.supabase.co/storage/v1/object/sign/files/documents/2026/10/398ccb....pdf?token=eyJ...",
      "status": "Pending",
      "correctionNote": null,
      "expiryDate": "2027-12-31",
      "rejectionCount": 0,
      "uploadedByUserId": 5,
      "uploadedByName": "Jane Smith",
      "uploadedAt": "2026-10-08T07:24:30+00:00",
      "reviewedByName": null,
      "reviewedAt": null
    }
  ]
}
```

(Ví dụ đã rút gọn: `checklist` thực tế có 4 dòng.)

| Trường | Ý nghĩa |
|---|---|
| `status` | Trạng thái hồ sơ (mục 3) |
| `requiredCount`, `approvedCount` | Số giấy bắt buộc và số giấy bắt buộc đã được duyệt. Dùng làm thanh tiến độ |
| `isReadyToSubmit` | `true` khi mọi giấy bắt buộc đã `Approved`. Dùng để bật nút "Nộp cơ quan chức năng" |
| `issueReason` | Lý do hồ sơ có vấn đề (khi `status` là `Issue`) |
| `clearanceNumber`, `clearedAt` | Số và thời điểm thông quan (khi `Cleared`) |
| `checklist[]` | Mỗi dòng là một loại giấy cần có |
| `checklist[].docTypeId`, `docTypeName` | Loại giấy; `docTypeId` dùng khi nộp |
| `checklist[].isMandatory` | Giấy bắt buộc hay không |
| `checklist[].validityDays` | Giấy phải còn hiệu lực trong bao nhiêu ngày; `null` là không quy định |
| `checklist[].regulationNote` | Ghi chú quy định, nên hiện cho khách đọc |
| `checklist[].requiredBy` | Nước nào yêu cầu, theo thứ tự đường đi: `VN Export` (nước đi), `CN Transit` (nước quá cảnh), `RU Import` (nước đến) |
| `transitCountryCodes` | Nước quá cảnh của đơn; mỗi nước có thể thêm giấy tờ hướng `Transit` vào checklist (ví dụ Giấy phép quá cảnh) |
| `checklist[].state` | `Missing` (chưa nộp), `Pending` (chờ duyệt), `Approved`, `Rejected` |
| `checklist[].latestDocument` | Giấy đã nộp cho dòng này; `null` nếu chưa nộp |
| `latestDocument.documentId` | Mã giấy, dùng khi duyệt / từ chối |
| `latestDocument.fileUrl` | Link xem file |
| `latestDocument.correctionNote` | Hướng dẫn sửa khi bị từ chối |
| `latestDocument.rejectionCount` | Số lần đã bị từ chối (tối đa 3) |
| `documents[]` | Tất cả giấy đã nộp của hồ sơ |

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 403 | `You do not have access to this dossier.` | Hồ sơ của khách khác, hoặc chuyên viên không phụ trách |

### POST /api/dossiers/{id}/request-documents — Yêu cầu khách nộp giấy tờ

Chuyển hồ sơ sang `AwaitingDocs` và gửi thông báo cho khách, liệt kê các giấy còn thiếu. Cũng dùng để mở lại hồ sơ đang `Issue`.

- **Ai gọi được:** Chuyên viên phụ trách, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã hồ sơ |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `note` | chuỗi | Không | Lời nhắn thêm cho khách, tối đa 500 ký tự |

Ví dụ request:

```json
{
  "note": "Vui lòng nộp bản scan màu, đủ 4 góc."
}
```

Response `200`: chi tiết hồ sơ với `status` là `AwaitingDocs`. Không có lời nhắn thì gửi `{}`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Documents can no longer be requested for a dossier in status Cleared.` | Hồ sơ đã nộp cơ quan hoặc đã thông quan |

### POST /api/dossiers/{id}/documents — Nộp hoặc nộp lại một giấy tờ

Nộp file cho **một** loại giấy của hồ sơ. Mỗi loại giấy chỉ có một bản: nộp lại (sau khi bị từ chối) là thay file cũ. Hồ sơ chuyển sang `Reviewing` và chuyên viên nhận thông báo.

- **Ai gọi được:** Chủ ngựa (Customer), chuyên viên phụ trách, Admin
- **Dữ liệu gửi lên:** `multipart/form-data` (xem mục 2.5)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã hồ sơ |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `docTypeId` | số | Có | Loại giấy, lấy từ `checklist[].docTypeId` |
| `documentNumber` | chuỗi | Không | Số hiệu ghi trên giấy, tối đa 100 ký tự |
| `expiryDate` | ngày | Không | Ngày hết hạn của giấy, dạng `2027-01-15` |
| `file` | file | Có | File giấy tờ: `pdf`, `jpg`, `jpeg`, `png`, tối đa 10 MB |

Ví dụ request:

```js
const form = new FormData();
form.append("docTypeId", "2");
form.append("documentNumber", "COGGINS-LAB-2026-888");
form.append("expiryDate", "2027-01-15");
form.append("file", fileInput.files[0]);

const res = await api.post(`/api/dossiers/${dossierId}/documents`, form);
```

Response `200`: chi tiết hồ sơ; dòng checklist tương ứng có `state` là `Pending` và `latestDocument.fileUrl` là link file vừa nộp.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Documents cannot be uploaded while the dossier is in status Cleared.` | Hồ sơ không còn nhận giấy (chỉ nhận khi `Draft`, `AwaitingDocs`, `Reviewing`) |
| 400 | `'...' has already been approved and cannot be replaced.` | Giấy này đã được duyệt |
| 400 | `'...' was rejected 3 times and can no longer be resubmitted. Please contact the specialist.` | Khách đã hết lượt nộp lại |
| 400 | `The document has already expired.` | `expiryDate` ở quá khứ |
| 400 | `File is required.` | Không gửi file |
| 403 | `You cannot upload documents to this dossier.` | Không phải chủ ngựa hay chuyên viên phụ trách |

### POST /api/documents/{id}/approve — Duyệt một giấy tờ

Duyệt một giấy đang `Pending`.

- **Ai gọi được:** Chuyên viên phụ trách, Admin
- **Dữ liệu gửi lên:** không có (không cần body)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã giấy (`documentId`, lấy từ `latestDocument.documentId`). **Không phải** mã hồ sơ |

Response `200`: chi tiết hồ sơ; `approvedCount` tăng lên.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `'...' expires on 2026-11-01, before the end of the pickup window 2026-11-30. Reject it and ask for a valid document.` | Giấy hết hạn trước ngày cuối của khoảng pickup |
| 400 | `Only a pending document can be reviewed (current status: Approved).` | Giấy đã được xử lý rồi |

### POST /api/documents/{id}/reject — Từ chối một giấy tờ

Từ chối một giấy đang `Pending`, kèm hướng dẫn sửa. Khách nhận thông báo. Hồ sơ quay về `AwaitingDocs`; nếu đây là lần từ chối thứ 3 của giấy này thì hồ sơ chuyển `Issue` và quản lý được báo.

- **Ai gọi được:** Chuyên viên phụ trách, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã giấy (`documentId`) |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `correctionNote` | chuỗi | Có | Hướng dẫn khách sửa, tối đa 500 ký tự |

Ví dụ request:

```json
{
  "correctionNote": "Bản scan bị mờ, vui lòng chụp lại."
}
```

Response `200`: chi tiết hồ sơ; dòng checklist có `state` là `Rejected`, `latestDocument.correctionNote` là nội dung vừa nhập.

### POST /api/dossiers/{id}/submit-to-authorities — Nộp hồ sơ cho cơ quan chức năng

Ghi nhận hồ sơ đã được nộp cho cơ quan thú y / hải quan. Chỉ làm được khi mọi giấy bắt buộc đã được duyệt.

- **Ai gọi được:** Chuyên viên phụ trách, Admin
- **Dữ liệu gửi lên:** không có (không cần body)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã hồ sơ |

Response `200`: chi tiết hồ sơ với `status` là `SubmittedToAuthorities`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Every mandatory document must be approved first. Outstanding: ...` | Còn giấy bắt buộc chưa duyệt; `message` liệt kê từng giấy |

### POST /api/dossiers/{id}/clear — Ghi nhận thông quan

Ghi nhận cơ quan chức năng đã cho phép ngựa xuất phát. Khi mọi con ngựa của đơn đã thông quan, khách và các điều phối viên nhận thông báo.

- **Ai gọi được:** Chuyên viên phụ trách, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã hồ sơ |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `clearanceNumber` | chuỗi | Có | Số giấy thông quan, tối đa 50 ký tự |

Ví dụ request:

```json
{
  "clearanceNumber": "CN-QT-2026-000123"
}
```

Response `200`: chi tiết hồ sơ với `status` là `Cleared`, có `clearanceNumber` và `clearedAt`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Only a dossier submitted to the authorities can be cleared (current status: Reviewing).` | Chưa nộp cơ quan chức năng |

### POST /api/dossiers/{id}/issue — Báo hồ sơ có vấn đề

Đánh dấu hồ sơ có vấn đề, ví dụ cơ quan chức năng không chấp nhận. Khách và quản lý nhận thông báo kèm lý do.

- **Ai gọi được:** Chuyên viên phụ trách, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã hồ sơ |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `reason` | chuỗi | Có | Mô tả vấn đề, tối đa 2000 ký tự |

Ví dụ request:

```json
{
  "reason": "Cơ quan thú y yêu cầu bổ sung kết quả xét nghiệm."
}
```

Response `200`: chi tiết hồ sơ với `status` là `Issue` và `issueReason`. Để tiếp tục, gọi `request-documents`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `A dossier in status Cleared cannot be flagged with an issue.` | Hồ sơ đã thông quan, hoặc đang ở `Issue` rồi |

---

## 9. Flow 3 – Lập kế hoạch chuyến

Điều phối viên gom ngựa của các đơn đã duyệt thành **chuyến** (trip): chọn xe, chọn người lái và áp tải, xếp từng con ngựa vào ô chuồng, và vạch lộ trình gồm các **mốc** (checkpoint). Chuyến được lập ở dạng bản nháp, trình quản lý duyệt, rồi mới lên lịch.

Vòng đời khi lập kế hoạch:

```
Draft ──(điều phối trình duyệt)──> PendingApproval ──(quản lý duyệt)──> Scheduled
  ▲                                      │
  └────────(quản lý trả về)──────────────┘      bị trả về lần thứ 3 thì chuyến thành Cancelled
```

Thứ tự gọi thông thường:

1. `GET /api/trips/unplanned-horses` để xem hàng chờ ngựa chưa xếp chuyến.
2. `GET /api/trips/available-vehicles` và `GET /api/trips/available-crew` để xem xe và người còn rảnh trong khoảng thời gian dự kiến.
3. (Nên dùng, nhất là khi ghép nhiều đơn) `POST /api/trips/suggest-route` để hệ thống gợi ý lộ trình và giờ dự kiến; chép `checkpoints` của gợi ý vào bản nháp.
4. `POST /api/trips` để tạo bản nháp.
5. Chỉnh sửa nếu cần: `PUT /api/trips/{id}`, các API mốc lộ trình.
6. `POST /api/trips/{id}/submit` để trình duyệt.
7. Quản lý: `POST /api/trips/{id}/approve` hoặc `/reject`.

**Ghép nhiều đơn vào một chuyến.** Một chuyến chở được ngựa của nhiều đơn (cùng phương thức Ground / Air), mỗi đơn có thể được đón và trả ở chỗ riêng:

- Mốc `PickUp` (điểm đón) và `DropOff` (điểm trả) luôn gắn với **một đơn** qua trường `bookingId`. Các loại mốc khác để `bookingId` trống.
- Đơn **không có** mốc `PickUp` riêng thì lên xe ở mốc `Origin`; **không có** mốc `DropOff` riêng thì được giao ở mốc `Destination` (mốc cuối). Chuyến chỉ có một đơn thì không cần hai loại mốc này.
- Mỗi đơn tối đa một `PickUp` và một `DropOff`, đón phải đứng trước trả.
- Khi trình / duyệt, hệ thống kiểm tra **theo từng đơn**: giờ tới điểm đón của đơn nằm trong khoảng pickup khách chọn, và giờ tới điểm trả của đơn không trễ hạn giao (`deliveryDeadline`) của đơn đó. Đơn được trả giữa đường không bị ràng buộc bởi giờ kết thúc của cả chuyến.
- Trên đường, đoàn xe ký biên bản bàn giao (e-POD) cho đơn ngay tại điểm trả của đơn đó (mục 12); đơn hoàn tất luôn mà không chờ đóng chuyến.

Mọi API ghi trong mục này trả về **chi tiết chuyến** mới nhất (dạng của `GET /api/trips/{id}`).

Quyền sửa: chỉ điều phối viên **đã lập chuyến đó** (và `Admin`) mới sửa được bản nháp; người khác nhận 403 `Only the coordinator who planned this trip can change it.` Chỉ sửa được khi chuyến còn `Draft`.

### GET /api/trips/unplanned-horses — Hàng chờ xếp chuyến

Các con ngựa thuộc đơn đã duyệt mà chưa nằm trên chuyến nào. Mọi điều phối viên thấy chung một hàng chờ.

- **Ai gọi được:** FleetCoordinator, LogisticsManager, Admin

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `transportMode` | chuỗi | Không | `Ground` hoặc `Air` |
| `bookingId` | số | Không | Chỉ lấy ngựa của một đơn |

Response `200` — phần `data`:

```json
[
  {
    "bookingHorseId": 3,
    "bookingId": 2,
    "bookingCode": "BKG-2026-0002",
    "customerName": "Jane Smith",
    "transportMode": "Ground",
    "pickupAddress": "Trang trại Yên Bài, Ba Vì, Hà Nội",
    "pickupCountryCode": "VN",
    "dropoffAddress": "Trường đua Tùng Hóa, Quảng Châu",
    "dropoffCountryCode": "CN",
    "transitCountryCodes": [],
    "pickupWindowStart": "2026-11-24T17:00:00+00:00",
    "pickupWindowEnd": "2026-11-29T17:00:00+00:00",
    "deliveryDeadline": "2026-12-05T17:00:00+00:00",
    "requiresClimateControl": true,
    "horseId": 1,
    "horseName": "Red Flash (Tia Chớp Đỏ)",
    "microchipNumber": "982000412345671",
    "specialCareRequirements": "Cần lót đệm rơm dày, nhiệt độ cabin 16-19°C",
    "stallClass": "Shared",
    "dossierStatus": "Cleared"
  }
]
```

`bookingHorseId` là giá trị cần gửi khi xếp ngựa vào chuyến. `pickupWindowStart` → `pickupWindowEnd` và `deliveryDeadline` là lịch của đơn: chuyến phải khởi hành trong khoảng pickup và kết thúc trước hạn giao của mọi đơn trên chuyến. `transitCountryCodes` là các nước ngựa đi qua: mỗi nước nên có mốc cửa khẩu (`BorderGate`) trên lộ trình. `dossierStatus` cho biết hồ sơ của con ngựa đã xong chưa: vẫn xếp chuyến được khi hồ sơ chưa `Cleared`, nhưng chuyến sẽ không xuất phát được cho tới khi mọi hồ sơ `Cleared`.

### GET /api/trips/available-vehicles — Xe còn rảnh

Các xe không bảo trì và không vướng chuyến nào khác trong khoảng thời gian đã cho.

- **Ai gọi được:** FleetCoordinator, LogisticsManager, Admin

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `from` | ngày giờ | Có | Thời điểm dự kiến bắt đầu chuyến |
| `to` | ngày giờ | Có | Thời điểm dự kiến kết thúc, phải sau `from` |
| `transportMode` | chuỗi | Không | `Air` thì chỉ trả về xe loại `AirStall`; `Ground` thì trả các loại còn lại |

Ví dụ: `GET /api/trips/available-vehicles?from=2026-11-07T06:00:00%2B07:00&to=2026-11-09T18:00:00%2B07:00&transportMode=Ground`

Dấu `+` của múi giờ phải viết thành `%2B` trên đường dẫn. Dùng `params` của axios (`api.get(url, { params: { from, to } })`) thì axios tự làm việc này.

Response `200` — phần `data`:

```json
[
  {
    "assetId": 1,
    "assetCode": "29B-888.99",
    "assetType": "HorseTruck_AirSuspension",
    "capacityHorses": 6,
    "hasClimateControl": true,
    "hasGpsTracker": true,
    "lastSanitizationDate": "2026-10-08T07:24:28+00:00"
  }
]
```

`assetId` là giá trị gửi vào `vehicleId` khi tạo chuyến. `capacityHorses` là số ngựa tối đa xe chở được.

### GET /api/trips/available-crew — Tài xế và áp tải còn rảnh

Các tài khoản `DriverEscort` đang hoạt động và không vướng chuyến nào khác trong khoảng thời gian đã cho.

- **Ai gọi được:** FleetCoordinator, LogisticsManager, Admin

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `from` | ngày giờ | Có | Thời điểm bắt đầu |
| `to` | ngày giờ | Có | Thời điểm kết thúc |

Response `200` — phần `data`:

```json
[
  {
    "userId": 4,
    "fullName": "Phạm Văn Đức",
    "email": "duc.pham@transport.com",
    "phoneNumber": "+84912000004",
    "role": "DriverEscort",
    "isActive": true,
    "createdAt": "2026-10-08T07:24:28+00:00"
  }
]
```

`userId` là giá trị gửi vào `crew[].userId` khi tạo chuyến.

### POST /api/trips/suggest-route — Gợi ý lộ trình

Hệ thống đề xuất lộ trình cho các con ngựa đã chọn (một hoặc nhiều đơn). Kết quả **chỉ là gợi ý**, không lưu gì: frontend hiển thị cho điều phối viên xem, rồi chép `checkpoints` vào body của `POST /api/trips` / `PUT /api/trips/{id}` (có thể sửa trước khi lưu).

Cách hệ thống xếp:

1. Mốc `Origin` là điểm đón của đơn có khoảng pickup sớm nhất. Đơn đón cùng chỗ thì lên xe ở đó luôn.
2. Các điểm đón còn lại: lần lượt tới chỗ **gần nhất** → mốc `PickUp`.
3. Các điểm trả: lần lượt tới chỗ gần nhất → mốc `DropOff`; điểm trả cuối cùng là `Destination`.
4. Đổi nước giữa hai điểm thì chèn mốc `BorderGate` (đi theo danh sách nước quá cảnh của đơn nếu có). Tên / địa chỉ cửa khẩu chỉ là tạm, điều phối viên nên sửa lại cho đúng cửa khẩu thật.
5. Giờ dự kiến tính theo quãng đường (bản đồ Goong / OSRM, dự phòng bảng cự ly) và các tham số ước tính thời gian của mục Transit: tốc độ xe tải, nghỉ giữa chặng, nghỉ qua đêm, 6 giờ mỗi cửa khẩu, 1 giờ xếp / dỡ ngựa mỗi điểm. Đường bay: chặng từ điểm đón cuối tới điểm trả đầu là chặng bay.

- **Ai gọi được:** FleetCoordinator, Admin
- **Dữ liệu gửi lên:** JSON

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `bookingHorseIds` | mảng số | Có | Các con ngựa định xếp lên chuyến (`bookingHorseId` từ hàng chờ). Không được trộn đơn Ground với Air |
| `plannedStartDate` | ngày giờ | Có | Giờ xuất phát dự kiến (giờ tới mốc `Origin`) |
| `driverCount` | số | Không | Số tài xế thay ca, 1–4 (mặc định 1). Từ 2 tài xế được lái nhiều giờ hơn mỗi ngày nên giờ dự kiến ngắn hơn |

Ví dụ request:

```json
{
  "bookingHorseIds": [201, 202, 203],
  "plannedStartDate": "2026-11-10T06:00:00+07:00",
  "driverCount": 1
}
```

Response `200` (2 đơn cùng đón ở Hà Nội: BKG-2026-0201 trả ở Lạng Sơn, BKG-2026-0202 trả ở Nam Ninh, Trung Quốc):

```json
{
  "success": true,
  "message": "Route suggested successfully",
  "data": {
    "transportMode": "Ground",
    "plannedStartDate": "2026-11-09T23:00:00+00:00",
    "suggestedEndDate": "2026-11-10T17:56:00+00:00",
    "totalDistanceKm": 500,
    "totalHours": 18.9,
    "checkpoints": [
      {
        "checkpointName": "Hà Nội",
        "checkpointType": "Origin",
        "address": "Hà Nội",
        "plannedTime": "2026-11-09T23:00:00+00:00",
        "mandatoryRestMinutes": 60,
        "bookingId": null,
        "bookingCode": null,
        "legDistanceKm": 0
      },
      {
        "checkpointName": "Drop off BKG-2026-0201 - Lạng Sơn",
        "checkpointType": "DropOff",
        "address": "Lạng Sơn",
        "plannedTime": "2026-11-10T02:42:00+00:00",
        "mandatoryRestMinutes": 60,
        "bookingId": 201,
        "bookingCode": "BKG-2026-0201",
        "legDistanceKm": 150
      },
      {
        "checkpointName": "Border VN - CN",
        "checkpointType": "BorderGate",
        "address": "Border crossing VN -> CN",
        "plannedTime": "2026-11-10T03:44:00+00:00",
        "mandatoryRestMinutes": 0,
        "bookingId": null,
        "bookingCode": null,
        "legDistanceKm": 0
      },
      {
        "checkpointName": "Nanning",
        "checkpointType": "Destination",
        "address": "Nanning",
        "plannedTime": "2026-11-10T16:56:00+00:00",
        "mandatoryRestMinutes": 0,
        "bookingId": null,
        "bookingCode": null,
        "legDistanceKm": 350
      }
    ],
    "bookings": [
      {
        "bookingId": 201,
        "bookingCode": "BKG-2026-0201",
        "pickupAt": "2026-11-09T23:00:00+00:00",
        "estimatedDeliveryAt": "2026-11-10T02:42:00+00:00",
        "deliveryDeadline": "2026-11-12T11:00:00+00:00",
        "onTime": true,
        "routeDistanceKm": 150,
        "directDistanceKm": 160,
        "detourRatio": 0.94
      },
      {
        "bookingId": 202,
        "bookingCode": "BKG-2026-0202",
        "pickupAt": "2026-11-09T23:00:00+00:00",
        "estimatedDeliveryAt": "2026-11-10T16:56:00+00:00",
        "deliveryDeadline": "2026-11-13T11:00:00+00:00",
        "onTime": true,
        "routeDistanceKm": 500,
        "directDistanceKm": 380,
        "detourRatio": 1.32
      }
    ],
    "warnings": [
      {
        "code": "Detour",
        "severity": "Warning",
        "message": "Booking BKG-2026-0202 travels 500 km on this route instead of 380 km direct (1.32x, limit 1.3x).",
        "bookingCode": "BKG-2026-0202",
        "params": {
          "routeKm": "500",
          "directKm": "380",
          "ratio": "1.32",
          "limit": "1.3",
          "bookingCode": "BKG-2026-0202"
        }
      }
    ]
  },
  "errors": null
}
```

Ý nghĩa các trường:

| Trường | Ý nghĩa |
|---|---|
| `suggestedEndDate` | Giờ tới mốc cuối + thời gian dỡ ngựa. Dùng làm `plannedEndDate` của chuyến |
| `totalDistanceKm`, `totalHours` | Tổng quãng đường và tổng thời gian dự kiến |
| `checkpoints[]` | Các mốc theo thứ tự đi, cùng dạng với `checkpoints` khi tạo chuyến (thêm `bookingCode`, `legDistanceKm` để hiển thị) |
| `checkpoints[].mandatoryRestMinutes` | Ở điểm đón / trả là thời gian xếp / dỡ ngựa |
| `checkpoints[].legDistanceKm` | Quãng đường từ mốc trước tới mốc này (cửa khẩu nằm giữa chặng nên là 0) |
| `bookings[]` | Từng đơn: `pickupAt` giờ đón, `estimatedDeliveryAt` giờ tới điểm trả, `deliveryDeadline` hạn giao, `onTime` kịp hạn hay không |
| `bookings[].routeDistanceKm`, `directDistanceKm`, `detourRatio` | Quãng đường ngựa của đơn thực đi trên tuyến ghép, quãng đường nếu đi thẳng (lúc báo giá), và tỉ lệ giữa hai số |
| `warnings[]` | Cảnh báo khi ghép đơn, xem bảng dưới |
| `warnings[].severity` | `Error`: khi trình duyệt sẽ bị chặn · `Warning`: nên xem lại · `Info`: gợi ý |
| `warnings[].params` | Các giá trị để frontend tự dựng câu theo ngôn ngữ người dùng; `message` là câu tiếng Anh dự phòng. Ngày giờ ở dạng ISO |

Các mã cảnh báo (`warnings[].code`):

| `code` | Mức | Khi nào | `params` |
|---|---|---|---|
| `DeadlineMissed` | Error | Giờ tới điểm trả trễ hơn hạn giao của đơn | `bookingCode`, `eta`, `deadline` |
| `PickupOutsideWindow` | Error | Giờ đón nằm ngoài khoảng pickup khách chọn | `bookingCode`, `pickupAt`, `from`, `to` |
| `Detour` | Warning | Đơn phải đi vòng: quãng đường trên tuyến ghép / đi thẳng > 1,3 (cấu hình `Transit:MaxDetourRatio`) | `bookingCode`, `routeKm`, `directKm`, `ratio`, `limit` |
| `PickupsFarApart` | Warning | Điểm đón của một đơn cách điểm xuất phát quá 150 km (cấu hình `Transit:MaxPickupSpreadKm`) | `bookingCode`, `km`, `limit` |
| `DifferentPickupCountry` | Warning | Các đơn được đón ở nhiều nước khác nhau | `countries` |
| `DossierNotCleared` | Warning | Hồ sơ của một con ngựa chưa thông quan: vẫn lập kế hoạch được nhưng xe chưa xuất phát được | `horseName`, `bookingCode`, `status` |
| `OvernightRest` | Info | Một chặng dài cần nghỉ qua đêm: nên thêm mốc `RestStop` | `from`, `to`, `km`, `count` |

```js
const res = await api.post("/api/trips/suggest-route", {
  bookingHorseIds: selectedIds,
  plannedStartDate: start.toISOString(),
});
const suggestion = res.data.data;
// Chép vào form lập chuyến
const checkpoints = suggestion.checkpoints.map((c) => ({
  checkpointName: c.checkpointName,
  checkpointType: c.checkpointType,
  address: c.address,
  plannedTime: c.plannedTime,
  mandatoryRestMinutes: c.mandatoryRestMinutes,
  bookingId: c.bookingId,
}));
```

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Choose at least one horse.` | `bookingHorseIds` rỗng |
| 400 | `Booking horse(s) with ID 999 were not found.` | Mã ngựa không tồn tại |
| 400 | `Ground and air bookings cannot be combined on the same trip.` | Trộn đơn đường bộ với đường bay |

### POST /api/trips — Tạo bản nháp chuyến

Tạo một chuyến ở trạng thái `Draft`. Ngay từ lúc tạo, xe, người và ngựa đã được giữ chỗ (không ai khác xếp trùng được). Một người có thể vừa là `Driver` vừa là `Escort` (khai hai dòng).

- **Ai gọi được:** FleetCoordinator, Admin
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `vehicleId` | số | Có | `assetId` của xe |
| `plannedStartDate` | ngày giờ | Có | Giờ khởi hành dự kiến, phải ở tương lai |
| `plannedEndDate` | ngày giờ | Có | Giờ tới đích dự kiến, phải sau giờ khởi hành |
| `plannedCost` | số | Không | Chi phí vận hành dự kiến, không âm |
| `crew` | mảng | Có | Đoàn xe, ít nhất 1 người. Xem bảng dưới |
| `horses` | mảng | Có | Ngựa trên chuyến, ít nhất 1 con. Xem bảng dưới |
| `checkpoints` | mảng | Không | Các mốc lộ trình theo đúng thứ tự đi. Có thể thêm sau |

Mỗi phần tử của `crew`:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `userId` | số | Có | Tài khoản `DriverEscort` |
| `crewRole` | chuỗi | Có | `Driver` (lái xe) hoặc `Escort` (áp tải) |
| `isLead` | true / false | Không | Trưởng nhóm của vai trò đó. Không chọn ai thì người đầu tiên là trưởng nhóm |
| `duty` | chuỗi | Không | Nhiệm vụ cụ thể, tối đa 200 ký tự |

Mỗi phần tử của `horses`:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `bookingHorseId` | số | Có | Lấy từ hàng chờ `unplanned-horses` |
| `stallSlotNumber` | số | Có | Số ô chuồng trên xe, từ 1 đến sức chứa của xe; mỗi ô một con |
| `escortUserId` | số | Không | Người áp tải phụ trách con này: `userId` của một người trong `crew` có `crewRole` = `Escort`. Mỗi người áp tải phụ trách tối đa 3 con. Bỏ trống = hệ thống tự phân (xem bên dưới) |
| `notes` | chuỗi | Không | Ghi chú, tối đa 200 ký tự |

**Phân công áp tải theo chuồng (tự phân):** con nào không gửi `escortUserId` thì hệ thống xếp các con đó theo số ô chuồng và giao cho người áp tải đầu tiên còn chỗ (trưởng nhóm áp tải trước, rồi theo thứ tự trong `crew`). Nghĩa là ô 1–3 cho người thứ nhất, ô 4–6 cho người thứ hai... Kết quả nằm trong `horses[].escortUserId` của response. Người áp tải chỉ ghi được nhật ký thể trạng cho ngựa mình phụ trách (trưởng nhóm áp tải ghi được cho mọi con).

Mỗi phần tử của `checkpoints`:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `checkpointName` | chuỗi | Có | Tên mốc, tối đa 100 ký tự |
| `checkpointType` | chuỗi | Có | `Origin` (mốc đầu), `PickUp`, `RestStop`, `BorderGate`, `DropOff`, `Destination` (mốc cuối) |
| `address` | chuỗi | Có | Địa chỉ, tối đa 255 ký tự |
| `plannedTime` | ngày giờ | Có | Giờ dự kiến tới mốc |
| `mandatoryRestMinutes` | số | Không | Số phút bắt buộc nghỉ tại mốc (0–2880). Đoàn xe chưa nghỉ đủ thì chưa rời mốc được |
| `bookingId` | số | Với `PickUp` / `DropOff` | Đơn được đón / trả ở mốc này (phải có ngựa trên chuyến). Để trống với các loại mốc khác |

Ví dụ request:

```json
{
  "vehicleId": 1,
  "plannedStartDate": "2026-11-07T06:00:00+07:00",
  "plannedEndDate": "2026-11-09T18:00:00+07:00",
  "plannedCost": 4200,
  "crew": [
    {
      "userId": 4,
      "crewRole": "Driver",
      "isLead": true
    },
    {
      "userId": 9,
      "crewRole": "Escort",
      "duty": "Chăm sóc ngựa trên đường"
    }
  ],
  "horses": [
    {
      "bookingHorseId": 3,
      "stallSlotNumber": 1
    },
    {
      "bookingHorseId": 4,
      "stallSlotNumber": 2,
      "escortUserId": 9,
      "notes": "Chuồng riêng"
    }
  ],
  "checkpoints": [
    {
      "checkpointName": "Trại Ba Vì",
      "checkpointType": "Origin",
      "address": "Ba Vì, Hà Nội",
      "plannedTime": "2026-11-07T06:00:00+07:00",
      "mandatoryRestMinutes": 0
    },
    {
      "checkpointName": "Cửa khẩu Hữu Nghị",
      "checkpointType": "BorderGate",
      "address": "Lạng Sơn",
      "plannedTime": "2026-11-07T12:00:00+07:00",
      "mandatoryRestMinutes": 0
    },
    {
      "checkpointName": "Trường đua Tùng Hóa",
      "checkpointType": "Destination",
      "address": "Quảng Châu",
      "plannedTime": "2026-11-09T18:00:00+07:00",
      "mandatoryRestMinutes": 0
    }
  ]
}
```

Response `201`: chi tiết chuyến vừa tạo, xem `GET /api/trips/{id}`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `PlannedStartDate must be in the future.` | Giờ khởi hành ở quá khứ |
| 400 | `Booking BKG-... can only be picked up between ... and ...; PlannedStartDate is outside this window.` | Ngày chạy nằm ngoài khoảng pickup của một đơn trên chuyến (ngày cuối khoảng được tính trọn ngày) |
| 400 | `Booking BKG-... must be delivered by ...; PlannedEndDate is later than that.` | Chuyến kết thúc sau hạn giao của một đơn trên chuyến |
| 400 | `These horses are already on another trip: ...` | Ngựa đã được xếp vào chuyến khác |
| 400 | `Ground and air bookings cannot be combined on the same trip.` | Trộn ngựa của đơn đường bộ và đơn hàng không |
| 400 | `Vehicle 29B-888.99 carries at most 6 horse(s); this plan has 7.` | Quá sức chứa của xe |
| 400 | `Vehicle ... is already booked on trip ... during this period.` | Xe trùng lịch |
| 400 | `Two horses cannot share the same stall slot.` | Hai con ngựa cùng một ô chuồng |
| 400 | `A trip needs at least one driver.` | Đoàn xe không có ai là `Driver` |
| 400 | `4 horse(s) need at least 2 escort(s) (one per 3 horses); this plan has 1.` | Thiếu người áp tải: cứ 3 con ngựa cần 1 `Escort` |
| 400 | `User(s) with ID 18 are not escorts on this trip, so they cannot be put in charge of a horse.` | `escortUserId` không phải người có vai trò `Escort` trong `crew` của chuyến |
| 400 | `An escort can look after at most 3 horses; user(s) with ID 9 have more.` | Chọn tay quá 3 con cho cùng một người áp tải |
| 400 | `These crew members are already assigned during this period: ...` | Người trong đoàn trùng lịch |

### PUT /api/trips/{id} — Sửa bản nháp chuyến

Sửa một chuyến còn `Draft`. Gửi cùng dạng dữ liệu với tạo chuyến.

- `crew` và `horses`: **thay toàn bộ** bằng danh sách mới.
- `checkpoints`: bỏ trống hoặc `null` thì **giữ nguyên** các mốc đang có; có giá trị thì thay toàn bộ.

- **Ai gọi được:** Người lập chuyến, Admin

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã chuyến (`tripId`) |

Dữ liệu gửi lên: JSON, giống `POST /api/trips`.

Response `200`: chi tiết chuyến sau khi sửa. Các lỗi giống tạo chuyến.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Only a draft trip can be changed (current status: Scheduled).` | Chuyến đã trình duyệt hoặc đã lên lịch |
| 403 | `Only the coordinator who planned this trip can change it.` | Không phải người lập chuyến |

### POST /api/trips/{id}/checkpoints — Thêm một mốc lộ trình

Thêm một mốc vào chuyến còn `Draft`.

- **Ai gọi được:** Người lập chuyến, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã chuyến |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `checkpointName` | chuỗi | Có | Tên mốc |
| `checkpointType` | chuỗi | Có | `Origin`, `PickUp`, `RestStop`, `BorderGate`, `DropOff`, `Destination` |
| `address` | chuỗi | Có | Địa chỉ |
| `plannedTime` | ngày giờ | Có | Giờ dự kiến tới mốc |
| `mandatoryRestMinutes` | số | Không | Số phút bắt buộc nghỉ (0–2880) |
| `bookingId` | số | Với `PickUp` / `DropOff` | Đơn được đón / trả ở mốc này; để trống với loại mốc khác |
| `position` | số | Không | Vị trí chèn, 1 là đầu tiên. Bỏ trống thì thêm vào cuối |

Ví dụ request:

```json
{
  "checkpointName": "Trạm nghỉ Nam Ninh",
  "checkpointType": "RestStop",
  "address": "Nam Ninh, Quảng Tây",
  "plannedTime": "2026-11-08T08:00:00+07:00",
  "mandatoryRestMinutes": 0,
  "position": 3
}
```

Response `200`: chi tiết chuyến; `checkpoints[].sequenceOrder` đã được đánh lại.

### PUT /api/trips/{id}/checkpoints/{checkpointId} — Sửa một mốc

Sửa thông tin một mốc của chuyến còn `Draft`. Vị trí của mốc không đổi.

- **Ai gọi được:** Người lập chuyến, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã chuyến |
| `checkpointId` | Mã mốc |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `checkpointName` | chuỗi | Có | Tên mốc |
| `checkpointType` | chuỗi | Có | `Origin`, `PickUp`, `RestStop`, `BorderGate`, `DropOff`, `Destination` |
| `address` | chuỗi | Có | Địa chỉ |
| `plannedTime` | ngày giờ | Có | Giờ dự kiến tới mốc |
| `mandatoryRestMinutes` | số | Không | Số phút bắt buộc nghỉ (0–2880) |
| `bookingId` | số | Với `PickUp` / `DropOff` | Đơn được đón / trả ở mốc này; để trống với loại mốc khác |

Ví dụ request:

```json
{
  "checkpointName": "Trạm nghỉ Nam Ninh",
  "checkpointType": "RestStop",
  "address": "Cao tốc Nam Ninh, Quảng Tây",
  "plannedTime": "2026-11-08T09:00:00+07:00",
  "mandatoryRestMinutes": 0
}
```

Response `200`: chi tiết chuyến.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 404 | `Checkpoint with ID 99 was not found on trip TRP-2026-0003.` | Mốc không thuộc chuyến này |

### DELETE /api/trips/{id}/checkpoints/{checkpointId} — Xóa một mốc

Xóa một mốc khỏi chuyến còn `Draft`. Các mốc còn lại được đánh lại thứ tự.

- **Ai gọi được:** Người lập chuyến, Admin

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã chuyến |
| `checkpointId` | Mã mốc |

Response `200`: chi tiết chuyến.

### PUT /api/trips/{id}/checkpoints/order — Đổi thứ tự các mốc

Sắp lại thứ tự toàn bộ các mốc của chuyến còn `Draft` (ví dụ sau khi người dùng kéo thả).

- **Ai gọi được:** Người lập chuyến, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã chuyến |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `checkpointIds` | mảng số | Có | Mã của **tất cả** các mốc của chuyến, theo thứ tự mới, mỗi mã đúng một lần |

Ví dụ request:

```json
{
  "checkpointIds": [
    9,
    10,
    12,
    11
  ]
}
```

Response `200`: chi tiết chuyến.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `CheckpointIds must contain every checkpoint of this trip exactly once.` | Thiếu hoặc thừa mã mốc |

### POST /api/trips/{id}/submit — Trình duyệt kế hoạch

Chuyển chuyến từ `Draft` sang `PendingApproval` và báo cho quản lý. Hệ thống kiểm tra lộ trình trước khi cho trình.

- **Ai gọi được:** Người lập chuyến, Admin
- **Dữ liệu gửi lên:** không có (không cần body)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã chuyến |

Response `200`: chi tiết chuyến với `overallStatus` là `PendingApproval`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `The route needs at least an origin and a destination checkpoint.` | Chưa có đủ mốc |
| 400 | `The first checkpoint must be of type Origin.` | Mốc đầu không phải `Origin` |
| 400 | `The last checkpoint must be of type Destination.` | Mốc cuối không phải `Destination` |
| 400 | `Checkpoint 'B' must be planned after 'A'.` | Giờ dự kiến của các mốc không tăng dần |
| 400 | `Checkpoint times must fall between the trip's planned start and end.` | Giờ của mốc nằm ngoài khoảng thời gian của chuyến |
| 400 | `Checkpoint 'X' (PickUp) must say which booking is picked up / dropped off there.` | Mốc `PickUp` / `DropOff` thiếu `bookingId` |
| 400 | `Booking BKG-2026-0201 can have at most one PickUp and one DropOff checkpoint.` | Một đơn có hai mốc đón hoặc hai mốc trả |
| 400 | `Booking BKG-2026-0201 must be picked up before it is dropped off.` | Mốc trả đứng trước mốc đón |
| 400 | `Booking BKG-2026-0201 can only be picked up between 2026-11-09 and 2026-11-12; it is planned at 'X' on ...` | Giờ đón của đơn (mốc `PickUp` hoặc `Origin`) nằm ngoài khoảng pickup |
| 400 | `Booking BKG-2026-0201 must be delivered by ...; it reaches 'X' at ...` | Giờ tới điểm trả của đơn (mốc `DropOff` hoặc `Destination`) trễ hạn giao |

`POST /api/trips/{id}/approve` kiểm tra lại đúng các quy tắc này trước khi duyệt.

### POST /api/trips/{id}/approve — Duyệt kế hoạch chuyến

Duyệt một chuyến đang `PendingApproval`. Chuyến thành `Scheduled`, đoàn xe nhận thông báo. Khách của từng đơn trên chuyến nhận thông báo; đơn nào còn phần còn lại chưa trả thì khách nhận thêm email "Thanh toán phần còn lại" (số tiền USD + VND, hạn trả = ngày xuất phát - `balanceDueDaysBeforeDeparture`, nút mở trang đơn).

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** không có (không cần body)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã chuyến |

Response `200`: chi tiết chuyến với `overallStatus` là `Scheduled`, có `approvedByName` và `approvedAt`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Only a trip in status PendingApproval can be approved (current status: Draft).` | Chuyến chưa được trình duyệt |

### POST /api/trips/{id}/reject — Trả kế hoạch về cho điều phối

Trả một chuyến đang `PendingApproval` về `Draft` kèm lý do, để điều phối viên sửa rồi trình lại. Lần trả về thứ 3 thì chuyến bị hủy (`Cancelled`) và ngựa quay lại hàng chờ.

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã chuyến |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `reason` | chuỗi | Có | Lý do, tối đa 500 ký tự |

Ví dụ request:

```json
{
  "reason": "Chi phí dự kiến cao, cần giải trình."
}
```

Response `200`: chi tiết chuyến; `planRejectionCount` tăng 1, `planRejectionReason` là lý do vừa nhập.

### POST /api/trips/{id}/cancel — Hủy chuyến chưa khởi hành

Hủy một chuyến chưa xuất phát (`Draft`, `PendingApproval` hoặc `Scheduled`). Ngựa trên chuyến quay lại hàng chờ xếp chuyến; xe và người được giải phóng.

- **Ai gọi được:** Người lập chuyến, LogisticsManager, Admin
- **Dữ liệu gửi lên:** không có (không cần body)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã chuyến |

Response `200`: chi tiết chuyến với `overallStatus` là `Cancelled`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `A trip can only be cancelled before departure (current status: InTransit).` | Chuyến đã xuất phát |
| 403 | `Only the coordinator who planned this trip or a manager can cancel it.` | Điều phối viên khác hủy chuyến không phải của mình |

### GET /api/trips — Danh sách chuyến

Danh sách chuyến có phân trang. Tài xế / áp tải chỉ thấy các chuyến mình có tên trong đoàn; các vai trò khác thấy tất cả. Mặc định chuyến khởi hành gần nhất lên đầu.

- **Ai gọi được:** Mọi nhân sự (không gồm Customer)

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `status` | chuỗi | Không | Lọc theo trạng thái chuyến, ví dụ `Scheduled` |
| `startFrom` | ngày giờ | Không | Chỉ lấy chuyến khởi hành từ thời điểm này |
| `startTo` | ngày giờ | Không | Chỉ lấy chuyến khởi hành tới thời điểm này |
| `search` | chuỗi | Không | Từ khóa tìm kiếm |
| `sort` | chuỗi | Không | Trường sắp xếp, thêm `-` phía trước để giảm dần |
| `page` | số | Không | Trang, bắt đầu từ 1 (mặc định 1) |
| `size` | số | Không | Số dòng mỗi trang, 1–100 (mặc định 10) |

Ví dụ: `GET /api/trips?status=Scheduled&page=1&size=10`. `search` tìm theo mã chuyến và biển số xe.

Response `200` — phần `data`:

```json
[
  {
    "tripId": 3,
    "tripCode": "TRP-2026-0003",
    "overallStatus": "Scheduled",
    "vehicleId": 1,
    "vehicleCode": "29B-888.99",
    "vehicleType": "HorseTruck_AirSuspension",
    "plannedStartDate": "2026-11-06T23:00:00+00:00",
    "plannedEndDate": "2026-11-09T11:00:00+00:00",
    "actualStartDate": null,
    "actualEndDate": null,
    "horseCount": 2,
    "crew": [
      {
        "userId": 4,
        "fullName": "Phạm Văn Đức",
        "phoneNumber": "+84912000004",
        "crewRole": "Driver",
        "isLead": true,
        "duty": null
      },
      {
        "userId": 9,
        "fullName": "Trần Thị Hoa",
        "phoneNumber": "+84912000077",
        "crewRole": "Escort",
        "isLead": true,
        "duty": "Chăm sóc ngựa trên đường"
      }
    ]
  }
]
```

### GET /api/trips/{id} — Chi tiết chuyến

Toàn bộ thông tin một chuyến: xe, đoàn xe, ngựa kèm yêu cầu chăm sóc, các mốc lộ trình, và các đơn có ngựa trên chuyến. Đây cũng là dạng dữ liệu mà mọi API ghi của chuyến trả về.

- **Ai gọi được:** Mọi nhân sự; tài xế / áp tải chỉ xem được chuyến của mình

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã chuyến (`tripId`) |

Response `200` — phần `data`:

```json
{
  "tripId": 3,
  "tripCode": "TRP-2026-0003",
  "overallStatus": "InTransit",
  "vehicleId": 1,
  "vehicleCode": "29B-888.99",
  "vehicleType": "HorseTruck_AirSuspension",
  "plannedStartDate": "2026-11-06T23:00:00+00:00",
  "plannedEndDate": "2026-11-09T11:00:00+00:00",
  "actualStartDate": "2026-10-08T07:24:31+00:00",
  "actualEndDate": null,
  "horseCount": 2,
  "crew": [
    {
      "userId": 4,
      "fullName": "Phạm Văn Đức",
      "phoneNumber": "+84912000004",
      "crewRole": "Driver",
      "isLead": true,
      "duty": null
    },
    {
      "userId": 9,
      "fullName": "Trần Thị Hoa",
      "phoneNumber": "+84912000077",
      "crewRole": "Escort",
      "isLead": true,
      "duty": "Chăm sóc ngựa trên đường",
      "assignedHorseCount": 2
    }
  ],
  "vehicleCapacity": 6,
  "plannedCost": 4500,
  "actualCost": 0,
  "plannedByUserId": 3,
  "plannedByName": "Lê Hoàng Nam",
  "approvedByUserId": 1,
  "approvedByName": "Trần Quốc Bảo",
  "approvedAt": "2026-10-08T07:24:31+00:00",
  "planRejectionCount": 1,
  "planRejectionReason": null,
  "delayMinutes": 0,
  "onTimeStatus": "OnTime",
  "kpiScore": 100,
  "closedByName": null,
  "closedAt": null,
  "executiveRemarks": null,
  "createdAt": "2026-10-08T07:24:31+00:00",
  "horses": [
    {
      "tripHorseId": 7,
      "stallSlotNumber": 1,
      "notes": null,
      "bookingHorseId": 3,
      "bookingId": 2,
      "bookingCode": "BKG-2026-0002",
      "horseId": 1,
      "horseName": "Red Flash (Tia Chớp Đỏ)",
      "microchipNumber": "982000412345671",
      "gender": "Stallion",
      "color": "Hồng sắc",
      "specialCareRequirements": "Cần lót đệm rơm dày, nhiệt độ cabin 16-19°C",
      "photoUrl": null,
      "stallClass": "Shared",
      "dossierStatus": "Cleared"
    }
  ],
  "checkpoints": [
    {
      "checkpointId": 9,
      "sequenceOrder": 1,
      "checkpointName": "Trại Ba Vì",
      "checkpointType": "Origin",
      "address": "Ba Vì, Hà Nội",
      "plannedTime": "2026-11-06T23:00:00+00:00",
      "mandatoryRestMinutes": 0,
      "actualArrivalTime": "2026-10-08T07:24:31+00:00",
      "actualDepartureTime": "2026-10-08T07:24:31+00:00",
      "status": "Departed",
      "isActive": true
    },
    {
      "checkpointId": 10,
      "sequenceOrder": 2,
      "checkpointName": "Cửa khẩu Hữu Nghị",
      "checkpointType": "BorderGate",
      "address": "Lạng Sơn",
      "plannedTime": "2026-11-07T05:00:00+00:00",
      "mandatoryRestMinutes": 0,
      "actualArrivalTime": "2026-10-08T07:24:31+00:00",
      "actualDepartureTime": "2026-10-08T07:24:31+00:00",
      "status": "Departed",
      "isActive": true
    }
  ],
  "bookings": [
    {
      "bookingId": 2,
      "bookingCode": "BKG-2026-0002",
      "customerName": "Jane Smith",
      "customerPhone": "+84988111222",
      "pickupAddress": "Trang trại Yên Bài, Ba Vì, Hà Nội",
      "dropoffAddress": "Trường đua Tùng Hóa, Quảng Châu",
      "horseCountOnTrip": 2,
      "handoverCompleted": false
    }
  ]
}
```

(Ví dụ đã rút gọn: `horses` còn 1 phần tử, `checkpoints` còn 2.)

| Trường | Ý nghĩa |
|---|---|
| `overallStatus` | Trạng thái chuyến (mục 3) |
| `vehicleCode`, `vehicleType`, `vehicleCapacity` | Biển số, loại xe, sức chứa |
| `plannedStartDate`, `plannedEndDate` | Kế hoạch |
| `actualStartDate`, `actualEndDate` | Thực tế; `null` khi chưa xảy ra |
| `plannedCost`, `actualCost` | Chi phí dự kiến và thực tế |
| `plannedByName` | Điều phối viên lập chuyến |
| `approvedByName`, `approvedAt` | Quản lý duyệt kế hoạch |
| `planRejectionCount`, `planRejectionReason` | Số lần kế hoạch bị trả về và lý do gần nhất |
| `delayMinutes`, `onTimeStatus`, `kpiScore`, `closedByName`, `closedAt`, `executiveRemarks` | Kết quả khi đóng chuyến (mục 12). Trước khi đóng, các trường này mang giá trị mặc định |
| `crew[]` | Đoàn xe: `userId`, `fullName`, `phoneNumber`, `crewRole`, `isLead`, `duty` |
| `crew[].assignedHorseCount` | Với dòng `Escort`: số ngựa người này đang phụ trách trên chuyến (tối đa 3). Với dòng `Driver`: `null` |
| `horses[]` | Ngựa trên chuyến |
| `horses[].tripHorseId` | Mã "ngựa trên chuyến", dùng khi ghi nhật ký và báo sự cố |
| `horses[].stallSlotNumber` | Ô chuồng |
| `horses[].escortUserId`, `escortName` | Người áp tải phụ trách con ngựa. Dùng để vẽ sơ đồ chuồng (tô màu ô theo người áp tải) và để màn của áp tải chỉ hiện ngựa của mình |
| `horses[].specialCareRequirements`, `photoUrl` | Yêu cầu chăm sóc và ảnh, để tài xế nhận diện |
| `horses[].dossierStatus` | Trạng thái hồ sơ; chuyến chỉ xuất phát được khi mọi con đều `Cleared` |
| `horses[].status` | Trạng thái của con ngựa trên chuyến (mục 3): `Planned`, `InTransit`, `Delivered`, `Offloaded`, `Deceased`, `Cancelled` |
| `horses[].pickedUpAt`, `deliveredAt` | Lúc xe rời điểm đón của con ngựa, lúc ký bàn giao |
| `horses[].offloadedAt`, `offloadReason` | Lúc và lý do con ngựa rời chuyến (qua sự cố) |
| `horses[].offloadOutcome` | Hướng xử lý tiếp khi rời chuyến (mục 3): `WaitRecover`, `ReturnToOrigin`, `HandOverHere`, `Deceased`; `null` nếu chưa rời chuyến hoặc đang chờ phương án |
| `checkpoints[].bookingId`, `bookingCode` | Với mốc `PickUp` / `DropOff`: đơn được đón / trả ở đây; `null` với mốc khác |
| `checkpoints[].estimatedArrivalTime` | Mốc đã tới: giờ tới thực tế. Mốc chưa tới: giờ kế hoạch cộng độ trễ hiện tại (cách tính xem "Giờ dự kiến theo check-in" ở `GET /api/tracking/trips`). `null` với mốc đã bị bỏ khi nắn tuyến |
| `liveDelayMinutes` | Số phút chuyến đang trễ theo check-in thực tế (0 là đúng giờ). Khác `delayMinutes` là kết quả chốt khi đóng chuyến |
| `bookings[].liveEstimatedDeliveryAt` | Giờ dự kiến tới mốc trả của đơn, cập nhật theo check-in thực tế |
| `checkpoints[]` | Các mốc, đã sắp theo thứ tự đi |
| `checkpoints[].sequenceOrder` | Thứ tự |
| `checkpoints[].plannedTime`, `actualArrivalTime`, `actualDepartureTime` | Giờ dự kiến và giờ thực tế |
| `checkpoints[].status` | Trạng thái mốc (mục 3) |
| `checkpoints[].isActive` | `false` là mốc đã bị bỏ khi nắn tuyến; nên hiện mờ hoặc gạch ngang |
| `bookings[]` | Các đơn có ngựa trên chuyến: khách, điểm giao, `horseCountOnTrip`, và `handoverCompleted` (đơn đã có biên bản bàn giao chưa, mục 12) |
| `bookings[].pickupCheckpointId`, `dropoffCheckpointId` | Mốc đón / mốc trả của đơn: `PickUp` / `DropOff` riêng, không có thì `Origin` / `Destination`. Tài xế ký e-POD cho đơn khi đang ở mốc `dropoffCheckpointId` |
| `bookings[].estimatedDeliveryAt`, `deliveryDeadline` | Giờ dự kiến tới mốc trả và hạn giao của đơn |
| `bookings[].deliveredHorseCount`, `offloadedHorseCount` | Số con đã giao, số con đã rời chuyến (`Offloaded`) |

Ví dụ một phần tử `horses[]`, `checkpoints[]` và `bookings[]` của chuyến ghép đơn (lấy từ chuyến thật trên máy test):

```json
{
  "horse": {
    "tripHorseId": 14,
    "stallSlotNumber": 3,
    "notes": null,
    "bookingHorseId": 203,
    "bookingId": 202,
    "bookingCode": "BKG-2026-0202",
    "horseId": 6,
    "horseName": "Sakura Wind",
    "microchipNumber": "982000412345676",
    "gender": "Mare",
    "color": "Hạt dẻ",
    "specialCareRequirements": "Duy trì nhiệt độ cabin 17-20°C",
    "photoUrl": null,
    "stallClass": "Shared",
    "dossierStatus": "Cleared",
    "status": "Offloaded",
    "pickedUpAt": "2026-10-18T12:59:25.307849+00:00",
    "deliveredAt": null,
    "offloadedAt": "2026-10-18T13:00:48.509219+00:00",
    "offloadReason": "INC-2026-0007: Lameness, vet stop",
    "offloadOutcome": "WaitRecover"
  },
  "checkpoint": {
    "checkpointId": 19,
    "sequenceOrder": 2,
    "checkpointName": "Drop off BKG-2026-0201 - Lạng Sơn",
    "checkpointType": "DropOff",
    "address": "Lạng Sơn",
    "plannedTime": "2026-11-09T02:42:00+00:00",
    "mandatoryRestMinutes": 0,
    "actualArrivalTime": "2026-10-18T12:59:46.397787+00:00",
    "actualDepartureTime": null,
    "status": "Arrived",
    "isActive": true,
    "bookingId": 201,
    "bookingCode": "BKG-2026-0201"
  },
  "booking": {
    "bookingId": 201,
    "bookingCode": "BKG-2026-0201",
    "customerName": "Trần Ngọc Anh",
    "customerPhone": "+84988111223",
    "pickupAddress": "Hà Nội",
    "dropoffAddress": "Lạng Sơn",
    "horseCountOnTrip": 1,
    "handoverCompleted": true,
    "pickupCheckpointId": 18,
    "dropoffCheckpointId": 19,
    "estimatedDeliveryAt": "2026-11-09T02:42:00+00:00",
    "deliveryDeadline": "2026-11-12T11:00:00+00:00",
    "deliveredHorseCount": 1,
    "offloadedHorseCount": 0
  }
}
```

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 403 | `You are not assigned to this trip.` | Tài xế xem chuyến không phải của mình |
| 403 | `Customers follow their shipment through their bookings, not trips.` | Khách gọi vào; khách dùng `GET /api/tracking/bookings/{id}` |

---

## 10. Flow 4 – Chạy chuyến và theo dõi

Khi chuyến đã `Scheduled`, đoàn xe (tài xế, áp tải) dùng ứng dụng mobile để cập nhật hành trình: bấm xuất phát, check-in từng mốc, và ghi nhật ký thể trạng từng con ngựa. Điều phối viên và khách hàng theo dõi qua các API ở cuối mục.

Vòng đời của chuyến khi chạy:

```
Scheduled ──(xuất phát)──> InTransit ──(tới mốc Destination)──> ArrivedDestination

Mỗi mốc:  Pending ──(tới)──> Arrived ──(thông quan, chỉ cửa khẩu)──> Cleared ──(rời)──> Departed

Mỗi con ngựa (horses[].status):
  Planned ──(xe rời điểm đón của con ngựa)──> InTransit ──(ký e-POD ở điểm trả)──> Delivered
                                               │
                                               └──(sự cố, phương án OffloadHorse được duyệt)──> Offloaded / Deceased
                                                     offloadOutcome = WaitRecover    → về hàng chờ xếp chuyến
                                                                    = ReturnToOrigin → đưa về nơi đi, con ngựa kết thúc
                                                                    = HandOverHere   → ký e-POD tại chỗ → Delivered
                                                                    = Deceased       → status Deceased
  Dỡ khẩn cấp (báo sự cố với emergencyOffload = true): InTransit → Offloaded ngay, offloadOutcome = null tới khi phương án được duyệt
```

Trạng thái chuyến là trạng thái của **xe**. Mỗi con ngựa có trạng thái riêng: ngựa của đơn không có mốc `PickUp` lên xe lúc xuất phát; ngựa của đơn có mốc `PickUp` lên xe khi đoàn xe rời mốc đó.

Thứ tự gọi thông thường của đoàn xe:

1. `GET /api/trips` để xem các chuyến của mình, `GET /api/trips/{id}` để xem chi tiết.
2. `POST /api/trips/{id}/start` khi xuất phát.
3. Với từng mốc theo thứ tự: `POST /api/checkpoints/{id}/arrive`, (cửa khẩu: `/clear`), `/depart`.
4. Bất cứ lúc nào trên đường: `POST /api/trips/{id}/welfare-logs` để ghi nhật ký cho từng con ngựa.

Quyền: chỉ người **có tên trong đoàn xe của chuyến** (và `Admin`) mới gọi được các API ghi; người khác nhận 403 `Only the crew assigned to this trip can update its progress.`

Các API xuất phát và check-in trả về **chi tiết chuyến** mới nhất (dạng của `GET /api/trips/{id}`, mục 9) và không cần body.

### POST /api/trips/{id}/start — Xuất phát

Bắt đầu chuyến: chuyến thành `InTransit`, mốc `Origin` được ghi là đã rời, ngựa của các đơn lên xe ở điểm xuất phát chuyển `InTransit`, khách và điều phối viên nhận thông báo. Bị chặn nếu còn con ngựa nào có hồ sơ chưa `Cleared`.

- **Ai gọi được:** Đoàn xe của chuyến, Admin
- **Dữ liệu gửi lên:** không có (không cần body)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã chuyến |

Response `200`: chi tiết chuyến với `overallStatus` là `InTransit` và `actualStartDate`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `The trip cannot depart: clearance is not complete for Red Flash, Golden Pegasus.` | Còn ngựa chưa thông quan; `message` nêu tên từng con |
| 400 | `The trip cannot depart: the remaining balance is not paid for BKG-2026-0007.` | Có đơn trên chuyến chưa trả đủ cọc + phần còn lại (mục 19) |
| 400 | `Only a scheduled trip can depart (current status: Draft).` | Chuyến chưa được duyệt, hoặc đã xuất phát rồi |
| 403 | `Only the crew assigned to this trip can update its progress.` | Người gọi không thuộc đoàn xe |

### POST /api/checkpoints/{id}/arrive — Đã tới mốc

Ghi nhận đoàn xe vừa tới một mốc. Phải đi đúng thứ tự: mốc trước chưa `Departed` thì chưa ghi được mốc sau. Khi tới mốc `Destination`, chuyến tự chuyển sang `ArrivedDestination`.

- **Ai gọi được:** Đoàn xe của chuyến, Admin
- **Dữ liệu gửi lên:** không có (không cần body)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã mốc (`checkpointId`, lấy từ `checkpoints[]` của chuyến). **Không phải** mã chuyến |

Response `200`: chi tiết chuyến; mốc có `status` là `Arrived` và `actualArrivalTime`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Record the departure from 'A' before arriving at 'B'.` | Chưa rời mốc trước |
| 400 | `Arrival at 'A' has already been recorded (status: Arrived).` | Bấm hai lần |
| 400 | `'A' is no longer part of the route.` | Mốc đã bị bỏ khi nắn tuyến |
| 400 | `Checkpoints can only be updated while the trip is on the road (current status: Scheduled).` | Chuyến chưa xuất phát hoặc đã tới đích |

### POST /api/checkpoints/{id}/clear — Đã thông quan tại cửa khẩu

Chỉ dùng cho mốc loại `BorderGate`: ghi nhận đã xong thủ tục hải quan và kiểm dịch tại cửa khẩu. Bắt buộc trước khi rời cửa khẩu.

- **Ai gọi được:** Đoàn xe của chuyến, Admin
- **Dữ liệu gửi lên:** không có (không cần body)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã mốc |

Response `200`: chi tiết chuyến; mốc có `status` là `Cleared`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Only a border gate checkpoint can be marked as cleared.` | Mốc không phải cửa khẩu |
| 400 | `'A' can only be cleared after arrival (status: Pending).` | Chưa ghi nhận tới mốc |

### POST /api/checkpoints/{id}/depart — Rời mốc

Ghi nhận đoàn xe rời mốc. Không rời được khi chưa nghỉ đủ `mandatoryRestMinutes` tính từ lúc tới, và cửa khẩu phải `Cleared` trước.

- Rời mốc `PickUp`: ngựa của đơn được đón ở đó chuyển `InTransit`.
- Rời mốc `DropOff`: phải ký biên bản bàn giao (mục 12) cho đơn được trả ở đó trước.

- **Ai gọi được:** Đoàn xe của chuyến, Admin
- **Dữ liệu gửi lên:** không có (không cần body)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã mốc |

Response `200`: chi tiết chuyến; mốc có `status` là `Departed` và `actualDepartureTime`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `'Cửa khẩu Hữu Nghị' must be cleared before departure.` | Cửa khẩu chưa thông quan |
| 400 | `The horses must rest 120 minutes at 'A'; 35 minute(s) remaining.` | Chưa nghỉ đủ; `message` cho biết còn bao nhiêu phút |
| 400 | `The destination is the last checkpoint; there is no departure from it.` | Mốc là điểm giao |
| 400 | `Record the handover (e-POD) of the horses dropped off at 'X' before leaving.` | Mốc `DropOff` mà đơn được trả ở đó chưa ký biên bản |

Khi xe tới mốc `PickUp` / `DropOff`, chỉ khách của đơn đó nhận thông báo; tới mốc `DropOff` thì khách của đơn được trả ở đó nhận thêm email "ngựa đã tới".

### POST /api/trips/{id}/horses/{tripHorseId}/offload — Đã bỏ (luôn trả 410)

Nút "Rời chuyến" riêng **đã bị bỏ** (R-12). Ngựa chỉ rời chuyến qua **sự cố**, để luôn có người duyệt và có hướng xử lý tiếp rõ ràng:

1. Tài xế báo sự cố cho con ngựa: `POST /api/trips/{tripId}/incidents` với `affectedTripHorseId`. Nếu thú y yêu cầu dỡ ngựa xuống **ngay**, gửi thêm `emergencyOffload: true`: con ngựa chuyển `Offloaded` liền, chưa có hướng xử lý.
2. Điều phối viên đề xuất phương án `OffloadHorse` kèm `offloadOutcome` (`POST /api/incidents/{id}/propose`).
3. Quản lý duyệt (`POST /api/incidents/{id}/approve`): con ngựa đổi trạng thái theo hướng xử lý.

Đường dẫn cũ vẫn còn để app bản cũ nhận thông báo rõ ràng thay vì 404. Frontend cần **xóa nút "Rời chuyến"** và dùng luồng sự cố.

- **Ai gọi được:** Đoàn xe của chuyến, FleetCoordinator, LogisticsManager, Admin (ai gọi cũng nhận 410)
- **Dữ liệu gửi lên:** không dùng

Response `410`:

```json
{
  "success": false,
  "message": "This endpoint was removed. To take a horse off trip 5, report an incident for trip horse 12 (POST /api/trips/5/incidents, emergencyOffload = true if it must leave the vehicle now); a coordinator then proposes an OffloadHorse plan for a manager to approve.",
  "data": null,
  "errors": null
}
```

### POST /api/trips/{id}/welfare-logs — Ghi nhật ký thể trạng ngựa

Ghi một lần kiểm tra thể trạng của **một** con ngựa: nhiệt độ khoang, lượng nước uống, tình trạng ăn, mức căng thẳng, kèm ảnh nếu có. Ghi được từ lúc xuất phát tới khi bàn giao. Nếu ngựa bỏ ăn (`Refused`) hoặc kích động (`Agitated`), điều phối viên nhận cảnh báo.

- **Ai gọi được:** Đoàn xe của chuyến, Admin. Nếu con ngựa đã có người áp tải phụ trách (`horses[].escortUserId`) thì chỉ người đó, trưởng nhóm áp tải (`Escort` có `isLead` = true) hoặc Admin mới ghi được
- **Dữ liệu gửi lên:** `multipart/form-data` (xem mục 2.5)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã chuyến |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `tripHorseId` | số | Có | Con ngựa được ghi, lấy từ `horses[].tripHorseId` của chuyến |
| `checkpointId` | số | Không | Ghi tại mốc nào; bỏ trống nếu ghi dọc đường |
| `cabinTemp` | số | Có | Nhiệt độ khoang (°C), từ -30 đến 60 |
| `waterIntakeLiters` | số | Có | Lượng nước đã uống (lít), 0–200 |
| `feedStatus` | chuỗi | Có | `Normal`, `Reduced` hoặc `Refused` |
| `stressLevel` | chuỗi | Có | `Calm`, `MildStress` hoặc `Agitated` |
| `notes` | chuỗi | Không | Ghi chú, tối đa 2000 ký tự |
| `photo` | file | Không | Ảnh chụp: `jpg`, `jpeg`, `png`, tối đa 10 MB |

Ví dụ request:

```js
const form = new FormData();
form.append("tripHorseId", "7");
form.append("checkpointId", "9");
form.append("cabinTemp", "17.5");
form.append("waterIntakeLiters", "10");
form.append("feedStatus", "Normal");
form.append("stressLevel", "Calm");
form.append("notes", "Ăn uống bình thường");
form.append("photo", photoFile);              // không bắt buộc

const res = await api.post(`/api/trips/${tripId}/welfare-logs`, form);
```

Response `201` — phần `data`:

```json
{
  "logId": 3,
  "tripHorseId": 7,
  "horseId": 1,
  "horseName": "Red Flash (Tia Chớp Đỏ)",
  "checkpointId": 9,
  "checkpointName": "Trại Ba Vì",
  "cabinTemp": 17.5,
  "waterIntakeLiters": 10,
  "feedStatus": "Normal",
  "stressLevel": "Calm",
  "needsAttention": false,
  "notes": "Ăn uống bình thường",
  "photoUrl": "https://abcd.supabase.co/storage/v1/object/sign/files/welfare/2026/10/f0e923....jpg?token=eyJ...",
  "recordedByUserId": 4,
  "recordedByName": "Phạm Văn Đức",
  "loggedAt": "2026-10-08T07:24:31+00:00"
}
```

`needsAttention` là `true` khi `feedStatus` là `Refused` hoặc `stressLevel` là `Agitated`; nên tô đỏ các dòng này.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Trip horse with ID 99 is not on trip TRP-2026-0003.` | `tripHorseId` không thuộc chuyến |
| 400 | `Welfare logs are recorded from departure until handover (current status: Scheduled).` | Chuyến chưa xuất phát hoặc đã đóng |
| 400 | `FeedStatus '...' is invalid. Allowed values: Normal, Reduced, Refused.` | Viết sai giá trị |
| 403 | `Only the escort in charge of this horse (or the lead escort) can record its welfare log.` | Người gọi không phụ trách con ngựa này và không phải trưởng nhóm áp tải |

### GET /api/trips/{id}/welfare-logs — Xem nhật ký của chuyến

Các lần ghi nhật ký của một chuyến, mới nhất trước, không phân trang.

- **Ai gọi được:** Mọi nhân sự; tài xế / áp tải chỉ xem được chuyến của mình

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã chuyến |

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `tripHorseId` | số | Không | Chỉ lấy nhật ký của một con ngựa |

Response `200`: `data` là mảng nhật ký, mỗi phần tử có dạng như response của API ghi nhật ký ở trên.

### GET /api/tracking/trips — Bảng theo dõi các chuyến

Bảng điều khiển của điều phối: tiến độ thực tế so với kế hoạch của từng chuyến. Không truyền `status` thì trả về các chuyến chưa đóng.

- **Ai gọi được:** FleetCoordinator, LogisticsManager, Admin

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `status` | chuỗi | Không | Chỉ lấy chuyến ở một trạng thái, ví dụ `InTransit` |

Response `200` — phần `data`:

```json
[
  {
    "tripId": 3,
    "tripCode": "TRP-2026-0003",
    "overallStatus": "InTransit",
    "vehicleCode": "29B-888.99",
    "leadDriverName": "Phạm Văn Đức",
    "leadDriverPhone": "+84912000004",
    "plannedStartDate": "2026-11-06T23:00:00+00:00",
    "plannedEndDate": "2026-11-09T11:00:00+00:00",
    "actualStartDate": "2026-10-08T07:24:31+00:00",
    "actualEndDate": null,
    "horseCount": 2,
    "checkpointsTotal": 4,
    "checkpointsReached": 2,
    "lastCheckpointName": "Cửa khẩu Hữu Nghị",
    "lastCheckpointStatus": "Departed",
    "nextCheckpointName": "Trạm nghỉ Nam Ninh",
    "nextCheckpointPlannedTime": "2026-11-08T02:00:00+00:00",
    "nextCheckpointEstimatedTime": "2026-11-08T02:00:00+00:00",
    "estimatedEndAt": "2026-11-09T11:00:00+00:00",
    "delayMinutes": 0,
    "isBehindSchedule": false,
    "openIncidentCount": 0,
    "welfareAlertCount": 1
  }
]
```

| Trường | Ý nghĩa |
|---|---|
| `leadDriverName`, `leadDriverPhone` | Tài xế trưởng, để gọi điện khi cần |
| `checkpointsTotal`, `checkpointsReached` | Tổng số mốc và số mốc đã tới. Dùng làm thanh tiến độ |
| `lastCheckpointName`, `lastCheckpointStatus` | Mốc gần nhất đã tới và trạng thái của nó |
| `nextCheckpointName`, `nextCheckpointPlannedTime` | Mốc sắp tới và giờ dự kiến theo kế hoạch |
| `nextCheckpointEstimatedTime` | Giờ dự kiến tới mốc sắp tới, đã cộng độ trễ hiện tại (xem "Giờ dự kiến theo check-in" bên dưới) |
| `estimatedEndAt` | Giờ dự kiến tới mốc cuối, đã cộng độ trễ hiện tại; đã tới thì là giờ tới thực tế |
| `delayMinutes` | Số phút đang trễ so với kế hoạch theo check-in thực tế (0 là đúng giờ) |
| `isBehindSchedule` | `true` khi đang trễ (`delayMinutes` > 0); nên tô màu cảnh báo |

**Giờ dự kiến theo check-in.** Server tính lại giờ dự kiến mỗi lần được hỏi, từ mốc gần nhất xe đã chạm tới:

- Đã rời mốc: độ trễ = giờ rời thực tế − (giờ dự kiến tới + số phút nghỉ bắt buộc của mốc).
- Đang dừng ở mốc: tính như thể xe rời ở thời điểm sớm nhất có thể (bây giờ, hoặc khi nghỉ đủ). Xe dừng càng lâu thì độ trễ càng tăng.
- Đang chạy mà đã quá giờ dự kiến của mốc kế tiếp: coi như xe tới mốc đó ngay bây giờ.
- Chưa xuất phát mà đã quá giờ khởi hành: độ trễ = bây giờ − giờ dự kiến của mốc đầu.
- Tới sớm không làm giờ dự kiến sớm lên, vì xe vẫn chạy theo lịch.

Mốc chưa tới có giờ dự kiến = giờ kế hoạch + độ trễ. Mỗi lần tài xế xuất phát, tới hoặc rời một mốc, server kiểm tra từng đơn còn ngựa trên xe. Nếu giờ dự kiến tới điểm trả của đơn đã sau hạn giao, khách nhận thông báo + email "Booking BKG-... may arrive late" và điều phối viên lập chuyến cũng nhận thông báo. Mỗi đơn chỉ báo một lần.
| `openIncidentCount` | Số sự cố chưa xử lý xong |
| `welfareAlertCount` | Số nhật ký cần chú ý (ngựa bỏ ăn hoặc kích động) |

### GET /api/tracking/bookings/{id} — Khách theo dõi đơn của mình

Màn hình theo dõi của khách: với mỗi chuyến đang chở ngựa của đơn, trả về dòng thời gian các mốc và nhật ký thể trạng của ngựa.

- **Ai gọi được:** Chủ đơn; chuyên viên phụ trách đơn; LogisticsManager, FleetCoordinator, Admin

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã đơn (`bookingId`) |

Response `200` — phần `data`:

```json
{
  "bookingId": 2,
  "bookingCode": "BKG-2026-0002",
  "status": "Approved",
  "pickupAddress": "Trang trại Yên Bài, Ba Vì, Hà Nội",
  "dropoffAddress": "Trường đua Tùng Hóa, Quảng Châu",
  "pickupWindowStart": "2026-11-24T17:00:00+00:00",
  "pickupWindowEnd": "2026-11-29T17:00:00+00:00",
  "deliveryDeadline": "2026-12-05T17:00:00+00:00",
  "trips": [
    {
      "tripId": 3,
      "tripCode": "TRP-2026-0003",
      "overallStatus": "InTransit",
      "vehicleCode": "29B-888.99",
      "leadDriverName": "Phạm Văn Đức",
      "plannedStartDate": "2026-11-06T23:00:00+00:00",
      "plannedEndDate": "2026-11-09T11:00:00+00:00",
      "actualStartDate": "2026-10-08T07:24:31+00:00",
      "actualEndDate": null,
      "delayMinutes": 0,
      "handoverCompleted": false,
      "estimatedDeliveryAt": "2026-11-09T11:00:00+00:00",
      "liveEstimatedDeliveryAt": "2026-11-09T11:00:00+00:00",
      "deliveryAtRisk": false,
      "checkpoints": [
        {
          "checkpointId": 9,
          "sequenceOrder": 1,
          "checkpointName": "Trại Ba Vì",
          "checkpointType": "Origin",
          "address": "Ba Vì, Hà Nội",
          "plannedTime": "2026-11-06T23:00:00+00:00",
          "actualArrivalTime": "2026-10-08T07:24:31+00:00",
          "actualDepartureTime": "2026-10-08T07:24:31+00:00",
          "status": "Departed",
          "delayMinutes": 0,
          "estimatedArrivalTime": "2026-10-08T07:24:31+00:00"
        },
        {
          "checkpointId": 10,
          "sequenceOrder": 2,
          "checkpointName": "Cửa khẩu Hữu Nghị",
          "checkpointType": "BorderGate",
          "address": "Lạng Sơn",
          "plannedTime": "2026-11-07T05:00:00+00:00",
          "actualArrivalTime": "2026-10-08T07:24:31+00:00",
          "actualDepartureTime": "2026-10-08T07:24:31+00:00",
          "status": "Departed",
          "delayMinutes": 0
        }
      ],
      "horses": [
        {
          "tripHorseId": 7,
          "horseId": 1,
          "horseName": "Red Flash (Tia Chớp Đỏ)",
          "stallSlotNumber": 1,
          "latestLog": {
            "logId": 3,
            "tripHorseId": 7,
            "horseId": 1,
            "horseName": "Red Flash (Tia Chớp Đỏ)",
            "checkpointId": 9,
            "checkpointName": "Trại Ba Vì",
            "cabinTemp": 17.5,
            "waterIntakeLiters": 10,
            "feedStatus": "Normal",
            "stressLevel": "Calm",
            "needsAttention": false,
            "notes": "Ăn uống bình thường",
            "photoUrl": "https://abcd.supabase.co/storage/v1/object/sign/files/welfare/2026/10/f0e923....jpg?token=eyJ...",
            "recordedByUserId": 4,
            "recordedByName": "Phạm Văn Đức",
            "loggedAt": "2026-10-08T07:24:31+00:00"
          },
          "logs": [
            {
              "logId": 3,
              "tripHorseId": 7,
              "horseId": 1,
              "horseName": "Red Flash (Tia Chớp Đỏ)",
              "checkpointId": 9,
              "checkpointName": "Trại Ba Vì",
              "cabinTemp": 17.5,
              "waterIntakeLiters": 10,
              "feedStatus": "Normal",
              "stressLevel": "Calm",
              "needsAttention": false,
              "notes": "Ăn uống bình thường",
              "photoUrl": "https://abcd.supabase.co/storage/v1/object/sign/files/welfare/2026/10/f0e923....jpg?token=eyJ...",
              "recordedByUserId": 4,
              "recordedByName": "Phạm Văn Đức",
              "loggedAt": "2026-10-08T07:24:31+00:00"
            }
          ]
        }
      ]
    }
  ],
  "horsesAwaitingPlanning": [],
  "generatedAt": "2026-10-08T07:24:31+00:00"
}
```

(Ví dụ đã rút gọn: `checkpoints` còn 2 mốc, `horses` còn 1 con.)

| Trường | Ý nghĩa |
|---|---|
| `trips[]` | Các chuyến đang chở ngựa của đơn này |
| `trips[].delayMinutes` | Số phút chuyến đang trễ |
| `trips[].handoverCompleted` | Đơn này đã được bàn giao trên chuyến đó chưa |
| `trips[].checkpoints[]` | Dòng thời gian. `actualArrivalTime` là `null` nghĩa là chưa tới; `delayMinutes` của mốc là `null` khi chưa tới |
| `trips[].horses[]` | Chỉ gồm ngựa **của đơn này**, kể cả khi chuyến chở chung ngựa của khách khác |
| `trips[].pickupCheckpointId`, `dropoffCheckpointId` | Mốc đón / mốc trả của đơn này trên chuyến |
| `trips[].estimatedDeliveryAt` | Giờ tới mốc trả của đơn này **theo kế hoạch** (không phải giờ kết thúc cả chuyến) |
| `trips[].liveEstimatedDeliveryAt` | Giờ dự kiến tới mốc trả của đơn này, **cập nhật theo check-in thực tế** (xem "Giờ dự kiến theo check-in" ở `GET /api/tracking/trips`). Nên hiện giờ này, kèm giờ kế hoạch nếu khác |
| `trips[].deliveryAtRisk` | `true` khi giờ dự kiến mới đã sau hạn giao của đơn (và đơn chưa được bàn giao); nên hiện cảnh báo "Có thể giao trễ hạn" |
| `checkpoints[].estimatedArrivalTime` | Mốc đã tới: giờ tới thực tế. Mốc chưa tới: giờ kế hoạch cộng độ trễ hiện tại |
| `checkpoints[].isMyPickup`, `isMyDropoff` | Mốc đón / trả của đơn này, để tô nổi |
| `checkpoints[].isOtherBookingStop` | `true` là điểm đón / trả của **khách khác** đi chung chuyến: `checkpointName` và `address` để trống, frontend hiện "Điểm dừng của đơn khác" |
| `horses[].status`, `deliveredAt`, `offloadedAt`, `offloadReason`, `offloadOutcome` | Trạng thái của từng con ngựa trên chuyến (mục 3) |

Ví dụ các trường mới của một chuyến ghép (khách đơn BKG-2026-0202; mốc thứ 2 là điểm trả của đơn khác nên bị ẩn tên):

```json
{
  "pickupCheckpointId": 18,
  "dropoffCheckpointId": 21,
  "estimatedDeliveryAt": "2026-11-09T16:56:00+00:00",
  "checkpoints": [
    {
      "checkpointId": 18,
      "sequenceOrder": 1,
      "checkpointName": "Hà Nội",
      "checkpointType": "Origin",
      "address": "Hà Nội",
      "plannedTime": "2026-11-08T23:00:00+00:00",
      "actualArrivalTime": "2026-10-18T12:59:25.307849+00:00",
      "actualDepartureTime": "2026-10-18T12:59:25.307849+00:00",
      "status": "Departed",
      "delayMinutes": 0,
      "isOtherBookingStop": false,
      "isMyPickup": true,
      "isMyDropoff": false
    },
    {
      "checkpointId": 19,
      "sequenceOrder": 2,
      "checkpointName": "",
      "checkpointType": "DropOff",
      "address": "",
      "plannedTime": "2026-11-09T02:42:00+00:00",
      "actualArrivalTime": "2026-10-18T12:59:46.397787+00:00",
      "actualDepartureTime": null,
      "status": "Arrived",
      "delayMinutes": 0,
      "isOtherBookingStop": true,
      "isMyPickup": false,
      "isMyDropoff": false
    }
  ],
  "horse": {
    "tripHorseId": 14,
    "horseId": 6,
    "horseName": "Sakura Wind",
    "stallSlotNumber": 3,
    "status": "Offloaded",
    "pickedUpAt": "2026-10-18T12:59:25.307849+00:00",
    "deliveredAt": null,
    "offloadedAt": "2026-10-18T13:00:48.509219+00:00",
    "offloadReason": "INC-2026-0007: Lameness, vet stop",
    "offloadOutcome": "WaitRecover"
  }
}
```
| `horses[].latestLog` | Lần ghi nhật ký mới nhất; `null` nếu chưa có |
| `horses[].logs[]` | Toàn bộ nhật ký của con ngựa, mới nhất trước |
| `horsesAwaitingPlanning` | Tên các con ngựa của đơn chưa được xếp chuyến |
| `generatedAt` | Thời điểm server tạo dữ liệu này |

**Cập nhật gần như tức thời:** gọi lại API này định kỳ, khoảng 30 giây một lần (dùng `setInterval`, nhớ `clearInterval` khi rời màn hình). Server giữ kết quả 30 giây và làm mới ngay khi đoàn xe cập nhật, nên gọi thường xuyên không gây tải.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 403 | `You do not have access to this booking.` | Đơn của khách khác |
| 404 | `Booking with ID 99 was not found.` | Không có đơn với mã đó |

---

## 11. Flow 5 – Sự cố và nắn tuyến

Khi chuyến đang trên đường gặp trục trặc (hỏng xe, tắc biên, ngựa có vấn đề sức khỏe...), đoàn xe **báo sự cố**. Điều phối viên **đề xuất phương án** xử lý kèm chi phí phát sinh; quản lý **duyệt hoặc từ chối**. Khi phương án được duyệt, điều phối viên có thể **nắn tuyến** (bỏ các mốc chưa tới, thêm mốc mới), và cuối cùng **đóng sự cố**.

Vòng đời của một sự cố:

```
Reported ──(điều phối đề xuất)──> PlanProposed ──(quản lý duyệt)──> Approved ──(điều phối đóng)──> Resolved
                                       ▲    │
                                       │    └──(quản lý từ chối)──> Rejected
                                       └────────(điều phối đề xuất lại)───────┘

Nắn tuyến chỉ làm được khi sự cố đang Approved. Khi nắn tuyến, chuyến chuyển sang EmergencyRerouting;
khi đóng sự cố, chuyến trở lại InTransit.
```

Thứ tự gọi thông thường:

1. Tài xế: `POST /api/trips/{tripId}/incidents`.
2. Điều phối: `GET /api/incidents?status=Reported`, rồi `POST /api/incidents/{id}/propose`.
3. Quản lý: `GET /api/incidents?status=PlanProposed`, rồi `POST /api/incidents/{id}/approve` hoặc `/reject`.
4. Điều phối: nếu cần đổi lộ trình, `POST /api/incidents/{id}/apply-reroute`.
5. Điều phối: `POST /api/incidents/{id}/resolve` khi đã xử lý xong.

Ai thấy sự cố nào: khách thấy sự cố trên chuyến có ngựa của mình; tài xế / áp tải thấy sự cố của các chuyến mình đi; các vai trò còn lại thấy tất cả.

**Ngựa rời chuyến (R-12).** Một con ngựa chỉ rời chuyến qua sự cố, với phương án `OffloadHorse`. Phương án bắt buộc chọn **hướng xử lý tiếp** (`offloadOutcome`):

| `offloadOutcome` | Ý nghĩa | Sau khi quản lý duyệt |
|---|---|---|
| `WaitRecover` | Chờ hồi phục rồi đi chuyến khác | Ngựa `Offloaded`, **quay về hàng chờ xếp chuyến** (`GET /api/trips/unplanned-horses`); người lập chuyến được báo |
| `ReturnToOrigin` | Đưa ngựa về nơi đi | Ngựa `Offloaded`, **không** về hàng chờ; việc chở về do điều phối sắp xếp. Con ngựa được tính là đã xong với đơn |
| `HandOverHere` | Khách nhận ngựa tại nơi dỡ | Ngựa `Offloaded`; đoàn xe ký e-POD tại chỗ (`POST /api/trips/{tripId}/handover` với `tripHorseId`) → `Delivered` |
| `Deceased` | Ngựa chết | Ngựa `Deceased` (cả trên chuyến và trong đơn). Chỉ chọn được ở sự cố mức `Critical` và **bắt buộc biên bản thú y** |

Mỗi trường hợp, khách của con ngựa nhận thông báo + email. Chuyến chỉ đóng được khi không còn con nào đang chờ phương án hoặc chờ ký e-POD tại chỗ. Hoàn tiền phần cước chưa đi và hồ sơ bồi thường bảo hiểm sẽ có ở các bản sau (BE-14b, BE-12).

Một sự cố (`incident`) có dạng:

```json
{
  "incidentId": 2,
  "incidentCode": "INC-2026-0002",
  "tripId": 3,
  "tripCode": "TRP-2026-0003",
  "tripStatus": "InTransit",
  "incidentType": "MechanicalBreakdown",
  "severity": "Major",
  "status": "Rejected",
  "location": "Cao tốc Nam Ninh, km 120",
  "description": "Hỏng hệ thống treo khí nén, xe không thể đi tiếp",
  "affectedTripHorseId": 7,
  "affectedHorseName": "Red Flash (Tia Chớp Đỏ)",
  "reportedByUserId": 4,
  "reportedByName": "Phạm Văn Đức",
  "reportedAt": "2026-10-08T07:24:31+00:00",
  "proposedAction": "Đổi xe dự phòng, nghỉ thêm tại trạm thú y Bằng Tường",
  "revisedRouteNotes": "Đã liên hệ trạm thú y Bằng Tường",
  "additionalCost": 650,
  "rejectionReason": "Chi phí quá cao, tìm phương án rẻ hơn.",
  "faultParty": null,
  "faultNote": null,
  "approvedByUserId": null,
  "approvedByName": null,
  "approvedAt": null,
  "resolvedAt": null,
  "emergencyOffload": false,
  "offloadOutcome": null,
  "vetReportUrl": null
}
```

| Trường | Ý nghĩa |
|---|---|
| `incidentId`, `incidentCode` | Mã sự cố (số) và mã hiển thị (`INC-2026-0002`) |
| `tripId`, `tripCode`, `tripStatus` | Chuyến gặp sự cố và trạng thái hiện tại của chuyến |
| `incidentType`, `severity` | Loại và mức độ (mục 3) |
| `status` | Trạng thái sự cố (mục 3) |
| `location`, `description` | Vị trí và mô tả do đoàn xe nhập |
| `affectedTripHorseId`, `affectedHorseName` | Con ngựa bị ảnh hưởng; `null` nếu sự cố không gắn với con nào |
| `reportedByName`, `reportedAt` | Ai báo và lúc nào |
| `proposedAction`, `revisedRouteNotes`, `additionalCost` | Phương án của điều phối viên: việc sẽ làm, ghi chú tuyến mới, chi phí phát sinh |
| `rejectionReason` | Lý do quản lý từ chối phương án gần nhất. Được giữ lại khi điều phối đề xuất lại, và xóa (`null`) khi phương án được duyệt |
| `approvedByName`, `approvedAt` | Quản lý duyệt phương án |
| `resolvedAt` | Thời điểm đóng sự cố |
| `faultParty`, `faultNote` | Nguyên nhân chọn khi đóng sự cố (`Company`, `Customer`, `ForceMajeure`) và giải thích; `null` khi sự cố chưa đóng |
| `emergencyOffload` | `true` khi tài xế đã dỡ con ngựa `affectedTripHorseId` xuống ngay lúc báo sự cố |
| `offloadOutcome` | Hướng xử lý tiếp của phương án `OffloadHorse` (`WaitRecover`, `ReturnToOrigin`, `HandOverHere`, `Deceased`); `null` với phương án khác |
| `vetReportUrl` | Link xem biên bản thú y (có hạn, mục 2.5); có khi phương án kèm biên bản |

### POST /api/trips/{tripId}/incidents — Báo sự cố

Báo một sự cố trên chuyến đang trên đường (`InTransit` hoặc `EmergencyRerouting`). Người lập chuyến, các quản lý và khách có ngựa trên chuyến nhận thông báo ngay.

- **Ai gọi được:** Đoàn xe của chuyến, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `tripId` | Mã chuyến |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `incidentType` | chuỗi | Có | Loại sự cố (mục 3), ví dụ `MechanicalBreakdown` |
| `severity` | chuỗi | Có | `Minor`, `Moderate`, `Major` hoặc `Critical` |
| `location` | chuỗi | Có | Vị trí xảy ra, tối đa 255 ký tự. App tài xế tự lấy từ GPS của thiết bị, dạng `"10.875115, 106.800901 (±90 m)"` (vĩ độ, kinh độ, sai số); backend lưu nguyên chuỗi |
| `description` | chuỗi | Có | Mô tả, tối đa 2000 ký tự |
| `affectedTripHorseId` | số | Không (bắt buộc khi `emergencyOffload` là `true`) | Con ngựa bị ảnh hưởng, lấy từ `horses[].tripHorseId` của chuyến |
| `emergencyOffload` | true/false | Không (mặc định `false`) | `true` khi thú y yêu cầu dỡ con ngựa xuống **ngay**. Con ngựa chuyển `Offloaded` liền (chưa có hướng xử lý), khách của con ngựa nhận thông báo + email. Điều phối viên vẫn phải đề xuất phương án `OffloadHorse` và quản lý duyệt |

Ví dụ request:

```json
{
  "incidentType": "MechanicalBreakdown",
  "severity": "Major",
  "location": "Cao tốc Nam Ninh, km 120",
  "description": "Hỏng hệ thống treo khí nén, xe không thể đi tiếp",
  "affectedTripHorseId": 7
}
```

Ví dụ dỡ khẩn cấp (thú y bắt buộc dỡ ngay):

```json
{
  "incidentType": "EquineHealthIssue",
  "severity": "Critical",
  "location": "10.875115, 106.800901 (±40 m)",
  "description": "Đau bụng cấp, thú y yêu cầu dỡ ngựa ngay",
  "affectedTripHorseId": 12,
  "emergencyOffload": true
}
```

Response `201`: `data` là sự cố vừa tạo, `status` là `Reported`. Khi `emergencyOffload` là `true`, con ngựa trên chuyến có `status` `Offloaded` và `offloadOutcome` `null`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Incidents can only be reported while the trip is on the road (current status: Scheduled).` | Chuyến chưa xuất phát hoặc đã tới đích |
| 400 | `Choose the horse that is offloaded in an emergency (AffectedTripHorseId).` | Gửi `emergencyOffload: true` mà không chọn ngựa |
| 400 | `Sakura Wind is not on the vehicle (status: Delivered), so it cannot be offloaded.` | Dỡ khẩn cấp con ngựa đã giao / đã rời chuyến |
| 400 | `IncidentType '...' is invalid. Allowed values: ...` | Viết sai loại sự cố |
| 400 | `Trip horse with ID 99 is not on trip TRP-2026-0003.` | Con ngựa không thuộc chuyến |
| 403 | `Only the crew assigned to this trip can report an incident.` | Người gọi không thuộc đoàn xe |

### GET /api/incidents — Danh sách sự cố

Danh sách sự cố có phân trang, mới nhất trước. Mỗi người chỉ thấy các sự cố mình được phép xem.

- **Ai gọi được:** Mọi người đã đăng nhập

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `status` | chuỗi | Không | Lọc theo trạng thái, ví dụ `Reported` |
| `severity` | chuỗi | Không | Lọc theo mức độ |
| `tripId` | số | Không | Chỉ lấy sự cố của một chuyến |
| `search` | chuỗi | Không | Từ khóa tìm kiếm |
| `sort` | chuỗi | Không | Trường sắp xếp, thêm `-` phía trước để giảm dần |
| `page` | số | Không | Trang, bắt đầu từ 1 (mặc định 1) |
| `size` | số | Không | Số dòng mỗi trang, 1–100 (mặc định 10) |

Ví dụ: `GET /api/incidents?status=PlanProposed&page=1&size=10`. `search` tìm theo mã sự cố, vị trí, mô tả.

Response `200`: `data` là mảng sự cố (dạng như trên), kèm `pagination`.

### GET /api/incidents/{id} — Chi tiết sự cố

Lấy một sự cố theo mã. Thông báo về sự cố có `referenceType` là `Incident` và `referenceId` là mã này.

- **Ai gọi được:** Mọi người đã đăng nhập (theo quyền xem)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã sự cố (`incidentId`) |

Response `200`: `data` là một sự cố.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 403 | `You cannot view this incident.` | Sự cố không thuộc chuyến của mình |
| 404 | `Incident with ID 99 was not found.` | Không có sự cố với mã đó |

### GET /api/trips/{tripId}/incidents — Các sự cố của một chuyến

Mọi sự cố của một chuyến, mới nhất trước, không phân trang. Dùng trong màn hình chi tiết chuyến.

- **Ai gọi được:** Mọi nhân sự; tài xế / áp tải chỉ xem được chuyến của mình

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `tripId` | Mã chuyến |

Response `200`: `data` là mảng sự cố. Khách hàng không gọi API này mà dùng `GET /api/incidents?tripId=`.

### POST /api/incidents/{id}/propose — Đề xuất phương án xử lý

Điều phối viên nhập phương án cho một sự cố đang `Reported`, hoặc đề xuất lại sau khi bị từ chối (`Rejected`). Sự cố chuyển sang `PlanProposed` và các quản lý nhận thông báo.

Phương án gồm **loại hành động**, **chỉ đạo cho đoàn xe**, và (tùy chọn) **thay đổi tuyến**: bỏ các mốc chưa tới, chèn mốc mới. Thay đổi tuyến được kiểm tra ngay trên lộ trình hiện tại nhưng **chưa áp**; tuyến chỉ được áp vào lộ trình khi quản lý duyệt (`approve`). Nhờ vậy quản lý duyệt đúng tuyến sẽ đi.

- **Ai gọi được:** FleetCoordinator, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã sự cố |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `responseAction` | chuỗi | Có | Loại hành động: `WaitAndContinue` (chờ rồi đi tiếp), `RepairAndContinue` (sửa xe / thiết bị rồi đi tiếp), `Reroute` (đổi tuyến), `VeterinaryCare` (dừng chăm sóc thú y), `OffloadHorse` (cho ngựa rời chuyến), `Other` |
| `proposedAction` | chuỗi | Có | Chỉ đạo cho đoàn xe, tối đa 255 ký tự |
| `routeChange` | object | Không (bắt buộc khi `responseAction` là `Reroute`) | Thay đổi tuyến, cùng dạng body của `apply-reroute` bên dưới: `deactivateCheckpointIds`, `newCheckpoints`, `insertAfterCheckpointId`. Phải có ít nhất một mốc bỏ hoặc thêm. Mỗi mốc mới bắt buộc có `address` |
| `revisedRouteNotes` | chuỗi | Không | Ghi chú thêm về tuyến đường, tối đa 2000 ký tự |
| `additionalCost` | số | Không | Chi phí phát sinh, không âm (mặc định 0) |
| `affectedTripHorseId` | số | Không | Chỉ dùng với `OffloadHorse`: con ngựa rời chuyến. Bỏ trống = con ngựa đã chọn lúc báo sự cố. Sự cố dỡ khẩn cấp thì phải là đúng con đã dỡ |
| `offloadOutcome` | chuỗi | Có khi `responseAction` là `OffloadHorse` | Hướng xử lý tiếp: `WaitRecover`, `ReturnToOrigin`, `HandOverHere`, `Deceased` (bảng ở đầu mục 11) |
| `vetReportPath` | chuỗi | Có khi `offloadOutcome` là `Deceased` | Đường dẫn biên bản thú y: tải file lên trước bằng `POST /api/files` với `category = documents` (PDF hoặc ảnh), rồi gửi `path` nhận được |

Ví dụ request:

```json
{
  "responseAction": "Reroute",
  "proposedAction": "Bỏ cửa khẩu Hữu Nghị đang tắc, đi qua Tân Thanh và nghỉ tại trạm thú y Bằng Tường",
  "routeChange": {
    "deactivateCheckpointIds": [12],
    "insertAfterCheckpointId": 10,
    "newCheckpoints": [
      {
        "checkpointName": "Cửa khẩu Tân Thanh",
        "checkpointType": "BorderGate",
        "address": "Tân Thanh, Văn Lãng, Lạng Sơn",
        "plannedTime": "2026-11-07T14:00:00+07:00",
        "mandatoryRestMinutes": 0
      }
    ]
  },
  "revisedRouteNotes": "Đã liên hệ trạm thú y Bằng Tường",
  "additionalCost": 650
}
```

Ví dụ phương án cho ngựa rời chuyến (ngựa chết, sự cố mức `Critical`):

```js
// 1. Tải biên bản thú y lên
const file = new FormData();
file.append("file", vetReportFile);       // PDF / JPG / PNG, tối đa 10 MB
file.append("category", "documents");
const uploaded = await api.post("/api/files", file);

// 2. Đề xuất phương án
await api.post(`/api/incidents/${incidentId}/propose`, {
  responseAction: "OffloadHorse",
  proposedAction: "Ngựa chết tại trạm thú y Bằng Tường, lập biên bản và báo khách",
  offloadOutcome: "Deceased",
  vetReportPath: uploaded.data.data.path,  // ví dụ "documents/2026/10/3f2a...e1.pdf"
});
```

Các hướng xử lý khác không cần biên bản, ví dụ:

```json
{
  "responseAction": "OffloadHorse",
  "proposedAction": "Gửi ngựa tại phòng khám thú y Lạng Sơn, xếp chuyến sau khi hồi phục",
  "affectedTripHorseId": 12,
  "offloadOutcome": "WaitRecover"
}
```

Response `200`: sự cố với `status` là `PlanProposed`, kèm `responseAction` và `routeChange` (dạng bên dưới, `appliedAt` là `null` vì chưa áp). Phương án `OffloadHorse` có thêm `offloadOutcome`, `affectedTripHorseId` và `vetReportUrl` (nếu có biên bản). Con ngựa **chưa** đổi trạng thái cho tới khi quản lý duyệt.

**Trường `routeChange` trong mọi response sự cố** (`null` nếu phương án không đổi tuyến):

| Trường | Kiểu | Mô tả |
|---|---|---|
| `insertAfterCheckpointId` | số / null | Mốc được chọn để chèn sau; `null` = sau mốc gần nhất đoàn xe đã tới lúc áp tuyến |
| `insertAfterName` | chuỗi / null | Tên mốc chèn sau (lúc đề xuất), để hiển thị |
| `removedCheckpoints` | mảng | `{ checkpointId, checkpointName, plannedTime }` các mốc bị bỏ |
| `newCheckpoints` | mảng | `{ checkpointId, checkpointName, checkpointType, address, plannedTime, mandatoryRestMinutes }`; `checkpointId` có giá trị sau khi tuyến đã áp (dùng để đánh dấu mốc mới trên timeline của tài xế) |
| `appliedAt` | thời gian / null | Lúc tuyến được áp vào lộ trình; `null` = chưa áp (đang chờ duyệt) |

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `A plan can only be proposed for a reported incident or after a rejection (current status: PlanProposed).` | Đã có phương án đang chờ duyệt, hoặc sự cố đã được duyệt / đóng |
| 400 | `ResponseAction 'X' is invalid. Allowed values: ...` | Sai loại hành động |
| 400 | `A reroute plan must remove or add at least one checkpoint.` | Chọn `Reroute` mà không gửi thay đổi tuyến |
| 400 | Các lỗi kiểm tra tuyến giống `apply-reroute` | Thay đổi tuyến không hợp lệ trên lộ trình hiện tại |
| 400 | `OffloadOutcome is required for an OffloadHorse plan. Allowed values: WaitRecover, ReturnToOrigin, HandOverHere, Deceased.` | Chọn `OffloadHorse` mà không chọn hướng xử lý |
| 400 | `Choose the horse that leaves the trip (AffectedTripHorseId).` | Sự cố không gắn con ngựa nào và request cũng không chọn |
| 400 | `Sakura Wind cannot leave the trip (status: Delivered).` | Con ngựa đã giao, đã chết hoặc đã rời chuyến theo phương án khác |
| 400 | `A horse can only be recorded as deceased on a Critical incident. Ask the crew to report a Critical incident for this horse.` | Chọn `Deceased` ở sự cố không phải mức `Critical` |
| 400 | `A veterinary report is required when a horse is recorded as deceased. ...` | Chọn `Deceased` mà không gửi `vetReportPath` |
| 400 | `VetReportPath is not a valid file path. ...` | `vetReportPath` không phải đường dẫn do `POST /api/files` (category `documents`) trả về |
| 400 | `A horse was offloaded in an emergency on this incident, so the plan must be OffloadHorse with the next step for that horse.` | Sự cố dỡ khẩn cấp mà đề xuất loại hành động khác |
| 400 | `This incident is about the horse offloaded in an emergency; the plan must be for that horse.` | Sự cố dỡ khẩn cấp mà chọn con ngựa khác |

### POST /api/incidents/{id}/approve — Duyệt phương án

Duyệt phương án đang `PlanProposed`. Nếu phương án có `routeChange`, tuyến được **áp vào lộ trình ngay trong lần duyệt này** (giống `apply-reroute`): mốc bị bỏ chuyển `Cancelled`, mốc mới được chèn, chuyến chuyển `EmergencyRerouting`, `routeChange.appliedAt` và `newCheckpoints[].checkpointId` có giá trị. Đoàn xe, người lập chuyến và khách nhận thông báo (kèm mô tả tuyến đổi nếu có).

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** không có (không cần body)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã sự cố |

Phương án `OffloadHorse`: con ngựa `affectedTripHorseId` đổi trạng thái theo `offloadOutcome` (bảng ở đầu mục 11): `Deceased` → ngựa `Deceased` cả trên chuyến và trong đơn; các hướng khác → `Offloaded` kèm `offloadOutcome`. Khách của con ngựa nhận thông báo + email; với `WaitRecover`, người lập chuyến được báo để xếp chuyến khác.

Response `200`: sự cố với `status` là `Approved`, có `approvedByName`, `approvedAt`; `rejectionReason` thành `null`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Only a proposed plan can be reviewed (current status: Reported).` | Chưa có phương án, hoặc phương án đã được xử lý |
| 400 | `The offload plan can no longer be applied (Sakura Wind is Delivered). Reject the plan so the coordinator can propose a new one.` | Từ lúc đề xuất tới lúc duyệt con ngựa đã được giao / đổi trạng thái, hoặc phương án `OffloadHorse` cũ (đề xuất trước bản này) chưa có hướng xử lý. Từ chối để điều phối viên đề xuất lại |
| 400 | `The proposed route change no longer fits the current route: ... Reject the plan so the coordinator can propose a new one.` | Từ lúc đề xuất tới lúc duyệt, đoàn xe đã đi qua mốc liên quan (hoặc chuyến không còn trên đường). Từ chối để điều phối viên đề xuất lại |

### POST /api/incidents/{id}/reject — Từ chối phương án

Từ chối phương án đang `PlanProposed`, bắt buộc ghi lý do. Lý do được lưu vào sự cố (`rejectionReason`) và gửi cho người lập chuyến qua thông báo. Điều phối viên sửa rồi gọi lại `propose`.

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã sự cố |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `reason` | chuỗi | Có | Lý do từ chối, tối đa 500 ký tự |

Ví dụ request:

```json
{
  "reason": "Chi phí quá cao, tìm phương án rẻ hơn."
}
```

Response `200`: sự cố với `status` là `Rejected` và `rejectionReason` vừa nhập (xem ví dụ ở đầu mục).

### POST /api/incidents/{id}/apply-reroute — Điều chỉnh tuyến thêm

Thay đổi tuyến chính nằm trong phương án (`propose` → `approve`). API này dùng khi cần **điều chỉnh thêm** sau khi phương án đã được duyệt: bỏ các mốc chưa tới và chèn các mốc mới. Chuyến chuyển sang `EmergencyRerouting`; đoàn xe và khách nhận thông báo. Mỗi lần gọi được ghi nối vào `routeChange` của sự cố. Gọi lại được nhiều lần khi sự cố còn `Approved`.

- **Ai gọi được:** FleetCoordinator, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã sự cố |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `deactivateCheckpointIds` | mảng số | Không | Mã các mốc cần bỏ. Chỉ bỏ được mốc chưa tới (`Pending`), không bỏ được `Origin` và `Destination` |
| `newCheckpoints` | mảng | Không | Các mốc mới theo đúng thứ tự đi. Mỗi phần tử có `checkpointName`, `checkpointType` (chỉ `RestStop` hoặc `BorderGate`), `address`, `plannedTime`, `mandatoryRestMinutes` |
| `insertAfterCheckpointId` | số | Không | Chèn các mốc mới ngay sau mốc này. Bỏ trống thì chèn sau mốc gần nhất đoàn xe đã tới |

Ví dụ request:

```json
{
  "insertAfterCheckpointId": 10,
  "deactivateCheckpointIds": [
    12
  ],
  "newCheckpoints": [
    {
      "checkpointName": "Trạm thú y Bằng Tường",
      "checkpointType": "RestStop",
      "address": "Bằng Tường, Quảng Tây",
      "plannedTime": "2026-11-07T16:00:00+07:00",
      "mandatoryRestMinutes": 0
    }
  ]
}
```

Phải có ít nhất một trong hai: `deactivateCheckpointIds` hoặc `newCheckpoints`.

Response `200` — `data` là **chi tiết chuyến** (dạng của `GET /api/trips/{id}`), trong đó:

- Mốc bị bỏ vẫn còn trong `checkpoints[]` với `isActive: false` và `status: "Cancelled"`.
- Các mốc còn hiệu lực được đánh lại `sequenceOrder` từ 1.
- `overallStatus` là `EmergencyRerouting`. Đoàn xe vẫn check-in các mốc bình thường.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `The route can only be changed after the plan is approved (current status: PlanProposed).` | Phương án chưa được duyệt, hoặc sự cố đã đóng |
| 400 | `'A' has already been reached (status: Departed) and cannot be removed.` | Bỏ một mốc đoàn xe đã tới |
| 400 | `'A' is the destination and cannot be removed from the route.` | Bỏ điểm đi hoặc điểm giao |
| 400 | `CheckpointType 'Origin' is invalid for a reroute. Allowed values: RestStop, BorderGate.` | Mốc mới sai loại |
| 400 | `New checkpoints must be listed in travel order with increasing PlannedTime.` | Giờ dự kiến của các mốc mới không tăng dần |
| 400 | `New checkpoints cannot be inserted before 'A', which the trip has already reached.` | `insertAfterCheckpointId` nằm trước vị trí hiện tại của đoàn xe |
| 400 | `Nothing to change: provide checkpoints to remove or new checkpoints to add.` | Gửi body rỗng |

### POST /api/incidents/{id}/resolve — Đóng sự cố

Ghi nhận sự cố đã xử lý xong và **chọn nguyên nhân** (bắt buộc). Chỉ đóng được sự cố có phương án đã duyệt (`Approved`). Nếu chuyến đang `EmergencyRerouting` và không còn sự cố nào khác đang xử lý, chuyến trở lại `InTransit`.

Nguyên nhân quyết định tiền (Chính sách mục 9.1), nên frontend nên hiện ngay dưới ô chọn hệ quả của từng lựa chọn:

| `faultParty` | Ví dụ | Chi phí phát sinh của sự cố | Bồi thường giao trễ |
|---|---|---|---|
| `Company` (lỗi công ty) | Xe hỏng do bảo dưỡng kém, thiếu giấy do chuyên viên, đi sai tuyến | **Công ty chịu**: không cộng vào phát sinh của khách | Có, nếu chuyến giao trễ (mục 19b) |
| `Customer` (lỗi khách) | Giấy sai / giả, khai sai tình trạng ngựa | Khách chịu (cộng vào phát sinh khi đóng chuyến) | Không |
| `ForceMajeure` (bất khả kháng) | Đóng biên, thiên tai, dịch bệnh, tai nạn do bên thứ ba | Khách chịu (cộng vào phát sinh khi đóng chuyến) | Không |

- **Ai gọi được:** FleetCoordinator, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã sự cố |

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `faultParty` | chuỗi | Có | `Company`, `Customer` hoặc `ForceMajeure` |
| `faultNote` | chuỗi | Không | Giải thích ngắn, tối đa 500 ký tự |

Ví dụ request:

```json
{
  "faultParty": "Company",
  "faultNote": "Phanh không được bảo dưỡng đúng hạn"
}
```

Response `200`: sự cố với `status` là `Resolved`, có `resolvedAt`, `faultParty`, `faultNote`; `tripStatus` cho biết trạng thái mới của chuyến.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Validation failed` (`FaultParty is required.`) | Không chọn nguyên nhân |
| 400 | `FaultParty 'Weather' is invalid. Allowed values: Company, Customer, ForceMajeure.` | Sai giá trị |
| 400 | `Only an incident with an approved plan can be resolved (current status: Reported).` | Phương án chưa được duyệt, hoặc sự cố đã đóng rồi |

---

## 12. Flow 6 – Bàn giao, đóng chuyến và báo cáo

Khi xe tới **điểm trả của một đơn**, đoàn xe **bàn giao** ngựa của đơn đó cho người nhận và lập **biên bản bàn giao điện tử** (e-POD): quét mã vi mạch từng con ngựa, ghi tình trạng, và lấy chữ ký vẽ tay của người nhận trên màn hình điện thoại. Một chuyến chở ngựa của nhiều đơn thì **mỗi đơn một biên bản**, ký ở điểm trả của đơn đó: mốc `DropOff` của đơn nếu có (giữa đường), không thì ở mốc `Destination`. Ký xong, ngựa của đơn thành `Delivered`; đơn giao hết ngựa thì **hoàn tất ngay** (`Completed`), không chờ đóng chuyến. Khi xe tới mốc cuối và mọi đơn đã ký, quản lý **đóng chuyến**: hệ thống chốt số phút trễ, điểm KPI, chi phí thực tế và chia chi phí phát sinh.

Vòng đời:

```
InTransit ──(tới DropOff của đơn A)──> ký e-POD đơn A ──> đơn A Completed ──(đi tiếp)──> ...
ArrivedDestination ──(ký e-POD các đơn còn lại)──> đủ biên bản ──(quản lý đóng chuyến)──> Completed

Khi chuyến Completed: xe được giải phóng; chi phí phát sinh của sự cố được chia cho các đơn
còn ngựa trên xe lúc xảy ra sự cố. Đơn đã Completed mà có phát sinh thì chuyển AwaitingFinalPayment.
```

**Giao tại chỗ (R-12).** Con ngựa rời chuyến với hướng xử lý `HandOverHere` được ký **biên bản riêng** tại nơi dỡ ngựa (gửi thêm `tripHorseId`), không cần xe ở điểm trả. Vì vậy một đơn có thể có nhiều biên bản trên cùng một chuyến: biên bản tại chỗ cho con đó và biên bản ở điểm trả cho các con còn lại. Đơn được **hoàn tất** khi mọi con ngựa đã xong: đã giao (ở điểm trả hoặc tại chỗ), đã chết, hoặc đã được đưa về nơi đi (`ReturnToOrigin`). Con `WaitRecover` còn phải đi chuyến sau nên đơn chưa hoàn tất.

Thứ tự gọi thông thường:

1. Tài xế: `GET /api/trips/{id}` để xem `bookings[]` (các đơn phải giao, `dropoffCheckpointId` là mốc trả của từng đơn) và `horses[]` (mã vi mạch, trạng thái từng con).
2. Tài xế: tới mốc trả của một đơn (`POST /api/checkpoints/{id}/arrive`), rồi `POST /api/trips/{tripId}/handover` cho đơn đó.
3. Quản lý: khi mọi `bookings[].handoverCompleted` là `true`, gọi `POST /api/trips/{tripId}/close`.
4. Quản lý: `GET /api/reports/summary` để xem báo cáo.

Một biên bản bàn giao (`handover`) có dạng:

```json
{
  "handoverId": 2,
  "tripId": 3,
  "tripCode": "TRP-2026-0003",
  "bookingId": 2,
  "bookingCode": "BKG-2026-0002",
  "customerName": "Jane Smith",
  "dropoffAddress": "Trường đua Tùng Hóa, Quảng Châu",
  "handoverDateTime": "2026-10-08T07:24:31+00:00",
  "recipientName": "Jane Smith",
  "recipientPhone": "+84988111222",
  "recipientIdCard": "PASSPORT-US-991283",
  "signatureUrl": "https://abcd.supabase.co/storage/v1/object/sign/files/signatures/2026/10/d17811....png?token=eyJ...",
  "overallCondition": "Excellent",
  "remarks": "Hai con ngựa đến nơi an toàn, không trầy xước.",
  "driverUserId": 4,
  "driverName": "Phạm Văn Đức",
  "horses": [
    {
      "tripHorseId": 7,
      "horseId": 1,
      "horseName": "Red Flash (Tia Chớp Đỏ)",
      "microchipNumber": "982000412345671"
    },
    {
      "tripHorseId": 8,
      "horseId": 2,
      "horseName": "Golden Pegasus (Kim Mã)",
      "microchipNumber": "982000412345672"
    }
  ]
}
```

| Trường | Ý nghĩa |
|---|---|
| `handoverId` | Mã biên bản |
| `tripId`, `tripCode` | Chuyến |
| `bookingId`, `bookingCode`, `customerName`, `dropoffAddress` | Đơn được bàn giao |
| `handoverDateTime` | Thời điểm bàn giao |
| `recipientName`, `recipientPhone`, `recipientIdCard` | Người nhận: họ tên, số điện thoại, số giấy tờ tùy thân |
| `signatureUrl` | Link ảnh chữ ký, dùng được ngay (mục 2.5) |
| `overallCondition` | Tình trạng ngựa khi giao: `Excellent` tốt · `NormalFatigue` mệt bình thường sau chuyến đi · `Injured` bị thương |
| `remarks` | Ghi chú |
| `driverUserId`, `driverName` | Người lập biên bản |
| `checkpointId` | Mốc nơi ký nhận (mốc `DropOff` của đơn hoặc `Destination`). Biên bản giao tại chỗ: mốc gần nhất đoàn xe đã tới lúc dỡ ngựa |
| `horses[]` | Các con ngựa được giao trong biên bản này (biên bản giao tại chỗ chỉ có một con) |

### POST /api/trips/{tripId}/handover — Lập biên bản bàn giao (e-POD)

Lập biên bản bàn giao cho **một** đơn khi xe đang dừng ở điểm trả của đơn đó (mốc `bookings[].dropoffCheckpointId` có `status` là `Arrived`). Mã vi mạch quét được phải khớp đúng các con ngựa **còn trên xe** của đơn (con đã rớt lại giữa đường không giao ở đây): thiếu một con hoặc thừa một mã lạ đều bị từ chối. Ký xong, các con ngựa thành `Delivered`; đơn giao hết ngựa thì thành `Completed` ngay. Khách nhận thông báo; khi đây là biên bản cuối cùng của chuyến đã tới đích, quản lý được báo để đóng chuyến.

- **Ai gọi được:** Đoàn xe của chuyến, Admin
- **Dữ liệu gửi lên:** `multipart/form-data` (xem mục 2.5)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `tripId` | Mã chuyến |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `bookingId` | số | Có | Đơn được bàn giao, lấy từ `bookings[].bookingId` của chuyến |
| `tripHorseId` | số | Không | Chỉ gửi khi **giao tại chỗ** con ngựa rời chuyến với hướng xử lý `HandOverHere` (`horses[].tripHorseId`). Khi đó không cần xe ở điểm trả, và `microchipNumbers` chỉ gồm mã của con này. Bỏ trống = giao ở điểm trả như bình thường |
| `recipientName` | chuỗi | Có | Họ tên người nhận, tối đa 100 ký tự |
| `recipientPhone` | chuỗi | Có | Số điện thoại người nhận |
| `recipientIdCard` | chuỗi | Có | Số CMND / CCCD / hộ chiếu của người nhận, tối đa 50 ký tự |
| `overallCondition` | chuỗi | Có | `Excellent`, `NormalFatigue` hoặc `Injured` |
| `remarks` | chuỗi | Không | Ghi chú, tối đa 2000 ký tự |
| `microchipNumbers` | nhiều giá trị | Có | Mã vi mạch của **từng** con ngựa được giao. Thêm trường này nhiều lần, mỗi lần một mã (xem ví dụ) |
| `signature` | file | Có | Ảnh chữ ký của người nhận: `jpg`, `jpeg`, `png`, tối đa 10 MB |

Ví dụ request:

```js
const form = new FormData();
form.append("bookingId", "2");
form.append("recipientName", "Jane Smith");
form.append("recipientPhone", "+84988111222");
form.append("recipientIdCard", "PASSPORT-US-991283");
form.append("overallCondition", "Excellent");
form.append("remarks", "Hai con ngựa đến nơi an toàn, không trầy xước.");

// Mỗi mã vi mạch một lần append, cùng tên trường
for (const chip of ["982000412345671", "982000412345672"]) {
  form.append("microchipNumbers", chip);
}

// Chữ ký vẽ trên <canvas>: đổi thành file ảnh rồi gửi
const blob = await new Promise((resolve) => canvas.toBlob(resolve, "image/png"));
form.append("signature", blob, "chu-ky.png");

const res = await api.post(`/api/trips/${tripId}/handover`, form);
```

Giao tại chỗ con ngựa rời chuyến (`HandOverHere`): như trên, thêm `tripHorseId` và chỉ quét mã vi mạch của con đó:

```js
form.append("bookingId", "2");
form.append("tripHorseId", "12");
form.append("microchipNumbers", "982000412345676");
// ... recipientName, recipientPhone, recipientIdCard, overallCondition, signature như trên
```

Response `201`: `data` là biên bản vừa lập (dạng như trên).

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Booking BKG-2026-0202 is handed over at 'Nanning'; record the arrival there first (status: Pending).` | Xe chưa tới điểm trả của đơn này |
| 400 | `Horses can only be handed over while the trip is under way or has arrived (current status: Scheduled).` | Chuyến chưa xuất phát hoặc đã đóng |
| 400 | `Booking BKG-2026-0202 has no horses on board to hand over.` | Không còn con nào của đơn trên xe (đã giao, hoặc đã rời chuyến hết) |
| 400 | `Microchip check failed. Not scanned: Golden Pegasus (Kim Mã) (982000412345672).` | Thiếu mã vi mạch của một con ngựa; `message` nêu tên con còn thiếu |
| 400 | `Microchip check failed. These microchips do not belong to booking BKG-2026-0002 on this trip: 999.` | Có mã vi mạch không thuộc đơn này |
| 400 | `Booking BKG-2026-0002 has already been handed over at 'Quảng Châu' on trip TRP-2026-0003.` | Đơn này đã có biên bản ở điểm trả trên chuyến |
| 400 | `Sakura Wind can only be handed over on site after an OffloadHorse plan with HandOverHere is approved (status: InTransit).` | Gửi `tripHorseId` cho con ngựa chưa có phương án `HandOverHere` được duyệt |
| 400 | `Trip horse with ID 99 does not belong to booking BKG-2026-0002 on trip TRP-2026-0003.` | `tripHorseId` không thuộc đơn trên chuyến |
| 400 | `Booking with ID 9 has no horses on trip TRP-2026-0003.` | Đơn không có ngựa trên chuyến này |
| 400 | `Signature is required.` | Không gửi ảnh chữ ký |
| 403 | `Only the crew assigned to this trip can record a handover.` | Người gọi không thuộc đoàn xe |

### GET /api/trips/{tripId}/handovers — Các biên bản bàn giao của một chuyến

Mọi biên bản đã lập trên một chuyến, theo thứ tự thời gian.

- **Ai gọi được:** Mọi nhân sự; tài xế / áp tải chỉ xem được chuyến của mình

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `tripId` | Mã chuyến |

Response `200`: `data` là mảng biên bản; mảng rỗng nếu chưa bàn giao đơn nào.

### GET /api/bookings/{bookingId}/handovers — Các biên bản bàn giao của một đơn

Khách dùng API này để xem biên bản của đơn mình: ai nhận, lúc nào, chữ ký, tình trạng ngựa. Một đơn chia ra nhiều chuyến thì có nhiều biên bản.

- **Ai gọi được:** Chủ đơn; nhân sự được xem đơn đó (như `GET /api/bookings/{id}`)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `bookingId` | Mã đơn |

Response `200`: `data` là mảng biên bản.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 403 | `You do not have access to this booking.` | Đơn của khách khác |

### POST /api/trips/{tripId}/close — Đóng chuyến và chốt KPI

Đóng một chuyến đã tới đích sau khi **mọi con ngựa trên chuyến đã xong**: đã ký biên bản bàn giao, hoặc đã rời chuyến theo phương án `OffloadHorse` được duyệt (R-12). Con ngựa đang chờ phương án (dỡ khẩn cấp) hoặc chờ ký e-POD tại chỗ (`HandOverHere`) sẽ chặn việc đóng chuyến. Hệ thống tự tính và lưu kết quả; không sửa lại được.

Quy tắc mới (R-09):

- Mọi sự cố đã duyệt phương án phải được **đóng** (chọn nguyên nhân) trước khi đóng chuyến.
- KPI: chuyến trễ chỉ được ghi `Delayed_AcceptableForceMajeure` khi **mọi** sự cố được chấp nhận đều do bất khả kháng hoặc do khách; còn lại (không có sự cố, hoặc có sự cố do công ty) là `Delayed_OperationalFault`.
- Chi phí phát sinh của sự cố do lỗi công ty **không** cộng vào phát sinh của khách.
- Chuyến trễ do lỗi công ty: với từng đơn ký e-POD sau hạn giao quá `Delay.GraceHours` giờ, hệ thống tạo một khoản **bồi thường giao trễ chờ Manager duyệt** (mục 19b) và báo các Manager.

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `tripId` | Mã chuyến |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `executiveRemarks` | chuỗi | Không | Nhận xét của quản lý về chuyến đi, tối đa 2000 ký tự |

Ví dụ request:

```json
{
  "executiveRemarks": "Trễ do hỏng xe, đã xử lý đúng quy trình."
}
```

Không có nhận xét thì gửi `{}`.

Response `200` — `data` là **chi tiết chuyến** (dạng của `GET /api/trips/{id}`) với `overallStatus` là `Completed`. Các trường kết quả:

| Trường | Ý nghĩa |
|---|---|
| `delayMinutes` | Số phút tới đích trễ so với `plannedEndDate` (đến sớm tính là 0) |
| `onTimeStatus` | `OnTime` đúng giờ · `Delayed_AcceptableForceMajeure` trễ vì lý do bất khả kháng · `Delayed_OperationalFault` trễ do lỗi vận hành |
| `kpiScore` | Điểm KPI của chuyến, từ 50 đến 100 |
| `actualCost` | Chi phí thực tế = `plannedCost` + chi phí phát sinh của các sự cố đã được duyệt |
| `closedByName`, `closedAt` | Ai đóng chuyến và lúc nào |
| `executiveRemarks` | Nhận xét vừa nhập |

Cách tính KPI:

| Trường hợp | `onTimeStatus` | `kpiScore` |
|---|---|---|
| Trễ không quá 30 phút | `OnTime` | 100 |
| Trễ hơn 30 phút, chuyến có sự cố đã được duyệt phương án (`Approved` hoặc `Resolved`) | `Delayed_AcceptableForceMajeure` | 100 |
| Trễ hơn 30 phút, không có sự cố nào được duyệt | `Delayed_OperationalFault` | 100 trừ 5 điểm cho mỗi 30 phút trễ, thấp nhất 50. Ví dụ trễ 125 phút: 80 điểm |

Sau khi đóng: xe trở lại trạng thái rảnh; mỗi đơn đã giao hết ngựa (và không còn chuyến nào dang dở) chuyển sang `Completed` và khách nhận thông báo.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Resolve incident(s) INC-2026-0007 and choose their cause before closing the trip.` | Còn sự cố đã duyệt phương án mà chưa đóng |
| 400 | `The trip cannot be closed: Sakura Wind (BKG-2026-0002): handover is not signed.` | Còn con ngựa chưa xong; `message` liệt kê từng con, mã đơn và lý do: `handover is not signed` (còn trên xe), `offloaded in an emergency, waiting for an approved OffloadHorse plan`, `on-site handover is not signed` |
| 400 | `Only a trip that has arrived at the destination can be closed (current status: InTransit).` | Chuyến chưa tới đích, hoặc đã đóng rồi |

### GET /api/reports/summary — Báo cáo tổng hợp

Số liệu tổng hợp của các chuyến **đã đóng** trong một khoảng thời gian: doanh thu, chi phí, tỉ lệ đúng giờ, KPI trung bình và số sự cố theo loại. Dùng cho màn hình dashboard của quản lý.

- **Ai gọi được:** LogisticsManager, Admin

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `from` | ngày giờ | Không | Chỉ tính chuyến đóng từ thời điểm này. Bỏ trống là không giới hạn |
| `to` | ngày giờ | Không | Chỉ tính chuyến đóng tới thời điểm này. Bỏ trống là không giới hạn |

Ví dụ: `GET /api/reports/summary?from=2026-10-01T00:00:00%2B07:00&to=2026-10-31T23:59:59%2B07:00`. Không truyền gì thì tính toàn bộ.

Response `200` — phần `data`:

```json
{
  "from": "2026-10-06T23:00:00+00:00",
  "to": "2026-10-08T23:00:00+00:00",
  "currencyCode": "USD",
  "completedTrips": 2,
  "completedBookings": 2,
  "horsesDelivered": 4,
  "revenue": 14288.48,
  "operatingCost": 9100,
  "grossProfit": 5188.48,
  "onTimeTrips": 0,
  "delayedForceMajeureTrips": 1,
  "delayedOperationalFaultTrips": 1,
  "onTimeRate": 0,
  "averageKpiScore": 90,
  "averageDelayMinutes": 152,
  "totalIncidents": 1,
  "incidentAdditionalCost": 400,
  "incidentsByType": [
    {
      "incidentType": "MechanicalBreakdown",
      "count": 1
    }
  ]
}
```

| Trường | Ý nghĩa |
|---|---|
| `completedTrips` | Số chuyến đã đóng trong kỳ |
| `completedBookings` | Số đơn đã hoàn tất có chuyến đóng trong kỳ |
| `horsesDelivered` | Số ngựa đã chở trên các chuyến đó |
| `revenue` | Doanh thu: tổng giá (`estimatedCost`) của các đơn đã hoàn tất |
| `operatingCost` | Tổng chi phí thực tế (`actualCost`) của các chuyến |
| `grossProfit` | `revenue` trừ `operatingCost` |
| `onTimeTrips`, `delayedForceMajeureTrips`, `delayedOperationalFaultTrips` | Số chuyến theo từng `onTimeStatus`; dùng vẽ biểu đồ tròn |
| `onTimeRate` | Tỉ lệ chuyến đúng giờ, tính theo phần trăm (0–100) |
| `averageKpiScore`, `averageDelayMinutes` | KPI trung bình và số phút trễ trung bình |
| `totalIncidents` | Số sự cố **được báo** trong kỳ |
| `incidentAdditionalCost` | Tổng chi phí phát sinh của các sự cố đã được duyệt |
| `incidentsByType[]` | Số sự cố theo từng loại, nhiều nhất trước |

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `'to' must not be before 'from'.` | `to` nhỏ hơn `from` |

---

## 13. Danh mục loại giấy tờ

Danh mục các loại giấy tờ dùng trong hồ sơ kiểm dịch (hộ chiếu ngựa, chứng nhận sức khỏe...). Mọi người đọc được; chuyên viên thủ tục và Admin quản lý.

Danh mục dùng chung cho hồ sơ kiểm dịch của đơn (mục 8) và hồ sơ thú y của ngựa (mục 6). Dữ liệu mẫu có 8 loại:

| `code` | Tên | Mặc định bắt buộc |
|---|---|---|
| `HORSE_PASSPORT` | Hộ chiếu ngựa | Có |
| `COGGINS_EIA` | Xét nghiệm thiếu máu truyền nhiễm (EIA/Coggins) | Có |
| `HEALTH_CERT` | Giấy chứng nhận kiểm dịch xuất khẩu | Có |
| `VACCINE` | Chứng nhận tiêm phòng cúm ngựa | Có |
| `IMPORT_PERMIT` | Giấy phép nhập khẩu | Không |
| `GLANDERS_TEST` | Xét nghiệm bệnh tỵ (Glanders) | Không |
| `PIROPLASMOSIS_TEST` | Xét nghiệm lê dạng trùng (Piroplasmosis) | Không |
| `EVA_TEST` | Xét nghiệm viêm động mạch do virus (EVA) | Không |

"Mặc định bắt buộc" (`isMandatory`) chỉ dùng khi tuyến đi chưa có quy định riêng. Giấy nào bắt buộc cho tuyến nào thì xem mục 16: ví dụ đi Singapore cần thêm giấy phép nhập khẩu và xét nghiệm Glanders.

Một loại giấy tờ có dạng:

```json
{
  "docTypeId": 1,
  "code": "HORSE_PASSPORT",
  "name": "Hộ chiếu ngựa",
  "description": "Hộ chiếu định danh cá thể (FEI hoặc hộ chiếu quốc gia có số UELN), có sơ đồ nhận dạng và lịch tiêm phòng",
  "isMandatory": true
}
```

### GET /api/document-types/lookup — Tất cả loại giấy tờ (cho dropdown)

Trả về toàn bộ loại giấy tờ, không phân trang. Dùng để đổ dropdown.

- **Ai gọi được:** Mọi người đã đăng nhập

Response `200` — phần `data`:

```json
[
  {
    "docTypeId": 4,
    "code": "VACCINE",
    "name": "Chứng nhận tiêm phòng cúm ngựa",
    "description": "Mũi nhắc gần nhất trong vòng 6 tháng và không tiêm trong 7 ngày trước ngày đi",
    "isMandatory": true
  },
  {
    "docTypeId": 2,
    "code": "COGGINS_EIA",
    "name": "Xét nghiệm thiếu máu truyền nhiễm (EIA/Coggins)",
    "description": "Kết quả âm tính bằng AGID hoặc ELISA, lấy mẫu trong vòng 30 ngày trước ngày đi",
    "isMandatory": true
  }
]
```

### GET /api/document-types — Danh sách loại giấy tờ

Danh sách có phân trang, dùng cho màn hình quản trị.

- **Ai gọi được:** Mọi người đã đăng nhập

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `search` | chuỗi | Không | Từ khóa tìm kiếm |
| `sort` | chuỗi | Không | Trường sắp xếp, thêm `-` phía trước để giảm dần |
| `page` | số | Không | Trang, bắt đầu từ 1 (mặc định 1) |
| `size` | số | Không | Số dòng mỗi trang, 1–100 (mặc định 10) |

`search` tìm theo `code` và `name`.

Response `200`: `data` là mảng loại giấy tờ, kèm `pagination`.

### GET /api/document-types/{id} — Chi tiết loại giấy tờ

Lấy một loại giấy tờ theo mã.

- **Ai gọi được:** Mọi người đã đăng nhập

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã loại giấy (`docTypeId`) |

Response `200`: `data` là một loại giấy tờ.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 404 | `Document type with ID 99 was not found.` | Không có loại giấy với mã đó |

### POST /api/document-types — Thêm loại giấy tờ

Tạo một loại giấy tờ mới.

- **Ai gọi được:** TransportSpecialist, Admin
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `code` | chuỗi | Có | Mã viết liền, chỉ gồm chữ, số, dấu gạch dưới; tối đa 30 ký tự; không trùng |
| `name` | chuỗi | Có | Tên hiển thị, tối đa 100 ký tự |
| `description` | chuỗi | Không | Mô tả, tối đa 255 ký tự |
| `isMandatory` | true / false | Không | Mặc định là giấy bắt buộc (mặc định `true`) |

Ví dụ request:

```json
{
  "code": "TRANSIT_PERMIT",
  "name": "Giấy phép quá cảnh",
  "description": "Cấp bởi nước quá cảnh",
  "isMandatory": false
}
```

Response `201`: `data` là loại giấy vừa tạo.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Document type code 'TRANSIT_PERMIT' already exists.` | Trùng `code` |

### PUT /api/document-types/{id} — Sửa loại giấy tờ

Sửa một loại giấy tờ; gửi lại đủ các trường.

- **Ai gọi được:** TransportSpecialist, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã loại giấy |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `code` | chuỗi | Có | Mã viết liền, chỉ gồm chữ, số, dấu gạch dưới; tối đa 30 ký tự; không trùng |
| `name` | chuỗi | Có | Tên hiển thị, tối đa 100 ký tự |
| `description` | chuỗi | Không | Mô tả, tối đa 255 ký tự |
| `isMandatory` | true / false | Không | Mặc định là giấy bắt buộc (mặc định `true`) |

Ví dụ request:

```json
{
  "code": "TRANSIT_PERMIT",
  "name": "Giấy phép quá cảnh động vật",
  "description": "Cấp bởi nước quá cảnh",
  "isMandatory": false
}
```

Response `200`: `data` là loại giấy sau khi sửa.

### DELETE /api/document-types/{id} — Xóa loại giấy tờ

Xóa một loại giấy tờ chưa được dùng ở đâu.

- **Ai gọi được:** TransportSpecialist, Admin

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã loại giấy |

Response `200`: `data` là `null`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `This document type is in use and cannot be deleted.` | Loại giấy đã có trong quy định của một nước hoặc trong hồ sơ |

---

## 14. Thông báo trong ứng dụng (chuông)

Mỗi khi có sự kiện (đơn được duyệt, giấy bị từ chối, chuyến xuất phát, có sự cố...), backend tự tạo một thông báo cho người liên quan. Các API dưới đây dùng để làm **biểu tượng chuông** trên thanh menu: hiện số thông báo chưa đọc, mở danh sách, bấm vào một thông báo thì đánh dấu đã đọc và chuyển tới trang liên quan.

Ai cũng chỉ thấy và thao tác được **thông báo của chính mình**. Gọi vào thông báo của người khác sẽ nhận `404` như thể thông báo đó không tồn tại.

Một thông báo có dạng:

```json
{
  "notificationId": 2,
  "title": "Ngựa đã tới điểm bàn giao",
  "message": "Chuyến TRP-2026-0001 đã tới Trường đua Tùng Hóa. Vui lòng kiểm tra và ký biên bản bàn giao.",
  "referenceType": "Trip",
  "referenceId": 1,
  "isRead": false,
  "createdAt": "2026-10-02T10:30:00+00:00"
}
```

| Trường | Ý nghĩa |
|---|---|
| `notificationId` | Mã thông báo (số nguyên lớn, kiểu `long` ở backend; trong JavaScript dùng như số bình thường) |
| `title` | Tiêu đề ngắn, hiện in đậm trong danh sách |
| `message` | Nội dung chi tiết |
| `referenceType` | Thông báo nói về cái gì: `Booking` (đơn), `Dossier` (hồ sơ kiểm dịch), `Trip` (chuyến), `Incident` (sự cố). Có thể `null` |
| `referenceId` | Mã của đối tượng đó, dùng để mở trang chi tiết. Có thể `null` |
| `isRead` | `false` là chưa đọc (nên tô đậm hoặc có chấm xanh) |
| `createdAt` | Thời điểm tạo (giờ UTC, xem mục 2.4) |

Gợi ý chuyển trang khi bấm vào thông báo (đổi đường dẫn cho khớp router của frontend):

| `referenceType` | Mở trang | API lấy dữ liệu trang đó |
|---|---|---|
| `Booking` | Chi tiết đơn | `GET /api/bookings/{referenceId}` |
| `Dossier` | Chi tiết hồ sơ | `GET /api/dossiers/{referenceId}` |
| `Trip` | Chi tiết chuyến (nhân sự) hoặc trang theo dõi đơn (khách) | `GET /api/trips/{referenceId}` |
| `Incident` | Chi tiết sự cố | `GET /api/incidents/{referenceId}` |

> Khách hàng không gọi được `GET /api/trips/{id}`. Với thông báo `Trip` gửi cho khách, hãy đưa khách về danh sách đơn của họ hoặc trang theo dõi đơn (`GET /api/tracking/bookings/{id}`).

### GET /api/notifications — Danh sách thông báo của tôi

Lấy thông báo của người đang đăng nhập, **mới nhất đứng đầu**. Có phân trang.

- **Ai gọi được:** Mọi người đã đăng nhập

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `isRead` | true / false | Không | `false` chỉ lấy thông báo chưa đọc, `true` chỉ lấy đã đọc; bỏ trống để lấy tất cả |
| `page` | số | Không | Trang, bắt đầu từ 1 (mặc định 1) |
| `size` | số | Không | Số dòng mỗi trang, 1–100 (mặc định 10) |

API này **không** có `search` và `sort`; thứ tự luôn là mới nhất trước.

Ví dụ: `GET /api/notifications?isRead=false&page=1&size=10`

Response `200`:

```json
{
  "success": true,
  "message": "Notifications retrieved successfully",
  "data": [
    {
      "notificationId": 2,
      "title": "Ngựa đã tới điểm bàn giao",
      "message": "Chuyến TRP-2026-0001 đã tới Trường đua Tùng Hóa. Vui lòng kiểm tra và ký biên bản bàn giao.",
      "referenceType": "Trip",
      "referenceId": 1,
      "isRead": false,
      "createdAt": "2026-10-02T10:30:00+00:00"
    },
    {
      "notificationId": 1,
      "title": "Đơn BKG-2026-0001 đã được phê duyệt",
      "message": "Yêu cầu vận chuyển của bạn đã được phê duyệt và phân công nhân sự phụ trách.",
      "referenceType": "Booking",
      "referenceId": 1,
      "isRead": true,
      "createdAt": "2026-09-20T02:00:00+00:00"
    }
  ],
  "errors": null,
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalItems": 2,
    "totalPages": 1
  }
}
```

Lỗi thường gặp:

| HTTP | `errors` | Khi nào |
|---|---|---|
| 400 | `The value 'abc' is not valid for IsRead.` | `isRead` không phải `true` / `false` |
| 400 | `Size must be between 1 and 100.` | `size` ngoài khoảng 1–100 |
| 401 | | Chưa đăng nhập hoặc token hết hạn |

### GET /api/notifications/unread-count — Số thông báo chưa đọc

Trả về số thông báo chưa đọc, để hiện con số đỏ trên biểu tượng chuông.

- **Ai gọi được:** Mọi người đã đăng nhập

Response `200`:

```json
{
  "success": true,
  "message": "Request processed successfully",
  "data": {
    "count": 1
  },
  "errors": null
}
```

Gọi API này **một lần** khi vừa đăng nhập / tải trang. Sau đó không cần gọi lại định kỳ: thông báo mới được backend đẩy tới ngay qua SignalR (xem mục 14.5 bên dưới), frontend chỉ việc cộng thêm 1.

### PATCH /api/notifications/{id}/read — Đánh dấu một thông báo đã đọc

Gọi khi người dùng bấm vào một thông báo. Gọi lại với thông báo đã đọc rồi vẫn trả `200` bình thường.

- **Ai gọi được:** Người nhận thông báo đó
- **Dữ liệu gửi lên:** không có body

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã thông báo (`notificationId`) |

Response `200`: `data` là thông báo với `isRead: true`.

```json
{
  "success": true,
  "message": "Notification marked as read successfully",
  "data": {
    "notificationId": 2,
    "title": "Ngựa đã tới điểm bàn giao",
    "message": "Chuyến TRP-2026-0001 đã tới Trường đua Tùng Hóa. Vui lòng kiểm tra và ký biên bản bàn giao.",
    "referenceType": "Trip",
    "referenceId": 1,
    "isRead": true,
    "createdAt": "2026-10-02T10:30:00+00:00"
  },
  "errors": null
}
```

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 404 | `Notification with ID 1 was not found.` | Không có thông báo đó, **hoặc** thông báo thuộc về người khác |

Ví dụ bấm vào một thông báo: đánh dấu đã đọc rồi chuyển trang.

```js
async function onClickNotification(n, navigate) {
  if (!n.isRead) {
    await api.patch(`/api/notifications/${n.notificationId}/read`);
  }
  const routes = {
    Booking: `/bookings/${n.referenceId}`,
    Dossier: `/dossiers/${n.referenceId}`,
    Trip: `/trips/${n.referenceId}`,
    Incident: `/incidents/${n.referenceId}`,
  };
  if (n.referenceType && routes[n.referenceType]) navigate(routes[n.referenceType]);
}
```

### PATCH /api/notifications/read-all — Đánh dấu tất cả đã đọc

Đánh dấu mọi thông báo chưa đọc của người đang đăng nhập là đã đọc (nút "Đánh dấu tất cả đã đọc").

- **Ai gọi được:** Mọi người đã đăng nhập
- **Dữ liệu gửi lên:** không có body

Response `200`: `data` là `null`. Sau đó `unread-count` sẽ trả `0`.

```json
{
  "success": true,
  "message": "All notifications marked as read successfully",
  "data": null,
  "errors": null
}
```

### 14.5. Nhận thông báo tức thời qua SignalR (`/hubs/notifications`)

Ngay khi backend tạo một thông báo, nó **đẩy** thông báo đó tới mọi tab trình duyệt đang mở của người nhận. Nhờ vậy chuông nhảy số và hiện toast ngay lập tức, không cần F5 hay gọi API định kỳ (polling).

**SignalR là gì?** Là thư viện của Microsoft giữ một kết nối mở giữa trình duyệt và server (thường là WebSocket). Server có thể chủ động gửi tin xuống trình duyệt bất cứ lúc nào. Phía frontend dùng gói npm `@microsoft/signalr`.

- **Địa chỉ hub:** `https://<backend>/hubs/notifications` (lưu ý: **không** có `/api` ở đầu)
- **Ai kết nối được:** Mọi người đã đăng nhập. Không có token hoặc token sai sẽ bị từ chối với `401`
- **Cách gửi token:** truyền qua `accessTokenFactory`. Thư viện tự gắn token vào query `?access_token=...`, vì WebSocket của trình duyệt không gửi được header `Authorization`
- **Sự kiện server gửi xuống:** `notificationReceived`. Payload giống hệt **một phần tử** trong `GET /api/notifications`:

```json
{
  "notificationId": 3,
  "title": "New booking BKG-2026-0002",
  "message": "A transport request for 1 horse(s) from VN to TH is waiting for review.",
  "referenceType": "Booking",
  "referenceId": 2,
  "isRead": false,
  "createdAt": "2026-10-19T20:32:02.964641+00:00"
}
```

- **Client gọi lên server:** không có method nào. Đọc danh sách và đánh dấu đã đọc vẫn dùng các API REST ở trên

Những điều nên biết:

| Tình huống | Hành vi |
|---|---|
| Một người mở nhiều tab | Tab nào cũng nhận được |
| Thao tác bị lỗi và rollback (ví dụ duyệt đơn thất bại giữa chừng) | Không có gì được đẩy. Backend chỉ đẩy **sau khi** dữ liệu đã lưu thành công |
| Mất mạng một lúc rồi kết nối lại | Trong lúc mất kết nối có thể lỡ vài thông báo. Sau khi kết nối lại, hãy gọi lại `GET /api/notifications` và `unread-count` |
| Token hết hạn | Kết nối đang mở vẫn chạy tiếp. Lần kết nối lại sau sẽ cần token mới (refresh token như với REST) |
| Server không đẩy được (lỗi mạng...) | Thông báo vẫn nằm trong database, lần gọi `GET /api/notifications` sau sẽ thấy |

Ví dụ (React, dùng file axios chung ở mục 1):

```js
// npm install @microsoft/signalr
import { HubConnectionBuilder } from '@microsoft/signalr';

const BACKEND = 'https://racehorsetransportbackend-production.up.railway.app';

const connection = new HubConnectionBuilder()
  .withUrl(`${BACKEND}/hubs/notifications`, {
    accessTokenFactory: () => localStorage.getItem('accessToken'), // token đang dùng cho REST
    withCredentials: false, // gửi token bằng accessTokenFactory, không dùng cookie
  })
  .withAutomaticReconnect() // tự kết nối lại khi rớt mạng
  .build();

connection.on('notificationReceived', (n) => {
  setUnread((count) => count + 1);
  setItems((list) => [n, ...list]);
  // ví dụ hiện toast: notification.open({ title: n.title, description: n.message });
});

// Kết nối lại xong thì tải lại cho chắc (có thể đã lỡ thông báo lúc mất mạng)
connection.onreconnected(() => reloadNotifications());

await connection.start();
// Khi đăng xuất hoặc component unmount: await connection.stop();
```

Frontend của dự án đã làm sẵn phần này. Xem `src/hooks/useNotifications.js` và `src/components/layout/NotificationBell.jsx`.

---

## 15. Quản lý phương tiện

Danh sách xe tải chở ngựa, xe van và chuồng hàng không (air stall) của công ty. Điều phối viên dùng màn hình này để thêm xe mới, cập nhật tình trạng (đang bảo dưỡng...) và ngày khử trùng gần nhất. Khi xếp chuyến (mục 9) chỉ những xe `Available` mới hiện trong `GET /api/trips/available-vehicles`.

- **Xem:** mọi nhân sự (LogisticsManager, TransportSpecialist, FleetCoordinator, DriverEscort, Admin). Khách hàng không xem được (`403`).
- **Thêm / sửa / xóa:** FleetCoordinator, Admin.

Một phương tiện có dạng:

```json
{
  "assetId": 2,
  "assetCode": "51C-246.80",
  "assetType": "HorseVan",
  "capacityHorses": 2,
  "hasClimateControl": false,
  "hasGpsTracker": true,
  "status": "Available",
  "lastSanitizationDate": "2026-09-25T01:00:00+00:00"
}
```

| Trường | Ý nghĩa |
|---|---|
| `assetId` | Mã phương tiện, dùng làm `vehicleId` khi tạo chuyến |
| `assetCode` | Biển số xe hoặc mã chuồng bay; luôn được lưu **chữ in hoa**, không trùng |
| `assetType` | Loại: `HorseTruck_AirSuspension` xe tải chở ngựa có giảm xóc khí · `HorseVan` xe van chở ngựa · `AirStall` chuồng dùng cho đường hàng không |
| `capacityHorses` | Chở tối đa bao nhiêu con ngựa (1–12) |
| `hasClimateControl` | Có điều hòa hay không |
| `hasGpsTracker` | Có thiết bị định vị hay không |
| `status` | Tình trạng: `Available` sẵn sàng · `InTransit` đang chạy chuyến · `Maintenance` đang bảo dưỡng |
| `lastSanitizationDate` | Lần khử trùng gần nhất (giờ UTC); có thể `null` |

### GET /api/transport-assets — Danh sách phương tiện

- **Ai gọi được:** Mọi nhân sự (không gồm Customer)

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `search` | chuỗi | Không | Tìm theo `assetCode` (một phần biển số) |
| `status` | chuỗi | Không | Lọc theo tình trạng: `Available`, `InTransit`, `Maintenance` |
| `assetType` | chuỗi | Không | Lọc theo loại: `HorseTruck_AirSuspension`, `HorseVan`, `AirStall` |
| `sort` | chuỗi | Không | Trường sắp xếp, thêm `-` phía trước để giảm dần (ví dụ `-capacityHorses`) |
| `page` | số | Không | Trang, bắt đầu từ 1 (mặc định 1) |
| `size` | số | Không | Số dòng mỗi trang, 1–100 (mặc định 10) |

Ví dụ: `GET /api/transport-assets?status=Available&assetType=HorseVan`

Response `200`:

```json
{
  "success": true,
  "message": "Transport assets retrieved successfully",
  "data": [
    {
      "assetId": 2,
      "assetCode": "51C-246.80",
      "assetType": "HorseVan",
      "capacityHorses": 2,
      "hasClimateControl": false,
      "hasGpsTracker": true,
      "status": "Available",
      "lastSanitizationDate": "2026-09-25T01:00:00+00:00"
    }
  ],
  "errors": null,
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Status 'Broken' is invalid. Allowed values: Available, InTransit, Maintenance.` | `status` sai giá trị (phân biệt hoa thường) |
| 400 | `AssetType 'Truck' is invalid. Allowed values: HorseTruck_AirSuspension, HorseVan, AirStall.` | `assetType` sai giá trị |
| 403 | `You do not have permission to perform this action.` | Khách hàng gọi |

### GET /api/transport-assets/{id} — Chi tiết phương tiện

- **Ai gọi được:** Mọi nhân sự (không gồm Customer)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã phương tiện (`assetId`) |

Response `200`: `data` là một phương tiện.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 404 | `Transport asset with ID 99 was not found.` | Không có phương tiện với mã đó |

### POST /api/transport-assets — Thêm phương tiện

- **Ai gọi được:** FleetCoordinator, Admin
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `assetCode` | chuỗi | Có | Biển số / mã, tối đa 30 ký tự, không trùng. Gõ chữ thường cũng được, server tự đổi sang in hoa |
| `assetType` | chuỗi | Có | `HorseTruck_AirSuspension`, `HorseVan` hoặc `AirStall` |
| `capacityHorses` | số | Có | Sức chứa, từ 1 đến 12 |
| `hasClimateControl` | true / false | Không | Mặc định `true` |
| `hasGpsTracker` | true / false | Không | Mặc định `true` |
| `status` | chuỗi | Có | `Available`, `InTransit` hoặc `Maintenance`. Xe mới thường là `Available` |
| `lastSanitizationDate` | ngày giờ | Không | Lần khử trùng gần nhất, dạng ISO 8601 (ví dụ `2026-10-14T08:00:00+07:00`) |

Ví dụ request:

```json
{
  "assetCode": "51c-999.01",
  "assetType": "HorseVan",
  "capacityHorses": 2,
  "hasClimateControl": true,
  "hasGpsTracker": true,
  "status": "Available",
  "lastSanitizationDate": "2026-10-14T08:00:00+07:00"
}
```

Response `201`:

```json
{
  "success": true,
  "message": "Transport asset created successfully",
  "data": {
    "assetId": 6,
    "assetCode": "51C-999.01",
    "assetType": "HorseVan",
    "capacityHorses": 2,
    "hasClimateControl": true,
    "hasGpsTracker": true,
    "status": "Available",
    "lastSanitizationDate": "2026-10-14T08:00:00+07:00"
  },
  "errors": null
}
```

Lỗi thường gặp:

| HTTP | `message` / `errors` | Khi nào |
|---|---|---|
| 400 | `Transport asset code '51C-999.01' already exists.` | Trùng biển số (không phân biệt hoa thường) |
| 400 | `AssetType 'Truck' is invalid. Allowed values: HorseTruck_AirSuspension, HorseVan, AirStall.` | Loại phương tiện sai |
| 400 | `CapacityHorses must be between 1 and 12.` | Sức chứa ngoài khoảng 1–12 |
| 403 | `You do not have permission to perform this action.` | Người gọi không phải FleetCoordinator / Admin |

```js
const res = await api.post('/api/transport-assets', {
  assetCode: form.assetCode,
  assetType: form.assetType,          // lấy từ dropdown
  capacityHorses: Number(form.capacityHorses),
  hasClimateControl: form.hasClimateControl,
  hasGpsTracker: form.hasGpsTracker,
  status: 'Available',
  lastSanitizationDate: form.lastSanitizationDate || null,
});
console.log(res.data.data.assetId);
```

### PUT /api/transport-assets/{id} — Sửa phương tiện

Sửa thông tin hoặc đổi tình trạng của một phương tiện (ví dụ đưa xe vào bảo dưỡng). Gửi lại **đủ** các trường, kể cả trường không đổi.

- **Ai gọi được:** FleetCoordinator, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã phương tiện |

Các trường gửi lên: giống `POST /api/transport-assets`, chỉ khác là `hasClimateControl` và `hasGpsTracker` **không có mặc định** — bỏ trống sẽ thành `false`, nên luôn gửi kèm.

Ví dụ request (đưa xe vào bảo dưỡng):

```json
{
  "assetCode": "51C-999.01",
  "assetType": "HorseVan",
  "capacityHorses": 2,
  "hasClimateControl": true,
  "hasGpsTracker": true,
  "status": "Maintenance",
  "lastSanitizationDate": "2026-10-14T08:00:00+07:00"
}
```

Response `200`: `data` là phương tiện sau khi sửa, `message` là `Transport asset updated successfully`.

Lỗi thường gặp: như khi thêm, cộng thêm:

| HTTP | `message` | Khi nào |
|---|---|---|
| 404 | `Transport asset with ID 99 was not found.` | Không có phương tiện với mã đó |

### DELETE /api/transport-assets/{id} — Xóa phương tiện

Xóa hẳn một phương tiện **chưa từng được xếp vào chuyến nào**. Xe đã chạy chuyến thì không xóa được; muốn ngừng dùng thì sửa `status` sang `Maintenance`.

- **Ai gọi được:** FleetCoordinator, Admin

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã phương tiện |

Response `200`: `data` là `null`, `message` là `Transport asset deleted successfully`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `This transport asset has been assigned to a trip and cannot be deleted.` | Xe đã có trong ít nhất một chuyến |
| 404 | `Transport asset with ID 99 was not found.` | Không có phương tiện với mã đó |

---

## 16. Quy định giấy tờ theo quốc gia

Bảng quy định "nước nào, chiều nào, cần những giấy gì". Backend dựa vào bảng này để lập **checklist giấy tờ** của hồ sơ kiểm dịch (mục 8): lấy các giấy **xuất khẩu** (`Export`) của nước đi, cộng các giấy **quá cảnh** (`Transit`) của từng nước ngựa đi qua (`transitCountryCodes` của đơn), cộng các giấy **nhập khẩu** (`Import`) của nước đến. Loại giấy mà nhiều nước cùng yêu cầu chỉ hiện một dòng, lấy thời hạn ngắn hơn.

Ví dụ: đơn đi từ Việt Nam sang Thái Lan sẽ cần các giấy `VN` + `Export` và các giấy `TH` + `Import`. Đơn đường bộ Việt Nam → Nga đi qua Trung Quốc cần thêm các giấy `CN` + `Transit` (giấy phép quá cảnh, chứng nhận sức khỏe còn hạn khi vào cửa khẩu).

Bảng này còn quyết định **ngày pickup sớm nhất** của đơn: `processingDays` là số ngày cần để có giấy đó (xin giấy phép, xét nghiệm, cách ly trước xuất). Thời gian làm hồ sơ của một tuyến = `processingDays` **lớn nhất** trong các quy định Export + Transit + Import của tuyến (các giấy làm song song nên không cộng dồn); tuyến chưa có số liệu thì dùng 14 ngày.

> Checklist được tính lại **mỗi lần xem hồ sơ**, nên sửa bảng này là checklist của mọi hồ sơ (kể cả hồ sơ đang làm) đổi theo ngay. Nếu cặp nước của đơn chưa có quy định nào, checklist dùng các loại giấy có `isMandatory: true` trong danh mục loại giấy tờ (mục 13).

- **Xem:** mọi người đã đăng nhập (kể cả khách, để biết trước cần chuẩn bị giấy gì).
- **Thêm / sửa / xóa:** TransportSpecialist, Admin.

Một quy định có dạng:

```json
{
  "requirementId": 4,
  "countryCode": "TH",
  "direction": "Import",
  "docTypeId": 2,
  "docTypeCode": "COGGINS_EIA",
  "docTypeName": "Xét nghiệm thiếu máu truyền nhiễm (EIA/Coggins)",
  "validityDays": 30,
  "processingDays": 7,
  "isMandatory": true,
  "regulationNote": "Xét nghiệm EIA âm tính trong 30 ngày"
}
```

| Trường | Ý nghĩa |
|---|---|
| `requirementId` | Mã quy định |
| `countryCode` | Mã nước 2 chữ cái in hoa theo chuẩn ISO (`VN`, `TH`, `JP`...) |
| `direction` | `Export` giấy cần khi **rời** nước này · `Import` giấy cần khi **vào** nước này · `Transit` giấy cần khi chỉ **đi qua** nước này |
| `docTypeId`, `docTypeCode`, `docTypeName` | Loại giấy tờ (lấy từ danh mục ở mục 13) |
| `validityDays` | Giấy chỉ có giá trị trong bao nhiêu ngày; `null` là không giới hạn |
| `processingDays` | Số ngày cần để có giấy (xin phép, xét nghiệm, cách ly); dùng tính ngày pickup sớm nhất của tuyến. `null` là không tính |
| `isMandatory` | `true` bắt buộc, `false` khuyến nghị |
| `regulationNote` | Ghi chú quy định, hiện cho khách đọc; có thể `null` |

Mỗi bộ ba (`countryCode`, `direction`, `docTypeId`) chỉ có **một** dòng.

### GET /api/country-doc-requirements — Danh sách quy định

- **Ai gọi được:** Mọi người đã đăng nhập

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `countryCode` | chuỗi | Không | Lọc theo nước, đúng 2 chữ cái (gõ thường cũng được) |
| `direction` | chuỗi | Không | `Export`, `Import` hoặc `Transit` |
| `page` | số | Không | Trang, bắt đầu từ 1 (mặc định 1) |
| `size` | số | Không | Số dòng mỗi trang, 1–100 (mặc định 10) |

API này **không** có `search`.

Ví dụ: `GET /api/country-doc-requirements?countryCode=th&direction=Import&size=2`

Response `200`:

```json
{
  "success": true,
  "message": "Country document requirements retrieved successfully",
  "data": [
    {
      "requirementId": 3,
      "countryCode": "TH",
      "direction": "Import",
      "docTypeId": 1,
      "docTypeCode": "HORSE_PASSPORT",
      "docTypeName": "Hộ chiếu ngựa",
      "validityDays": null,
      "isMandatory": true,
      "regulationNote": "Hộ chiếu FEI còn hiệu lực"
    },
    {
      "requirementId": 4,
      "countryCode": "TH",
      "direction": "Import",
      "docTypeId": 2,
      "docTypeCode": "COGGINS_EIA",
      "docTypeName": "Xét nghiệm thiếu máu truyền nhiễm (EIA/Coggins)",
      "validityDays": 30,
      "isMandatory": true,
      "regulationNote": "Xét nghiệm EIA âm tính trong 30 ngày"
    }
  ],
  "errors": null,
  "pagination": {
    "page": 1,
    "pageSize": 2,
    "totalItems": 4,
    "totalPages": 2
  }
}
```

Lỗi thường gặp:

| HTTP | `message` / `errors` | Khi nào |
|---|---|---|
| 400 | `CountryCode must contain exactly two letters.` | `countryCode` không phải đúng 2 chữ cái (ví dụ `VNM`) |
| 400 | `Direction 'Inbound' is invalid. Allowed values: Export, Import, Transit.` | `direction` sai giá trị |

Ví dụ hiện cho khách "sang Thái Lan cần chuẩn bị gì":

```js
const res = await api.get('/api/country-doc-requirements', {
  params: { countryCode: 'TH', direction: 'Import', size: 100 },
});
const docs = res.data.data; // mỗi phần tử: docTypeName, validityDays, isMandatory, regulationNote
```

### GET /api/country-doc-requirements/{id} — Chi tiết một quy định

- **Ai gọi được:** Mọi người đã đăng nhập

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã quy định (`requirementId`) |

Response `200`: `data` là một quy định.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 404 | `Country document requirement with ID 999 was not found.` | Không có quy định với mã đó |

### POST /api/country-doc-requirements — Thêm quy định

- **Ai gọi được:** TransportSpecialist, Admin
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `countryCode` | chuỗi | Có | Đúng 2 chữ cái; server tự đổi sang in hoa |
| `direction` | chuỗi | Có | `Export`, `Import` hoặc `Transit` |
| `docTypeId` | số | Có | Mã loại giấy, lấy từ `GET /api/document-types/lookup` |
| `validityDays` | số | Không | Số ngày giấy còn hiệu lực, lớn hơn 0; bỏ trống là không giới hạn |
| `processingDays` | số | Không | Số ngày cần để có giấy, 0–365; bỏ trống là không tính vào thời gian làm hồ sơ |
| `isMandatory` | true / false | Không | Mặc định `true` |
| `regulationNote` | chuỗi | Không | Ghi chú, tối đa 255 ký tự |

Ví dụ request:

```json
{
  "countryCode": "vn",
  "direction": "Export",
  "docTypeId": 4,
  "validityDays": 180,
  "isMandatory": false,
  "regulationNote": "Một số nước nhận yêu cầu thêm sổ tiêm phòng"
}
```

Response `201`:

```json
{
  "success": true,
  "message": "Country document requirement created successfully",
  "data": {
    "requirementId": 63,
    "countryCode": "VN",
    "direction": "Export",
    "docTypeId": 4,
    "docTypeCode": "VACCINE",
    "docTypeName": "Chứng nhận tiêm phòng cúm ngựa",
    "validityDays": 180,
    "isMandatory": false,
    "regulationNote": "Một số nước nhận yêu cầu thêm sổ tiêm phòng"
  },
  "errors": null
}
```

Lỗi thường gặp:

| HTTP | `message` / `errors` | Khi nào |
|---|---|---|
| 400 | `A document requirement for SG, Import, document type ID 2 already exists.` | Nước + chiều + loại giấy này đã có quy định; hãy sửa dòng cũ |
| 400 | `Document type with ID 999 does not exist.` | `docTypeId` không có trong danh mục |
| 400 | `Direction 'Inbound' is invalid. Allowed values: Export, Import, Transit.` | `direction` sai giá trị |
| 400 | `CountryCode must contain exactly two letters.` | Mã nước sai |
| 403 | `You do not have permission to perform this action.` | Người gọi không phải TransportSpecialist / Admin |

### PUT /api/country-doc-requirements/{id} — Sửa quy định

Gửi lại **đủ** các trường như khi thêm.

- **Ai gọi được:** TransportSpecialist, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã quy định |

Ví dụ request:

```json
{
  "countryCode": "VN",
  "direction": "Export",
  "docTypeId": 4,
  "validityDays": 90,
  "isMandatory": true,
  "regulationNote": "Tiêm phòng cúm ngựa trong 90 ngày trước ngày đi"
}
```

Response `200`: `data` là quy định sau khi sửa, `message` là `Country document requirement updated successfully`.

Lỗi thường gặp: như khi thêm (đổi sang một bộ nước + chiều + loại giấy đã có dòng khác cũng bị báo trùng), cộng thêm:

| HTTP | `message` | Khi nào |
|---|---|---|
| 404 | `Country document requirement with ID 999 was not found.` | Không có quy định với mã đó |

### DELETE /api/country-doc-requirements/{id} — Xóa quy định

- **Ai gọi được:** TransportSpecialist, Admin

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã quy định |

Response `200`: `data` là `null`, `message` là `Country document requirement deleted successfully`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 404 | `Country document requirement with ID 999 was not found.` | Không có quy định với mã đó |

---

## 17. Bảng giá

Các dòng giá mà backend dùng để **tính báo giá** cho đơn (`POST /api/bookings/quote-preview` và `POST /api/bookings`, mục 7). Quản lý dùng màn hình này để chỉnh giá cước, phụ phí, chiết khấu... mà không cần sửa code. Giá mới có hiệu lực với các báo giá tính **sau** khi lưu; đơn đã gửi giữ nguyên giá cũ.

- **Ai gọi được (cả xem lẫn sửa):** LogisticsManager, Admin. Khách hàng và các nhân sự khác nhận `403`.
- Không có API xóa: muốn ngừng một dòng giá thì **tắt** nó bằng `PATCH /api/price-items/{id}/active`.

Một dòng giá có dạng:

```json
{
  "priceItemId": 1,
  "category": "Freight",
  "itemCode": "FREIGHT",
  "itemName": "Zone A - Đông Nam Á (Thái Lan)",
  "transportMode": "Air",
  "destCountryCode": "TH",
  "calcType": "PerHorse",
  "value": 3000.0000,
  "minHorses": null,
  "isActive": true
}
```

| Trường | Ý nghĩa |
|---|---|
| `priceItemId` | Mã dòng giá |
| `category` | Nhóm giá (bảng bên dưới) |
| `itemCode` | Mã khoản giá; chỉ gồm chữ, số, dấu gạch dưới; luôn được lưu **in hoa** |
| `itemName` | Tên hiển thị, cũng là tên dòng trong báo giá gửi khách |
| `transportMode` | Chỉ áp dụng cho `Ground` hoặc `Air`; `null` là áp dụng cho cả hai |
| `destCountryCode` | Chỉ áp dụng khi nước đến là nước này; `null` là mọi nước |
| `calcType` | Cách tính (bảng bên dưới) |
| `value` | Con số dùng để tính: số tiền, hệ số hoặc phần trăm tùy `calcType` |
| `minHorses` | Chỉ dùng cho chiết khấu: đơn có từ bao nhiêu con ngựa trở lên thì được hưởng |
| `isActive` | `false` là đã tắt, không được dùng khi tính giá |

**Nhóm giá (`category`) và các mã mà công thức đang dùng:**

| `category` | Ý nghĩa | Mã (`itemCode`) công thức đọc |
|---|---|---|
| `Freight` | Cước vận chuyển gốc | `FREIGHT` (đường bay tính theo con, đường bộ tính theo km × con) |
| `StallClass` | Hệ số theo hạng chuồng, nhân vào cước gốc | `STALL_SHARED`, `STALL_COMFORT`, `STALL_PRIVATE` |
| `Discount` | Chiết khấu theo số ngựa (%) | Mọi mã; lấy bậc có `minHorses` cao nhất mà đơn đạt tới |
| `Surcharge` | Phụ phí (%) tính trên cước sau chiết khấu | `SUR_FUEL` (luôn áp), `SUR_EXPRESS` (khởi hành dưới 7 ngày), `SUR_CLIMATE` (khi khách cần điều hòa) |
| `Clearance` | Phí thủ tục, thông quan, kiểm dịch | Mọi mã có `value` > 0 (ví dụ `FEE_DOCS`, `FEE_CLEARANCE`, `FEE_QUARANTINE`) |
| `Escort` | Phí người chăm ngựa đi kèm (1 người cho mỗi 3 ngựa) | `ESCORT` |
| `Insurance` | Bảo hiểm (% giá trị khai báo) | `INS_TRANSIT` |

> **Lưu ý khi thêm mã mới:** chỉ nhóm `Discount` và `Clearance` tự nhận mọi mã mới. Ở các nhóm còn lại, công thức chỉ đọc đúng những mã trong bảng trên — thêm một phụ phí mới như `SUR_HOLIDAY` sẽ lưu được nhưng **không** được cộng vào báo giá. Đổi `itemCode` của một dòng đang dùng (ví dụ đổi `FREIGHT`) có thể làm báo giá báo lỗi thiếu cước.

**Cách tính (`calcType`):**

| `calcType` | `value` nghĩa là |
|---|---|
| `PerHorse` | Số tiền cho mỗi con ngựa |
| `PerKmPerHorse` | Số tiền cho mỗi km, mỗi con ngựa |
| `Multiplier` | Hệ số nhân (ví dụ `1.5`) |
| `Percent` | Phần trăm (ví dụ `10` là 10%) |
| `PerBooking` | Số tiền cho cả đơn |
| `PerGroom` | Số tiền cho mỗi người chăm ngựa |
| `PerGroomDay` | Số tiền cho mỗi người chăm ngựa mỗi ngày |

**Dòng nào được chọn khi có nhiều dòng cùng mã?** Ví dụ `FREIGHT` có một dòng cho từng nước đến. Khi tính giá, backend chỉ xét các dòng đang bật, khớp phương thức (hoặc `null`) và khớp nước đến (hoặc `null`); trong số đó, dòng **ghi rõ nước đến** được ưu tiên, rồi tới dòng **ghi rõ phương thức**. Vì vậy mỗi bộ (`itemCode`, `transportMode`, `destCountryCode`) chỉ được có một dòng.

### GET /api/price-items — Danh sách dòng giá

- **Ai gọi được:** LogisticsManager, Admin

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `search` | chuỗi | Không | Tìm theo `itemCode` và `itemName` |
| `category` | chuỗi | Không | Lọc theo nhóm (`Freight`, `StallClass`, `Surcharge`, `Discount`, `Clearance`, `Escort`, `Insurance`) |
| `transportMode` | chuỗi | Không | `Ground` hoặc `Air` (chỉ lấy dòng ghi đúng phương thức đó, không gồm dòng `null`) |
| `isActive` | true / false | Không | Lọc dòng đang bật / đã tắt |
| `sort` | chuỗi | Không | Trường sắp xếp, thêm `-` phía trước để giảm dần (ví dụ `-value`) |
| `page` | số | Không | Trang, bắt đầu từ 1 (mặc định 1) |
| `size` | số | Không | Số dòng mỗi trang, 1–100 (mặc định 10) |

Ví dụ: `GET /api/price-items?category=Discount`

Response `200`:

```json
{
  "success": true,
  "message": "Price items retrieved successfully",
  "data": [
    {
      "priceItemId": 23,
      "category": "Discount",
      "itemCode": "DISC_VOL_2",
      "itemName": "Chiết khấu 2-3 ngựa",
      "transportMode": null,
      "destCountryCode": null,
      "calcType": "Percent",
      "value": 5.0000,
      "minHorses": 2,
      "isActive": true
    },
    {
      "priceItemId": 24,
      "category": "Discount",
      "itemCode": "DISC_VOL_4",
      "itemName": "Chiết khấu 4-6 ngựa",
      "transportMode": null,
      "destCountryCode": null,
      "calcType": "Percent",
      "value": 10.0000,
      "minHorses": 4,
      "isActive": true
    },
    {
      "priceItemId": 25,
      "category": "Discount",
      "itemCode": "DISC_VOL_7",
      "itemName": "Chiết khấu từ 7 ngựa",
      "transportMode": null,
      "destCountryCode": null,
      "calcType": "Percent",
      "value": 15.0000,
      "minHorses": 7,
      "isActive": true
    }
  ],
  "errors": null,
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalItems": 3,
    "totalPages": 1
  }
}
```

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Category 'Tax' is invalid. Allowed values: Freight, StallClass, Surcharge, Discount, Clearance, Escort, Insurance.` | `category` sai giá trị |
| 400 | `TransportMode 'Sea' is invalid. Allowed values: Ground, Air.` | `transportMode` sai giá trị |
| 403 | `You do not have permission to perform this action.` | Người gọi không phải LogisticsManager / Admin |

### GET /api/price-items/{id} — Chi tiết dòng giá

- **Ai gọi được:** LogisticsManager, Admin

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã dòng giá (`priceItemId`) |

Response `200`: `data` là một dòng giá.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 404 | `Price item with ID 999 was not found.` | Không có dòng giá với mã đó |

### POST /api/price-items — Thêm dòng giá

Dòng mới luôn ở trạng thái đang bật (`isActive: true`).

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `category` | chuỗi | Có | Nhóm giá, xem bảng ở đầu mục |
| `itemCode` | chuỗi | Có | Chỉ gồm chữ, số, dấu gạch dưới; tối đa 30 ký tự; server tự đổi sang in hoa |
| `itemName` | chuỗi | Có | Tên hiển thị, tối đa 100 ký tự |
| `transportMode` | chuỗi | Không | `Ground`, `Air`, hoặc bỏ trống / `null` để áp dụng cả hai |
| `destCountryCode` | chuỗi | Không | Mã nước 2 chữ cái, hoặc bỏ trống / `null` để áp dụng mọi nước |
| `calcType` | chuỗi | Có | Cách tính, xem bảng ở đầu mục |
| `value` | số | Có | Không âm, tối đa 4 chữ số thập phân |
| `minHorses` | số | Không | Lớn hơn 0; chỉ cần cho nhóm `Discount` |

Ví dụ request:

```json
{
  "category": "Surcharge",
  "itemCode": "sur_holiday",
  "itemName": "Phụ phí ngày lễ",
  "transportMode": "Ground",
  "destCountryCode": null,
  "calcType": "Percent",
  "value": 10,
  "minHorses": null
}
```

Response `201`:

```json
{
  "success": true,
  "message": "Price item created successfully",
  "data": {
    "priceItemId": 37,
    "category": "Surcharge",
    "itemCode": "SUR_HOLIDAY",
    "itemName": "Phụ phí ngày lễ",
    "transportMode": "Ground",
    "destCountryCode": null,
    "calcType": "Percent",
    "value": 10,
    "minHorses": null,
    "isActive": true
  },
  "errors": null
}
```

(Ví dụ này chỉ để minh họa API; như lưu ý ở đầu mục, phụ phí `SUR_HOLIDAY` chưa được công thức tính giá dùng tới.)

Lỗi thường gặp:

| HTTP | `message` / `errors` | Khi nào |
|---|---|---|
| 400 | `A price item with code 'SUR_HOLIDAY', transport mode 'Ground' and destination country 'Any' already exists.` | Đã có dòng cùng mã + phương thức + nước đến |
| 400 | `Category 'Tax' is invalid. Allowed values: Freight, StallClass, Surcharge, Discount, Clearance, Escort, Insurance.` | `category` sai |
| 400 | `CalcType 'Each' is invalid. Allowed values: PerHorse, PerKmPerHorse, Multiplier, Percent, PerBooking, PerGroom, PerGroomDay.` | `calcType` sai |
| 400 | `TransportMode 'Sea' is invalid. Allowed values: Ground, Air.` | `transportMode` sai |
| 400 | `ItemCode may only contain letters, digits and underscores.` | `itemCode` có dấu cách, gạch ngang... |
| 400 | `Value must fit PostgreSQL numeric(18,4) and cannot be negative.` | `value` âm hoặc quá lớn |
| 400 | `MinHorses must be greater than zero when provided.` | `minHorses` ≤ 0 |

### PUT /api/price-items/{id} — Sửa dòng giá

Gửi lại **đủ** các trường như khi thêm. API này không đổi `isActive`; muốn bật / tắt thì dùng API ngay dưới.

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã dòng giá |

Ví dụ request:

```json
{
  "category": "Surcharge",
  "itemCode": "SUR_HOLIDAY",
  "itemName": "Phụ phí ngày lễ, Tết",
  "transportMode": "Ground",
  "destCountryCode": null,
  "calcType": "Percent",
  "value": 12.5,
  "minHorses": null
}
```

Response `200`: `data` là dòng giá sau khi sửa, `message` là `Price item updated successfully`.

Lỗi thường gặp: như khi thêm, cộng thêm:

| HTTP | `message` | Khi nào |
|---|---|---|
| 404 | `Price item with ID 999 was not found.` | Không có dòng giá với mã đó |

### PATCH /api/price-items/{id}/active — Bật hoặc tắt dòng giá

Dòng đã tắt vẫn hiện trong danh sách quản trị nhưng **không** được dùng khi tính giá.

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã dòng giá |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `isActive` | true / false | Có | `false` để tắt, `true` để bật lại |

Ví dụ request:

```json
{
  "isActive": false
}
```

Response `200`: `data` là dòng giá với `isActive` mới, `message` là `Price item active status updated successfully`.

Lỗi thường gặp:

| HTTP | `errors` | Khi nào |
|---|---|---|
| 400 | `IsActive is required.` | Gửi body rỗng `{}` |
| 404 | `Price item with ID 999 was not found.` | Không có dòng giá với mã đó |

```js
await api.patch(`/api/price-items/${item.priceItemId}/active`, { isActive: !item.isActive });
```

---

## 18. Danh mục giá trị cho dropdown

### GET /api/lookups/enums — Lấy mọi danh sách giá trị cố định

Trả về **trong một lần gọi** tất cả các giá trị cố định (enum) mà backend chấp nhận, để frontend đổ vào các ô chọn (dropdown) và bộ lọc thay vì tự gõ cứng trong code. Đây chính là các giá trị liệt kê ở mục 3. Danh sách không đổi khi chạy, nên chỉ cần gọi **một lần sau khi đăng nhập** rồi giữ lại dùng chung.

- **Ai gọi được:** Mọi người đã đăng nhập (cần `accessToken`)
- **Dữ liệu gửi lên:** không có

Các danh sách trong `data`:

| Trường | Dùng cho |
|---|---|
| `userRoles` | Vai trò người dùng (lọc danh sách người dùng, form tạo tài khoản của Admin) |
| `horseGenders` | Giới tính ngựa |
| `bookingStatuses` | Trạng thái đơn |
| `bookingHorseStatuses` | Trạng thái từng ngựa trong đơn |
| `transportModes` | Hình thức vận chuyển (đường bộ / hàng không) |
| `stallClasses` | Hạng chuồng |
| `dossierStatuses` | Trạng thái hồ sơ kiểm dịch |
| `documentStatuses` | Trạng thái giấy tờ đã nộp |
| `requirementDirections` | Chiều quy định (xuất / nhập khẩu) |
| `assetTypes` | Loại phương tiện |
| `assetStatuses` | Trạng thái phương tiện |
| `tripStatuses` | Trạng thái chuyến |
| `onTimeStatuses` | Kết quả đúng giờ khi đóng chuyến |
| `crewRoles` | Vai trò trong tổ lái |
| `checkpointTypes` | Loại điểm dừng |
| `checkpointStatuses` | Trạng thái điểm dừng |
| `tripHorseStatuses` | Trạng thái của từng con ngựa trên chuyến |
| `feedStatuses` | Tình trạng ăn uống của ngựa |
| `stressLevels` | Mức căng thẳng của ngựa |
| `incidentTypes` | Loại sự cố |
| `incidentSeverities` | Mức độ sự cố |
| `incidentStatuses` | Trạng thái sự cố |
| `handoverConditions` | Tình trạng ngựa lúc bàn giao |
| `priceCategories` | Nhóm dòng giá |
| `priceCalcTypes` | Cách tính dòng giá |
| `paymentTypes` | Loại khoản thanh toán |
| `paymentStatuses` | Trạng thái giao dịch |
| `paymentProviders` | Hình thức thanh toán |

Mỗi trường là một mảng chuỗi. Giá trị trong mảng là đúng chuỗi phải gửi lên API (phân biệt hoa thường); muốn hiện tiếng Việt cho người dùng thì frontend tự đổi tên hiển thị.

Response `200`:

```json
{
  "success": true,
  "message": "Enum lookups retrieved successfully",
  "data": {
    "userRoles": [
      "Customer",
      "LogisticsManager",
      "TransportSpecialist",
      "FleetCoordinator",
      "DriverEscort",
      "Admin"
    ],
    "horseGenders": [
      "Stallion",
      "Mare",
      "Gelding"
    ],
    "bookingStatuses": [
      "Submitted",
      "PreApproved",
      "RoutePlanned",
      "Quoted",
      "AwaitingDeposit",
      "Approved",
      "Assigned",
      "AwaitingFinalPayment",
      "Completed",
      "Rejected",
      "Expired",
      "QuoteExpired",
      "Cancelled"
    ],
    "bookingHorseStatuses": [
      "Pending",
      "Approved",
      "Cancelled"
    ],
    "transportModes": [
      "Ground",
      "Air"
    ],
    "stallClasses": [
      "Shared",
      "Comfort",
      "Private"
    ],
    "dossierStatuses": [
      "Draft",
      "AwaitingDocs",
      "Reviewing",
      "SubmittedToAuthorities",
      "Cleared",
      "Issue"
    ],
    "documentStatuses": [
      "Pending",
      "Approved",
      "Rejected"
    ],
    "requirementDirections": [
      "Export",
      "Import"
    ],
    "assetTypes": [
      "HorseTruck_AirSuspension",
      "HorseVan",
      "AirStall"
    ],
    "assetStatuses": [
      "Available",
      "InTransit",
      "Maintenance"
    ],
    "tripStatuses": [
      "Draft",
      "PendingApproval",
      "Scheduled",
      "InTransit",
      "EmergencyRerouting",
      "ArrivedDestination",
      "Completed",
      "Cancelled"
    ],
    "onTimeStatuses": [
      "OnTime",
      "Delayed_AcceptableForceMajeure",
      "Delayed_OperationalFault"
    ],
    "crewRoles": [
      "Driver",
      "Escort"
    ],
    "checkpointTypes": [
      "Origin",
      "PickUp",
      "RestStop",
      "BorderGate",
      "DropOff",
      "Destination"
    ],
    "checkpointStatuses": [
      "Pending",
      "Arrived",
      "Cleared",
      "Departed",
      "Cancelled"
    ],
    "tripHorseStatuses": [
      "Planned",
      "InTransit",
      "Delivered",
      "Offloaded",
      "Deceased",
      "Cancelled"
    ],
    "feedStatuses": [
      "Normal",
      "Reduced",
      "Refused"
    ],
    "stressLevels": [
      "Calm",
      "MildStress",
      "Agitated"
    ],
    "incidentTypes": [
      "MechanicalBreakdown",
      "BorderCongestion",
      "EquineHealthIssue",
      "Weather",
      "ClimateControlFailure",
      "CustomsHold",
      "Other"
    ],
    "incidentSeverities": [
      "Minor",
      "Moderate",
      "Major",
      "Critical"
    ],
    "incidentStatuses": [
      "Reported",
      "PlanProposed",
      "Approved",
      "Rejected",
      "Resolved"
    ],
    "handoverConditions": [
      "Excellent",
      "NormalFatigue",
      "Injured"
    ],
    "priceCategories": [
      "Freight",
      "StallClass",
      "Surcharge",
      "Discount",
      "Clearance",
      "Escort",
      "Insurance"
    ],
    "priceCalcTypes": [
      "PerHorse",
      "PerKmPerHorse",
      "Multiplier",
      "Percent",
      "PerBooking",
      "PerGroom",
      "PerGroomDay"
    ],
    "paymentTypes": [
      "Deposit",
      "Balance",
      "Extra",
      "Refund"
    ],
    "paymentStatuses": [
      "Pending",
      "Succeeded",
      "Failed",
      "Expired"
    ],
    "paymentProviders": [
      "VnPay",
      "Mock",
      "Manual"
    ]
  },
  "errors": null
}
```

Lỗi thường gặp:

| HTTP | Khi nào |
|---|---|
| 401 | Chưa đăng nhập hoặc `accessToken` hết hạn |

```js
import api from './api'; // file axios dùng chung ở mục 1

let cached = null;
export async function getLookups() {
  if (!cached) {
    const res = await api.get('/api/lookups/enums');
    cached = res.data.data;
  }
  return cached;
}

// Ví dụ đổ dropdown hạng chuồng:
// const { stallClasses } = await getLookups();
// stallClasses.map(v => <option key={v} value={v}>{v}</option>)
```

---

## 19. Thanh toán (đặt cọc, phần còn lại, phát sinh, hoàn tiền)

Báo giá của đơn tính bằng **USD**; khách trả bằng **VND** qua cổng **VNPay**. Số VND = số USD × tỉ giá trong cấu hình (làm tròn tới đồng). Tài xế không bao giờ thu tiền.

Đơn đi qua các khoản tiền sau:

| Khoản (`paymentType`) | Khi nào phải trả | Số tiền |
|---|---|---|
| `Deposit` đặt cọc | Ngay sau khi quản lý duyệt (đơn ở `AwaitingDeposit`) | `% cọc` × báo giá; % được **chốt lúc duyệt** |
| `Balance` phần còn lại | Khi đơn đã `Approved` / `Assigned`, hạn là trước ngày xuất phát của chuyến (cấu hình số ngày) | Báo giá − tiền cọc |
| `Extra` phát sinh | Sau khi đóng chuyến, nếu sự cố có chi phí được duyệt (đơn ở `AwaitingFinalPayment`) | Chi phí sự cố phân bổ cho đơn |
| `Refund` hoàn tiền | Tự tạo khi khách hủy đơn đã trả tiền | `% hoàn cọc` × tiền cọc + 100% phần còn lại đã trả |

Vòng đời đơn có thanh toán:

```
Submitted ─(duyệt)─> AwaitingDeposit ─(trả đủ cọc)─> Approved ─(xếp chuyến)─> Assigned ─(giao xong)─> Completed
                          │                              │                                    │
                          │                              │                                    └─(còn phát sinh chưa trả)─> AwaitingFinalPayment ─(trả xong)─> Completed
                          ├─(quá hạn không cọc)─> Expired
                          └─(khách hủy)─> Cancelled   (khách hủy khi Approved: tự hoàn tiền)
```

Quy tắc cần biết khi làm giao diện:

- Cấu hình `% cọc = 0` thì duyệt là `Approved` luôn, không có bước cọc. `% cọc = 100` là thu đủ ngay khi duyệt (khi đó không còn khoản `Balance`).
- Hồ sơ kiểm dịch chỉ được mở **sau khi đã cọc** (lúc chuyển sang `Approved`).
- Chuyến **không xuất phát được** nếu có đơn trên chuyến chưa trả đủ cọc + phần còn lại (`POST /api/trips/{id}/start` trả 400).
- Khoản phát sinh quá hạn: hệ thống nhắc khách ở ngày đến hạn, +3 và +7 ngày; trong lúc còn nợ quá hạn khách **không tạo được đơn mới**.
- Đơn tạo trước khi có tính năng này (`paymentRequired = false`) không bắt thanh toán cọc / phần còn lại.
- Luôn để server quyết định số tiền: frontend chỉ gửi `paymentType`, không gửi số tiền.

Luồng thanh toán online trên frontend:

1. Gọi `GET /api/bookings/{id}/payments`. Nếu `nextPaymentType` khác `null` thì hiện nút "Thanh toán" với `nextPaymentAmount` (USD) và `nextPaymentAmountVnd`.
2. Khi khách bấm: `POST /api/bookings/{id}/payments` với `{ "paymentType": nextPaymentType }`, rồi chuyển trình duyệt sang `paymentUrl` (`window.location.href = paymentUrl`).
3. Khách trả trên trang VNPay. VNPay gọi server (IPN) và đưa khách về server; server chuyển tiếp khách về trang frontend `/customer/payments/result?status=success|failed|invalid&code=PAY-...&bookingId=...`.
4. Trang kết quả chỉ để hiển thị; trạng thái thật đọc lại bằng `GET /api/bookings/{id}/payments` hoặc `GET /api/bookings/{id}`.

Thẻ test VNPay sandbox: ngân hàng **NCB**, số thẻ `9704198526191432198`, tên `NGUYEN VAN A`, ngày phát hành `07/15`, OTP `123456`.

Khi backend chạy với `Payment:Provider = Mock` (demo không cần VNPay), `paymentUrl` trỏ về trang frontend `/customer/payments/mock?txnRef=...&amountVnd=...`; trang này gọi `POST /api/payments/mock/{txnRef}/complete` để giả lập kết quả.

### GET /api/bookings/{id}/payments — Tóm tắt thanh toán của đơn

Số tiền từng khoản, đã trả bao nhiêu, khoản cần trả tiếp theo và toàn bộ lịch sử giao dịch (gồm cả hoàn tiền). Cùng dữ liệu này cũng có trong trường `payment` của `GET /api/bookings/{id}`.

- **Ai gọi được:** Customer (chủ đơn), LogisticsManager, TransportSpecialist, FleetCoordinator, Admin
- **Dữ liệu gửi lên:** không có

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã đơn (`bookingId`) |

Response `200` (đơn đã cọc rồi bị khách hủy, được hoàn 50% cọc):

```json
{
  "success": true,
  "message": "Payments retrieved successfully",
  "data": {
    "paymentRequired": true,
    "currencyCode": "USD",
    "usdVndRate": 26000,
    "estimatedCost": 2948.0,
    "depositPercent": 30.0,
    "depositAmount": 884.4,
    "depositPaid": 884.4,
    "balanceAmount": 2063.6,
    "balancePaid": 0,
    "balanceDueAt": null,
    "extraAmount": 0.0,
    "extraPaid": 0,
    "extraDueAt": null,
    "extraOverdue": false,
    "refundedAmount": 442.2,
    "totalDue": 2948.0,
    "totalPaid": 884.4,
    "outstanding": 0,
    "nextPaymentType": null,
    "nextPaymentAmount": null,
    "nextPaymentAmountVnd": null,
    "payments": [
      {
        "paymentId": 12,
        "paymentCode": "PAY-2026-0012",
        "bookingId": 6,
        "bookingCode": "BKG-2026-0006",
        "paymentType": "Refund",
        "provider": "Mock",
        "status": "Succeeded",
        "amount": 442.2,
        "currencyCode": "USD",
        "exchangeRate": 26000.0,
        "amountVnd": 11497200,
        "providerTxnNo": "MOCK-PAY20260012",
        "bankCode": null,
        "responseCode": "00",
        "refundOfPaymentId": 11,
        "note": "Refund 50% of PAY-2026-0011 (deposit) after cancellation",
        "createdByName": "Trần Ngọc Anh",
        "createdAt": "2026-10-18T08:22:24.287456+00:00",
        "paidAt": "2026-10-18T08:22:24.291111+00:00"
      },
      {
        "paymentId": 11,
        "paymentCode": "PAY-2026-0011",
        "bookingId": 6,
        "bookingCode": "BKG-2026-0006",
        "paymentType": "Deposit",
        "provider": "Mock",
        "status": "Succeeded",
        "amount": 884.4,
        "currencyCode": "USD",
        "exchangeRate": 26000.0,
        "amountVnd": 22994400,
        "providerTxnNo": "MOCK20261018082134",
        "bankCode": null,
        "responseCode": "00",
        "refundOfPaymentId": null,
        "note": null,
        "createdByName": "Trần Ngọc Anh",
        "createdAt": "2026-10-18T08:21:28.619495+00:00",
        "paidAt": "2026-10-18T08:21:34.116482+00:00"
      }
    ]
  },
  "errors": null
}
```

Ý nghĩa các trường:

| Trường | Ý nghĩa |
|---|---|
| `paymentRequired` | `false` với đơn chưa duyệt hoặc đơn tạo trước khi có thanh toán online |
| `usdVndRate` | Tỉ giá hiện tại trong cấu hình |
| `depositPercent`, `depositAmount` | % cọc đã chốt lúc duyệt và số tiền cọc (USD) |
| `depositPaid`, `balancePaid`, `extraPaid` | Đã trả thành công cho từng khoản (USD) |
| `balanceAmount` | Báo giá − tiền cọc |
| `balanceDueAt` | Hạn trả phần còn lại; chỉ có khi ngựa của đơn đã được xếp chuyến |
| `extraAmount`, `extraDueAt`, `extraOverdue` | Chi phí phát sinh sau khi đóng chuyến, hạn trả, và đã quá hạn hay chưa |
| `refundedAmount` | Tổng đã hoàn cho khách |
| `totalDue`, `totalPaid`, `outstanding` | Tổng phải trả, tổng đã trả, còn thiếu (đơn đã hủy / từ chối / hết hạn thì `outstanding = 0`) |
| `nextPaymentType`, `nextPaymentAmount`, `nextPaymentAmountVnd` | Khoản khách trả được **ngay bây giờ**; `null` nếu không có gì để trả |
| `payments[]` | Lịch sử giao dịch, mới nhất trước |
| `payments[].status` | `Pending` đang chờ cổng · `Succeeded` thành công · `Failed` thất bại · `Expired` link cũ bị thay bằng link mới |
| `payments[].provider` | `VnPay` · `Mock` cổng giả lập · `Manual` ghi nhận tay (ngoài hệ thống) |
| `payments[].amountVnd`, `exchangeRate` | Số VND thực trả và tỉ giá tại thời điểm tạo giao dịch |
| `payments[].refundOfPaymentId` | Với dòng `Refund`: giao dịch gốc được hoàn |
| `payments[].providerTxnNo` | Mã giao dịch phía cổng / mã chuyển khoản |
| `payments[].bookingCode` | Mã đơn của giao dịch |

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 403 | `You do not have access to this booking.` | Khách xem đơn của khách khác |
| 403 | `Drivers and escorts do not see booking payments.` | Tài xế / áp tải gọi |
| 404 | `Booking with ID 99 was not found.` | Không có đơn |

### GET /api/payments/my — Trang Thanh toán của khách

Dữ liệu cho trang "Thanh toán" của khách hàng (menu Thanh toán). Một lần gọi trả về:

- các con số tổng: đã trả, đã được hoàn, còn phải trả, khoản trả được ngay;
- danh sách **đơn có liên quan đến tiền** của khách (đơn đã duyệt có cọc, đơn có phát sinh hoặc đã có giao dịch), mỗi đơn kèm tóm tắt thanh toán giống `GET /api/bookings/{id}/payments`;
- **toàn bộ giao dịch** của khách trên mọi đơn, mới nhất trước.

Đơn còn `Submitted` (chưa duyệt) và đơn cũ không có thanh toán sẽ không xuất hiện.

- **Ai gọi được:** Customer (chỉ thấy đơn của chính mình)
- **Dữ liệu gửi lên:** không có, không có tham số

Response `200` (rút gọn: khách có 4 đơn, ở đây chỉ giữ 1 đơn và 1 giao dịch):

```json
{
  "success": true,
  "message": "Billing retrieved successfully",
  "data": {
    "currencyCode": "USD",
    "usdVndRate": 26000,
    "totalPaid": 13900.0,
    "totalRefunded": 750.0,
    "totalOutstanding": 10500.0,
    "totalOutstandingVnd": 273000000,
    "totalPayableNow": 10500.0,
    "totalPayableNowVnd": 273000000,
    "payableNowCount": 2,
    "overdueCount": 0,
    "bookings": [
      {
        "bookingId": 101,
        "bookingCode": "BKG-2026-0101",
        "status": "Approved",
        "pickupCountryCode": "VN",
        "dropoffCountryCode": "TH",
        "pickupAddress": "Ba Vì, Hà Nội",
        "dropoffAddress": "Bangkok",
        "totalHorses": 1,
        "createdAt": "2026-10-17T02:00:00+00:00",
        "summary": {
          "paymentRequired": true,
          "currencyCode": "USD",
          "usdVndRate": 26000,
          "estimatedCost": 6000.0,
          "depositPercent": 30.0,
          "depositAmount": 1800.0,
          "depositPaid": 1800.0,
          "balanceAmount": 4200.0,
          "balancePaid": 0,
          "balanceDueAt": null,
          "extraAmount": 0.0,
          "extraPaid": 0,
          "extraDueAt": null,
          "extraOverdue": false,
          "refundedAmount": 0,
          "totalDue": 6000.0,
          "totalPaid": 1800.0,
          "outstanding": 4200.0,
          "nextPaymentType": "Balance",
          "nextPaymentAmount": 4200.0,
          "nextPaymentAmountVnd": 109200000,
          "payments": []
        }
      }
    ],
    "payments": [
      {
        "paymentId": 5,
        "paymentCode": "PAY-2026-0005",
        "bookingId": 101,
        "bookingCode": "BKG-2026-0101",
        "paymentType": "Deposit",
        "provider": "Mock",
        "status": "Succeeded",
        "amount": 1800.0,
        "currencyCode": "USD",
        "exchangeRate": 26000.0,
        "amountVnd": 46800000,
        "providerTxnNo": "MOCK20261018084618",
        "bankCode": null,
        "responseCode": "00",
        "refundOfPaymentId": null,
        "note": null,
        "createdByName": "Jane Smith",
        "createdAt": "2026-10-18T08:46:04.929332+00:00",
        "paidAt": "2026-10-18T08:46:18.67219+00:00"
      }
    ]
  },
  "errors": null
}
```

Ý nghĩa các trường:

| Trường | Ý nghĩa |
|---|---|
| `currencyCode`, `usdVndRate` | Đơn vị tiền của báo giá (USD) và tỉ giá hiện tại trong cấu hình |
| `totalPaid` | Tổng khách đã trả thành công (cọc + phần còn lại + phát sinh) trên mọi đơn, USD |
| `totalRefunded` | Tổng đã được hoàn (khi hủy đơn), USD |
| `totalOutstanding`, `totalOutstandingVnd` | Tổng còn phải trả của các đơn đang mở, **gồm cả phần còn lại chưa đến hạn**. Đơn đã hủy / từ chối / hết hạn không tính |
| `totalPayableNow`, `totalPayableNowVnd` | Tổng các khoản khách **trả được ngay bây giờ** (cộng `nextPaymentAmount` của từng đơn) |
| `payableNowCount` | Số đơn đang có khoản trả được ngay |
| `overdueCount` | Số đơn có chi phí phát sinh đã quá hạn. Lớn hơn 0 thì khách đang bị chặn tạo đơn mới, nên hiện cảnh báo |
| `bookings[]` | Các đơn, mới tạo trước |
| `bookings[].summary` | Tóm tắt thanh toán của đơn, các trường giống `data` của `GET /api/bookings/{id}/payments`; riêng `summary.payments` luôn là mảng rỗng (giao dịch nằm ở `payments[]` bên dưới) |
| `payments[]` | Mọi giao dịch của khách, mới nhất trước. Các trường giống `payments[]` của `GET /api/bookings/{id}/payments`, có thêm `bookingCode` để hiện mã đơn |

Cách dùng trên giao diện:

1. Bốn ô thống kê lấy từ `totalPayableNow`, `totalOutstanding`, `totalPaid`, `totalRefunded`.
2. Bảng "Khoản có thể thanh toán": lọc `bookings` có `summary.nextPaymentType` khác `null`. Nút "Thanh toán" làm giống trang chi tiết đơn: `POST /api/bookings/{bookingId}/payments` với `{ "paymentType": summary.nextPaymentType }` rồi chuyển sang `paymentUrl`.
3. Hạn trả: `summary.balanceDueAt` cho khoản `Balance` (chỉ có khi đã xếp chuyến), `summary.extraDueAt` cho khoản `Extra`.
4. Bảng lịch sử giao dịch: hiển thị `payments`, lọc theo `paymentType` / `status` ngay trên frontend.

```js
const res = await api.get('/api/payments/my');
const billing = res.data.data;
const payable = billing.bookings.filter((b) => b.summary.nextPaymentType);

async function pay(booking) {
  const r = await api.post(`/api/bookings/${booking.bookingId}/payments`, {
    paymentType: booking.summary.nextPaymentType,
  });
  window.location.href = r.data.data.paymentUrl;
}
```

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 401 | (không có body) | Chưa đăng nhập hoặc token hết hạn |
| 403 | `You do not have permission to perform this action.` | Tài khoản không phải Customer (nhân viên xem thanh toán theo từng đơn bằng `GET /api/bookings/{id}/payments`) |

### POST /api/bookings/{id}/payments — Tạo link thanh toán

Tạo một giao dịch cho khoản đang mở của đơn và trả về đường dẫn sang cổng thanh toán. Link cũ chưa trả của cùng khoản sẽ chuyển `Expired`, nên khách bấm nhiều lần cũng không bị trả trùng. Link VNPay có hiệu lực 15 phút.

- **Ai gọi được:** Customer (chủ đơn)
- **Dữ liệu gửi lên:** JSON

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `paymentType` | chuỗi | Có | Phải đúng bằng `nextPaymentType` của tóm tắt: `Deposit`, `Balance` hoặc `Extra` |

Ví dụ request:

```json
{
  "paymentType": "Balance"
}
```

Response `201`:

```json
{
  "success": true,
  "message": "Payment created; open paymentUrl to pay",
  "data": {
    "payment": {
      "paymentId": 14,
      "paymentCode": "PAY-2026-0014",
      "bookingId": 7,
      "bookingCode": "BKG-2026-0007",
      "paymentType": "Balance",
      "provider": "Mock",
      "status": "Pending",
      "amount": 2063.6,
      "currencyCode": "USD",
      "exchangeRate": 26000,
      "amountVnd": 53653600,
      "providerTxnNo": null,
      "bankCode": null,
      "responseCode": null,
      "refundOfPaymentId": null,
      "note": null,
      "createdByName": null,
      "createdAt": "2026-10-18T08:24:27.1393283+00:00",
      "paidAt": null
    },
    "paymentUrl": "http://localhost:3001/customer/payments/mock?txnRef=PAY20260014&amountVnd=53653600"
  },
  "errors": null
}
```

Khi chạy VNPay thật, `paymentUrl` có dạng `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount=...&vnp_SecureHash=...`.

```js
const res = await api.post(`/api/bookings/${bookingId}/payments`, { paymentType: summary.nextPaymentType });
window.location.href = res.data.data.paymentUrl;
```

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Booking BKG-2026-0007 is waiting for the Balance payment, not Extra.` | Gửi sai khoản |
| 400 | `Booking BKG-2026-0006 has nothing to pay right now (status: Cancelled).` | Không có khoản nào đang mở |
| 400 | `PaymentType 'Abc' is invalid. Allowed values: Deposit, Balance, Extra.` | Giá trị `paymentType` không hợp lệ |
| 400 | `VNPay is not configured on the server (VnPay:TmnCode / VnPay:HashSecret).` | Server chưa cấu hình khóa VNPay |
| 403 | `You can only pay for your own bookings.` | Trả cho đơn của người khác |

### GET /api/payments/vnpay-return — VNPay đưa khách về

Frontend **không gọi** API này. Đây là `vnp_ReturnUrl`: sau khi khách trả, VNPay chuyển trình duyệt về đây kèm các tham số `vnp_*`. Server kiểm tra chữ ký, cập nhật giao dịch rồi chuyển hướng (HTTP 302) tới:

```
{FrontendBaseUrl}/customer/payments/result?status=success|failed|invalid&code=PAY-2026-0011&bookingId=6
```

| `status` | Ý nghĩa |
|---|---|
| `success` | Thanh toán thành công |
| `failed` | Khách hủy hoặc ngân hàng từ chối |
| `invalid` | Chữ ký sai hoặc không tìm thấy giao dịch |

- **Ai gọi được:** không cần đăng nhập (VNPay gọi)

### GET /api/payments/vnpay-ipn — VNPay báo kết quả cho server

Frontend **không gọi** API này. VNPay gọi trực tiếp server-to-server để báo kết quả (kể cả khi khách đóng trình duyệt giữa chừng). Địa chỉ này phải được đăng ký với VNPay: `https://racehorsetransportbackend-production.up.railway.app/api/payments/vnpay-ipn`.

- **Ai gọi được:** không cần đăng nhập (VNPay gọi)
- Response là JSON theo chuẩn VNPay (không bọc trong khuôn `success/data`): `{ "RspCode": "00", "Message": "Confirm Success" }`. Các mã khác: `01` không thấy giao dịch, `02` giao dịch đã xử lý rồi, `04` sai số tiền, `97` sai chữ ký, `99` lỗi khác.

### POST /api/payments/mock/{txnRef}/complete — Hoàn tất giao dịch giả lập

Chỉ dùng khi backend chạy `Payment:Provider = Mock`. Trang `/customer/payments/mock` gọi API này khi khách bấm "Thanh toán thành công" hoặc "Giả lập thất bại".

- **Ai gọi được:** Customer (chủ giao dịch)
- **Dữ liệu gửi lên:** JSON

| Tham số / trường | Mô tả |
|---|---|
| `txnRef` (đường dẫn) | Lấy từ query `txnRef` của `paymentUrl` |
| `success` (body, true/false) | Kết quả muốn giả lập; mặc định `true` |

Response `200`: `data` gồm `success` (true/false), `message`, `payment` (giao dịch sau khi cập nhật). Frontend chuyển sang `/customer/payments/result?status=...&code=...&bookingId=...`.

| HTTP | `message` | Khi nào |
|---|---|---|
| 403 | `You can only complete your own payments.` | Giao dịch của người khác |
| 404 | `Mock payment PAY20260099 was not found.` | Sai `txnRef`, hoặc giao dịch không phải của cổng giả lập |

### POST /api/bookings/{id}/payments/manual — Ghi nhận khoản trả ngoài hệ thống

Quản lý ghi nhận tiền khách đã chuyển khoản / trả tại văn phòng. Có tác dụng y như thanh toán online: ghi đủ cọc thì đơn chuyển `Approved` và mở hồ sơ; ghi đủ phát sinh thì đơn `Completed`.

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `paymentType` | chuỗi | Có | `Deposit`, `Balance` hoặc `Extra` |
| `amount` | số (USD) | Có | > 0 và không vượt số còn thiếu của khoản đó; được phép trả một phần |
| `reference` | chuỗi | Có | Mã chuyển khoản / số biên lai, tối đa 100 ký tự |
| `note` | chuỗi | Không | Tối đa 300 ký tự |

Ví dụ request:

```json
{
  "paymentType": "Deposit",
  "amount": 884.4,
  "reference": "FT26291123456",
  "note": "Khách chuyển khoản Vietcombank"
}
```

Response `201`: `data` là giao dịch vừa tạo (`provider` = `Manual`, `status` = `Succeeded`).

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Amount 1,000.00 is more than the outstanding Deposit amount 884.40 USD.` | Ghi quá số còn thiếu |
| 400 | `Nothing is outstanding for the Balance payment of booking BKG-2026-0007.` | Khoản đó đã trả đủ |
| 400 | `Booking BKG-2026-0006 is Cancelled; no payment can be recorded.` | Đơn đã hủy / từ chối / hết hạn |

### POST /api/payments/{id}/retry-refund — Thử hoàn tiền lại qua cổng

Khi khách hủy đơn, server tự gọi API hoàn tiền của VNPay. Nếu thất bại, dòng `Refund` ở trạng thái `Failed` và quản lý nhận thông báo; dùng API này để thử lại.

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** không có. `id` là `paymentId` của dòng `Refund`.

Response `200`: `data` là dòng hoàn tiền sau khi thử (`status` `Succeeded` hoặc vẫn `Failed`).

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `This refund was paid outside the system; refund it manually and mark it as refunded.` | Tiền gốc trả ngoài hệ thống (`Manual`): phải hoàn tay |
| 400 | `Refund PAY-2026-0012 has already been completed.` | Đã hoàn xong |
| 404 | `Refund with ID 99 was not found.` | Sai mã, hoặc không phải dòng hoàn tiền |

### POST /api/payments/{id}/mark-refunded — Đánh dấu đã hoàn tiền thủ công

Quản lý đã chuyển trả tiền cho khách ngoài hệ thống (bắt buộc với khoản gốc trả tay). Khách nhận thông báo + email.

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `reference` | chuỗi | Có | Mã chuyển khoản hoàn, tối đa 50 ký tự |
| `note` | chuỗi | Không | Tối đa 300 ký tự |

Response `200`: `data` là dòng hoàn tiền với `status` = `Succeeded`. Lỗi giống `retry-refund` (trừ lỗi `Manual`).

### GET /api/settings/payment — Xem cấu hình thanh toán

- **Ai gọi được:** Mọi người đã đăng nhập (khách dùng để hiện % cọc / % hoàn khi hủy)
- **Dữ liệu gửi lên:** không có

Response `200`:

```json
{
  "success": true,
  "message": "Request processed successfully",
  "data": {
    "depositPercent": 30,
    "cancelRefundPercent": 50,
    "usdVndRate": 26000,
    "balanceDueDaysBeforeDeparture": 2,
    "extraPaymentDueDays": 7,
    "updatedAt": "2026-10-18T08:08:56.354544+00:00",
    "updatedByName": "Trần Quốc Bảo"
  },
  "errors": null
}
```

| Trường | Ý nghĩa |
|---|---|
| `depositPercent` | % cọc áp dụng cho đơn được duyệt từ bây giờ (0–100) |
| `cancelRefundPercent` | % tiền cọc được hoàn khi khách hủy (phần còn lại đã trả luôn hoàn 100%) |
| `usdVndRate` | Tỉ giá quy đổi USD → VND khi thanh toán |
| `balanceDueDaysBeforeDeparture` | Phần còn lại phải trả trước ngày xuất phát bao nhiêu ngày |
| `extraPaymentDueDays` | Phát sinh phải trả trong bao nhiêu ngày sau khi đóng chuyến |

### PUT /api/settings/payment — Sửa cấu hình thanh toán

Giá trị mới chỉ áp dụng cho đơn duyệt sau đó; đơn đã duyệt giữ % cọc đã chốt.

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON, gửi đủ cả 5 trường

| Trường | Kiểu | Giới hạn |
|---|---|---|
| `depositPercent` | số | 0 – 100 |
| `cancelRefundPercent` | số | 0 – 100 |
| `usdVndRate` | số | 1 – 1.000.000 |
| `balanceDueDaysBeforeDeparture` | số nguyên | 0 – 30 |
| `extraPaymentDueDays` | số nguyên | 1 – 90 |

Ví dụ request:

```json
{
  "depositPercent": 30,
  "cancelRefundPercent": 50,
  "usdVndRate": 26000,
  "balanceDueDaysBeforeDeparture": 2,
  "extraPaymentDueDays": 7
}
```

Response `200`: `data` là cấu hình sau khi lưu (dạng giống GET).

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Validation failed` (kèm `errors`, ví dụ `DepositPercent must be between 0 and 100.`) | Giá trị ngoài giới hạn |
| 403 | | Không phải quản lý / Admin |

---

## 19b. Bồi thường giao trễ

Chính sách mục 7: giao trễ quá `Delay.GraceHours` (6) giờ so với hạn giao **do lỗi công ty** thì công ty hoàn `Delay.PercentPer12Hours` (5)% **tiền cước** cho mỗi 12 giờ trễ (chưa đủ 12 giờ tính tròn lên), tối đa `Delay.MaxPercent` (25)%.

- **Giờ trễ** = lúc ký e-POD của đơn − hạn giao của đơn.
- **Tiền cước** = báo giá − bảo hiểm (`INS_*`) − phí thủ tục (`FEE_DOCS`, `FEE_CLEARANCE`, `FEE_QUARANTINE`, `FEE_TRANSIT`). Đơn cũ không có chi tiết báo giá thì lấy cả tổng báo giá.
- **Do lỗi công ty**: chuyến không có sự cố nào được chấp nhận, hoặc có ít nhất một sự cố có `faultParty = Company`.

Ví dụ: tiền cước 2.528,80 USD, trễ 20 giờ → (20 − 6) / 12 = 1,17 → tròn lên 2 bậc → 10% → 252,88 USD.

Luồng: Manager đóng chuyến → hệ thống tạo khoản bồi thường `AwaitingApproval` và báo các Manager → Manager **duyệt** (có thể sửa số tiền kèm lý do) hoặc **từ chối** → khi duyệt, tiền được hoàn vào phương thức thanh toán của khách: hệ thống tạo các dòng `Refund` (xem `GET /api/bookings/{id}/payments`), hoàn qua VNPay; khoản khách trả ngoài hệ thống thì Manager hoàn tay rồi bấm `mark-refunded`. Khách nhận thông báo + email.

Một khoản bồi thường (`delayCompensation`) có dạng:

```json
{
  "delayCompensationId": 1,
  "bookingId": 100,
  "bookingCode": "BKG-2026-0012",
  "customerName": "Hoàng Minh Châu",
  "tripId": 100,
  "tripCode": "TRP-2026-0009",
  "delayHours": 20.0,
  "freightAmount": 7500.0,
  "percent": 10.0,
  "calculatedAmount": 750.0,
  "approvedAmount": 150.0,
  "currencyCode": "USD",
  "status": "Approved",
  "reviewReason": "Một phần thời gian trễ do kẹt biên",
  "reviewedByName": "Trần Quốc Bảo",
  "reviewedAt": "2026-10-27T07:42:11.120+00:00",
  "createdAt": "2026-10-27T07:41:58.004+00:00"
}
```

| Trường | Ý nghĩa |
|---|---|
| `delayHours` | Số giờ ký e-POD sau hạn giao |
| `freightAmount` | Tiền cước dùng để tính |
| `percent`, `calculatedAmount` | % và số tiền hệ thống tính |
| `approvedAmount` | Số tiền Manager duyệt (`null` khi chưa duyệt / bị từ chối) |
| `status` | `AwaitingApproval` chờ duyệt · `Approved` đã duyệt (tiền đang / đã hoàn) · `Rejected` từ chối |
| `reviewReason` | Lý do khi sửa số tiền hoặc từ chối |

### GET /api/delay-compensations — Danh sách bồi thường giao trễ (Manager)

- **Ai gọi được:** LogisticsManager, Admin

| Tham số query | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `status` | chuỗi | Không | `AwaitingApproval`, `Approved` hoặc `Rejected`; bỏ trống = tất cả |

Ví dụ: `GET /api/delay-compensations?status=AwaitingApproval`

Response `200`: `data` là mảng bồi thường (dạng như trên), mới nhất trước.

### GET /api/bookings/{bookingId}/delay-compensations — Bồi thường của một đơn

Khách xem trên trang đơn: bị trễ bao lâu, được bồi thường bao nhiêu, trạng thái.

- **Ai gọi được:** Khách của đơn, nhân sự xem được đơn

Response `200`: `data` là mảng bồi thường của đơn (thường 0 hoặc 1 phần tử).

| HTTP | Khi nào |
|---|---|
| 403 | Đơn không phải của mình |
| 404 | Không có đơn |

### POST /api/delay-compensations/{id}/approve — Duyệt bồi thường

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `amount` | số | Có | Số tiền hoàn, lớn hơn 0. Thường gửi lại `calculatedAmount` |
| `reason` | chuỗi | Có khi `amount` khác `calculatedAmount` | Lý do sửa số tiền, tối đa 500 ký tự |

Ví dụ request:

```json
{
  "amount": 150,
  "reason": "Một phần thời gian trễ do kẹt biên"
}
```

Response `200`: khoản bồi thường với `status` = `Approved`. Các dòng hoàn tiền xem ở `GET /api/bookings/{id}/payments`.

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Give a reason when the amount differs from the calculated 750.00 USD.` | Sửa số tiền mà không có lý do |
| 400 | `Booking BKG-2026-0012 has only 2,000.00 USD that can still be refunded.` | Số tiền vượt quá số khách đã trả mà chưa hoàn |
| 400 | `This delay compensation has already been reviewed (status: Approved).` | Đã duyệt / từ chối rồi |
| 404 | `Delay compensation with ID 99 was not found.` | Không có |

### POST /api/delay-compensations/{id}/reject — Từ chối bồi thường

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON `{ "reason": "..." }` (bắt buộc, tối đa 500 ký tự)

Response `200`: khoản bồi thường với `status` = `Rejected`, `reviewReason` là lý do.

---

## 20. Chính sách dịch vụ và cấu hình

Trang **Chính sách dịch vụ** cho khách xem các quy tắc: thanh toán, hủy đơn, giao trễ, bảo hiểm, điều kiện ngựa, giao nhận. Văn bản chính sách nằm ở `docs/CHINH_SACH_DICH_VU.md`; các con số trong ngoặc vuông của văn bản (ví dụ `[Delay.GraceHours = 6]`) là **giá trị cấu hình**, frontend phải đọc từ API dưới đây thay vì viết cứng. Quản lý sửa được các giá trị này; trang Chính sách tự hiện đúng giá trị mới.

Khi khách gửi yêu cầu vận chuyển và khi chấp nhận báo giá, khách phải tích "Tôi đã đọc và đồng ý với Chính sách dịch vụ"; frontend gửi kèm `acceptPolicy = true` và `policyVersion` lấy từ `GET /api/policy` (`POST /api/bookings` và `POST /api/bookings/{id}/accept-quote`). Khi quản lý đổi phiên bản chính sách, server từ chối phiên bản cũ: frontend tải lại chính sách và cho khách tích lại.

### GET /api/policy — Chính sách dịch vụ (công khai)

Phiên bản chính sách hiện hành và mọi giá trị cấu hình mà chính sách nhắc tới, kể cả các khóa thanh toán `Payment.*`. **Không cần đăng nhập**, dùng cho trang Chính sách, ô đồng ý chính sách và bảng so sánh gói bảo hiểm.

- **Ai gọi được:** Mọi người, không cần đăng nhập
- **Dữ liệu gửi lên:** không có

Response `200`:

```json
{
  "success": true,
  "message": "Request processed successfully",
  "data": {
    "version": "1.0",
    "updatedAt": "2026-10-27T07:05:09.094261+00:00",
    "payment": {
      "depositPercent": 30,
      "cancelRefundPercent": 50,
      "usdVndRate": 26000,
      "balanceDueDaysBeforeDeparture": 2,
      "extraPaymentDueDays": 7
    },
    "booking": { "quoteAcceptDays": 3, "dossierSubmitDays": 7 },
    "delay": { "graceHours": 6, "percentPer12Hours": 5, "maxPercent": 25 },
    "insurance": {
      "premiumPercent": 1,
      "baseLiabilityUsd": 5000,
      "baseVetCapUsd": 1000,
      "vetCapPercent": 20,
      "proofThresholdUsd": 50000
    },
    "horse": { "minAgeMonths": 6, "gestationDays": 340, "latePregnancyPercent": 90, "postFoalingDays": 7 },
    "delivery": { "freeWaitingHours": 2, "waitingFeePerHourUsd": 40, "maxWaitingHours": 24, "decisionHours": 24 },
    "values": {
      "Policy.Version": "1.0",
      "Payment.DepositPercent": 30,
      "Payment.CancelRefundPercent": 50,
      "Payment.UsdVndRate": 26000,
      "Payment.BalanceDueDaysBeforeDeparture": 2,
      "Payment.ExtraPaymentDueDays": 7,
      "Booking.QuoteAcceptDays": 3,
      "Dossier.SubmitDays": 7,
      "Delay.GraceHours": 6,
      "Delay.PercentPer12Hours": 5,
      "Delay.MaxPercent": 25,
      "Insurance.PremiumPercent": 1,
      "Insurance.BaseLiabilityUsd": 5000,
      "Insurance.BaseVetCapUsd": 1000,
      "Insurance.VetCapPercent": 20,
      "Insurance.ProofThresholdUsd": 50000,
      "Horse.MinAgeMonths": 6,
      "Horse.GestationDays": 340,
      "Horse.LatePregnancyPercent": 90,
      "Horse.PostFoalingDays": 7,
      "Delivery.FreeWaitingHours": 2,
      "Delivery.WaitingFeePerHourUsd": 40,
      "Delivery.MaxWaitingHours": 24,
      "Delivery.DecisionHours": 24
    }
  },
  "errors": null
}
```

| Trường | Ý nghĩa |
|---|---|
| `version` | Phiên bản chính sách hiện hành. Gửi kèm khi khách đồng ý chính sách (`policyVersion`) |
| `updatedAt` | Lần sửa cấu hình gần nhất (thanh toán hoặc chính sách) |
| `payment` | % cọc, % hoàn cọc khi hủy, tỉ giá USD → VND, hạn trả phần còn lại, hạn trả phát sinh (giống `GET /api/settings/payment`) |
| `booking.quoteAcceptDays` | Số ngày khách có để chấp nhận báo giá chốt và đặt cọc |
| `booking.dossierSubmitDays` | Số ngày khách có để nộp đủ giấy tờ, tính từ lúc đặt cọc |
| `delay.graceHours` | Số giờ trễ được miễn bồi thường |
| `delay.percentPer12Hours`, `delay.maxPercent` | % tiền cước bồi thường cho mỗi 12 giờ trễ sau ân hạn, và mức tối đa |
| `insurance.premiumPercent` | % phí bảo hiểm toàn giá trị trên giá trị thỏa thuận mỗi con |
| `insurance.baseLiabilityUsd`, `baseVetCapUsd` | Không mua bảo hiểm: trách nhiệm tối đa mỗi con khi ngựa chết / mất, và chi phí thú y tối đa khi ngựa bị thương (USD) |
| `insurance.vetCapPercent` | Có bảo hiểm: chi phí thú y tối đa, % giá trị thỏa thuận |
| `insurance.proofThresholdUsd` | Giá trị khai báo mỗi con từ mức này phải kèm chứng từ |
| `horse.minAgeMonths` | Tuổi tối thiểu (tháng) của ngựa |
| `horse.gestationDays`, `latePregnancyPercent` | Thai kỳ trung bình (ngày) và ngưỡng: không chở ngựa cái đã quá `latePregnancyPercent`% thai kỳ (mặc định 340 × 10% = 34 ngày trước ngày dự sinh) |
| `horse.postFoalingDays` | Không chở ngựa cái vừa đẻ trong số ngày này |
| `delivery.freeWaitingHours`, `waitingFeePerHourUsd`, `maxWaitingHours` | Phí chờ tại điểm giao: số giờ miễn phí, phí mỗi giờ (USD), số giờ tính phí tối đa |
| `delivery.decisionHours` | Số giờ khách có để chọn cách xử lý khi không giao được |
| `values` | Cùng các giá trị trên, theo **đúng tên khóa** trong văn bản chính sách. Dùng để thay các chỗ `[Tên.Khóa = ...]` trong văn bản |

Ví dụ thay giá trị vào văn bản chính sách:

```js
const { data } = (await api.get("/api/policy")).data;

// "[Delay.GraceHours = 6]" -> "6"
const render = (text) =>
  text.replace(/\[([A-Za-z]+\.[A-Za-z0-9]+)\s*=\s*[^\]]*\]/g,
    (whole, key) => (key in data.values ? String(data.values[key]) : whole));
```

### GET /api/settings/policy — Xem cấu hình chính sách (màn quản trị)

- **Ai gọi được:** Mọi nhân sự (không phải khách hàng). Khách dùng `GET /api/policy`
- **Dữ liệu gửi lên:** không có

Response `200`:

```json
{
  "success": true,
  "message": "Request processed successfully",
  "data": {
    "policyVersion": "1.0",
    "quoteAcceptDays": 3,
    "dossierSubmitDays": 7,
    "delayGraceHours": 6,
    "delayPercentPer12Hours": 5,
    "delayMaxPercent": 25,
    "insurancePremiumPercent": 1,
    "insuranceBaseLiabilityUsd": 5000,
    "insuranceBaseVetCapUsd": 1000,
    "insuranceVetCapPercent": 20,
    "insuranceProofThresholdUsd": 50000,
    "horseMinAgeMonths": 6,
    "horseGestationDays": 340,
    "horseLatePregnancyPercent": 90,
    "horsePostFoalingDays": 7,
    "deliveryFreeWaitingHours": 2,
    "deliveryWaitingFeePerHourUsd": 40,
    "deliveryMaxWaitingHours": 24,
    "deliveryDecisionHours": 24,
    "updatedAt": "2026-10-27T07:05:09.094261+00:00",
    "updatedByName": "Trần Quốc Bảo"
  },
  "errors": null
}
```

Ý nghĩa từng trường giống bảng của `GET /api/policy`, chỉ khác cách đặt tên: ở đây tên trường ghép cả nhóm, ví dụ `deliveryWaitingFeePerHourUsd` tương ứng `delivery.waitingFeePerHourUsd`. Bảng khóa cấu hình của từng trường xem ở `PUT` bên dưới. `updatedByName` là người sửa gần nhất.

| Lỗi | Khi nào |
|---|---|
| 401 | Chưa đăng nhập |
| 403 | Khách hàng gọi API này |

### PUT /api/settings/policy — Sửa cấu hình chính sách

Giá trị mới áp dụng từ lúc lưu. Đơn đã đồng ý chính sách giữ phiên bản đã đồng ý. Khi đổi nội dung quy tắc, nhớ **tăng `policyVersion`** (ví dụ `1.0` → `1.1`) để các đơn mới lưu đúng phiên bản.

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** JSON, gửi **đủ cả 19 trường** (kể cả trường không đổi)

| Trường | Kiểu | Giới hạn | Khóa cấu hình |
|---|---|---|---|
| `policyVersion` | chuỗi | Bắt buộc, tối đa 20 ký tự | `Policy.Version` |
| `quoteAcceptDays` | số nguyên | 1 – 30 | `Booking.QuoteAcceptDays` |
| `dossierSubmitDays` | số nguyên | 1 – 60 | `Dossier.SubmitDays` |
| `delayGraceHours` | số nguyên | 0 – 168 | `Delay.GraceHours` |
| `delayPercentPer12Hours` | số | 0 – 100 | `Delay.PercentPer12Hours` |
| `delayMaxPercent` | số | 0 – 100 | `Delay.MaxPercent` |
| `insurancePremiumPercent` | số | 0 – 100 | `Insurance.PremiumPercent` |
| `insuranceBaseLiabilityUsd` | số | ≥ 0 | `Insurance.BaseLiabilityUsd` |
| `insuranceBaseVetCapUsd` | số | ≥ 0 | `Insurance.BaseVetCapUsd` |
| `insuranceVetCapPercent` | số | 0 – 100 | `Insurance.VetCapPercent` |
| `insuranceProofThresholdUsd` | số | ≥ 0 | `Insurance.ProofThresholdUsd` |
| `horseMinAgeMonths` | số nguyên | 0 – 240 | `Horse.MinAgeMonths` |
| `horseGestationDays` | số nguyên | 300 – 400 | `Horse.GestationDays` |
| `horseLatePregnancyPercent` | số | 0 – 100 | `Horse.LatePregnancyPercent` |
| `horsePostFoalingDays` | số nguyên | 0 – 60 | `Horse.PostFoalingDays` |
| `deliveryFreeWaitingHours` | số nguyên | 0 – 72 | `Delivery.FreeWaitingHours` |
| `deliveryWaitingFeePerHourUsd` | số | ≥ 0 | `Delivery.WaitingFeePerHourUsd` |
| `deliveryMaxWaitingHours` | số nguyên | 1 – 168 | `Delivery.MaxWaitingHours` |
| `deliveryDecisionHours` | số nguyên | 1 – 168 | `Delivery.DecisionHours` |

Ví dụ request (cách dễ nhất: lấy `data` của `GET /api/settings/policy`, sửa vài trường rồi gửi lại):

```js
const current = (await api.get("/api/settings/policy")).data.data;
await api.put("/api/settings/policy", {
  ...current,
  policyVersion: "1.1",
  deliveryWaitingFeePerHourUsd: 45,
});
```

Response `200`: `data` là cấu hình sau khi lưu (dạng giống GET).

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Validation failed`, kèm `errors`, ví dụ `["PolicyVersion is required.", "DelayMaxPercent must be between 0 and 100."]` | Thiếu trường hoặc giá trị ngoài giới hạn; `errors` liệt kê mọi lỗi cùng lúc |
| 403 | | Không phải quản lý / Admin |

---

## 21. Những phần chưa có API

Các phần dưới đây backend chưa làm. Frontend có thể dựng giao diện trước nhưng chưa nối được.

| Phần | Ghi chú |
|---|---|
| Đẩy thông báo tức thời (WebSocket / push) | Hiện chỉ có API đọc thông báo (mục 14); frontend gọi lại `unread-count` định kỳ |

Tiến độ từng phần xem ở [TASKS.md](TASKS.md).

---

## 22. Bảng tra nhanh mọi endpoint

Tổng cộng 121 endpoint. Chi tiết từng cái xem ở mục tương ứng.

**4. Đăng nhập và tài khoản của tôi**

| Method | Đường dẫn | Việc | Ai gọi được |
|---|---|---|---|
| POST | `/api/auth/register` | Đăng ký tài khoản khách hàng | Ai cũng gọi được, không cần đăng nhập |
| POST | `/api/auth/login` | Đăng nhập | Ai cũng gọi được, không cần đăng nhập |
| POST | `/api/auth/google` | Đăng nhập bằng Google | Ai cũng gọi được, không cần đăng nhập |
| POST | `/api/auth/refresh` | Xin token mới | Ai cũng gọi được, không cần `accessToken` |
| POST | `/api/auth/logout` | Đăng xuất | Mọi người đã đăng nhập |
| GET | `/api/auth/me` | Xem hồ sơ của tôi | Mọi người đã đăng nhập |
| PUT | `/api/auth/me` | Sửa hồ sơ của tôi | Mọi người đã đăng nhập |
| PUT | `/api/auth/change-password` | Đổi mật khẩu | Mọi người đã đăng nhập |
| POST | `/api/auth/forgot-password` | Quên mật khẩu: xin link đặt lại | Ai cũng gọi được, không cần đăng nhập |
| POST | `/api/auth/reset-password` | Đặt mật khẩu mới bằng token trong link | Ai cũng gọi được, không cần đăng nhập |

**5. Quản lý người dùng**

| Method | Đường dẫn | Việc | Ai gọi được |
|---|---|---|---|
| GET | `/api/users` | Danh sách tài khoản | Admin |
| GET | `/api/users/{id}` | Chi tiết một tài khoản | Admin |
| POST | `/api/users` | Tạo tài khoản nhân sự | Admin |
| PUT | `/api/users/{id}` | Sửa tài khoản | Admin |
| PATCH | `/api/users/{id}/active` | Khóa hoặc mở tài khoản | Admin |
| PUT | `/api/users/{id}/password` | Đặt lại mật khẩu cho người khác | Admin |
| GET | `/api/users/staff` | Danh sách nhân sự cho dropdown | LogisticsManager, TransportSpecialist, FleetCoordinator, Admin |

**6. Ngựa**

| Method | Đường dẫn | Việc | Ai gọi được |
|---|---|---|---|
| GET | `/api/horses` | Danh sách ngựa | Mọi người đã đăng nhập |
| GET | `/api/horses/{id}` | Chi tiết một con ngựa | Chủ ngựa, hoặc nhân sự |
| POST | `/api/horses` | Thêm ngựa | Customer |
| PUT | `/api/horses/{id}` | Sửa ngựa | Chủ ngựa (Customer) |
| DELETE | `/api/horses/{id}` | Xóa ngựa | Chủ ngựa (Customer) |
| GET | `/api/horses/{id}/documents` | Danh sách hồ sơ thú y của ngựa | Chủ ngựa, hoặc nhân sự |
| POST | `/api/horses/{id}/documents` | Thêm một hồ sơ thú y | Chủ ngựa (Customer) |
| DELETE | `/api/horses/{id}/documents/{documentId}` | Xóa một hồ sơ thú y | Chủ ngựa (Customer) |
| GET | `/api/geo/search` | Gợi ý địa chỉ khi gõ | Mọi người đã đăng nhập |
| GET | `/api/geo/reverse` | Tọa độ → địa chỉ + nước | Mọi người đã đăng nhập |
| POST | `/api/geo/route` | Chỉ đường qua nhiều điểm: từng chặng, tổng, đường vẽ, các nước đi qua | Mọi người đã đăng nhập |

**7. Flow 1 – Đơn vận chuyển**

| Method | Đường dẫn | Việc | Ai gọi được |
|---|---|---|---|
| POST | `/api/bookings/quote-preview` | Xem giá trước khi gửi đơn | Mọi người đã đăng nhập |
| POST | `/api/bookings` | Gửi đơn vận chuyển | Customer |
| GET | `/api/bookings` | Danh sách đơn | Customer, LogisticsManager, TransportSpecialist, FleetCoordinator, Admin |
| GET | `/api/bookings/{id}` | Chi tiết đơn | Như danh sách đơn |
| POST | `/api/bookings/{id}/cancel` | Khách hủy đơn | Customer (chủ đơn) |
| POST | `/api/bookings/{id}/accept-quote` | Khách chấp nhận báo giá chốt + hợp đồng (→ `AwaitingDeposit`) | Customer (chủ đơn) |
| GET | `/api/bookings/{id}/contract` | Hợp đồng hiện hành của đơn + link tải PDF | Customer (chủ đơn); nhân sự xem được đơn |
| GET | `/api/bookings/{id}/contracts` | Lịch sử các phiên bản hợp đồng | Như trên |
| GET | `/api/contracts/{contractId}/file` | Tải PDF (302 tới link có hạn) | Như trên |
| POST | `/api/contracts/{contractId}/accept` | Khách chấp nhận bản hợp đồng sửa đổi | Customer (chủ đơn) |
| GET | `/api/trips/{id}/air-waybill` | Vận đơn hàng không của chuyến | Nhân sự (trừ tài xế); khách có ngựa trên chuyến |
| PUT | `/api/trips/{id}/air-waybill` | Nhập số / file vận đơn hàng không | FleetCoordinator, Admin |
| POST | `/api/bookings/{id}/decline-quote` | Khách từ chối báo giá chốt (→ `Cancelled`) | Customer (chủ đơn) |
| PUT | `/api/bookings/{id}/locations` | Khách đổi điểm đón / trả trước khi trả tiền (lập tuyến + báo giá lại) | Customer (chủ đơn) |
| POST | `/api/bookings/{id}/cancel-route-change` | Khách hủy vì công ty đổi tuyến, hoàn 100% | Customer (chủ đơn) |
| POST | `/api/bookings/{id}/approve` | Duyệt sơ bộ đơn (→ `PreApproved`) | LogisticsManager, Admin |
| PUT | `/api/bookings/{id}/horse-values` | Chốt giá trị thỏa thuận từng con trước khi báo giá | LogisticsManager, Admin |
| POST | `/api/bookings/{id}/reject` | Từ chối đơn (trước khi gửi báo giá) | LogisticsManager, Admin |
| GET | `/api/bookings/{bookingId}/route-plans` | Các phiên bản tuyến dự kiến của đơn | Nhân sự; khách (chỉ tuyến đã duyệt) |
| GET | `/api/route-plans/{id}` | Chi tiết một tuyến | Nhân sự; khách (chỉ tuyến đã duyệt) |
| POST | `/api/bookings/{bookingId}/route-plans/preview` | Tính thử tuyến, không lưu | FleetCoordinator, LogisticsManager, Admin |
| POST | `/api/bookings/{bookingId}/route-plans` | Tạo bản nháp tuyến | FleetCoordinator, Admin |
| PUT | `/api/route-plans/{id}` | Sửa tuyến nháp / bị từ chối | FleetCoordinator, Admin |
| POST | `/api/route-plans/{id}/submit` | Trình duyệt tuyến | FleetCoordinator, Admin |
| POST | `/api/route-plans/{id}/approve` | Duyệt tuyến (server gửi báo giá chốt, đơn → `Quoted`) | LogisticsManager, Admin |
| POST | `/api/route-plans/{id}/reject` | Từ chối tuyến | LogisticsManager, Admin |
| POST | `/api/bookings/{id}/reassign-specialist` | Đổi chuyên viên phụ trách | LogisticsManager, Admin |

**8. Flow 2 – Hồ sơ kiểm dịch**

| Method | Đường dẫn | Việc | Ai gọi được |
|---|---|---|---|
| GET | `/api/dossiers` | Danh sách hồ sơ | Customer, LogisticsManager, TransportSpecialist, FleetCoordinator, Admin |
| GET | `/api/dossiers/{id}` | Chi tiết hồ sơ và checklist giấy tờ | Như danh sách hồ sơ |
| POST | `/api/dossiers/{id}/request-documents` | Yêu cầu khách nộp giấy tờ | Chuyên viên phụ trách, Admin |
| POST | `/api/dossiers/{id}/documents` | Nộp hoặc nộp lại một giấy tờ | Chủ ngựa (Customer), chuyên viên phụ trách, Admin |
| POST | `/api/documents/{id}/approve` | Duyệt một giấy tờ | Chuyên viên phụ trách, Admin |
| POST | `/api/documents/{id}/reject` | Từ chối một giấy tờ | Chuyên viên phụ trách, Admin |
| POST | `/api/dossiers/{id}/submit-to-authorities` | Nộp hồ sơ cho cơ quan chức năng | Chuyên viên phụ trách, Admin |
| POST | `/api/dossiers/{id}/clear` | Ghi nhận thông quan | Chuyên viên phụ trách, Admin |
| POST | `/api/dossiers/{id}/issue` | Báo hồ sơ có vấn đề | Chuyên viên phụ trách, Admin |

**9. Flow 3 – Lập kế hoạch chuyến**

| Method | Đường dẫn | Việc | Ai gọi được |
|---|---|---|---|
| GET | `/api/trips/unplanned-horses` | Hàng chờ xếp chuyến | FleetCoordinator, LogisticsManager, Admin |
| POST | `/api/trips/suggest-route` | Gợi ý lộ trình (ghép nhiều đơn) | FleetCoordinator, Admin |
| GET | `/api/trips/available-vehicles` | Xe còn rảnh | FleetCoordinator, LogisticsManager, Admin |
| GET | `/api/trips/available-crew` | Tài xế và áp tải còn rảnh | FleetCoordinator, LogisticsManager, Admin |
| POST | `/api/trips` | Tạo bản nháp chuyến | FleetCoordinator, Admin |
| PUT | `/api/trips/{id}` | Sửa bản nháp chuyến | Người lập chuyến, Admin |
| POST | `/api/trips/{id}/checkpoints` | Thêm một mốc lộ trình | Người lập chuyến, Admin |
| PUT | `/api/trips/{id}/checkpoints/{checkpointId}` | Sửa một mốc | Người lập chuyến, Admin |
| DELETE | `/api/trips/{id}/checkpoints/{checkpointId}` | Xóa một mốc | Người lập chuyến, Admin |
| PUT | `/api/trips/{id}/checkpoints/order` | Đổi thứ tự các mốc | Người lập chuyến, Admin |
| POST | `/api/trips/{id}/submit` | Trình duyệt kế hoạch | Người lập chuyến, Admin |
| POST | `/api/trips/{id}/approve` | Duyệt kế hoạch chuyến | LogisticsManager, Admin |
| POST | `/api/trips/{id}/reject` | Trả kế hoạch về cho điều phối | LogisticsManager, Admin |
| POST | `/api/trips/{id}/cancel` | Hủy chuyến chưa khởi hành | Người lập chuyến, LogisticsManager, Admin |
| GET | `/api/trips` | Danh sách chuyến | Mọi nhân sự (không gồm Customer) |
| GET | `/api/trips/{id}` | Chi tiết chuyến | Mọi nhân sự; tài xế / áp tải chỉ xem được chuyến của mình |

**10. Flow 4 – Chạy chuyến và theo dõi**

| Method | Đường dẫn | Việc | Ai gọi được |
|---|---|---|---|
| POST | `/api/trips/{id}/start` | Xuất phát | Đoàn xe của chuyến, Admin |
| POST | `/api/checkpoints/{id}/arrive` | Đã tới mốc | Đoàn xe của chuyến, Admin |
| POST | `/api/checkpoints/{id}/clear` | Đã thông quan tại cửa khẩu | Đoàn xe của chuyến, Admin |
| POST | `/api/checkpoints/{id}/depart` | Rời mốc | Đoàn xe của chuyến, Admin |
| POST | `/api/trips/{id}/horses/{tripHorseId}/offload` | **Đã bỏ**, luôn trả 410: ngựa rời chuyến qua sự cố (phương án `OffloadHorse`) | Đoàn xe của chuyến, FleetCoordinator, LogisticsManager, Admin |
| POST | `/api/trips/{id}/welfare-logs` | Ghi nhật ký thể trạng ngựa | Áp tải phụ trách con ngựa, trưởng nhóm áp tải, Admin |
| GET | `/api/trips/{id}/welfare-logs` | Xem nhật ký của chuyến | Mọi nhân sự; tài xế / áp tải chỉ xem được chuyến của mình |
| GET | `/api/tracking/trips` | Bảng theo dõi các chuyến | FleetCoordinator, LogisticsManager, Admin |
| GET | `/api/tracking/bookings/{id}` | Khách theo dõi đơn của mình | Chủ đơn; chuyên viên phụ trách đơn; LogisticsManager, FleetCoordinator, Admin |

**11. Flow 5 – Sự cố và nắn tuyến**

| Method | Đường dẫn | Việc | Ai gọi được |
|---|---|---|---|
| POST | `/api/trips/{tripId}/incidents` | Báo sự cố | Đoàn xe của chuyến, Admin |
| GET | `/api/incidents` | Danh sách sự cố | Mọi người đã đăng nhập |
| GET | `/api/incidents/{id}` | Chi tiết sự cố | Mọi người đã đăng nhập (theo quyền xem) |
| GET | `/api/trips/{tripId}/incidents` | Các sự cố của một chuyến | Mọi nhân sự; tài xế / áp tải chỉ xem được chuyến của mình |
| POST | `/api/incidents/{id}/propose` | Đề xuất phương án xử lý | FleetCoordinator, Admin |
| POST | `/api/incidents/{id}/approve` | Duyệt phương án | LogisticsManager, Admin |
| POST | `/api/incidents/{id}/reject` | Từ chối phương án | LogisticsManager, Admin |
| POST | `/api/incidents/{id}/apply-reroute` | Nắn tuyến | FleetCoordinator, Admin |
| POST | `/api/incidents/{id}/resolve` | Đóng sự cố, chọn nguyên nhân (`faultParty`) | FleetCoordinator, Admin |
| GET | `/api/delay-compensations` | Danh sách bồi thường giao trễ | LogisticsManager, Admin |
| GET | `/api/bookings/{bookingId}/delay-compensations` | Bồi thường giao trễ của một đơn | Khách của đơn, nhân sự |
| POST | `/api/delay-compensations/{id}/approve` | Duyệt bồi thường (có thể sửa số tiền) | LogisticsManager, Admin |
| POST | `/api/delay-compensations/{id}/reject` | Từ chối bồi thường | LogisticsManager, Admin |

**12. Flow 6 – Bàn giao, đóng chuyến và báo cáo**

| Method | Đường dẫn | Việc | Ai gọi được |
|---|---|---|---|
| POST | `/api/trips/{tripId}/handover` | Lập biên bản bàn giao (e-POD) | Đoàn xe của chuyến, Admin |
| GET | `/api/trips/{tripId}/handovers` | Các biên bản bàn giao của một chuyến | Mọi nhân sự; tài xế / áp tải chỉ xem được chuyến của mình |
| GET | `/api/bookings/{bookingId}/handovers` | Các biên bản bàn giao của một đơn | Chủ đơn; nhân sự được xem đơn đó (như `GET /api/bookings/{id}`) |
| POST | `/api/trips/{tripId}/close` | Đóng chuyến và chốt KPI | LogisticsManager, Admin |
| GET | `/api/reports/summary` | Báo cáo tổng hợp | LogisticsManager, Admin |

**13. Danh mục loại giấy tờ**

| Method | Đường dẫn | Việc | Ai gọi được |
|---|---|---|---|
| GET | `/api/document-types/lookup` | Tất cả loại giấy tờ (cho dropdown) | Mọi người đã đăng nhập |
| GET | `/api/document-types` | Danh sách loại giấy tờ | Mọi người đã đăng nhập |
| GET | `/api/document-types/{id}` | Chi tiết loại giấy tờ | Mọi người đã đăng nhập |
| POST | `/api/document-types` | Thêm loại giấy tờ | TransportSpecialist, Admin |
| PUT | `/api/document-types/{id}` | Sửa loại giấy tờ | TransportSpecialist, Admin |
| DELETE | `/api/document-types/{id}` | Xóa loại giấy tờ | TransportSpecialist, Admin |

**14. Thông báo trong ứng dụng (chuông)**

| Method | Đường dẫn | Việc | Ai gọi được |
|---|---|---|---|
| GET | `/api/notifications` | Danh sách thông báo của tôi | Mọi người đã đăng nhập |
| GET | `/api/notifications/unread-count` | Số thông báo chưa đọc | Mọi người đã đăng nhập |
| PATCH | `/api/notifications/{id}/read` | Đánh dấu một thông báo đã đọc | Người nhận thông báo đó |
| PATCH | `/api/notifications/read-all` | Đánh dấu tất cả đã đọc | Mọi người đã đăng nhập |
| WS (SignalR) | `/hubs/notifications` | Nhận thông báo mới tức thời (sự kiện `notificationReceived`) | Mọi người đã đăng nhập |

**15. Quản lý phương tiện**

| Method | Đường dẫn | Việc | Ai gọi được |
|---|---|---|---|
| GET | `/api/transport-assets` | Danh sách phương tiện | Mọi nhân sự (không gồm Customer) |
| GET | `/api/transport-assets/{id}` | Chi tiết phương tiện | Mọi nhân sự (không gồm Customer) |
| POST | `/api/transport-assets` | Thêm phương tiện | FleetCoordinator, Admin |
| PUT | `/api/transport-assets/{id}` | Sửa phương tiện | FleetCoordinator, Admin |
| DELETE | `/api/transport-assets/{id}` | Xóa phương tiện | FleetCoordinator, Admin |

**16. Quy định giấy tờ theo quốc gia**

| Method | Đường dẫn | Việc | Ai gọi được |
|---|---|---|---|
| GET | `/api/country-doc-requirements` | Danh sách quy định | Mọi người đã đăng nhập |
| GET | `/api/country-doc-requirements/{id}` | Chi tiết một quy định | Mọi người đã đăng nhập |
| POST | `/api/country-doc-requirements` | Thêm quy định | TransportSpecialist, Admin |
| PUT | `/api/country-doc-requirements/{id}` | Sửa quy định | TransportSpecialist, Admin |
| DELETE | `/api/country-doc-requirements/{id}` | Xóa quy định | TransportSpecialist, Admin |

**17. Bảng giá**

| Method | Đường dẫn | Việc | Ai gọi được |
|---|---|---|---|
| GET | `/api/price-items` | Danh sách dòng giá | LogisticsManager, Admin |
| GET | `/api/price-items/{id}` | Chi tiết dòng giá | LogisticsManager, Admin |
| POST | `/api/price-items` | Thêm dòng giá | LogisticsManager, Admin |
| PUT | `/api/price-items/{id}` | Sửa dòng giá | LogisticsManager, Admin |
| PATCH | `/api/price-items/{id}/active` | Bật hoặc tắt dòng giá | LogisticsManager, Admin |

**18. Danh mục giá trị cho dropdown**

| Method | Đường dẫn | Việc | Ai gọi được |
|---|---|---|---|
| GET | `/api/lookups/enums` | Lấy mọi danh sách giá trị cố định | Mọi người đã đăng nhập |

**19. Thanh toán**

| Method | Đường dẫn | Việc | Ai gọi được |
|---|---|---|---|
| GET | `/api/bookings/{id}/payments` | Tóm tắt thanh toán của đơn | Customer (chủ đơn), LogisticsManager, TransportSpecialist, FleetCoordinator, Admin |
| GET | `/api/payments/my` | Trang Thanh toán của khách: mọi đơn có thanh toán + mọi giao dịch | Customer |
| POST | `/api/bookings/{id}/payments` | Tạo link thanh toán | Customer (chủ đơn) |
| POST | `/api/bookings/{id}/payments/manual` | Ghi nhận khoản trả ngoài hệ thống | LogisticsManager, Admin |
| GET | `/api/payments/vnpay-return` | VNPay đưa khách về (frontend không gọi) | Không cần đăng nhập |
| GET | `/api/payments/vnpay-ipn` | VNPay báo kết quả (frontend không gọi) | Không cần đăng nhập |
| POST | `/api/payments/mock/{txnRef}/complete` | Hoàn tất giao dịch giả lập | Customer |
| POST | `/api/payments/{id}/retry-refund` | Thử hoàn tiền lại qua cổng | LogisticsManager, Admin |
| POST | `/api/payments/{id}/mark-refunded` | Đánh dấu đã hoàn tiền thủ công | LogisticsManager, Admin |
| GET | `/api/settings/payment` | Xem cấu hình thanh toán | Mọi người đã đăng nhập |
| PUT | `/api/settings/payment` | Sửa cấu hình thanh toán | LogisticsManager, Admin |
| GET | `/api/policy` | Chính sách dịch vụ: phiên bản + giá trị cấu hình (công khai) | Mọi người, không cần đăng nhập |
| GET | `/api/settings/policy` | Xem cấu hình chính sách | Mọi nhân sự |
| PUT | `/api/settings/policy` | Sửa cấu hình chính sách | LogisticsManager, Admin |
