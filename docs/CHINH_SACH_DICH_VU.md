# CHÍNH SÁCH DỊCH VỤ VẬN CHUYỂN NGỰA — RACEHORSE TRANSPORT

Phiên bản: 1.0 (bản nháp, 26/10/2026)

> Ghi chú cho nhóm phát triển (không hiển thị cho khách):
> - Các con số trong ngoặc vuông `[...]` là **giá trị cấu hình**, trang web phải đọc từ cấu hình hệ thống thay vì viết cứng. Tên khóa cấu hình ghi bên cạnh.
> - Khách phải tích **"Tôi đã đọc và đồng ý với Chính sách dịch vụ"** khi gửi yêu cầu vận chuyển và khi chấp nhận báo giá. Hệ thống lưu số phiên bản chính sách và thời điểm đồng ý vào đơn.
> - Nguồn gốc từng quy tắc: xem `PLAN_NGHIEP_VU_R09.md` (cùng thư mục).
> - **Bản gốc nằm ở repo backend `docs/`.** Sửa ở đó trước rồi chép sang `doc/` của repo frontend để hai bản luôn giống nhau.

---

## 1. Phạm vi áp dụng

Chính sách này áp dụng cho mọi yêu cầu vận chuyển ngựa đặt qua hệ thống Racehorse Transport, bằng đường bộ hoặc đường hàng không, trong nước và quốc tế. Khi đặt đơn, khách hàng ("Khách") đồng ý với toàn bộ chính sách dưới đây.

Mọi số tiền được báo bằng **USD**. Khi thanh toán qua VNPay, số tiền được quy đổi sang VND theo tỉ giá hệ thống tại thời điểm thanh toán `[Payment.UsdVndRate = 26.000 đ/USD]`; tỉ giá và số tiền VND được ghi lại trên từng giao dịch.

---

## 2. Quy trình đặt chuyến và báo giá

1. **Gửi yêu cầu.** Khách chọn điểm đón, điểm giao trên bản đồ, chọn ngựa, khoảng ngày ngựa sẵn sàng được đón và hạn giao trễ nhất. Hệ thống hiển thị **khoảng giá ước tính** (thấp nhất – cao nhất). Khoảng giá này chỉ để tham khảo, **không phải giá cuối cùng**. Ở bước này Khách chưa phải trả tiền.
2. **Duyệt sơ bộ.** Quản lý kiểm tra yêu cầu có thực hiện được không (ngựa đủ điều kiện, tuyến phục vụ, lịch). Yêu cầu có thể bị từ chối kèm lý do.
3. **Lập tuyến và báo giá chốt.** Công ty lập tuyến đường cụ thể (các điểm dừng, cửa khẩu, nước quá cảnh) và gửi **báo giá chốt** cho Khách qua email và trên ứng dụng. Báo giá chốt gồm: cước vận chuyển theo quãng đường thực tế, hạng chuồng, phí thủ tục cho từng nước đi qua, phí bảo hiểm (nếu chọn), phụ phí gấp (nếu có).
4. **Xem hợp đồng, chấp nhận báo giá và đặt cọc.** Cùng báo giá chốt, hệ thống tạo **Hợp đồng vận chuyển** (mục 12). Khách có `[Booking.QuoteAcceptDays = 3]` ngày để xem hợp đồng, chấp nhận báo giá và đặt cọc. Quá hạn, đơn tự hủy và Khách không mất phí.
5. **Làm hồ sơ.** Sau khi cọc, chuyên viên yêu cầu các giấy tờ cần thiết theo nước đi, nước quá cảnh và nước đến. Khách nộp giấy trên ứng dụng trong thời hạn được thông báo.
6. **Xếp chuyến.** Công ty xếp ngựa vào chuyến cụ thể (xe, đoàn, lịch chạy). Một chuyến có thể chở ngựa của nhiều khách cùng tuyến.
7. **Thanh toán phần còn lại** trước ngày xuất phát (mục 3).
8. **Vận chuyển và giao nhận.** Khách theo dõi hành trình trên ứng dụng. Người nhận ký biên bản bàn giao điện tử khi nhận ngựa.

