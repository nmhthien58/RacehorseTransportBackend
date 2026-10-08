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
7. [Flow 1 – Đơn vận chuyển](#7-flow-1--đơn-vận-chuyển)
8. [Flow 2 – Hồ sơ kiểm dịch](#8-flow-2--hồ-sơ-kiểm-dịch)
9. [Flow 3 – Lập kế hoạch chuyến](#9-flow-3--lập-kế-hoạch-chuyến)
10. [Flow 4 – Chạy chuyến và theo dõi](#10-flow-4--chạy-chuyến-và-theo-dõi)
11. [Flow 5 – Sự cố và nắn tuyến](#11-flow-5--sự-cố-và-nắn-tuyến)
12. [Flow 6 – Bàn giao, đóng chuyến và báo cáo](#12-flow-6--bàn-giao-đóng-chuyến-và-báo-cáo)
13. [Danh mục loại giấy tờ](#13-danh-mục-loại-giấy-tờ)
14. [Những phần chưa có API](#14-những-phần-chưa-có-api)
15. [Bảng tra nhanh mọi endpoint](#15-bảng-tra-nhanh-mọi-endpoint)

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

Cả 6 tài khoản dùng chung một mật khẩu, ghi ở dòng đầu file `database/02_seed.sql` trong repo backend. Trong các ví dụ bên dưới mật khẩu được viết là `<mật khẩu>`.

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
form.append("passportNumber", "FEI-VN-2024-09");
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
| Trạng thái đơn (`status`) | `Submitted` chờ duyệt · `Approved` đã duyệt · `Completed` đã giao xong · `Rejected` bị từ chối · `Expired` quá 48 giờ không ai duyệt · `Cancelled` khách hủy |
| Trạng thái ngựa trong đơn | `Pending` · `Approved` · `Cancelled` |
| `transportMode` | `Ground` đường bộ · `Air` đường hàng không |
| `stallClass` (hạng chuồng) | `Shared` chuồng ghép · `Comfort` chuồng rưỡi · `Private` chuồng riêng |
| Giới tính ngựa (`gender`) | `Stallion` ngựa đực · `Mare` ngựa cái · `Gelding` ngựa thiến |
| Trạng thái hồ sơ | `Draft` mới mở · `AwaitingDocs` chờ khách nộp giấy · `Reviewing` đang thẩm định · `SubmittedToAuthorities` đã nộp cơ quan chức năng · `Cleared` đã thông quan · `Issue` có vấn đề |
| Trạng thái giấy tờ | `Pending` chờ duyệt · `Approved` đã duyệt · `Rejected` bị từ chối |
| Trạng thái dòng checklist (`state`) | `Missing` chưa nộp · `Pending` · `Approved` · `Rejected` |
| Trạng thái chuyến (`overallStatus`) | `Draft` bản nháp · `PendingApproval` chờ duyệt · `Scheduled` đã lên lịch · `InTransit` đang đi · `EmergencyRerouting` đang nắn tuyến vì sự cố · `ArrivedDestination` đã tới đích · `Completed` đã đóng · `Cancelled` đã hủy |
| `crewRole` | `Driver` lái xe · `Escort` áp tải |
| `checkpointType` (loại mốc) | `Origin` điểm đi · `RestStop` trạm nghỉ · `BorderGate` cửa khẩu · `Destination` điểm giao |
| Trạng thái mốc | `Pending` chưa tới · `Arrived` đã tới · `Cleared` đã thông quan (chỉ cửa khẩu) · `Departed` đã rời · `Cancelled` bị bỏ khi nắn tuyến |
| `feedStatus` (ăn uống) | `Normal` bình thường · `Reduced` ăn ít · `Refused` bỏ ăn |
| `stressLevel` | `Calm` bình tĩnh · `MildStress` hơi căng thẳng · `Agitated` kích động |
| `incidentType` (loại sự cố) | `MechanicalBreakdown` hỏng xe · `BorderCongestion` tắc biên · `EquineHealthIssue` sức khỏe ngựa · `Weather` thời tiết · `ClimateControlFailure` hỏng điều hòa · `CustomsHold` bị giữ ở hải quan · `Other` khác |
| `severity` (mức độ sự cố) | `Minor` nhẹ · `Moderate` vừa · `Major` nặng · `Critical` nghiêm trọng |
| Trạng thái sự cố | `Reported` mới báo · `PlanProposed` đã có phương án chờ duyệt · `Approved` phương án được duyệt · `Rejected` phương án bị từ chối · `Resolved` đã xử lý xong |
| `overallCondition` (tình trạng khi bàn giao) | `Excellent` tốt · `NormalFatigue` mệt bình thường · `Injured` bị thương |
| `onTimeStatus` (kết quả đúng giờ của chuyến) | `OnTime` đúng giờ · `Delayed_AcceptableForceMajeure` trễ do bất khả kháng · `Delayed_OperationalFault` trễ do lỗi vận hành |
| Loại phương tiện (`assetType`) | `HorseTruck_AirSuspension` · `HorseVan` · `AirStall` |

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
- **Thông báo cho khách hàng** ở các sự kiện chính: đơn được duyệt / bị từ chối, cần bổ sung giấy tờ, giấy bị từ chối, ngựa đã thông quan, hồ sơ có vấn đề, có sự cố trên chuyến, ngựa tới điểm giao. Nhân sự không nhận email, chỉ nhận thông báo trong ứng dụng.

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
  "passportNumber": "FEI-VN-2024-09",
  "breed": "Thoroughbred",
  "gender": "Gelding",
  "dateOfBirth": "2019-05-20",
  "color": "Xám",
  "specialCareRequirements": "Dễ say xe, cần cỏ tươi",
  "photoUrl": "https://abcd.supabase.co/storage/v1/object/sign/files/horses/2026/10/6f316c....png?token=eyJ...",
  "isActive": true,
  "createdAt": "2026-10-08T07:24:30+00:00"
}
```

| Trường | Ý nghĩa |
|---|---|
| `horseId` | Mã ngựa, dùng khi đặt chuyến |
| `ownerUserId`, `ownerName` | Chủ ngựa |
| `microchipNumber` | Mã vi mạch gắn trên ngựa, không trùng trong toàn hệ thống |
| `passportNumber` | Số hộ chiếu ngựa, không trùng trong toàn hệ thống |
| `breed` | Giống ngựa |
| `gender` | `Stallion`, `Mare` hoặc `Gelding` |
| `dateOfBirth` | Ngày sinh, dạng `2019-05-20` |
| `color` | Màu lông |
| `specialCareRequirements` | Yêu cầu chăm sóc đặc biệt, tài xế sẽ đọc được |
| `photoUrl` | Link ảnh, dùng được ngay; `null` nếu không có ảnh |
| `isActive` | `false` là ngựa đã bị xóa |

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
| `microchipNumber` | chuỗi | Có | Mã vi mạch, tối đa 30 ký tự, không trùng |
| `passportNumber` | chuỗi | Có | Số hộ chiếu, tối đa 50 ký tự, không trùng |
| `breed` | chuỗi | Không | Giống ngựa; bỏ trống thì mặc định `Thoroughbred` |
| `gender` | chuỗi | Có | `Stallion`, `Mare` hoặc `Gelding` |
| `dateOfBirth` | ngày | Có | Dạng `2019-05-20`, phải ở quá khứ |
| `color` | chuỗi | Có | Màu lông, tối đa 30 ký tự |
| `specialCareRequirements` | chuỗi | Không | Yêu cầu chăm sóc đặc biệt |
| `photo` | file | Không | Ảnh ngựa: `jpg`, `jpeg`, `png`, tối đa 10 MB |

Ví dụ code: xem mục 2.5.

Response `201`: `data` là con ngựa vừa tạo, `photoUrl` là link ảnh vừa tải lên.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Microchip number '...' is already registered.` | Mã vi mạch đã có trong hệ thống |
| 400 | `Passport number '...' is already registered.` | Số hộ chiếu đã có trong hệ thống |
| 400 | `DateOfBirth must be in the past.` | Ngày sinh ở tương lai |
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
| `microchipNumber` | chuỗi | Có | Mã vi mạch, tối đa 30 ký tự, không trùng |
| `passportNumber` | chuỗi | Có | Số hộ chiếu, tối đa 50 ký tự, không trùng |
| `breed` | chuỗi | Không | Giống ngựa; bỏ trống thì mặc định `Thoroughbred` |
| `gender` | chuỗi | Có | `Stallion`, `Mare` hoặc `Gelding` |
| `dateOfBirth` | ngày | Có | Dạng `2019-05-20`, phải ở quá khứ |
| `color` | chuỗi | Có | Màu lông, tối đa 30 ký tự |
| `specialCareRequirements` | chuỗi | Không | Yêu cầu chăm sóc đặc biệt |
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

---

## 7. Flow 1 – Đơn vận chuyển

Khách hàng gửi một **đơn vận chuyển** (booking) cho một hoặc nhiều con ngựa. Quản lý duyệt hoặc từ chối. Khi duyệt, quản lý chọn một chuyên viên thủ tục phụ trách, và hệ thống tự mở một hồ sơ kiểm dịch cho mỗi con ngựa (Flow 2).

Vòng đời của một đơn:

```
Submitted ──(quản lý duyệt)──────────────> Approved ──(mọi ngựa đã giao xong)──> Completed
    │
    ├──(quản lý từ chối)──────────────────> Rejected
    ├──(khách hủy)────────────────────────> Cancelled
    └──(quá 48 giờ không ai duyệt)────────> Expired
```

Thứ tự gọi thông thường:

1. Khách: `GET /api/horses` để chọn ngựa.
2. Khách: `POST /api/bookings/quote-preview` để xem giá (gọi lại mỗi khi khách đổi lựa chọn).
3. Khách: `POST /api/bookings` để gửi đơn.
4. Quản lý: `GET /api/bookings?status=Submitted` để xem đơn chờ duyệt, `GET /api/users/staff?role=TransportSpecialist` để lấy danh sách chuyên viên.
5. Quản lý: `POST /api/bookings/{id}/approve` hoặc `POST /api/bookings/{id}/reject`.

Ai thấy đơn nào: khách thấy đơn của mình; chuyên viên thấy đơn mình phụ trách; `LogisticsManager`, `FleetCoordinator`, `Admin` thấy tất cả; tài xế không xem đơn (gọi vào nhận 403).

### POST /api/bookings/quote-preview — Xem giá trước khi gửi đơn

Tính giá cho một đơn dự kiến. Không lưu gì vào hệ thống, gọi bao nhiêu lần cũng được.

- **Ai gọi được:** Mọi người đã đăng nhập
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `transportMode` | chuỗi | Có | `Ground` (đường bộ) hoặc `Air` (hàng không) |
| `dropoffCountryCode` | chuỗi | Có | Mã nước đến, 2 chữ cái, ví dụ `CN`, `TH` |
| `distanceKm` | số nguyên | Có với `Ground` | Quãng đường (km), 1–50000. Đường hàng không thì bỏ qua |
| `requiresClimateControl` | true / false | Không | Cần khoang điều hòa (có phụ phí) |
| `declaredValue` | số | Không | Giá trị khai báo để mua bảo hiểm. Bỏ trống hoặc `null` là không mua |
| `departureDate` | ngày giờ | Có | Ngày khởi hành, phải ở tương lai |
| `deliveryDate` | ngày giờ | Có | Ngày giao, phải sau ngày khởi hành |
| `horses` | mảng | Có | Mỗi phần tử là một con ngựa, chỉ cần `stallClass`: `Shared`, `Comfort` hoặc `Private` |

Ví dụ request:

```json
{
  "transportMode": "Ground",
  "dropoffCountryCode": "CN",
  "distanceKm": 900,
  "requiresClimateControl": true,
  "declaredValue": 50000,
  "departureDate": "2026-11-07T06:00:00+07:00",
  "deliveryDate": "2026-11-09T18:00:00+07:00",
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
  "currencyCode": "USD",
  "horseCount": 2,
  "groomCount": 1,
  "transitDays": 3,
  "isExpress": false
}
```

(`lines` trong ví dụ đã rút gọn còn 4 dòng; thực tế có đủ cước, chiết khấu, phụ phí, phí thủ tục, áp tải, bảo hiểm.)

| Trường | Ý nghĩa |
|---|---|
| `lines[]` | Các dòng của bảng giá: `name` (tên hiển thị), `quantity`, `unitPrice`, `amount` (thành tiền). Dòng có `amount` âm là chiết khấu |
| `total` | Tổng tiền |
| `currencyCode` | Đơn vị tiền (`USD`) |
| `horseCount`, `groomCount` | Số ngựa và số người áp tải được tính |
| `transitDays` | Số ngày vận chuyển |
| `isExpress` | `true` nếu khởi hành trong vòng 7 ngày tới (có phụ phí gấp). Server tự xác định |

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `No freight rate is configured ...` | Tuyến này chưa có trong bảng giá |
| 400 | `Validation failed` | Thiếu trường, mã nước không phải 2 chữ cái, không có con ngựa nào |

### POST /api/bookings — Gửi đơn vận chuyển

Tạo đơn mới ở trạng thái `Submitted`. Server tự tính giá và lưu kèm đơn; các quản lý nhận thông báo có đơn mới.

- **Ai gọi được:** Customer
- **Dữ liệu gửi lên:** JSON

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `pickupAddress` | chuỗi | Có | Địa chỉ nhận ngựa, tối đa 255 ký tự |
| `pickupCountryCode` | chuỗi | Có | Mã nước đi, 2 chữ cái, ví dụ `VN` |
| `dropoffAddress` | chuỗi | Có | Địa chỉ giao ngựa |
| `dropoffCountryCode` | chuỗi | Có | Mã nước đến, 2 chữ cái |
| `transportMode` | chuỗi | Có | `Ground` (đường bộ) hoặc `Air` (hàng không) |
| `distanceKm` | số nguyên | Có với `Ground` | Quãng đường (km), 1–50000. Đường hàng không thì bỏ qua |
| `requiresClimateControl` | true / false | Không | Cần khoang điều hòa (có phụ phí) |
| `declaredValue` | số | Không | Giá trị khai báo để mua bảo hiểm. Bỏ trống hoặc `null` là không mua |
| `departureDate` | ngày giờ | Có | Ngày khởi hành, phải ở tương lai |
| `deliveryDate` | ngày giờ | Có | Ngày giao, phải sau ngày khởi hành |
| `specialInstructions` | chuỗi | Không | Ghi chú cho công ty, tối đa 2000 ký tự |
| `horses` | mảng | Có | Ít nhất 1 phần tử, xem bảng dưới |

Mỗi phần tử của `horses`:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `horseId` | số | Có | Mã ngựa của khách (lấy từ `GET /api/horses`) |
| `stallClass` | chuỗi | Có | `Shared`, `Comfort` hoặc `Private` |
| `notes` | chuỗi | Không | Ghi chú riêng cho con ngựa này, tối đa 300 ký tự |

Ví dụ request:

```json
{
  "pickupAddress": "Trang trại Yên Bài, Ba Vì, Hà Nội",
  "pickupCountryCode": "VN",
  "dropoffAddress": "Trường đua Tùng Hóa, Quảng Châu",
  "dropoffCountryCode": "CN",
  "departureDate": "2026-11-07T06:00:00+07:00",
  "deliveryDate": "2026-11-09T18:00:00+07:00",
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
  "departureDate": "2026-11-06T23:00:00+00:00",
  "deliveryDate": "2026-11-09T11:00:00+00:00",
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
| 400 | `DepartureDate must be in the future.` | Ngày khởi hành ở quá khứ |
| 400 | `Horse(s) with ID 9 were not found among your active horses.` | Ngựa không phải của khách, hoặc đã xóa |
| 400 | `These horses are already in another booking for an overlapping period: ...` | Ngựa đang nằm trong một đơn khác trùng thời gian |
| 400 | `The same horse appears more than once in this booking.` | Chọn trùng một con ngựa |
| 403 | `You do not have permission to perform this action.` | Người gọi không phải `Customer` |

### GET /api/bookings — Danh sách đơn

Danh sách đơn có phân trang. Mỗi người chỉ thấy các đơn mình được phép xem (xem đầu mục).

- **Ai gọi được:** Customer, LogisticsManager, TransportSpecialist, FleetCoordinator, Admin

Tham số query (gắn sau dấu `?`):

| Tham số | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `status` | chuỗi | Không | Lọc theo trạng thái, ví dụ `Submitted` |
| `departureFrom` | ngày giờ | Không | Chỉ lấy đơn khởi hành từ thời điểm này |
| `departureTo` | ngày giờ | Không | Chỉ lấy đơn khởi hành tới thời điểm này |
| `customerUserId` | số | Không | Chỉ có tác dụng với nhân sự: lọc theo khách hàng |
| `search` | chuỗi | Không | Từ khóa tìm kiếm |
| `sort` | chuỗi | Không | Trường sắp xếp, thêm `-` phía trước để giảm dần |
| `page` | số | Không | Trang, bắt đầu từ 1 (mặc định 1) |
| `size` | số | Không | Số dòng mỗi trang, 1–100 (mặc định 10) |

Ví dụ: `GET /api/bookings?status=Submitted&sort=-createdAt&page=1&size=10`. `search` tìm theo mã đơn và địa chỉ.

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
    "departureDate": "2026-11-06T23:00:00+00:00",
    "deliveryDate": "2026-11-09T11:00:00+00:00",
    "transportMode": "Ground",
    "totalHorses": 2,
    "estimatedCost": 6788.48,
    "currencyCode": "USD",
    "status": "Submitted",
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
| `departureDate`, `deliveryDate` | Ngày đi và ngày giao (giờ UTC) |
| `totalHorses` | Số ngựa trong đơn |
| `estimatedCost`, `currencyCode` | Giá đã tính lúc gửi đơn |
| `status` | Trạng thái đơn (mục 3) |
| `assignedSpecialistId`, `assignedSpecialistName` | Chuyên viên phụ trách; `null` khi đơn chưa duyệt |

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
  "departureDate": "2026-11-06T23:00:00+00:00",
  "deliveryDate": "2026-11-09T11:00:00+00:00",
  "transportMode": "Ground",
  "totalHorses": 2,
  "estimatedCost": 6788.48,
  "currencyCode": "USD",
  "status": "Approved",
  "assignedSpecialistId": 2,
  "assignedSpecialistName": "Nguyễn Thị Mai",
  "createdAt": "2026-10-08T07:24:30+00:00",
  "customerEmail": "jane.smith@racehorseowner.com",
  "customerPhone": "+84988111222",
  "specialInstructions": "Giữ cabin 17 độ",
  "distanceKm": 900,
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
      "dossierId": 3,
      "dossierCode": "DOS-2026-0003",
      "dossierStatus": "Cleared",
      "trips": [
        {
          "tripId": 2,
          "tripCode": "TRP-2026-0002",
          "tripStatus": "Cancelled",
          "stallSlotNumber": 1,
          "plannedStartDate": "2026-11-06T23:00:00+00:00",
          "plannedEndDate": "2026-11-09T11:00:00+00:00"
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
| `rejectionReason` | Lý do bị từ chối (chỉ có khi `status` là `Rejected`) |
| `reviewedByName`, `reviewedAt` | Ai duyệt / từ chối và lúc nào |
| `quoteLines[]` | Các dòng báo giá đã lưu, dạng giống `lines` của xem giá |
| `horses[]` | Từng con ngựa trong đơn |
| `horses[].bookingHorseId` | Mã "ngựa trong đơn", dùng khi xếp chuyến |
| `horses[].dossierId`, `dossierCode`, `dossierStatus` | Hồ sơ kiểm dịch của con ngựa; có sau khi đơn được duyệt |
| `horses[].trips[]` | Các chuyến con ngựa được xếp lên, kèm `tripStatus` |

Một đơn có thể đi nhiều chuyến và một chuyến có thể chở ngựa của nhiều đơn, nên luôn đọc chuyến theo từng con ngựa.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 403 | `You do not have access to this booking.` | Đơn của khách khác, hoặc chuyên viên không phụ trách đơn này |
| 404 | `Booking with ID 99 was not found.` | Không có đơn với mã đó |

### POST /api/bookings/{id}/cancel — Khách hủy đơn

Hủy đơn của mình khi đơn còn ở trạng thái `Submitted` (chưa ai duyệt).

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
| 400 | `Only a Submitted booking can be cancelled (current status: Approved).` | Đơn đã được duyệt hoặc đã kết thúc |
| 403 | `You can only cancel your own bookings.` | Hủy đơn của người khác |

### POST /api/bookings/{id}/approve — Duyệt đơn

Duyệt một đơn đang `Submitted` và chọn chuyên viên phụ trách. Hệ thống tự mở một hồ sơ kiểm dịch cho mỗi con ngựa và báo cho khách cùng chuyên viên.

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

Response `200`: `data` là chi tiết đơn với `status` là `Approved`; mỗi con ngựa đã có `dossierId`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Only a Submitted booking can be approved (current status: ...).` | Đơn không còn ở trạng thái chờ duyệt |
| 400 | `User with ID 9 is not an active transport specialist.` | Người được chọn không phải chuyên viên, hoặc đã bị khóa |

### POST /api/bookings/{id}/reject — Từ chối đơn

Từ chối một đơn đang `Submitted`, bắt buộc ghi lý do. Khách nhận thông báo kèm lý do.

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
    "departureDate": "2026-11-06T23:00:00+00:00",
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
  "departureDate": "2026-11-06T23:00:00+00:00",
  "specialistUserId": 2,
  "specialistName": "Nguyễn Thị Mai",
  "clearanceNumber": null,
  "clearedAt": null,
  "createdAt": "2026-10-08T07:24:30+00:00",
  "passportNumber": "FEI-VN-2023-02",
  "issueReason": null,
  "checklist": [
    {
      "docTypeId": 2,
      "docTypeCode": "COGGINS_EIA",
      "docTypeName": "Chứng nhận xét nghiệm Coggins âm tính",
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
        "docTypeName": "Chứng nhận xét nghiệm Coggins âm tính",
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
      "docTypeName": "Giấy chứng nhận kiểm dịch xuất nhập khẩu",
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
      "docTypeName": "Chứng nhận xét nghiệm Coggins âm tính",
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
| `checklist[].requiredBy` | Nước nào yêu cầu, ví dụ `CN Import` |
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
| 400 | `'...' expires on 2026-11-01, before the departure date 2026-11-07. Reject it and ask for a valid document.` | Giấy hết hạn trước ngày khởi hành |
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
3. `POST /api/trips` để tạo bản nháp.
4. Chỉnh sửa nếu cần: `PUT /api/trips/{id}`, các API mốc lộ trình.
5. `POST /api/trips/{id}/submit` để trình duyệt.
6. Quản lý: `POST /api/trips/{id}/approve` hoặc `/reject`.

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
    "departureDate": "2026-11-06T23:00:00+00:00",
    "deliveryDate": "2026-11-09T11:00:00+00:00",
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

`bookingHorseId` là giá trị cần gửi khi xếp ngựa vào chuyến. `dossierStatus` cho biết hồ sơ của con ngựa đã xong chưa: vẫn xếp chuyến được khi hồ sơ chưa `Cleared`, nhưng chuyến sẽ không xuất phát được cho tới khi mọi hồ sơ `Cleared`.

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
| `notes` | chuỗi | Không | Ghi chú, tối đa 200 ký tự |

Mỗi phần tử của `checkpoints`:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `checkpointName` | chuỗi | Có | Tên mốc, tối đa 100 ký tự |
| `checkpointType` | chuỗi | Có | `Origin` (mốc đầu), `RestStop`, `BorderGate`, `Destination` (mốc cuối) |
| `address` | chuỗi | Có | Địa chỉ, tối đa 255 ký tự |
| `plannedTime` | ngày giờ | Có | Giờ dự kiến tới mốc |
| `mandatoryRestMinutes` | số | Không | Số phút bắt buộc nghỉ tại mốc (0–2880). Đoàn xe chưa nghỉ đủ thì chưa rời mốc được |

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
| 400 | `These horses are already on another trip: ...` | Ngựa đã được xếp vào chuyến khác |
| 400 | `Ground and air bookings cannot be combined on the same trip.` | Trộn ngựa của đơn đường bộ và đơn hàng không |
| 400 | `Vehicle 29B-888.99 carries at most 6 horse(s); this plan has 7.` | Quá sức chứa của xe |
| 400 | `Vehicle ... is already booked on trip ... during this period.` | Xe trùng lịch |
| 400 | `Two horses cannot share the same stall slot.` | Hai con ngựa cùng một ô chuồng |
| 400 | `A trip needs at least one driver.` | Đoàn xe không có ai là `Driver` |
| 400 | `4 horse(s) need at least 2 escort(s) (one per 3 horses); this plan has 1.` | Thiếu người áp tải: cứ 3 con ngựa cần 1 `Escort` |
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
| `checkpointType` | chuỗi | Có | `Origin`, `RestStop`, `BorderGate`, `Destination` |
| `address` | chuỗi | Có | Địa chỉ |
| `plannedTime` | ngày giờ | Có | Giờ dự kiến tới mốc |
| `mandatoryRestMinutes` | số | Không | Số phút bắt buộc nghỉ (0–2880) |
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
| `checkpointType` | chuỗi | Có | `Origin`, `RestStop`, `BorderGate`, `Destination` |
| `address` | chuỗi | Có | Địa chỉ |
| `plannedTime` | ngày giờ | Có | Giờ dự kiến tới mốc |
| `mandatoryRestMinutes` | số | Không | Số phút bắt buộc nghỉ (0–2880) |

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

### POST /api/trips/{id}/approve — Duyệt kế hoạch chuyến

Duyệt một chuyến đang `PendingApproval`. Chuyến thành `Scheduled`, đoàn xe nhận thông báo.

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
      "duty": "Chăm sóc ngựa trên đường"
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
| `horses[]` | Ngựa trên chuyến |
| `horses[].tripHorseId` | Mã "ngựa trên chuyến", dùng khi ghi nhật ký và báo sự cố |
| `horses[].stallSlotNumber` | Ô chuồng |
| `horses[].specialCareRequirements`, `photoUrl` | Yêu cầu chăm sóc và ảnh, để tài xế nhận diện |
| `horses[].dossierStatus` | Trạng thái hồ sơ; chuyến chỉ xuất phát được khi mọi con đều `Cleared` |
| `checkpoints[]` | Các mốc, đã sắp theo thứ tự đi |
| `checkpoints[].sequenceOrder` | Thứ tự |
| `checkpoints[].plannedTime`, `actualArrivalTime`, `actualDepartureTime` | Giờ dự kiến và giờ thực tế |
| `checkpoints[].status` | Trạng thái mốc (mục 3) |
| `checkpoints[].isActive` | `false` là mốc đã bị bỏ khi nắn tuyến; nên hiện mờ hoặc gạch ngang |
| `bookings[]` | Các đơn có ngựa trên chuyến: khách, điểm giao, `horseCountOnTrip`, và `handoverCompleted` (đơn đã có biên bản bàn giao chưa, mục 12) |

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
```

Thứ tự gọi thông thường của đoàn xe:

1. `GET /api/trips` để xem các chuyến của mình, `GET /api/trips/{id}` để xem chi tiết.
2. `POST /api/trips/{id}/start` khi xuất phát.
3. Với từng mốc theo thứ tự: `POST /api/checkpoints/{id}/arrive`, (cửa khẩu: `/clear`), `/depart`.
4. Bất cứ lúc nào trên đường: `POST /api/trips/{id}/welfare-logs` để ghi nhật ký cho từng con ngựa.

Quyền: chỉ người **có tên trong đoàn xe của chuyến** (và `Admin`) mới gọi được các API ghi; người khác nhận 403 `Only the crew assigned to this trip can update its progress.`

Các API xuất phát và check-in trả về **chi tiết chuyến** mới nhất (dạng của `GET /api/trips/{id}`, mục 9) và không cần body.

### POST /api/trips/{id}/start — Xuất phát

Bắt đầu chuyến: chuyến thành `InTransit`, mốc `Origin` được ghi là đã rời, khách và điều phối viên nhận thông báo. Bị chặn nếu còn con ngựa nào có hồ sơ chưa `Cleared`.

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

### POST /api/trips/{id}/welfare-logs — Ghi nhật ký thể trạng ngựa

Ghi một lần kiểm tra thể trạng của **một** con ngựa: nhiệt độ khoang, lượng nước uống, tình trạng ăn, mức căng thẳng, kèm ảnh nếu có. Ghi được từ lúc xuất phát tới khi bàn giao. Nếu ngựa bỏ ăn (`Refused`) hoặc kích động (`Agitated`), điều phối viên nhận cảnh báo.

- **Ai gọi được:** Đoàn xe của chuyến, Admin
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
| `nextCheckpointName`, `nextCheckpointPlannedTime` | Mốc sắp tới và giờ dự kiến |
| `delayMinutes` | Số phút đang trễ so với kế hoạch (0 là đúng giờ) |
| `isBehindSchedule` | `true` khi đang trễ; nên tô màu cảnh báo |
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
  "departureDate": "2026-11-06T23:00:00+00:00",
  "deliveryDate": "2026-11-09T11:00:00+00:00",
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
          "delayMinutes": 0
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
  "approvedByUserId": null,
  "approvedByName": null,
  "approvedAt": null,
  "resolvedAt": null
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
| `location` | chuỗi | Có | Vị trí xảy ra, tối đa 255 ký tự |
| `description` | chuỗi | Có | Mô tả, tối đa 2000 ký tự |
| `affectedTripHorseId` | số | Không | Con ngựa bị ảnh hưởng, lấy từ `horses[].tripHorseId` của chuyến |

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

Response `201`: `data` là sự cố vừa tạo, `status` là `Reported`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Incidents can only be reported while the trip is on the road (current status: Scheduled).` | Chuyến chưa xuất phát hoặc đã tới đích |
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

- **Ai gọi được:** FleetCoordinator, Admin
- **Dữ liệu gửi lên:** JSON

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã sự cố |

Các trường gửi lên:

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `proposedAction` | chuỗi | Có | Việc sẽ làm, tối đa 255 ký tự |
| `revisedRouteNotes` | chuỗi | Không | Ghi chú về tuyến đường mới, tối đa 2000 ký tự |
| `additionalCost` | số | Không | Chi phí phát sinh, không âm (mặc định 0) |

Ví dụ request:

```json
{
  "proposedAction": "Đổi xe dự phòng, nghỉ thêm tại trạm thú y Bằng Tường",
  "revisedRouteNotes": "Đã liên hệ trạm thú y Bằng Tường",
  "additionalCost": 650
}
```

Response `200`: sự cố với `status` là `PlanProposed`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `A plan can only be proposed for a reported incident or after a rejection (current status: PlanProposed).` | Đã có phương án đang chờ duyệt, hoặc sự cố đã được duyệt / đóng |

### POST /api/incidents/{id}/approve — Duyệt phương án

Duyệt phương án đang `PlanProposed`. Đoàn xe, người lập chuyến và khách nhận thông báo.

- **Ai gọi được:** LogisticsManager, Admin
- **Dữ liệu gửi lên:** không có (không cần body)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã sự cố |

Response `200`: sự cố với `status` là `Approved`, có `approvedByName`, `approvedAt`; `rejectionReason` thành `null`.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Only a proposed plan can be reviewed (current status: Reported).` | Chưa có phương án, hoặc phương án đã được xử lý |

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

### POST /api/incidents/{id}/apply-reroute — Nắn tuyến

Đổi lộ trình của chuyến sau khi phương án đã được duyệt: bỏ các mốc chưa tới và chèn các mốc mới. Chuyến chuyển sang `EmergencyRerouting`; đoàn xe và khách nhận thông báo. Gọi lại được nhiều lần khi sự cố còn `Approved`.

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

Ghi nhận sự cố đã xử lý xong. Chỉ đóng được sự cố có phương án đã duyệt (`Approved`). Nếu chuyến đang `EmergencyRerouting` và không còn sự cố nào khác đang xử lý, chuyến trở lại `InTransit`.

- **Ai gọi được:** FleetCoordinator, Admin
- **Dữ liệu gửi lên:** không có (không cần body)

Tham số trên đường dẫn:

| Tham số | Mô tả |
|---|---|
| `id` | Mã sự cố |

Response `200`: sự cố với `status` là `Resolved`, có `resolvedAt`; `tripStatus` cho biết trạng thái mới của chuyến.

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Only an incident with an approved plan can be resolved (current status: Reported).` | Phương án chưa được duyệt, hoặc sự cố đã đóng rồi |

---

## 12. Flow 6 – Bàn giao, đóng chuyến và báo cáo

Khi chuyến đã tới điểm giao (`ArrivedDestination`), đoàn xe **bàn giao** ngựa cho người nhận và lập **biên bản bàn giao điện tử** (e-POD): quét mã vi mạch từng con ngựa, ghi tình trạng, và lấy chữ ký vẽ tay của người nhận trên màn hình điện thoại. Một chuyến chở ngựa của nhiều đơn thì **mỗi đơn một biên bản**. Khi mọi đơn trên chuyến đã ký, quản lý **đóng chuyến**: hệ thống chốt số phút trễ, điểm KPI và chi phí thực tế.

Vòng đời:

```
ArrivedDestination ──(đoàn xe lập biên bản cho từng đơn)──> đủ biên bản ──(quản lý đóng chuyến)──> Completed

Khi chuyến Completed: xe được giải phóng; đơn nào đã giao hết ngựa thì chuyển sang Completed.
```

Thứ tự gọi thông thường:

1. Tài xế: `GET /api/trips/{id}` để xem `bookings[]` (các đơn phải giao) và `horses[]` (mã vi mạch từng con).
2. Tài xế: với mỗi đơn, `POST /api/trips/{tripId}/handover`.
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
| `horses[]` | Các con ngựa của đơn được giao trong biên bản |

### POST /api/trips/{tripId}/handover — Lập biên bản bàn giao (e-POD)

Lập biên bản bàn giao cho **một** đơn trên chuyến đã tới đích. Mã vi mạch quét được phải khớp đúng các con ngựa của đơn đó trên chuyến: thiếu một con hoặc thừa một mã lạ đều bị từ chối. Khách nhận thông báo; khi đây là biên bản cuối cùng của chuyến, quản lý được báo để đóng chuyến.

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

Response `201`: `data` là biên bản vừa lập (dạng như trên).

Lỗi thường gặp:

| HTTP | `message` | Khi nào |
|---|---|---|
| 400 | `Horses can only be handed over after the trip has arrived at the destination (current status: InTransit).` | Chuyến chưa tới mốc `Destination` |
| 400 | `Microchip check failed. Not scanned: Golden Pegasus (Kim Mã) (982000412345672).` | Thiếu mã vi mạch của một con ngựa; `message` nêu tên con còn thiếu |
| 400 | `Microchip check failed. These microchips do not belong to booking BKG-2026-0002 on this trip: 999.` | Có mã vi mạch không thuộc đơn này |
| 400 | `Booking BKG-2026-0002 has already been handed over on trip TRP-2026-0003.` | Đơn này đã có biên bản trên chuyến |
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

Đóng một chuyến đã tới đích sau khi **mọi đơn trên chuyến đã có biên bản bàn giao**. Hệ thống tự tính và lưu kết quả; không sửa lại được.

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
| 400 | `The trip cannot be closed: handover is not signed for BKG-2026-0002.` | Còn đơn chưa có biên bản; `message` nêu mã đơn |
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

Một loại giấy tờ có dạng:

```json
{
  "docTypeId": 1,
  "code": "HORSE_PASSPORT",
  "name": "Hộ chiếu ngựa quốc tế (FEI Passport)",
  "description": "Hộ chiếu định danh cá thể do FEI cấp",
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
    "description": "Lịch tiêm phòng đầy đủ trong 6 tháng",
    "isMandatory": true
  },
  {
    "docTypeId": 2,
    "code": "COGGINS_EIA",
    "name": "Chứng nhận xét nghiệm Coggins âm tính",
    "description": "Xét nghiệm thiếu máu truyền nhiễm ngựa trong 30 ngày",
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

## 14. Những phần chưa có API

Các phần dưới đây backend chưa làm. Frontend có thể dựng giao diện trước nhưng chưa nối được.

| Phần | Ghi chú |
|---|---|
| Chuông thông báo (danh sách, đếm chưa đọc, đánh dấu đã đọc) | Backend đã tạo thông báo cho mọi sự kiện; chưa có API để đọc. Riêng khách hàng đã nhận được email ở các sự kiện chính (mục 4) |
| Quản lý phương tiện | Hiện chỉ có `GET /api/trips/available-vehicles` |
| Quy định giấy tờ theo quốc gia, bảng giá | Dữ liệu có sẵn và đang được dùng để tính; chưa có màn hình quản trị |
| API trả danh sách enum cho dropdown | Tạm dùng bảng ở mục 3 |

Tiến độ từng phần xem ở [TASKS.md](TASKS.md).

---

## 15. Bảng tra nhanh mọi endpoint

Tổng cộng 82 endpoint. Chi tiết từng cái xem ở mục tương ứng.

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

**7. Flow 1 – Đơn vận chuyển**

| Method | Đường dẫn | Việc | Ai gọi được |
|---|---|---|---|
| POST | `/api/bookings/quote-preview` | Xem giá trước khi gửi đơn | Mọi người đã đăng nhập |
| POST | `/api/bookings` | Gửi đơn vận chuyển | Customer |
| GET | `/api/bookings` | Danh sách đơn | Customer, LogisticsManager, TransportSpecialist, FleetCoordinator, Admin |
| GET | `/api/bookings/{id}` | Chi tiết đơn | Như danh sách đơn |
| POST | `/api/bookings/{id}/cancel` | Khách hủy đơn | Customer (chủ đơn) |
| POST | `/api/bookings/{id}/approve` | Duyệt đơn | LogisticsManager, Admin |
| POST | `/api/bookings/{id}/reject` | Từ chối đơn | LogisticsManager, Admin |
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
| POST | `/api/trips/{id}/welfare-logs` | Ghi nhật ký thể trạng ngựa | Đoàn xe của chuyến, Admin |
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
| POST | `/api/incidents/{id}/resolve` | Đóng sự cố | FleetCoordinator, Admin |

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
