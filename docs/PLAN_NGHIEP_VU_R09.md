# Kế hoạch sửa nghiệp vụ (R-09 → R-15) — chờ Thiên duyệt

Ngày viết: 22/10/2026, chốt 26/10/2026. Trạng thái: **đã chốt, chưa làm**. Chính sách cho khách nằm ở `CHINH_SACH_DICH_VU.md` (cùng thư mục). Bản gốc của cả hai file nằm ở repo backend `docs/`; sửa ở đó rồi chép sang `doc/` của repo frontend.

## Quyết định đã chốt (26/10/2026)

1. **Cọc khi khách chấp nhận báo giá chốt.** Báo giá có hạn `Booking.QuoteAcceptDays` ngày (cấu hình, mặc định 3): quá hạn mà khách chưa chấp nhận + cọc → đơn tự hủy (`QuoteExpired`), không mất phí.
2. **Bồi thường trễ** theo đề xuất: ân hạn 6 giờ, 5% tiền cước mỗi 12 giờ, trần 25%, chỉ khi lỗi công ty.
3. **Bảo hiểm** theo đề xuất: không mua → trách nhiệm tối đa 5.000 USD / con (chỉ khi lỗi công ty); mua → 100% giá trị thỏa thuận, thú y tối đa 20%; phí 1%.
4. **Ngựa tối thiểu 6 tháng tuổi; giá trị từ 50.000 USD / con phải kèm chứng từ.**
5. **Bỏ nút "Rời chuyến" riêng**, rời chuyến chỉ qua sự cố.
6. **Bản đồ: không dùng Google Maps.** Google liệt kê Việt Nam (và Trung Quốc) là "Prohibited Territories" của Google Maps Platform (https://cloud.google.com/maps-platform/terms/maps-prohibited-territories): không tạo được dự án Maps với tài khoản thanh toán ở Việt Nam, và tuyến của mình đi qua cả hai nước. Dùng: hiển thị **Leaflet + OpenStreetMap**; tìm địa chỉ / chỉ đường trong VN **Goong** (đã có key); ngoài VN **Nominatim + OSRM** (miễn phí, có giới hạn lượt gọi → cache), **Mapbox** làm dự phòng nếu cần.
8. **Người nhận (R-16):** liên hệ người nhận chỉ bằng **email + tài xế gọi điện** (không SMS). **Khách (người đặt) xác nhận người nhận có mặt trên app** 24 giờ trước ETA; khách được báo khi người nhận ký xong. Phí chờ **40 USD/giờ** sau 2 giờ miễn phí, tối đa 24 giờ, khách trả qua khoản phát sinh.
9. **Hợp đồng vận chuyển (R-17):** hệ thống tự tạo hợp đồng PDF từ báo giá chốt. Khách **xem trước** rồi tích "đã đọc" và bấm **Chấp nhận & đặt cọc**; cú bấm đó là đồng ý giao kết. **Không có bước ký riêng, không OTP.** Lưu bằng chứng (người bấm, thời điểm, IP, phiên bản chính sách, mã băm file). Điều khoản đổi → bản hợp đồng mới, khách xem và chấp nhận lại. Thiên làm.
7. Mọi quy tắc cho khách được viết thành trang **Chính sách**; khách phải tích "Tôi đồng ý với Chính sách dịch vụ" khi gửi yêu cầu và khi chấp nhận báo giá (lưu phiên bản chính sách + thời điểm đồng ý vào đơn).

Nguồn tham khảo đã kiểm tra (ghi ở cuối file): quy định EU 1/2005 về vận chuyển động vật (bản UK/Pháp), mẫu chứng nhận USDA APHIS, tài liệu của vài nhà bảo hiểm ngựa (mortality / agreed value).

---

## 0. Nguyên tắc chung: ai chịu lỗi thì người đó chịu tiền

Mọi tình huống xấu bên dưới đều quy về **một bảng lỗi** để tính tiền cho thống nhất. Khi đóng sự cố, Coordinator chọn nguyên nhân; Manager duyệt.

| Nguyên nhân | Ví dụ | Tiền cước phần chưa đi | Chi phí phát sinh (chờ, quay đầu, chuồng tạm) | Bồi thường trễ |
|---|---|---|---|---|
| **Lỗi công ty** | xe hỏng do bảo dưỡng kém, Specialist thiếu giấy, tài xế đi sai tuyến | hoàn 100% | công ty chịu | có (mục R-09) |
| **Lỗi khách** | giấy giả / sai, khai sai tình trạng ngựa, người nhận vắng mặt / từ chối | không hoàn | khách chịu (thu qua khoản phát sinh) | không |
| **Bất khả kháng** | đóng biên, đổi chính sách, thiên tai, dịch bệnh | hoàn phần chưa đi (theo tỉ lệ quãng đường) trừ phí đã nộp không lấy lại được | chia: công ty chịu chi phí của xe / người, khách chịu chuồng tạm + thú y | không |

Hệ thống hiện đã có sẵn: khoản **phát sinh (Extra)** để thu thêm, **hoàn tiền (Refund)** qua VNPay, phân loại KPI `OnTime / DelayedForceMajeure / DelayedOperationalFault`. Kế hoạch dùng lại các phần này, chỉ thêm trường "nguyên nhân" cho sự cố.

---

## R-10 · Đổi luồng đặt chuyến: báo giá khoảng → lập tuyến → báo giá chốt → cọc (mục 4) — **làm trước tiên, các mục khác dựa vào nó**

### Vấn đề hiện tại
Khách tự khai nước quá cảnh, hệ thống tính **một giá cụ thể** ngay lúc tạo đơn, Manager duyệt là khách phải cọc. Lúc đó chưa có tuyến thật, chưa biết quãng đường thật, chưa biết đi qua nước nào → giá và cọc đều không có cơ sở.

### Luồng đề xuất (giống cách các công ty vận chuyển ngựa quốc tế làm: hỏi giá → báo giá → xác nhận đặt chỗ + cọc → làm giấy → thu đủ trước khi chạy)

```
Khách gửi yêu cầu (khoảng giá ước tính, KHÔNG cọc)
  → Manager duyệt sơ bộ (khả thi không?)
  → Coordinator lập tuyến dự kiến trên bản đồ (quãng đường từng chặng, nước quá cảnh tự suy ra)
  → Manager duyệt tuyến → hệ thống tính BÁO GIÁ CHỐT, gửi email cho khách (hạn chấp nhận 3 ngày)
  → Khách chấp nhận báo giá + đặt cọc            (không chấp nhận / quá hạn → đơn đóng, không mất phí)
  → Specialist yêu cầu giấy tờ theo nước đi / quá cảnh / đến → khách nộp (email + hạn)
  → Specialist xin giấy phép / kiểm dịch với cơ quan chức năng → hồ sơ Cleared
  → Coordinator xếp chuyến cụ thể (xe, đoàn, ghép đơn) theo tuyến đã duyệt → Manager duyệt chuyến
  → Khách trả phần còn lại trước ngày chạy (đã có)
  → Trước khi xuất phát: kiểm tra lại hạn giấy + giấy khám lâm sàng trong 48h (R-12)
```

**Vì sao cọc ở bước "chấp nhận báo giá" chứ không đợi duyệt xong giấy tờ** (khác ý Thiên một chút): làm giấy tốn tiền thật (xét nghiệm, phí xin giấy phép nhập khẩu / quá cảnh, công Specialist). Nếu đợi giấy xong mới thu tiền, khách bỏ ngang thì công ty mất trắng chi phí này. Cọc sau khi giá đã chốt thì vừa công bằng cho khách (biết chính xác mình trả bao nhiêu) vừa an toàn cho công ty. Phần còn lại vẫn thu trước ngày chạy như hiện tại.

### Khoảng giá lúc khách tạo đơn
- Khách chọn điểm đón / trả trên bản đồ (R-11) → hệ thống lấy quãng đường đường bộ thẳng từ A đến B qua API bản đồ.
- Giá thấp = quãng đường × đơn giá + hạng chuồng + bảo hiểm (nếu chọn), không quá cảnh.
- Giá cao = quãng đường × 1.25 (đi vòng, mốc nghỉ) + phí thủ tục cho 1–2 nước quá cảnh nếu tuyến cắt qua biên giới khác.
- Màn hình ghi rõ: "Giá ước tính, giá chốt gửi sau khi lập tuyến".
- Bỏ ô "Nước quá cảnh" khỏi form khách.

### Đổi tuyến sau khi khách đã chấp nhận báo giá
- **Do công ty** (Coordinator đổi tuyến khi xếp chuyến): công ty chịu chênh lệch giá. Nếu tuyến mới qua nước mới → Specialist yêu cầu thêm giấy, gửi email + hạn. Khách không lo được giấy → khách được hủy **hoàn 100%**.
- **Do khách** (đổi điểm đón / trả): báo giá lại, khách chấp nhận mới đi tiếp.
- Khách không nộp giấy cho tuyến đã chốt từ đầu, quá hạn (nhắc 2 lần) → hủy theo chính sách hủy của khách (hoàn % cọc như hiện tại), **không phải mất trắng**, vì luật bảo vệ người tiêu dùng thường không cho giữ toàn bộ tiền khi chưa cung cấp dịch vụ.

### Thay đổi hệ thống (ước lượng)
- Trạng thái đơn: `Submitted → PreApproved → RoutePlanned → Quoted → AwaitingDeposit → Approved (làm giấy) → Assigned → …`. Thêm `QuoteExpired`.
- Bảng mới `booking.route_plans` (tuyến dự kiến của đơn: các mốc có tọa độ, quãng đường từng chặng, nước quá cảnh, người lập, người duyệt). Khi xếp chuyến, các mốc được chép sang `route_checkpoints` của chuyến.
- `booking_requests`: thêm `quote_min`, `quote_max`, `quote_expires_at`; `estimated_cost` chỉ có khi báo giá chốt.
- Màn mới: Coordinator lập tuyến cho đơn (dùng bản đồ của R-11); Manager duyệt tuyến; khách xem báo giá chốt + nút "Chấp nhận & đặt cọc".
- Sửa: form tạo đơn (bỏ nước quá cảnh, hiện khoảng giá), luồng cọc (dời sang sau báo giá), luồng hồ sơ (yêu cầu giấy theo nước của tuyến đã duyệt).

### Cần Thiên chốt
1. Đồng ý cọc ở bước "chấp nhận báo giá" (không đợi giấy duyệt xong)?
2. Hạn chấp nhận báo giá: 3 ngày?
3. Hệ số khoảng giá (×1.25) và số nước quá cảnh tối đa tính vào giá cao?

---

## R-11 · Chọn vị trí trên bản đồ, tính quãng đường theo từng chặng (mục 6) — làm cùng R-10

- **Bản đồ:** Leaflet + OpenStreetMap trên frontend (miễn phí, không cần key). Click lên bản đồ để chọn điểm, hoặc gõ tên → gợi ý → chọn → ghim lên bản đồ.
- **Tìm địa chỉ / tọa độ:** Goong (đang có key) cho Việt Nam; ngoài Việt Nam dùng Nominatim (OpenStreetMap) hoặc Mapbox. Backend có một dịch vụ chung, chọn nhà cung cấp theo quốc gia.
- **Quãng đường:** gọi API chỉ đường (Goong trong VN, OSRM / Mapbox xuyên biên giới) cho **từng chặng** mốc 1→2→3…, cộng lại. Đường hàng không: tính theo đường tròn lớn giữa hai sân bay. Không gọi được API thì quay về cách tính dự phòng hiện tại và đánh dấu "ước tính".
- **Nước quá cảnh tự suy ra:** đổi tọa độ của từng mốc (và vài điểm dọc đường) sang mã quốc gia. Mỗi lần đổi nước mà chưa có mốc cửa khẩu → hệ thống cảnh báo Coordinator phải thêm mốc cửa khẩu.
- **DB:** thêm `latitude`, `longitude` cho điểm đón / trả của đơn và cho `route_checkpoints`; `distance_km_from_previous` cho từng mốc.
- **Rủi ro:** API chỉ đường miễn phí có giới hạn số lần gọi; tuyến VN–TQ có thể thiếu dữ liệu đường. Cần cache kết quả theo cặp tọa độ.

### Cần Thiên chốt
1. Đồng ý dùng Leaflet + OSM (miễn phí) thay vì bản đồ Goong cho phần hiển thị?
2. Có muốn đăng ký Mapbox (miễn phí 100k lượt / tháng) làm dự phòng xuyên biên giới không?

---

## R-09 · Bồi thường giao trễ (mục 1, đã đồng ý) — chính sách riêng, **không thuộc bảo hiểm**

Bảo hiểm vận chuyển ngựa ngoài đời chỉ trả cho ngựa chết / bị thương, **không trả cho chuyện trễ** (trễ là cam kết dịch vụ của công ty vận chuyển). Nên tách riêng:

- **Ân hạn:** trễ dưới 6 giờ so với hạn giao → không bồi thường.
- **Mức:** sau ân hạn, hoàn **5% tiền cước vận chuyển cho mỗi 12 giờ trễ**, tối đa **25%**. Tiền cước = giá báo trừ bảo hiểm và phí thủ tục (những phần này không phụ thuộc giờ giao).
- **Chỉ áp khi lỗi công ty** (`DelayedOperationalFault`). Bất khả kháng → không bồi thường nhưng vẫn báo khách lý do. Lỗi khách (giấy nộp trễ, người nhận vắng) → không bồi thường.
- **Không bồi thường thiệt hại gián tiếp** (lỡ cuộc đua, mất tiền thưởng…), ghi rõ trong điều khoản.
- **Cách chạy:** khi đóng chuyến, hệ thống tính số giờ trễ của **từng đơn** (theo giờ ký e-POD so với hạn giao), tạo dòng hoàn tiền ở trạng thái chờ duyệt; Manager duyệt hoặc chỉnh (ghi lý do); hoàn qua VNPay + email cho khách.
- **Cấu hình** (`system_settings`): ân hạn, % mỗi 12 giờ, trần %.

### Cần Thiên chốt
Các con số 6 giờ / 5% mỗi 12 giờ / trần 25% có ổn không?

---

## R-12 · Ngựa: tuổi, sức khỏe, rời chuyến, chết, bị thương (mục 2)

### 12.1 Tuổi và tình trạng được phép chở — đã kiểm tra lại
- **Không có quy định cấm ngựa già** theo tuổi. Luật (EU 1/2005, bản UK và Pháp) cấm chở ngựa **"không đủ sức khỏe"**: ốm, bị thương, yếu; ngựa cái mang thai **đã quá 90% thai kỳ** hoặc **vừa đẻ trong 7 ngày**; ngựa con **chưa lành rốn**.
- **Không tìm thấy một "giấy fit-to-travel" chuẩn riêng** ở EU. Đề xuất trước đây của mình chưa chính xác. Ngoài đời, việc xác nhận "đủ sức khỏe" nằm trong **giấy chứng nhận sức khỏe / kiểm dịch xuất khẩu** do bác sĩ thú y chính thức cấp sau khi khám lâm sàng sát ngày đi. Mẫu của USDA APHIS (Mỹ) còn yêu cầu xác nhận ngựa cái không đẻ trong chuyến và **ngựa con trên 6 tháng tuổi**.
- **Đề xuất:**
  - Chặn ngựa **dưới 6 tháng** (theo `DateOfBirth`, tính tới ngày cuối của khoảng đón). Cấu hình được.
  - Ngựa cái: khi thêm ngựa vào đơn hỏi "đang mang thai?" + ngày dự sinh. Chặn nếu ngày đi đã quá 90% thai kỳ (~306/340 ngày) hoặc vừa đẻ trong 7 ngày.
  - **Không chặn ngựa già.** Thay vào đó mọi con ngựa phải có **giấy khám lâm sàng trong 48 giờ trước giờ xuất phát** (thêm loại giấy vào danh mục, hạn 2 ngày). Áp chung, không chỉ ngựa già.

### 12.2 Kiểm tra lại hạn giấy lúc xuất phát (đã đồng ý)
`StartTripAsync` hiện chỉ kiểm tra hồ sơ `Cleared`. Thêm: mọi giấy có hạn phải còn hiệu lực **tại giờ xuất phát thực tế** (và tại giờ dự kiến tới cửa khẩu nếu nước đó yêu cầu). Giấy hết hạn vì chuyến bị lùi → chặn xuất phát, báo Specialist + khách làm lại. Nếu lùi chuyến là lỗi công ty → công ty chịu phí làm lại giấy.

### 12.3 "Ngựa rời chuyến" (Offloaded) — có hợp lý không?
Ngoài đời có thật: ngựa có biểu hiện bệnh / chấn thương giữa đường được dỡ xuống ở phòng khám thú y hoặc chuồng gần nhất. **Nhưng cách hệ thống đang làm thì chưa đúng:**
- Hiện là một **nút riêng** của tài xế, không gắn với sự cố, không cần ai duyệt.
- Ngựa rời chuyến **tự quay về hàng chờ xếp chuyến**, như thể chắc chắn sẽ đi tiếp.

**Đề xuất:**
- Bỏ nút riêng. Rời chuyến chỉ xảy ra qua **sự cố**: tài xế báo sự cố (loại "Sức khỏe ngựa", chọn con ngựa) → Coordinator đề xuất phương án hành động `OffloadHorse` (đã có trong bản vừa làm) kèm nơi gửi ngựa → Manager duyệt → ngựa chuyển `Offloaded`. Trường hợp khẩn cấp (thú y bắt buộc dỡ ngay) cho phép tài xế dỡ trước, sự cố vẫn bắt buộc và duyệt sau.
- Khi rời chuyến phải chọn **hướng xử lý tiếp**:
  - **Chờ hồi phục rồi xếp chuyến khác** → quay về hàng chờ. Chi phí chuồng + thú y theo bảng lỗi ở mục 0.
  - **Trả về nơi đi.**
  - **Khách nhận tại chỗ** (ký e-POD tại nơi dỡ) → đơn của con này kết thúc, hoàn phần cước chưa đi nếu không phải lỗi khách.
  - **Chết** → 12.4.

### 12.4 Ngựa chết
- Trạng thái mới `Deceased` cho ngựa trên chuyến (và trong đơn). Chỉ ghi được qua sự cố mức Critical.
- **Bắt buộc:** biên bản của bác sĩ thú y (giấy chứng tử). Có bảo hiểm thì thêm **khám nghiệm tử thi**, vì nhà bảo hiểm ngoài đời gần như luôn đòi, trừ khi họ miễn. **An tử** chỉ hợp lệ khi bác sĩ thú y xác nhận là vì lý do nhân đạo khẩn cấp; ngoài đời đây là điều kiện để bảo hiểm còn trả.
- Báo khách ngay (thông báo + email). Mở **hồ sơ bồi thường bảo hiểm** tự động (R-13). Phần cước của con ngựa đó: hoàn phần chưa đi nếu không phải lỗi khách.

### 12.5 Ngựa bị thương
- **Trên đường:** đã có sự cố loại "Sức khỏe ngựa" → phương án `VeterinaryCare` (đã có). Thêm: ghi mức độ thương tích + ảnh, tự mở hồ sơ bồi thường nếu có chi phí thú y.
- **Lúc giao** (e-POD chọn `Injured`): bắt buộc ảnh + mô tả → tự tạo sự cố + hồ sơ bồi thường. Khách vẫn ký nhận được. Khách phải báo thương tích **trong 24 giờ** sau khi nhận, quá thời hạn thì không nhận khiếu nại (ngoài đời bên vận chuyển hay áp thời hạn kiểu này).

### 12.6 Giá trị ngựa khai báo — dựa vào đâu để bồi thường?
- Khách khai **giá trị từng con** lúc tạo đơn (hiện chỉ có một ô giá trị cho cả đơn → đổi thành theo từng con).
- **Chứng từ:** giá trị trên ngưỡng (ví dụ 50.000 USD) bắt buộc kèm hóa đơn mua / giấy định giá của bác sĩ thú y hoặc chuyên gia định giá / kết quả đấu giá. Dưới ngưỡng thì không bắt buộc (một số nhà bảo hiểm ngoài đời cũng miễn giấy dưới mức tương tự).
- Manager xem và **chốt "giá trị thỏa thuận"** khi duyệt báo giá. Phí bảo hiểm tính trên giá trị này. Khi có sự cố, **bồi thường theo giá trị đã chốt**, không định giá lại (đây là kiểu "agreed value" phổ biến trong bảo hiểm ngựa).

### Cần Thiên chốt
1. Ngưỡng tuổi tối thiểu 6 tháng, ngưỡng giá trị cần chứng từ 50.000 USD?
2. Đồng ý bỏ nút "Rời chuyến" riêng, chuyển hết sang qua sự cố?

---

## R-13 · Chính sách bảo hiểm (mục 5)

**Hiện tại:** bảo hiểm chỉ là một dòng phí `INS_TRANSIT` (1% giá trị khai báo) cộng vào báo giá. Không có điều khoản, không có quy trình đòi bồi thường.

**Bảo hiểm KHÔNG xử lý giao trễ.** Trễ là chính sách riêng R-09.

### Điều khoản đề xuất (viết lên trang điều khoản + hiển thị lúc khách chọn)

**Hai mức:**

| | Không mua bảo hiểm (mặc định) | Bảo hiểm toàn giá trị (tùy chọn) |
|---|---|---|
| Phí | 0 | % giá trị thỏa thuận (cấu hình, hiện 1%) |
| Khi nào trả | **chỉ khi lỗi công ty** | mọi nguyên nhân được bảo hiểm, kể cả bất khả kháng |
| Ngựa chết / phải an tử | tối đa **mức trách nhiệm cố định** (ví dụ 5.000 USD / con, cấu hình) | **100% giá trị thỏa thuận** |
| Bị thương | chi phí thú y thực tế, tối đa 1.000 USD / con | chi phí thú y thực tế, tối đa **20% giá trị thỏa thuận** |
| Mất / thất lạc | như ngựa chết | 100% giá trị thỏa thuận |

(Giống cách hãng bay / bên vận chuyển ngoài đời có "mức trách nhiệm giới hạn" nếu khách không khai giá trị và mua thêm bảo hiểm.)

**Thời gian được bảo hiểm:** từ lúc ngựa lên xe ở điểm đón tới lúc ký e-POD. Thời gian ngựa rời chuyến chờ hồi phục ở chuồng tạm do công ty sắp xếp cũng được tính.

**Không trả (loại trừ):**
- Bệnh / thương tật có từ trước (đã có trước lúc nhận ngựa, hoặc phát bệnh do ủ bệnh trước khi đi mà giấy khám không phát hiện được).
- Khách khai sai: tuổi, mang thai, giá trị, tình trạng sức khỏe, giấy tờ.
- An tử không có xác nhận của bác sĩ thú y về lý do nhân đạo khẩn cấp; chết mà không cho khám nghiệm khi được yêu cầu.
- Thiệt hại gián tiếp: lỡ cuộc đua, mất tiền thưởng, giảm giá trị giống.
- Ngựa bị giữ / tiêu hủy theo lệnh cơ quan chức năng vì giấy của khách hoặc vì dịch bệnh.
- Giao trễ (thuộc R-09).

**Quy trình đòi bồi thường:**
1. Tự mở khi có sự cố Critical, ngựa `Deceased`, ngựa rời chuyến vì sức khỏe, hoặc e-POD `Injured`. Khách cũng tự mở được trong 24 giờ sau khi nhận ngựa.
2. Nộp chứng từ: biên bản thú y, khám nghiệm (nếu chết), ảnh, hóa đơn thú y.
3. Manager xem xét trong 7 ngày: duyệt số tiền (≤ mức của gói) hoặc từ chối (ghi lý do theo điều khoản loại trừ).
4. Chi trả: ghi một khoản chi cho khách (hoàn qua VNPay nếu ≤ số đã trả, phần vượt thì ghi nhận chuyển khoản thủ công kèm mã tham chiếu).
5. Trạng thái: `Open → UnderReview → Approved / Rejected → Paid`.

**DB:** bảng mới `billing.insurance_claims` (đơn, con ngựa, sự cố, loại, số tiền yêu cầu / duyệt, trạng thái, chứng từ), cột `insurance_plan` + `agreed_value` theo từng con ngựa.

### Cần Thiên chốt
Các mức: trách nhiệm cố định 5.000 USD / con, thú y 1.000 USD / 20%, phí 1%?

---

## R-14 · Các tình huống xấu trên đường (mục 3)

Tất cả đi qua **sự cố** (đã có) với các **loại sự cố** và **hành động** mới. Mỗi sự cố khi đóng phải chọn nguyên nhân theo bảng lỗi ở mục 0, để tính hoàn / thu tiền.

### 14.1 Xe hỏng
- Sửa được tại chỗ → hành động `RepairAndContinue` (đã có), trễ thì tính theo R-09.
- Không sửa được → hành động mới **`ReplaceVehicle`**: Coordinator chọn xe đang rảnh, đủ chuồng, cùng loại (có điều hòa nếu đơn yêu cầu). Khi duyệt: đổi xe của chuyến, xe cũ chuyển `Maintenance`, ngựa giữ nguyên số chuồng nếu được. Trong lúc chờ xe thay thế, ngựa phải được dỡ xuống chuồng tạm nếu chờ lâu (cấu hình, ví dụ > 4 giờ).
- Nguyên nhân thường là **lỗi công ty** (bảo dưỡng). Tai nạn do bên thứ ba thì tính bất khả kháng.

### 14.2 Tài xế / người áp tải ốm — đánh giá ý của Thiên
- **"Gửi nhiều người ngay từ đầu"**: hợp lý và là cách làm thật cho tuyến dài. Ở EU, giờ lái của tài xế xe tải bị giới hạn (khoảng 9 giờ/ngày), nên tuyến dài thường chạy **2 tài xế thay phiên**. Đề xuất thành **quy tắc khi lập chuyến**: chuyến có thời gian lái dự kiến trên 9 giờ thì bắt buộc **≥ 2 tài xế**. Như vậy một người ốm, người kia vẫn lái được.
- **"Phái đoàn theo sau"**: tốn kém nếu điều cả đoàn. Đề xuất điều **người thay thế** (chỉ một tài xế / người áp tải) tới chỗ xe đang dừng, đoàn đợi (hành động `WaitAndContinue`). Hệ thống thêm hành động mới **`ReplaceCrew`**: chọn người rảnh, người ốm rời đoàn, người mới vào đoàn khi tới nơi.
- Người áp tải (Escort) không được lái thay tài xế, trừ khi có bằng lái phù hợp (ngoài phạm vi, bỏ qua).

### 14.3 Cửa khẩu từ chối nhập cảnh
**Đã duyệt giấy rồi mà sao vẫn bị từ chối?** Quy trình "nộp cơ quan chức năng" là có thật: xin **giấy phép nhập khẩu** (nước đến) / **quá cảnh** (nước đi qua) trước, rồi **chứng nhận kiểm dịch xuất khẩu** do thú y chính thức của nước đi cấp sát ngày chạy. Nhưng giấy phép trước **không bảo đảm** qua được cửa khẩu. Ở cửa khẩu, thú y biên giới vẫn khám thực tế và có thể từ chối khi:
- Ngựa có triệu chứng bệnh lúc khám.
- Vi mạch / đặc điểm nhận dạng không khớp giấy.
- Giấy hết hạn do chuyến bị trễ, thiếu bản gốc, niêm phong bị hỏng.
- Nước đó vừa đổi quy định / đóng nhập vì dịch.

→ Hệ thống giữ nguyên bước nộp cơ quan chức năng, nhưng **đổi tên trạng thái** cho đúng nghĩa: "Đã có giấy phép" thay vì ngụ ý chắc chắn qua được.

**Xử lý:** loại sự cố mới `BorderRejected`, phương án chọn một trong ba:
- **Bổ sung / sửa giấy rồi thử lại** (chờ tại cửa khẩu, có chuồng tạm).
- **Đi cửa khẩu khác** (đổi tuyến; có thể cần giấy mới).
- **Quay đầu về nơi đi.**

**Tiền** (theo bảng lỗi):
- **Lỗi khách** (giấy sai / giả, ngựa bệnh không khai): không hoàn cước, khách trả chi phí quay đầu.
- **Lỗi công ty** (Specialist thiếu giấy, giấy hết hạn vì công ty để trễ): hoàn 100%, công ty chịu quay đầu.
- **Bất khả kháng** (đổi chính sách, dịch): hoàn phần cước chưa đi theo tỉ lệ quãng đường, trừ phí giấy tờ đã nộp không lấy lại được. Chi phí quay đầu: công ty chịu xe + người, khách chịu chuồng tạm + thú y.

### 14.4 Người nhận vắng mặt / từ chối nhận
Ngựa là tài sản của khách và là động vật sống, **không thể "hủy đơn rồi bỏ đó"**; công ty vẫn phải chăm ngựa. Đề xuất:
- **Chờ miễn phí 2 giờ** tại điểm giao. Sau đó tính **phí chờ theo giờ** (khoản phát sinh).
- Hệ thống báo khách (người đặt đơn) ngay, yêu cầu chọn trong **24 giờ**:
  1. **Chỉ định người nhận khác / địa chỉ khác gần đó** (phí theo quãng đường thêm).
  2. **Gửi chuồng tạm gần nhất** (khách trả phí chuồng + chăm sóc theo ngày), khách tự tới nhận.
  3. **Chở về nơi đi** (khách trả chi phí chiều về theo báo giá mới).
- Không hoàn tiền cước (dịch vụ đã làm xong, lỗi khách).
- Khách không trả lời sau 24 giờ → mặc định **phương án 2**. Nợ phí chuồng dồn vào khoản phát sinh, nhắc nợ như hiện tại.
- **Từ chối vì ngựa bị thương / sai ngựa** → không phải lỗi khách: ghi e-POD từ chối có lý do + ảnh → mở sự cố + hồ sơ bồi thường (R-13). Ngựa vẫn được chăm tại chuồng tạm, **công ty chịu phí** cho tới khi xử lý xong.
- e-POD hiện chỉ có nhánh ký nhận → thêm nhánh **"Không giao được"** (vắng mặt / từ chối, lý do, ảnh).

### 14.5 Biên giới đóng
Loại sự cố mới `BorderClosed`, ba phương án như 14.3:
- **Chờ mở lại** nếu dự kiến ngắn (ví dụ < 48 giờ), ngựa vào chuồng tạm.
- **Đi đường vòng** qua cửa khẩu khác: nếu tốn thêm tiền thì **phải hỏi khách đồng ý** khoản chênh lệch (báo giá phát sinh), vì bất khả kháng không phải lỗi công ty.
- **Quay đầu:** hoàn phần cước chưa đi theo tỉ lệ quãng đường, trừ phí đã nộp không lấy lại được (bất khả kháng).
- Khách không đồng ý đi vòng → quay đầu theo cách trên.

### 14.6 Tóm tắt thay đổi cho sự cố
- **Loại sự cố mới:** `BorderRejected`, `BorderClosed`, `CrewUnavailable`, `RecipientUnavailable`.
- **Hành động mới:** `ReplaceVehicle`, `ReplaceCrew`, `ReturnToOrigin`, `HoldAtStable` (gửi chuồng tạm).
- **Cột mới:** `fault_party` (`Company / Customer / ForceMajeure`) khi đóng sự cố → dùng cho hoàn tiền, thu phát sinh, KPI, bồi thường trễ.
- **Quy tắc lập chuyến:** ≥ 2 tài xế khi thời gian lái dự kiến > 9 giờ.

---

## R-16 · Người nhận ngựa và giao nhận

**Hiện trạng:** đơn không có thông tin người nhận; tài xế chỉ gõ tên / SĐT / CCCD người nhận lúc ký e-POD; hệ thống không liên hệ người nhận lần nào trước khi giao.

**Làm:**
- Đơn có thông tin người nhận: họ tên, SĐT (bắt buộc), email, số giấy tờ (tùy chọn), nút "Tôi tự nhận". Khách sửa được tới khi xe check-in điểm trả.
- Email cho người nhận: chuyến được lên lịch; xe xuất phát (ETA); xe rời mốc ngay trước điểm trả. Không SMS.
- 24 giờ trước ETA: nhắc khách xác nhận "người nhận sẽ có mặt" trên app (hoặc đổi người nhận). Ghi thời điểm xác nhận + khung giờ đã báo.
- App tài xế: hiện người nhận + nút gọi điện (`tel:`); e-POD điền sẵn từ đơn, tài xế đối chiếu giấy tờ. Người nhận khác tên trong đơn → khách phải xác nhận trên app.
- Ký e-POD xong → báo khách (thông báo + email).
- e-POD thêm nhánh **"Không giao được"** (vắng mặt / từ chối, lý do, ảnh). Phí chờ tự tính từ lúc check-in điểm trả tới lúc ký / ghi không giao được, trừ 2 giờ; chỉ tính khi xe tới ≥ đầu khung giờ đã báo; cộng vào khoản phát sinh. Khách có 24 giờ chọn: người nhận khác / chuồng tạm / chở về; quá hạn → chuồng tạm. Từ chối vì ngựa bị thương / nhầm ngựa → sự cố + hồ sơ bồi thường, công ty chịu phí chuồng.

## R-17 · Hợp đồng vận chuyển

**Vì sao:** tích đồng ý chính sách chưa đủ khi tranh chấp (bồi thường, giao trễ, ngựa chết): cần một văn bản gom đúng điều khoản của từng đơn mà khách đã đồng ý. Theo Luật Giao dịch điện tử, hợp đồng giao kết bằng thao tác chấp nhận trên hệ thống có giá trị nếu chứng minh được ai đồng ý, lúc nào, với nội dung nào → không cần bước ký riêng hay OTP.

**Làm:**
- **Tạo lúc nào:** khi đơn có báo giá chốt (`Quoted`), hệ thống tạo **bản hợp đồng nháp** (PDF) từ dữ liệu báo giá. Không tạo lúc khách mới gửi yêu cầu (chưa có giá, chưa có tuyến).
- **Nội dung:** số hợp đồng; hai bên (công ty, khách) và người nhận; danh sách ngựa (tên, vi mạch, giá trị thỏa thuận, gói bảo hiểm); tuyến đã duyệt (điểm đón / trả, nước quá cảnh); khoảng ngày đón, hạn giao; bảng giá chốt, tiền cọc, phần còn lại, các hạn thanh toán; toàn văn Chính sách dịch vụ đúng phiên bản hiện hành làm phụ lục.
- **Chấp nhận:** trang báo giá có nút **Xem hợp đồng**; khách tích "Tôi đã đọc hợp đồng và Chính sách dịch vụ" rồi bấm **Chấp nhận & đặt cọc**. Request gửi kèm mã băm của bản đã xem; server so với bản nháp hiện hành (khác → báo "hợp đồng đã thay đổi, vui lòng xem lại"), rồi **khóa** bản đó và lưu: người bấm, thời điểm, IP, User-Agent, phiên bản chính sách, mã băm SHA-256. Gửi PDF qua email; khách tải lại được trên trang đơn.
- **Phía công ty:** không ký tay; hợp đồng ghi tên Manager đã duyệt tuyến / báo giá và ngày duyệt.
- **Thay đổi điều khoản sau khi đã chấp nhận** (khách đổi điểm đón / trả → báo giá lại; khách đồng ý trả thêm để đi đường vòng; đổi người nhận): tạo **bản hợp đồng mới** (số phiên bản tăng, ghi lý do), bản cũ chuyển "đã thay thế"; khách xem và bấm chấp nhận lại như trên. Các khoản phát sinh đã có sẵn trong chính sách (phí chờ, chuồng tạm…) **không** cần bản mới.
- **Đơn đường hàng không:** thêm ô lưu số vận đơn hàng không (Air Waybill) + tệp đính kèm do hãng / đại lý phát hành (ngoài phạm vi tạo của hệ thống).

## R-15 · Kiểm tra trước khi xuất phát (gom lại)
Trước `StartTripAsync`, hệ thống kiểm tra và liệt kê tất cả lỗi một lần:
1. Hồ sơ mọi ngựa `Cleared` (đã có).
2. Mọi giấy còn hạn tại giờ xuất phát (12.2).
3. Có giấy khám lâm sàng trong 48 giờ (12.1).
4. Đơn đã trả đủ phần còn lại (đã có).
5. Đủ số tài xế theo quy tắc 14.2.

---

## Thứ tự làm

| Đợt | Nội dung | Phụ thuộc |
|---|---|---|
| 1 | R-11 bản đồ + tọa độ + quãng đường từng chặng | — |
| 2 | R-10 luồng báo giá khoảng → lập tuyến → báo giá chốt → cọc (hạn chấp nhận cấu hình) + tích đồng ý chính sách | R-11 |
| 3 | R-12 tuổi / mang thai / giấy 48h / kiểm tra hạn + R-15 | — |
| 4 | R-12 rời chuyến qua sự cố, `Deceased`, giá trị theo từng con | — |
| 5 | R-13 bảo hiểm (gói, hồ sơ bồi thường) | đợt 4 |
| 6 | R-09 bồi thường trễ + cột `fault_party` | — |
| 7 | R-14 tình huống xấu (đổi xe, đổi người, cửa khẩu, người nhận, biên giới) | đợt 6 |
| 7b | R-16 người nhận, thông báo người nhận, xác nhận có mặt, không giao được, phí chờ | R-10 (đơn có người nhận), R-09 |
| 7c | R-17 hợp đồng vận chuyển (tạo PDF, xem trước, chấp nhận kèm bằng chứng, bản mới khi đổi điều khoản) | R-10 (báo giá chốt) |
| 8 | Trang Chính sách trên web (từ `CHINH_SACH_DICH_VU.md`) | các đợt trên |

Mỗi đợt: một script `03_update_*.sql` cho Supabase, cập nhật `API.md` + xuất lại file Word, test backend, lint frontend.

## Nguồn đã kiểm tra
- IFCE (Pháp), "Protecting horses during transport": https://equipedia.ifce.fr/en/equipedia-the-universe-of-the-horse-ifce/economy-and-the-horse-sector/regulations/transport/protecting-horses-during-transport
- Anglesey County Council (UK), hướng dẫn phúc lợi động vật khi vận chuyển: https://www.anglesey.gov.wales/en/business/trading-standards/trading-standards-guidance/animal-health-and-welfare/280537
- Business Companion (UK), "The welfare of animals during transport": https://www.businesscompanion.info/en/quick-guides/animals-and-agriculture/the-welfare-of-animals-during-transport
- Mẫu chứng nhận USDA APHIS (ngựa con > 6 tháng, ngựa cái không đẻ trong chuyến): https://corpora.tika.apache.org/base/docs/govdocs1/289/289304.pdf
- Howden, "Understanding your bloodstock policy": https://www.howdengroup.com/au-en/news-insights/bloodstock-policy-essential-insights
- Marshall & Sterling, "Equine Mortality": https://www.marshallsterling.com/node/73
- UK Financial Ombudsman, quyết định về an tử và bảo hiểm ngựa: https://www.financial-ombudsman.org.uk/decision/DRN-5255982.pdf
- Google Maps Platform Prohibited Territories: https://cloud.google.com/maps-platform/terms/maps-prohibited-territories