**Đổi tuyến sau khi đã chốt giá:**
- Công ty đổi tuyến vì lý do vận hành: **giá đã chốt giữ nguyên**, công ty chịu phần chênh lệch. Nếu tuyến mới đi qua nước mới cần thêm giấy tờ mà Khách không thể cung cấp, Khách được hủy đơn và **hoàn 100%** số tiền đã trả.
- Khách đổi điểm đón / điểm giao / lịch: công ty gửi báo giá mới, Khách chấp nhận thì đơn mới tiếp tục.

---

## 3. Thanh toán

| Khoản | Số tiền | Thời hạn |
|---|---|---|
| **Tiền cọc** | `[Payment.DepositPercent = 30]`% báo giá chốt | Khi chấp nhận báo giá, trong `[Booking.QuoteAcceptDays = 3]` ngày |
| **Phần còn lại** | Báo giá chốt − tiền cọc | Trước ngày xuất phát `[Payment.BalanceDueDaysBeforeDeparture = 2]` ngày |
| **Chi phí phát sinh** | Theo mục 9, 10 (nếu có) | Trong `[Payment.ExtraPaymentDueDays = 7]` ngày kể từ khi kết thúc chuyến |

- Ngựa **không được lên xe** nếu đơn chưa thanh toán đủ phần còn lại.
- Chưa trả chi phí phát sinh quá hạn: hệ thống nhắc nợ, và Khách **không tạo được đơn mới** cho tới khi trả xong.
- Tài xế và nhân viên áp tải **không thu tiền mặt**. Mọi khoản thanh toán thực hiện trên ứng dụng.

---

## 4. Hủy đơn và hoàn tiền

| Thời điểm Khách hủy | Hoàn tiền |
|---|---|
| Trước khi đặt cọc | Không có khoản nào phải hoàn |
| Sau khi cọc, trước khi xuất phát | Hoàn `[Payment.CancelRefundPercent = 50]`% tiền cọc + 100% phần còn lại đã trả |
| Sau khi xuất phát | Khách không tự hủy được. Mọi thay đổi xử lý theo mục 9 |

**Khách không nộp đủ giấy tờ đúng hạn:** hệ thống nhắc 2 lần. Hết hạn mà vẫn thiếu, đơn bị hủy và tính như **Khách hủy sau khi cọc** (bảng trên).

**Công ty hủy đơn** vì lý do của công ty: hoàn **100%** số tiền Khách đã trả.

Tiền hoàn được trả về phương thức thanh toán ban đầu (VNPay). Trường hợp không hoàn tự động được, công ty chuyển khoản thủ công và thông báo mã giao dịch cho Khách.

---

## 5. Điều kiện ngựa được vận chuyển

Công ty **từ chối vận chuyển** ngựa thuộc một trong các trường hợp sau:
- Ngựa dưới `[Horse.MinAgeMonths = 6]` tháng tuổi (tính đến ngày cuối của khoảng ngày đón).
- Ngựa cái mang thai **đã quá 90% thai kỳ** tại ngày đi, hoặc **vừa đẻ trong 7 ngày**.
- Ngựa ốm, bị thương, suy yếu hoặc có dấu hiệu bệnh lúc nhận ngựa.

Công ty **không giới hạn tuổi tối đa**. Thay vào đó, **mọi con ngựa** phải có giấy khám lâm sàng của bác sĩ thú y xác nhận đủ sức khỏe để đi, cấp trong vòng **48 giờ** trước giờ xuất phát.

**Khách cam kết khai báo trung thực** về tuổi, tình trạng mang thai, tiền sử bệnh và giá trị của ngựa. Khai sai là lỗi của Khách (mục 9) và có thể làm mất quyền bồi thường bảo hiểm (mục 8).

---

## 6. Giấy tờ và thủ tục

