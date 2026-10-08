# Hướng dẫn dùng API cho Frontend

Tài liệu này mô tả cách gọi API của hệ thống vận chuyển ngựa đua: quy ước chung, đăng nhập, và các endpoint theo từng luồng nghiệp vụ.

| | |
|---|---|
| Bản deploy | `https://racehorsetransportbackend-production.up.railway.app` |
| Swagger (thử trực tiếp, xem đủ schema) | `https://racehorsetransportbackend-production.up.railway.app/swagger` |
| Chạy ở máy | `http://localhost:5080` |

Swagger luôn là nguồn chính xác nhất về tên trường và kiểu dữ liệu. Tài liệu này giải thích những thứ Swagger không nói: thứ tự gọi, ai được gọi, và quy tắc nghiệp vụ.

**Mục lục**

1. [Quy ước chung](#1-quy-ước-chung)
2. [Đăng nhập và token](#2-đăng-nhập-và-token)
3. [Giá trị cố định (enum)](#3-giá-trị-cố-định-enum)
4. [Upload file](#4-upload-file)
5. [Tài khoản và người dùng](#5-tài-khoản-và-người-dùng)
6. [Ngựa](#6-ngựa)
7. [Flow 1 – Đơn vận chuyển](#7-flow-1--đơn-vận-chuyển)
8. [Flow 2 – Hồ sơ kiểm dịch](#8-flow-2--hồ-sơ-kiểm-dịch)
9. [Flow 3 – Lập kế hoạch chuyến](#9-flow-3--lập-kế-hoạch-chuyến)
10. [Flow 4 – Chạy chuyến và theo dõi](#10-flow-4--chạy-chuyến-và-theo-dõi)
11. [Danh mục](#11-danh-mục)
12. [Chưa có](#12-chưa-có)

---

## 1. Quy ước chung

### Khuôn response

Mọi endpoint, kể cả khi lỗi, trả về cùng một khuôn:

```json
{
  "success": true,
  "message": "Booking submitted successfully",
  "data": { },
  "errors": null
}
```

Endpoint danh sách có phân trang thêm `pagination`:

```json
{
  "success": true,
  "message": "Bookings retrieved successfully",
  "data": [ ],
  "errors": null,
  "pagination": { "page": 1, "pageSize": 10, "totalItems": 37, "totalPages": 4 }
}
```

- Tên trường là **camelCase**.
- `message` là tiếng Anh, hiển thị thẳng cho người dùng được.
- Tên các dòng báo giá, tên loại giấy tờ là tiếng Việt vì lấy từ dữ liệu.

### Mã lỗi

| HTTP | Khi nào | Frontend nên làm gì |
|---|---|---|
| 400 | Dữ liệu sai định dạng, hoặc vi phạm quy tắc nghiệp vụ | Hiện `message`; nếu `message` là `Validation failed` thì hiện danh sách trong `errors` |
| 401 | Chưa đăng nhập, token sai hoặc hết hạn | Thử làm mới token (mục 2); không được thì về trang đăng nhập |
| 403 | Đã đăng nhập nhưng không có quyền | Hiện `message`, không thử lại |
| 404 | Không tìm thấy | Hiện `message` |
| 500 | Lỗi server | Báo lỗi chung, thử lại sau |

Ví dụ lỗi validation:

```json
{
  "success": false,
  "message": "Validation failed",
  "data": null,
  "errors": ["Email format is invalid.", "Password is required."]
}
```

### Tham số của endpoint danh sách

| Tham số | Ý nghĩa | Ví dụ |
|---|---|---|
| `search` | Tìm theo từ khóa, không phân biệt hoa thường | `search=golden` |
| `sort` | Tên trường; thêm `-` phía trước để giảm dần; nhiều trường cách nhau dấu phẩy | `sort=-createdAt,bookingCode` |
| `page` | Trang, bắt đầu từ 1 | `page=2` |
| `size` | Số dòng mỗi trang, từ 1 đến 100, mặc định 10 | `size=20` |

Mỗi danh sách có thêm bộ lọc riêng, ghi ở từng mục bên dưới.

### Ngày giờ

- Gửi lên theo ISO 8601 **kèm múi giờ**: `2026-11-07T06:00:00+07:00`.
- Server trả về giờ **UTC**: `2026-11-06T23:00:00+00:00`. Frontend tự đổi sang giờ địa phương khi hiển thị (`new Date(value)` làm đúng việc này).
- Trường chỉ có ngày (ngày sinh của ngựa, hạn giấy tờ) dùng dạng `2027-05-20`.

### CORS

Server chỉ nhận request từ các địa chỉ frontend đã khai báo. Hiện đang cho phép `http://localhost:5173`. Khi frontend có domain thật, báo backend để thêm vào biến `Cors__AllowedOrigins__N` trên Railway.

---

## 2. Đăng nhập và token

### Các endpoint

| Method | Đường dẫn | Mô tả |
|---|---|---|
| POST | `/api/auth/register` | Khách hàng tự đăng ký. Tài khoản nhân sự do Admin tạo |
| POST | `/api/auth/login` | Đăng nhập |
| POST | `/api/auth/refresh` | Đổi refresh token lấy cặp token mới |
| POST | `/api/auth/logout` | Hủy refresh token |
| GET | `/api/auth/me` | Hồ sơ người đang đăng nhập |
| PUT | `/api/auth/me` | Sửa họ tên, số điện thoại |
| PUT | `/api/auth/change-password` | Đổi mật khẩu (cần mật khẩu hiện tại) |

Đăng nhập:

```http
POST /api/auth/login
Content-Type: application/json

{ "email": "jane.smith@racehorseowner.com", "password": "..." }
```

```json
{
  "success": true,
  "message": "Logged in successfully",
  "data": {
    "accessToken": "eyJ...",
    "accessTokenExpiresAt": "2026-10-08T05:00:00+00:00",
    "refreshToken": "kJ8s...",
    "user": {
      "userId": 5,
      "fullName": "Jane Smith",
      "email": "jane.smith@racehorseowner.com",
      "phoneNumber": "+84988111222",
      "role": "Customer",
      "isActive": true,
      "createdAt": "2026-10-08T03:00:00+00:00"
    }
  },
  "errors": null
}
```

`register`, `login` và `refresh` trả cùng một dạng `data`.

### Cách dùng token

- Gửi access token trong mọi request cần đăng nhập: `Authorization: Bearer <accessToken>`.
- Access token sống **60 phút**, refresh token sống **7 ngày**.
- Refresh token **chỉ dùng được một lần**: mỗi lần gọi `/refresh` nhận về refresh token mới và phải lưu đè cái cũ.
- Dùng `user.role` để quyết định hiển thị màn hình nào. Role cũng nằm trong access token (claim `role`), cùng `sub` (id), `email`, `name`.

Lưu ý ở giai đoạn hiện tại: mỗi lần backend deploy lại, mọi refresh token mất hiệu lực và người dùng phải đăng nhập lại.

### Mẫu xử lý token (axios)

```js
import axios from "axios";

const api = axios.create({ baseURL: import.meta.env.VITE_API_URL });

api.interceptors.request.use((config) => {
  const token = localStorage.getItem("accessToken");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

let refreshing = null; // gộp nhiều request cùng hết hạn vào một lần refresh

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

### Vai trò

| `role` | Ai | Màn hình chính |
|---|---|---|
| `Customer` | Chủ ngựa / CLB | Ngựa, đặt chuyến, nộp giấy tờ, theo dõi |
| `LogisticsManager` | Quản lý điều hành | Duyệt đơn, duyệt kế hoạch chuyến |
| `TransportSpecialist` | Chuyên viên thủ tục | Hồ sơ kiểm dịch được giao |
| `FleetCoordinator` | Điều phối đội xe | Xếp chuyến, bảng theo dõi |
| `DriverEscort` | Tài xế / áp tải | Chuyến của mình, check-in, nhật ký ngựa (mobile) |
| `Admin` | Quản trị | Tài khoản, danh mục |

`DriverEscort` là role tài khoản. Vai trò trên từng chuyến (lái xe hay áp tải) nằm ở `crew[].crewRole` của chuyến đó.

---

## 3. Giá trị cố định (enum)

Mọi trạng thái và loại đều là chuỗi, phân biệt hoa thường.

| Trường | Giá trị |
|---|---|
| Trạng thái đơn (`status`) | `Submitted`, `Approved`, `Completed`, `Rejected`, `Expired`, `Cancelled` |
| Trạng thái ngựa trong đơn | `Pending`, `Approved`, `Cancelled` |
| `transportMode` | `Ground`, `Air` |
| `stallClass` | `Shared`, `Comfort`, `Private` |
| Giới tính ngựa (`gender`) | `Stallion`, `Mare`, `Gelding` |
| Trạng thái hồ sơ | `Draft`, `AwaitingDocs`, `Reviewing`, `SubmittedToAuthorities`, `Cleared`, `Issue` |
| Trạng thái giấy tờ | `Pending`, `Approved`, `Rejected` |
| Trạng thái dòng checklist (`state`) | `Missing`, `Pending`, `Approved`, `Rejected` |
| Trạng thái chuyến (`overallStatus`) | `Draft`, `PendingApproval`, `Scheduled`, `InTransit`, `EmergencyRerouting`, `ArrivedDestination`, `Completed`, `Cancelled` |
| `crewRole` | `Driver`, `Escort` |
| `checkpointType` | `Origin`, `RestStop`, `BorderGate`, `Destination` |
| Trạng thái mốc | `Pending`, `Arrived`, `Cleared`, `Departed`, `Cancelled` |
| `feedStatus` | `Normal`, `Reduced`, `Refused` |
| `stressLevel` | `Calm`, `MildStress`, `Agitated` |
| Loại phương tiện (`assetType`) | `HorseTruck_AirSuspension`, `HorseVan`, `AirStall` |
| Loại file (`category`) | `documents`, `horses`, `welfare`, `signatures` |

Trạng thái đơn `Assigned` còn trong database nhưng không được dùng.

---

## 4. Upload file

File gửi **kèm ngay trong request nghiệp vụ**. Ba request có file đều dùng `multipart/form-data` thay cho JSON:

| Request | Trường file | Bắt buộc | Loại file |
|---|---|---|---|
| `POST /api/horses`, `PUT /api/horses/{id}` | `photo` | Không | `jpg`, `jpeg`, `png` |
| `POST /api/dossiers/{id}/documents` | `file` | Có | `pdf`, `jpg`, `jpeg`, `png` |
| `POST /api/trips/{id}/welfare-logs` | `photo` | Không | `jpg`, `jpeg`, `png` |

Tối đa 10 MB mỗi file. Nội dung file phải khớp với đuôi file. Các trường còn lại gửi thành trường form cùng tên như trước (ngày viết `2027-01-15`, số thập phân dùng dấu chấm). Gửi JSON vào ba request này sẽ nhận `415`.

```js
const form = new FormData();
form.append("name", "Silver Wind");
form.append("microchipNumber", "982000412345699");
// ... các trường khác
form.append("photo", fileInput.files[0]); // bỏ dòng này nếu không có ảnh

const { data } = (await api.post("/api/horses", form)).data;
// data.photoUrl -> "horses/2026/10/9b1c...e4.png"
```

Không tự đặt header `Content-Type`; để trình duyệt / axios tự thêm kèm `boundary`.

| Method | Đường dẫn | Mô tả |
|---|---|---|
| GET | `/api/files/url?path=...` | Lấy link xem mới cho một `path` |

**Hiển thị file:** các response nghiệp vụ trả về `path` (ở trường `fileUrl`, `photoUrl`), không phải link xem được. Gọi `GET /api/files/url?path=...` để lấy link có hạn 60 phút rồi mới gắn vào `<img>` hoặc mở PDF. Không lưu link này lâu dài.

Dữ liệu mẫu có sẵn chứa vài giá trị bắt đầu bằng `https://`; với các giá trị đó dùng thẳng, không gọi `/api/files/url`.

---

## 5. Tài khoản và người dùng

| Method | Đường dẫn | Ai được gọi | Mô tả |
|---|---|---|---|
| GET | `/api/users` | Admin | Danh sách. Lọc: `role`, `isActive` |
| GET | `/api/users/{id}` | Admin | Chi tiết |
| POST | `/api/users` | Admin | Tạo tài khoản nhân sự (mọi role trừ `Customer`) |
| PUT | `/api/users/{id}` | Admin | Sửa họ tên, số điện thoại, role |
| PATCH | `/api/users/{id}/active` | Admin | Khóa / mở: `{ "isActive": false }` |
| PUT | `/api/users/{id}/password` | Admin | Đặt lại mật khẩu: `{ "newPassword": "..." }` |
| GET | `/api/users/staff?role=` | Manager, Specialist, Coordinator, Admin | Nhân sự đang hoạt động, dùng cho dropdown chọn người |

Mật khẩu từ 8 đến 64 ký tự. Admin không tự khóa hoặc tự đổi role của chính mình. Không đổi được tài khoản giữa `Customer` và role nhân sự.

---

## 6. Ngựa

| Method | Đường dẫn | Ai được gọi | Mô tả |
|---|---|---|---|
| GET | `/api/horses` | Mọi người đã đăng nhập | Khách: chỉ ngựa của mình. Nhân sự: tất cả; lọc thêm `ownerUserId`, `includeInactive` |
| GET | `/api/horses/{id}` | Chủ ngựa, nhân sự | Chi tiết |
| POST | `/api/horses` | Customer | Thêm ngựa |
| PUT | `/api/horses/{id}` | Chủ ngựa | Sửa |
| DELETE | `/api/horses/{id}` | Chủ ngựa | Xóa mềm; không xóa được ngựa đang trong đơn chưa kết thúc |

`POST` và `PUT` dùng `multipart/form-data` (mục 4):

| Trường | Ví dụ | Ghi chú |
|---|---|---|
| `name` | `Silver Wind` | Bắt buộc |
| `microchipNumber` | `982000412345699` | Bắt buộc, không trùng trên toàn hệ thống |
| `passportNumber` | `FEI-VN-2024-09` | Bắt buộc, không trùng trên toàn hệ thống |
| `breed` | `Thoroughbred` | Bỏ trống thì mặc định `Thoroughbred` |
| `gender` | `Gelding` | Bắt buộc |
| `dateOfBirth` | `2019-05-20` | Bắt buộc |
| `color` | `Xám` | Bắt buộc |
| `specialCareRequirements` | `Dễ say xe, cần cỏ tươi` | |
| `photo` | (file ảnh) | Không bắt buộc |
| `removePhoto` | `true` | Chỉ có ở `PUT` |

Ảnh khi sửa (`PUT`): gửi `photo` để thay ảnh; không gửi `photo` thì giữ ảnh đang có; gửi `removePhoto=true` để xóa ảnh.

Response trả `photoUrl` là `path` của ảnh (xem "Hiển thị file" ở mục 4).

---

## 7. Flow 1 – Đơn vận chuyển

```
Submitted ──duyệt──> Approved ──(mọi ngựa đã giao xong)──> Completed
    │ ├──từ chối──> Rejected
    │ ├──khách hủy──> Cancelled
    │ └──quá 48 giờ không ai duyệt──> Expired
```

| Method | Đường dẫn | Ai được gọi | Mô tả |
|---|---|---|---|
| POST | `/api/bookings/quote-preview` | Mọi người đã đăng nhập | Tính giá, không lưu gì |
| POST | `/api/bookings` | Customer | Gửi đơn |
| GET | `/api/bookings` | Customer, Manager, Specialist, Coordinator, Admin | Danh sách. Lọc: `status`, `departureFrom`, `departureTo`, `customerUserId` (nhân sự) |
| GET | `/api/bookings/{id}` | Như trên | Chi tiết |
| POST | `/api/bookings/{id}/cancel` | Customer (chủ đơn) | Hủy khi đơn còn `Submitted` |
| POST | `/api/bookings/{id}/approve` | Manager, Admin | Duyệt, kèm chọn chuyên viên |
| POST | `/api/bookings/{id}/reject` | Manager, Admin | Từ chối: `{ "reason": "..." }` |
| POST | `/api/bookings/{id}/reassign-specialist` | Manager, Admin | Đổi chuyên viên: `{ "specialistUserId": 9 }` |

Ai thấy đơn nào: khách thấy đơn của mình; chuyên viên thấy đơn mình phụ trách; Manager, Coordinator, Admin thấy tất cả; tài xế không xem đơn.

### Xem giá trước khi gửi

```json
{
  "transportMode": "Ground",
  "dropoffCountryCode": "CN",
  "distanceKm": 900,
  "requiresClimateControl": false,
  "declaredValue": null,
  "departureDate": "2026-11-07T06:00:00+07:00",
  "deliveryDate": "2026-11-09T06:00:00+07:00",
  "horses": [ { "stallClass": "Shared" }, { "stallClass": "Private" } ]
}
```

`data` trả về (phần `lines` đã rút gọn, thực tế có đủ các dòng cước, phụ phí, phí thủ tục, áp tải):

```json
{
  "lines": [
    { "code": "FREIGHT_SHARED", "name": "Cước vận chuyển - Chuồng ghép (Shared) - 3 ngựa / pallet", "quantity": 1, "unitPrice": 900, "amount": 900 },
    { "code": "DISC_VOL_2", "name": "Chiết khấu 2-3 ngựa", "quantity": 1, "unitPrice": -144, "amount": -144 }
  ],
  "total": 5894.88,
  "currencyCode": "USD",
  "horseCount": 2,
  "groomCount": 1,
  "transitDays": 2,
  "isExpress": false
}
```

- `distanceKm` bắt buộc với `Ground`, bỏ qua với `Air`.
- `declaredValue` bỏ trống nghĩa là không mua bảo hiểm.
- `isExpress` do server tự xác định (khởi hành dưới 7 ngày thì có phụ phí gấp); client không gửi.
- Dòng có `amount` âm là chiết khấu.
- Tuyến chưa có trong bảng giá sẽ trả 400 `No freight rate is configured...`.

### Gửi đơn

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
  "requiresClimateControl": false,
  "declaredValue": null,
  "specialInstructions": "Giữ cabin 17 độ",
  "horses": [
    { "horseId": 1, "stallClass": "Shared", "notes": "Chuồng bên trái" },
    { "horseId": 2, "stallClass": "Private" }
  ]
}
```

Server tự tính giá và lưu kèm đơn; số tiền client gửi lên (nếu có) bị bỏ qua. Bị từ chối (400) khi: ngựa không phải của khách, ngựa đang nằm trong đơn khác trùng thời gian, ngày khởi hành ở quá khứ, ngày giao không sau ngày đi.

### Chi tiết đơn

Ngoài các trường của danh sách, chi tiết có thêm:

- `quoteLines`: các dòng báo giá đã lưu.
- `horses[]`: mỗi con ngựa kèm `dossierId`, `dossierCode`, `dossierStatus` (có sau khi đơn được duyệt) và `trips[]` — các chuyến con ngựa đó được xếp lên.

Một đơn có thể đi nhiều chuyến và một chuyến có thể chở nhiều đơn, nên luôn đọc chuyến theo từng con ngựa.

### Duyệt đơn

```json
{ "specialistUserId": 2 }
```

Lấy danh sách chuyên viên từ `GET /api/users/staff?role=TransportSpecialist`. Khi duyệt, hệ thống tự mở một hồ sơ kiểm dịch cho mỗi con ngựa.

---

## 8. Flow 2 – Hồ sơ kiểm dịch

Mỗi con ngựa trong đơn có một hồ sơ riêng.

```
Draft ──yêu cầu nộp──> AwaitingDocs <──────> Reviewing ──nộp cơ quan──> SubmittedToAuthorities ──> Cleared
                            ▲     (khách nộp / bị từ chối)                       │
                            └──────────── Issue <───────────────────────────────┘
                                   (bị từ chối 3 lần, hoặc cơ quan không chấp nhận)
```

| Method | Đường dẫn | Ai được gọi | Mô tả |
|---|---|---|---|
| GET | `/api/dossiers` | Customer, Manager, Specialist, Coordinator, Admin | Danh sách. Lọc: `status`, `bookingId` |
| GET | `/api/dossiers/{id}` | Như trên | Chi tiết kèm checklist |
| POST | `/api/dossiers/{id}/request-documents` | Chuyên viên phụ trách | Yêu cầu khách nộp: `{ "note": "..." }` (không bắt buộc) |
| POST | `/api/dossiers/{id}/documents` | Chủ ngựa, chuyên viên phụ trách | Nộp hoặc nộp lại một giấy |
| POST | `/api/documents/{id}/approve` | Chuyên viên phụ trách | Duyệt một giấy |
| POST | `/api/documents/{id}/reject` | Chuyên viên phụ trách | Từ chối: `{ "correctionNote": "..." }` |
| POST | `/api/dossiers/{id}/submit-to-authorities` | Chuyên viên phụ trách | Nộp cơ quan chức năng |
| POST | `/api/dossiers/{id}/clear` | Chuyên viên phụ trách | Ghi nhận thông quan: `{ "clearanceNumber": "..." }` |
| POST | `/api/dossiers/{id}/issue` | Chuyên viên phụ trách | Báo có vấn đề: `{ "reason": "..." }` |

Mọi endpoint ghi ở trên trả về chi tiết hồ sơ sau khi thay đổi, nên không cần gọi lại GET.

### Chi tiết hồ sơ

```json
{
  "dossierId": 3,
  "dossierCode": "DOS-2026-0003",
  "status": "Reviewing",
  "horseName": "Red Flash (Tia Chớp Đỏ)",
  "bookingCode": "BKG-2026-0002",
  "pickupCountryCode": "VN",
  "dropoffCountryCode": "CN",
  "specialistName": "Nguyễn Thị Mai",
  "requiredCount": 4,
  "approvedCount": 1,
  "isReadyToSubmit": false,
  "checklist": [
    {
      "docTypeId": 2,
      "docTypeCode": "COGGINS_EIA",
      "docTypeName": "Chứng nhận xét nghiệm Coggins âm tính",
      "isMandatory": true,
      "validityDays": 30,
      "regulationNote": "Xét nghiệm EIA âm tính trong 30 ngày",
      "requiredBy": ["CN Import"],
      "state": "Missing",
      "latestDocument": null
    }
  ],
  "documents": [ ]
}
```

- `checklist` là danh sách giấy tờ cần có, ghép từ quy định xuất khẩu của nước đi và nhập khẩu của nước đến. Dựng màn hình nộp giấy tờ từ danh sách này.
- `state` của từng dòng: `Missing` (chưa nộp), `Pending` (chờ duyệt), `Approved`, `Rejected`.
- `latestDocument` chứa `documentId` (dùng để duyệt / từ chối), `fileUrl` (là `path`), `correctionNote`, `rejectionCount`.
- `isReadyToSubmit` là `true` khi mọi giấy bắt buộc đã `Approved`; dùng để bật nút "Nộp cơ quan chức năng".

### Nộp giấy tờ

`POST /api/dossiers/{id}/documents`, `multipart/form-data` (mục 4):

| Trường | Ví dụ | Ghi chú |
|---|---|---|
| `docTypeId` | `2` | Bắt buộc |
| `documentNumber` | `COGGINS-LAB-2026-888` | |
| `expiryDate` | `2027-01-15` | |
| `file` | (file pdf / ảnh) | Bắt buộc |

Quy tắc:

- Mỗi loại giấy trong một hồ sơ chỉ có một bản. Nộp lại là thay file của bản đó.
- Giấy đã `Approved` không thay được.
- Giấy hết hạn trước ngày khởi hành của đơn sẽ không duyệt được.
- Một giấy bị từ chối 3 lần: hồ sơ chuyển `Issue` và khách không nộp lại được nữa. Chuyên viên bấm "yêu cầu nộp" để mở lại hồ sơ và nộp hộ khách.
- Chỉ nộp được khi hồ sơ ở `Draft`, `AwaitingDocs`, `Reviewing`.

---

## 9. Flow 3 – Lập kế hoạch chuyến

```
Draft ──trình──> PendingApproval ──duyệt──> Scheduled
  ▲                    │
  └──────trả về────────┘   (bị trả lần thứ 3 thì chuyến thành Cancelled)
```

| Method | Đường dẫn | Ai được gọi | Mô tả |
|---|---|---|---|
| GET | `/api/trips/unplanned-horses` | Coordinator, Manager, Admin | Hàng chờ: ngựa của đơn đã duyệt chưa lên chuyến. Lọc: `transportMode`, `bookingId` |
| GET | `/api/trips/available-vehicles?from=&to=&transportMode=` | Như trên | Xe rảnh trong khoảng thời gian |
| GET | `/api/trips/available-crew?from=&to=` | Như trên | Tài xế / áp tải rảnh |
| GET | `/api/trips` | Nhân sự | Danh sách. Tài xế chỉ thấy chuyến của mình. Lọc: `status`, `startFrom`, `startTo` |
| GET | `/api/trips/{id}` | Nhân sự | Chi tiết |
| POST | `/api/trips` | Coordinator, Admin | Tạo bản nháp |
| PUT | `/api/trips/{id}` | Người lập chuyến, Admin | Sửa bản nháp |
| POST | `/api/trips/{id}/checkpoints` | Người lập chuyến, Admin | Thêm mốc |
| PUT | `/api/trips/{id}/checkpoints/{checkpointId}` | Như trên | Sửa mốc |
| DELETE | `/api/trips/{id}/checkpoints/{checkpointId}` | Như trên | Xóa mốc |
| PUT | `/api/trips/{id}/checkpoints/order` | Như trên | Đổi thứ tự: `{ "checkpointIds": [5, 7, 6, 8] }` |
| POST | `/api/trips/{id}/submit` | Người lập chuyến, Admin | Trình duyệt |
| POST | `/api/trips/{id}/approve` | Manager, Admin | Duyệt |
| POST | `/api/trips/{id}/reject` | Manager, Admin | Trả về: `{ "reason": "..." }` |
| POST | `/api/trips/{id}/cancel` | Người lập chuyến, Manager, Admin | Hủy chuyến chưa khởi hành |

Mọi endpoint ghi trả về chi tiết chuyến sau khi thay đổi.

### Tạo hoặc sửa bản nháp

```json
{
  "vehicleId": 1,
  "plannedStartDate": "2026-11-07T06:00:00+07:00",
  "plannedEndDate": "2026-11-09T18:00:00+07:00",
  "plannedCost": 4200,
  "crew": [
    { "userId": 4, "crewRole": "Driver", "isLead": true },
    { "userId": 8, "crewRole": "Escort", "duty": "Chăm sóc ngựa trên đường" }
  ],
  "horses": [
    { "bookingHorseId": 3, "stallSlotNumber": 1 },
    { "bookingHorseId": 4, "stallSlotNumber": 2 }
  ],
  "checkpoints": [
    { "checkpointName": "Trại Ba Vì", "checkpointType": "Origin", "address": "Ba Vì, Hà Nội", "plannedTime": "2026-11-07T06:00:00+07:00", "mandatoryRestMinutes": 0 },
    { "checkpointName": "Cửa khẩu Hữu Nghị", "checkpointType": "BorderGate", "address": "Lạng Sơn", "plannedTime": "2026-11-07T12:00:00+07:00", "mandatoryRestMinutes": 120 },
    { "checkpointName": "Trường đua Tùng Hóa", "checkpointType": "Destination", "address": "Quảng Châu", "plannedTime": "2026-11-09T18:00:00+07:00", "mandatoryRestMinutes": 0 }
  ]
}
```

- `horses[].bookingHorseId` lấy từ hàng chờ `unplanned-horses`. Có thể chọn ngựa của nhiều đơn.
- Khi sửa (`PUT`): `crew` và `horses` bị thay toàn bộ. `checkpoints` bỏ trống thì giữ nguyên các mốc hiện có; có giá trị thì thay toàn bộ.
- Thêm một mốc: gửi cùng dạng một phần tử của `checkpoints`, thêm `"position": 2` để chèn vào vị trí thứ 2 (bỏ trống thì thêm vào cuối).

Quy tắc hệ thống kiểm tra (vi phạm thì trả 400 kèm lý do cụ thể):

- Ngựa thuộc đơn `Approved`, chưa nằm trên chuyến khác; không trộn đơn `Ground` với `Air`.
- Xe không bảo trì, đúng loại (`Air` dùng `AirStall`), đủ sức chứa, không trùng lịch; mỗi ô chuồng một con.
- Ít nhất một `Driver`; một `Escort` cho mỗi 3 con ngựa; không ai trùng lịch. Một người được vừa lái vừa áp tải.
- Khi trình duyệt: mốc đầu là `Origin`, mốc cuối là `Destination`, thời gian tăng dần và nằm trong khoảng kế hoạch của chuyến.

Bản nháp đã giữ chỗ xe, người và ngựa ngay từ lúc tạo.

### Chi tiết chuyến

Các phần chính của response:

- `crew[]`: `userId`, `fullName`, `phoneNumber`, `crewRole`, `isLead`, `duty`.
- `horses[]`: `tripHorseId`, `stallSlotNumber`, `horseName`, `microchipNumber`, `specialCareRequirements`, `bookingCode`, `dossierStatus`.
- `checkpoints[]`: `checkpointId`, `sequenceOrder`, loại, thời gian kế hoạch và thực tế, `status`, `isActive`.
- `bookings[]`: các đơn có ngựa trên chuyến, kèm `customerName`, `dropoffAddress`, `horseCountOnTrip`, `handoverCompleted`.
- `planRejectionCount`, `planRejectionReason`: lý do bị trả về gần nhất.

---

## 10. Flow 4 – Chạy chuyến và theo dõi

```
Scheduled ──xuất phát──> InTransit ──tới mốc Destination──> ArrivedDestination
```

Mỗi mốc: `Pending` → `Arrived` → (`Cleared`, chỉ cửa khẩu) → `Departed`.

### Dành cho đoàn xe (mobile)

| Method | Đường dẫn | Mô tả |
|---|---|---|
| GET | `/api/trips` | Các chuyến mình có tên trong đoàn |
| GET | `/api/trips/{id}` | Lịch trình, ngựa phụ trách, các mốc |
| POST | `/api/trips/{id}/start` | Xuất phát |
| POST | `/api/checkpoints/{id}/arrive` | Đã tới mốc |
| POST | `/api/checkpoints/{id}/clear` | Đã thông quan (chỉ mốc `BorderGate`) |
| POST | `/api/checkpoints/{id}/depart` | Rời mốc |
| POST | `/api/trips/{id}/welfare-logs` | Ghi nhật ký thể trạng một con ngựa |
| GET | `/api/trips/{id}/welfare-logs?tripHorseId=` | Xem nhật ký của chuyến |

Chỉ người có tên trong đoàn xe của chuyến mới gọi được các endpoint ghi (người khác nhận 403). Các endpoint check-in không cần body.

Quy tắc, kèm thông báo lỗi tương ứng để hiển thị:

- Không xuất phát được khi còn ngựa có hồ sơ chưa `Cleared`; `message` nêu tên từng con.
- Phải đi đúng thứ tự: chưa rời mốc trước thì không ghi được tới mốc sau.
- Cửa khẩu phải `clear` rồi mới `depart` được.
- Chưa nghỉ đủ `mandatoryRestMinutes` thì không rời được; `message` cho biết còn bao nhiêu phút.
- Tới mốc `Destination` thì chuyến tự chuyển `ArrivedDestination`; không có bước rời mốc này.

Ghi nhật ký:

`POST /api/trips/{id}/welfare-logs`, `multipart/form-data` (mục 4):

| Trường | Ví dụ |
|---|---|
| `tripHorseId` | `3` |
| `checkpointId` | `8` |
| `cabinTemp` | `17.5` |
| `waterIntakeLiters` | `10` |
| `feedStatus` | `Normal` |
| `stressLevel` | `Calm` |
| `notes` | `Ăn uống bình thường` |
| `photo` | (file ảnh) |

`checkpointId`, `notes`, `photo` không bắt buộc. Nhật ký có `feedStatus = Refused` hoặc `stressLevel = Agitated` được đánh dấu `needsAttention: true` và điều phối viên nhận cảnh báo.

### Bảng theo dõi của điều phối

`GET /api/tracking/trips?status=` — Coordinator, Manager, Admin. Không truyền `status` thì trả các chuyến chưa đóng.

```json
{
  "tripId": 2,
  "tripCode": "TRP-2026-0002",
  "overallStatus": "InTransit",
  "vehicleCode": "29B-888.99",
  "leadDriverName": "Phạm Văn Đức",
  "leadDriverPhone": "+84912000004",
  "horseCount": 3,
  "checkpointsTotal": 4,
  "checkpointsReached": 3,
  "lastCheckpointName": "Trạm nghỉ Nam Ninh",
  "lastCheckpointStatus": "Departed",
  "nextCheckpointName": "Trường đua Tùng Hóa",
  "nextCheckpointPlannedTime": "2026-11-09T11:00:00+00:00",
  "delayMinutes": 45,
  "isBehindSchedule": true,
  "openIncidentCount": 0,
  "welfareAlertCount": 1
}
```

### Theo dõi đơn của khách

`GET /api/tracking/bookings/{id}` — chủ đơn; chuyên viên phụ trách đơn; Manager, Coordinator, Admin.

- `trips[]`: mỗi chuyến đang chở ngựa của đơn, gồm `checkpoints[]` (dòng thời gian, mỗi mốc có `delayMinutes`; `null` nghĩa là chưa tới) và `horses[]`.
- `horses[]` chỉ gồm ngựa **của đơn này**, kể cả khi chuyến chở chung ngựa của khách khác. Mỗi con có `latestLog` và `logs[]`.
- `horsesAwaitingPlanning`: tên các con ngựa chưa được xếp chuyến.
- `handoverCompleted`: đơn này đã bàn giao trên chuyến đó chưa.

**Cập nhật "real-time":** gọi lại endpoint này định kỳ, khoảng 30 giây một lần. Server có cache 30 giây và xóa cache ngay khi đoàn xe cập nhật, nên gọi thường xuyên không gây tải.

---

## 11. Flow 5 – Sự cố và nắn tuyến

```
Reported ──đề xuất──> PlanProposed ──duyệt──> Approved ──đóng──> Resolved
                          ▲   │                  │
                          │   └──từ chối──> Rejected (đề xuất lại)
                          └──────────────────────┘
                                                 └─ nắn tuyến (chuyến thành EmergencyRerouting)
```

| Method | Đường dẫn | Ai được gọi | Mô tả |
|---|---|---|---|
| POST | `/api/trips/{tripId}/incidents` | Đoàn xe của chuyến, Admin | Báo sự cố khi chuyến đang trên đường |
| GET | `/api/trips/{tripId}/incidents` | Nhân sự | Mọi sự cố của một chuyến, mới nhất trước |
| GET | `/api/incidents` | Mọi người đã đăng nhập | Danh sách. Lọc: `status`, `severity`, `tripId`, `search` |
| GET | `/api/incidents/{id}` | Như trên | Chi tiết |
| POST | `/api/incidents/{id}/propose` | Coordinator, Admin | Đề xuất phương án (khi `Reported` hoặc `Rejected`) |
| POST | `/api/incidents/{id}/approve` | Manager, Admin | Duyệt phương án |
| POST | `/api/incidents/{id}/reject` | Manager, Admin | Từ chối: `{ "reason": "..." }` |
| POST | `/api/incidents/{id}/apply-reroute` | Coordinator, Admin | Nắn tuyến (khi `Approved`). Trả về chi tiết chuyến |
| POST | `/api/incidents/{id}/resolve` | Coordinator, Admin | Đóng sự cố (khi `Approved`) |

Ai thấy sự cố nào: khách thấy sự cố trên chuyến có ngựa của mình; tài xế / áp tải thấy sự cố của chuyến mình đi; các vai trò còn lại thấy tất cả.

Báo sự cố:

```json
{
  "incidentType": "MechanicalBreakdown",
  "severity": "Major",
  "location": "QL1A km 120, Lạng Sơn",
  "description": "Hỏng hệ thống treo khí nén, xe không thể đi tiếp",
  "affectedTripHorseId": 3
}
```

`affectedTripHorseId` không bắt buộc. Người lập chuyến, các Manager và khách có ngựa trên chuyến nhận thông báo ngay (`referenceType = Incident`).

Đề xuất phương án:

```json
{
  "proposedAction": "Đổi xe dự phòng, đi vòng qua cửa khẩu Tân Thanh",
  "revisedRouteNotes": "Đã liên hệ trạm thú y Tân Thanh",
  "additionalCost": 400
}
```

Lý do từ chối không lưu trong sự cố: nó được gửi cho người lập chuyến qua thông báo.

Nắn tuyến:

```json
{
  "insertAfterCheckpointId": 12,
  "deactivateCheckpointIds": [13],
  "newCheckpoints": [
    { "checkpointName": "Cửa khẩu Tân Thanh", "checkpointType": "BorderGate", "address": "Lạng Sơn", "plannedTime": "2026-11-07T15:00:00+07:00", "mandatoryRestMinutes": 90 }
  ]
}
```

- `deactivateCheckpointIds`: các mốc chưa tới cần bỏ. Mốc bị bỏ vẫn còn trong chi tiết chuyến với `isActive: false`, `status: "Cancelled"`. Không bỏ được điểm đi, điểm giao, hay mốc đã tới.
- `newCheckpoints`: theo đúng thứ tự đi, chỉ nhận `RestStop` hoặc `BorderGate`.
- `insertAfterCheckpointId`: bỏ trống thì chèn ngay sau mốc gần nhất đoàn xe đã tới.
- Gọi lại được nhiều lần khi sự cố còn `Approved`. Thứ tự các mốc còn hiệu lực được đánh lại từ 1.
- Chuyến chuyển `EmergencyRerouting`; đoàn xe vẫn check-in mốc bình thường. Khi đóng sự cố (`resolve`), chuyến trở lại `InTransit`.

Response sự cố:

```json
{
  "incidentId": 2,
  "incidentCode": "INC-2026-0002",
  "tripId": 1,
  "tripCode": "TRP-2026-0001",
  "tripStatus": "EmergencyRerouting",
  "incidentType": "MechanicalBreakdown",
  "severity": "Major",
  "status": "Approved",
  "location": "QL1A km 120, Lạng Sơn",
  "description": "Hỏng hệ thống treo khí nén, xe không thể đi tiếp",
  "affectedTripHorseId": 3,
  "affectedHorseName": "Red Flash",
  "reportedByUserId": 4,
  "reportedByName": "Phạm Văn Đức",
  "reportedAt": "2026-11-07T04:10:00+00:00",
  "proposedAction": "Đổi xe dự phòng, đi vòng qua cửa khẩu Tân Thanh",
  "revisedRouteNotes": "Đã liên hệ trạm thú y Tân Thanh",
  "additionalCost": 400,
  "approvedByUserId": 1,
  "approvedByName": "Trần Quốc Bảo",
  "approvedAt": "2026-11-07T04:40:00+00:00",
  "resolvedAt": null
}
```

---

## 12. Danh mục

Loại giấy tờ:

| Method | Đường dẫn | Ai được gọi | Mô tả |
|---|---|---|---|
| GET | `/api/document-types/lookup` | Mọi người đã đăng nhập | Toàn bộ, không phân trang; dùng cho dropdown |
| GET | `/api/document-types` | Mọi người đã đăng nhập | Danh sách có phân trang |
| GET | `/api/document-types/{id}` | Mọi người đã đăng nhập | Chi tiết |
| POST / PUT / DELETE | `/api/document-types[/{id}]` | Specialist, Admin | Thêm, sửa, xóa |

---

## 13. Chưa có

Các phần dưới đây chưa có API. Frontend có thể dựng giao diện trước, nhưng chưa nối được.

| Phần | Ghi chú |
|---|---|
| Chuông thông báo (đọc, đếm chưa đọc, đánh dấu đã đọc) | Backend đã tạo thông báo cho mọi sự kiện; chưa có endpoint để đọc |
| Quản lý phương tiện | Hiện chỉ có `GET /api/trips/available-vehicles` |
| Quy định giấy tờ theo quốc gia, bảng giá | Dữ liệu có sẵn và đang được dùng để tính; chưa có màn hình quản trị |
| Danh sách enum cho dropdown | Tạm dùng bảng ở mục 3 |
| Bàn giao e-POD, đóng chuyến, báo cáo (Flow 6) | |
| Xác thực email, quên mật khẩu | |

Tiến độ từng phần xem ở [TASKS.md](TASKS.md).