- Khách cung cấp giấy tờ của ngựa (hộ chiếu ngựa, vi mạch, kết quả xét nghiệm, tiêm phòng…) theo danh sách chuyên viên yêu cầu cho từng nước đi, quá cảnh và đến, trong vòng `[Dossier.SubmitDays = 7]` ngày kể từ khi đặt cọc. Hệ thống nhắc qua email khi còn 3 ngày và 1 ngày. Quá hạn mà còn thiếu giấy, đơn bị hủy và hoàn tiền theo mục 4 (như Khách tự hủy sau khi cọc).
- Nếu công ty đổi tuyến sau khi Khách đã chấp nhận báo giá và tuyến mới đi qua nước mới, Khách được báo danh sách giấy cần thêm cùng hạn nộp mới; nếu không lo được giấy, Khách được hủy đơn và **hoàn 100%** số tiền đã trả (mục 2).
- Công ty thay mặt Khách xin giấy phép nhập khẩu / quá cảnh và chứng nhận kiểm dịch với cơ quan chức năng.
- **Giấy phép trước khi đi không bảo đảm chắc chắn qua được cửa khẩu.** Tại cửa khẩu, cơ quan thú y và hải quan vẫn kiểm tra thực tế và có quyền từ chối (xem mục 9.3).
- Giấy tờ phải **còn hiệu lực tại giờ xuất phát thực tế**. Nếu chuyến bị lùi làm giấy hết hạn:
  - Lùi do công ty: công ty chịu chi phí làm lại giấy.
  - Lùi do Khách: Khách chịu chi phí làm lại giấy.

---

## 7. Cam kết thời gian giao và bồi thường giao trễ

- Công ty cam kết giao ngựa **trước hạn giao trễ nhất** Khách đã chọn. Hạn giao được hệ thống kiểm tra để đủ thời gian cho trường hợp ngựa được đón vào **ngày cuối** của khoảng ngày đón.
- **Bồi thường giao trễ** (chỉ khi trễ do lỗi của công ty):
  - Trễ không quá `[Delay.GraceHours = 6]` giờ: không bồi thường.
  - Sau đó, hoàn `[Delay.PercentPer12Hours = 5]`% **tiền cước vận chuyển** cho mỗi 12 giờ trễ (chưa đủ 12 giờ tính tròn lên 12 giờ), tối đa `[Delay.MaxPercent = 25]`%.
  - "Tiền cước vận chuyển" không bao gồm phí bảo hiểm và phí thủ tục giấy tờ.
- **Không bồi thường** khi trễ do bất khả kháng (mục 9) hoặc do Khách (nộp giấy trễ, người nhận vắng mặt…).
- **Không bồi thường thiệt hại gián tiếp** do giao trễ: lỡ cuộc đua, mất tiền thưởng, mất hợp đồng…
- Bồi thường được tự tính khi chuyến kết thúc, Quản lý duyệt và hoàn vào phương thức thanh toán của Khách.

Bồi thường giao trễ là **chính sách riêng của công ty**, không thuộc bảo hiểm (mục 8).

---

## 8. Bảo hiểm và giới hạn trách nhiệm

### 8.1 Giá trị ngựa
- Khách khai giá trị **từng con ngựa** khi gửi yêu cầu.
- Ngựa có giá trị từ `[Insurance.ProofThresholdUsd = 50.000]` USD trở lên phải kèm chứng từ: hóa đơn mua, giấy định giá của bác sĩ thú y hoặc chuyên gia định giá, hoặc kết quả đấu giá.
- Quản lý xác nhận **giá trị thỏa thuận** khi gửi báo giá chốt. Mọi khoản bồi thường tính theo giá trị thỏa thuận này, **không định giá lại** sau sự cố.

### 8.2 Hai lựa chọn

| | Không mua bảo hiểm (mặc định) | Bảo hiểm toàn giá trị (tùy chọn) |
|---|---|---|
| Phí | Không | `[Insurance.PremiumPercent = 1]`% giá trị thỏa thuận |
| Khi nào được bồi thường | **Chỉ khi do lỗi công ty** | Mọi nguyên nhân trong phạm vi bảo hiểm, **kể cả bất khả kháng** |
| Ngựa chết / phải an tử / mất | Tối đa `[Insurance.BaseLiabilityUsd = 5.000]` USD mỗi con | **100%** giá trị thỏa thuận |
| Ngựa bị thương | Chi phí thú y thực tế, tối đa `[Insurance.BaseVetCapUsd = 1.000]` USD mỗi con | Chi phí thú y thực tế, tối đa `[Insurance.VetCapPercent = 20]`% giá trị thỏa thuận |

### 8.3 Thời gian được bảo hiểm
Từ lúc ngựa được đưa lên xe tại điểm đón đến lúc người nhận ký biên bản bàn giao. Thời gian ngựa tạm dừng tại chuồng hoặc phòng khám thú y do công ty sắp xếp giữa hành trình cũng được tính.

### 8.4 Không bồi thường
- Bệnh hoặc thương tật có từ trước khi nhận ngựa, kể cả bệnh đang ủ chưa phát hiện được lúc khám.
- Khách khai sai tuổi, tình trạng mang thai, tiền sử bệnh, giá trị, hoặc giấy tờ sai / giả.
- An tử không có xác nhận của bác sĩ thú y về lý do nhân đạo khẩn cấp.
- Ngựa chết mà Khách không đồng ý khám nghiệm khi được yêu cầu.
- Thiệt hại gián tiếp: lỡ cuộc đua, mất tiền thưởng, giảm giá trị giống.
- Ngựa bị giữ, cách ly hoặc tiêu hủy theo lệnh cơ quan chức năng vì giấy tờ của Khách hoặc vì dịch bệnh.
- Giao trễ (thuộc mục 7).

### 8.5 Yêu cầu bồi thường
1. Hồ sơ bồi thường được **tự mở** khi có sự cố nghiêm trọng với ngựa, ngựa chết, ngựa phải dừng hành trình vì sức khỏe, hoặc khi biên bản bàn giao ghi ngựa bị thương.
2. Khách tự mở yêu cầu **trong vòng 24 giờ** sau khi nhận ngựa nếu phát hiện thương tích. Quá thời hạn này, yêu cầu không được chấp nhận.
3. Chứng từ cần có: biên bản của bác sĩ thú y, khám nghiệm tử thi (nếu ngựa chết, trừ khi công ty miễn), ảnh, hóa đơn thú y.
4. Công ty trả lời trong **7 ngày làm việc**: chấp nhận (kèm số tiền) hoặc từ chối (kèm lý do theo mục 8.4).
5. Tiền bồi thường được chuyển cho Khách qua phương thức thanh toán ban đầu hoặc chuyển khoản.

---

## 9. Sự cố trong hành trình

### 9.1 Nguyên tắc chung: ai chịu lỗi thì người đó chịu chi phí

| Nguyên nhân | Ví dụ | Tiền cước phần chưa đi | Chi phí phát sinh (chờ, quay đầu, chuồng tạm, thú y) | Bồi thường trễ |
|---|---|---|---|---|
| **Lỗi công ty** | Xe hỏng do bảo dưỡng, thiếu giấy do chuyên viên, đi sai tuyến | Hoàn 100% | Công ty chịu | Có (mục 7) |
| **Lỗi Khách** | Giấy sai / giả, khai sai tình trạng ngựa, người nhận vắng mặt / từ chối | Không hoàn | Khách chịu | Không |
| **Bất khả kháng** | Đóng biên giới, đổi chính sách nhập khẩu, thiên tai, dịch bệnh, tai nạn do bên thứ ba | Hoàn theo tỉ lệ quãng đường chưa đi, trừ phí thủ tục đã nộp không lấy lại được | Công ty chịu chi phí xe và nhân sự; Khách chịu chuồng tạm và thú y | Không |

Khi có sự cố, Khách được thông báo ngay trên ứng dụng và qua email: nguyên nhân, phương án xử lý đã được duyệt, thay đổi tuyến (nếu có). Mọi chi phí Khách phải chịu được báo trước khi thực hiện, trừ trường hợp khẩn cấp để bảo vệ tính mạng ngựa.

### 9.2 Xe hỏng, nhân sự không thể tiếp tục
- Công ty sửa xe tại chỗ hoặc điều xe thay thế cùng loại. Nếu phải chờ lâu, ngựa được đưa xuống chuồng tạm gần nhất.
- Chuyến có thời gian lái trên 9 giờ luôn có ít nhất 2 tài xế. Khi một người không thể tiếp tục, công ty điều người thay thế tới, đoàn tạm dừng chờ.
- Các trường hợp này là trách nhiệm của công ty (trừ tai nạn do bên thứ ba).

### 9.3 Cửa khẩu từ chối cho qua
Công ty chọn một trong các phương án: bổ sung / sửa giấy rồi thử lại, đi cửa khẩu khác, hoặc quay về nơi đi. Chi phí và hoàn tiền theo bảng 9.1:
- Do giấy tờ hoặc tình trạng ngựa Khách khai sai: lỗi Khách.
- Do công ty thiếu giấy, hoặc giấy hết hạn vì công ty làm chậm chuyến: lỗi công ty.
- Do nước sở tại đổi quy định hoặc đóng nhập vì dịch: bất khả kháng.

### 9.4 Biên giới đóng
Công ty có thể cho đoàn chờ biên giới mở lại (ngựa ở chuồng tạm), đi cửa khẩu khác, hoặc quay về. Nếu đi đường vòng tốn thêm chi phí, công ty **hỏi ý Khách trước**. Khách không đồng ý thì đoàn quay về và Khách được hoàn theo bảng 9.1 (bất khả kháng).

### 9.5 Ngựa có vấn đề sức khỏe giữa đường
- Ngựa có thể phải dừng hành trình tại phòng khám hoặc chuồng gần nhất theo chỉ định của bác sĩ thú y.
- Sau đó Khách được thông báo và cùng công ty chọn: chờ hồi phục rồi đi chuyến sau, đưa về nơi đi, hoặc Khách nhận ngựa tại chỗ.
- Chi phí theo bảng 9.1. Bồi thường theo mục 8.

### 9.6 Ngựa chết
- Chỉ bác sĩ thú y được quyết định an tử, và chỉ vì lý do nhân đạo khẩn cấp.
- Công ty thông báo Khách ngay, lập biên bản có xác nhận của bác sĩ thú y, và mở hồ sơ bồi thường (mục 8).
- Phần cước chưa đi của con ngựa đó được hoàn theo bảng 9.1.

---

## 10. Giao nhận

### 10.1 Thông tin người nhận
- Khi gửi yêu cầu, Khách nhập thông tin **người nhận ngựa**: họ tên, số điện thoại (bắt buộc), email (nên có), số CMND / CCCD / hộ chiếu (không bắt buộc, dùng để đối chiếu khi giao). Khách có thể chọn "Tôi tự nhận ngựa".
- Khách được sửa thông tin người nhận cho tới khi xe tới điểm giao.

### 10.2 Liên hệ trước khi giao
- Người nhận được gửi **email** khi chuyến được lên lịch, khi xe xuất phát (kèm giờ dự kiến tới), và khi xe sắp tới (rời điểm dừng ngay trước điểm giao).
- **24 giờ trước giờ dự kiến tới**, Khách được nhắc **xác nhận trên ứng dụng** rằng người nhận sẽ có mặt (hoặc cập nhật người nhận khác).
- Khi xe sắp tới, tài xế **gọi điện** cho người nhận theo số điện thoại trong đơn.
- Khi người nhận ký biên bản bàn giao, Khách được thông báo ngay trên ứng dụng và qua email.

### 10.3 Bàn giao
- Người nhận xuất trình giấy tờ tùy thân và ký biên bản bàn giao điện tử. Biên bản ghi tình trạng ngựa lúc nhận.
- Người khác nhận thay phải được Khách xác nhận trên ứng dụng trước khi bàn giao.
- Thương tích phát hiện sau khi nhận phải báo **trong vòng 24 giờ** (mục 8.5).

### 10.4 Phí chờ
- Xe chờ **miễn phí 2 giờ** tại điểm giao. Sau đó tính `[Delivery.WaitingFeePerHourUsd = 40]` USD mỗi giờ (chưa đủ giờ tính tròn lên), tối đa `[Delivery.MaxWaitingHours = 24]` giờ.
- Chỉ tính phí chờ khi xe tới **đúng hoặc sau** khung giờ đã báo cho người nhận. Xe tới sớm hơn khung đã báo thì không tính.
- **Khách (người đặt đơn) trả phí chờ**, không phải người nhận. Phí được cộng vào **chi phí phát sinh** của đơn, Khách thanh toán qua VNPay trên ứng dụng theo mục 3. Trạng thái "đã thanh toán" được ghi tự động khi VNPay xác nhận.

### 10.5 Người nhận vắng mặt hoặc từ chối nhận
- **Không có lý do chính đáng:** công ty báo Khách ngay. Trong **24 giờ**, Khách chọn trên ứng dụng một trong các cách:
  1. Chỉ định người nhận hoặc địa chỉ khác gần đó (tính phí theo quãng đường thêm).
  2. Gửi ngựa ở chuồng tạm gần nhất (Khách trả phí chuồng và chăm sóc theo ngày), Khách tự tới nhận.
  3. Chở ngựa về nơi đi (Khách trả chi phí chiều về theo báo giá mới).

  Khách không trả lời sau 24 giờ thì áp dụng **cách 2**. Tiền cước của đơn **không được hoàn** vì dịch vụ vận chuyển đã hoàn thành.
- **Từ chối nhận vì ngựa bị thương hoặc giao nhầm ngựa:** không phải lỗi Khách. Người nhận ghi rõ lý do kèm ảnh vào biên bản. Công ty chăm sóc ngựa tại chuồng tạm **bằng chi phí của công ty** cho tới khi xử lý xong, và mở hồ sơ bồi thường (mục 8).

---

## 11. Khiếu nại

- Khiếu nại về dịch vụ, chi phí phát sinh hoặc bồi thường gửi qua ứng dụng trong vòng **7 ngày** kể từ khi chuyến kết thúc (riêng thương tích của ngựa: 24 giờ, mục 8.5).
- Công ty trả lời trong **7 ngày làm việc**.

---

## 12. Hợp đồng vận chuyển

- Khi có báo giá chốt, hệ thống tạo **Hợp đồng vận chuyển** cho đơn, gồm: thông tin hai bên và người nhận; danh sách ngựa, giá trị thỏa thuận và gói bảo hiểm từng con; tuyến đường, khoảng ngày đón, hạn giao; giá chốt, tiền cọc, phần còn lại và các hạn thanh toán. Chính sách dịch vụ này (đúng phiên bản hiện hành) là phụ lục không tách rời của hợp đồng.
- Khách xem toàn bộ hợp đồng trên ứng dụng **trước khi** đồng ý. Khi Khách tích "Tôi đã đọc hợp đồng và Chính sách dịch vụ" và bấm **Chấp nhận & đặt cọc**, hợp đồng được giao kết theo hình thức điện tử; Khách không phải ký thêm.
- Hệ thống lưu bằng chứng giao kết: tài khoản đồng ý, thời điểm, địa chỉ IP, phiên bản chính sách và mã nhận dạng nội dung hợp đồng. Hợp đồng đã giao kết không thể bị sửa. Bản PDF được gửi qua email và Khách tải lại được bất cứ lúc nào trên trang đơn.
- Khi điều khoản thay đổi theo yêu cầu hoặc với sự đồng ý của Khách (đổi điểm đón / giao, đồng ý trả thêm để đi đường vòng, đổi người nhận), hệ thống tạo **bản hợp đồng mới** thay thế bản cũ, Khách xem và chấp nhận lại theo cách trên. Các khoản phát sinh đã quy định trong chính sách này (phí chờ, chuồng tạm, chi phí theo bảng 9.1) được áp dụng mà không cần bản hợp đồng mới.

---

## 13. Thay đổi chính sách

Công ty có thể cập nhật chính sách này. Đơn đã đặt áp dụng **phiên bản chính sách Khách đã đồng ý** lúc chấp nhận báo giá (ghi trong hợp đồng, mục 12). Các con số trong ngoặc vuông là giá trị cấu hình hiện hành và được hiển thị đúng trên trang Chính sách.
