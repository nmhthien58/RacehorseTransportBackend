using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Implementations
{
    // R-17: vẽ hợp đồng vận chuyển ra PDF bằng QuestPDF (font Lato đi kèm gói, đủ dấu tiếng Việt).
    // Cùng dữ liệu luôn ra cùng một file (metadata cố định theo IssuedAt) nên mã băm ổn định khi sinh lại.
    public static class ContractPdfRenderer
    {
        public const string CompanyName = "Racehorse Transport";
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
        private static readonly CultureInfo VietnameseCulture = new("vi-VN");

        static ContractPdfRenderer()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static byte[] Render(ContractDocumentModel model, string policyText)
        {
            var issued = model.IssuedAt.ToUniversalTime();
            return Document.Create(container => container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(t => t.FontSize(10));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Text(CompanyName).Bold().FontColor(Colors.Green.Darken3);
                        row.RelativeItem().AlignRight().Text($"{model.ContractCode} · phiên bản {model.Version}").FontSize(9);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(6);
                        col.Item().AlignCenter().Text("HỢP ĐỒNG VẬN CHUYỂN NGỰA").Bold().FontSize(16);
                        col.Item().AlignCenter().Text(
                            $"Số {model.ContractCode} · Phiên bản {model.Version} ({ReasonText(model.Reason)}) · Đơn {model.BookingCode} · Lập ngày {Date(model.IssuedAt)}");

                        Section(col, "1. Các bên");
                        col.Item().Text(t =>
                        {
                            t.Span("Bên vận chuyển: ").Bold();
                            t.Span($"{CompanyName} (\"Công ty\").");
                        });
                        col.Item().Text(t =>
                        {
                            t.Span("Khách hàng: ").Bold();
                            t.Span($"{model.CustomerName} · {model.CustomerEmail}" + (string.IsNullOrWhiteSpace(model.CustomerPhone) ? "" : $" · {model.CustomerPhone}") + " (\"Khách\").");
                        });
                        col.Item().Text(t =>
                        {
                            t.Span("Người nhận ngựa: ").Bold();
                            t.Span(model.RecipientInfo ?? "Khách hàng (cập nhật khi Khách khai báo người nhận khác trên đơn).");
                        });

                        Section(col, "2. Ngựa được vận chuyển");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(24);
                                c.RelativeColumn(3);
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });
                            HeaderRow(table, "#", "Tên ngựa", "Số vi mạch", "Hạng chuồng", "Giá trị thỏa thuận", "Bảo hiểm");
                            int i = 1;
                            foreach (var horse in model.Horses)
                            {
                                Cell(table, (i++).ToString(CultureInfo.InvariantCulture));
                                Cell(table, horse.Name);
                                Cell(table, horse.MicrochipNumber);
                                Cell(table, horse.StallClass);
                                Cell(table, horse.AgreedValue.HasValue ? Money(horse.AgreedValue.Value, model.CurrencyCode) : "Không khai báo", right: true);
                                Cell(table, horse.InsurancePlan == InsurancePlans.Full ? "Toàn giá trị" : "Không mua");
                            }
                        });

                        Section(col, "3. Tuyến đường và thời gian");
                        col.Item().Text($"Phương thức: {(model.TransportMode == TransportModes.Air ? "Hàng không" : "Đường bộ")}.");
                        col.Item().Text($"Điểm đón: {model.PickupAddress} ({model.PickupCountryCode}).");
                        col.Item().Text($"Điểm giao: {model.DropoffAddress} ({model.DropoffCountryCode}).");
                        col.Item().Text("Nước quá cảnh: " + (model.TransitCountryCodes.Count == 0 ? "không có." : string.Join(" → ", model.TransitCountryCodes) + "."));
                        col.Item().Text($"Tổng quãng đường theo tuyến đã duyệt: {model.TotalDistanceKm.ToString("#,##0.#", VietnameseCulture)} km.");
                        if (model.Stops.Count > 0)
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(24);
                                    c.RelativeColumn(3);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(5);
                                });
                                HeaderRow(table, "#", "Mốc", "Loại", "Địa chỉ");
                                int i = 1;
                                foreach (var stop in model.Stops)
                                {
                                    Cell(table, (i++).ToString(CultureInfo.InvariantCulture));
                                    Cell(table, stop.Name);
                                    Cell(table, StopTypeText(stop.StopType));
                                    Cell(table, stop.Address + (stop.CountryCode == null ? "" : $" ({stop.CountryCode})"));
                                }
                            });
                        }
                        col.Item().Text($"Khoảng ngày ngựa sẵn sàng được đón: {Date(model.PickupWindowStart)} – {Date(model.PickupWindowEnd)}. Hạn giao trễ nhất: {Date(model.DeliveryDeadline)}.");

                        Section(col, "4. Giá và thanh toán");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(6);
                                c.RelativeColumn(1);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });
                            HeaderRow(table, "Khoản mục", "SL", "Đơn giá", "Thành tiền");
                            foreach (var line in model.Lines)
                            {
                                Cell(table, line.Name);
                                Cell(table, line.Quantity.ToString("#,##0.##", VietnameseCulture), right: true);
                                Cell(table, Money(line.UnitPrice, model.CurrencyCode), right: true);
                                Cell(table, Money(line.Amount, model.CurrencyCode), right: true);
                            }
                            table.Cell().ColumnSpan(3).Padding(3).AlignRight().Text("Tổng giá chốt").Bold();
                            table.Cell().Padding(3).AlignRight().Text(Money(model.Total, model.CurrencyCode)).Bold();
                        });
                        col.Item().Text(t =>
                        {
                            t.Span($"Tiền cọc {model.DepositPercent.ToString("#,##0.##", VietnameseCulture)}%: ").Bold();
                            t.Span(Money(model.DepositAmount, model.CurrencyCode)
                                + (model.DepositDueAt.HasValue ? $", trả trước {DateTime(model.DepositDueAt.Value)} (giờ Việt Nam)." : "."));
                        });
                        col.Item().Text(t =>
                        {
                            t.Span("Phần còn lại: ").Bold();
                            t.Span($"{Money(model.BalanceAmount, model.CurrencyCode)}, trả trước ngày xuất phát {model.BalanceDueDaysBeforeDeparture} ngày.");
                        });
                        col.Item().Text($"Chi phí phát sinh (nếu có, theo Chính sách dịch vụ mục 9, 10) trả trong {model.ExtraPaymentDueDays} ngày kể từ khi kết thúc chuyến.");

                        Section(col, "5. Giao kết hợp đồng");
                        col.Item().Text("Tuyến đường và báo giá do Quản lý điều phối duyệt"
                            + (model.ApprovedByName == null ? "" : $": {model.ApprovedByName}")
                            + (model.ApprovedAt.HasValue ? $", ngày {Date(model.ApprovedAt.Value)}" : "") + ".");
                        col.Item().Text($"Chính sách dịch vụ phiên bản {model.PolicyVersion} (phụ lục dưới đây) là phần không tách rời của hợp đồng. "
                            + "Khi Khách tích \"Tôi đã đọc hợp đồng và Chính sách dịch vụ\" và bấm \"Chấp nhận & đặt cọc\" trên ứng dụng, hợp đồng được giao kết theo hình thức điện tử; "
                            + "hệ thống lưu tài khoản, thời điểm, địa chỉ IP, phiên bản chính sách và mã băm SHA-256 của file này làm bằng chứng.");

                        col.Item().PageBreak();
                        col.Item().Text("PHỤ LỤC – CHÍNH SÁCH DỊCH VỤ").Bold().FontSize(13);
                        RenderMarkdown(col, policyText);
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span($"{model.ContractCode} v{model.Version} · Trang ").FontSize(8);
                        t.CurrentPageNumber().FontSize(8);
                        t.Span(" / ").FontSize(8);
                        t.TotalPages().FontSize(8);
                    });
                }))
                .WithMetadata(new DocumentMetadata
                {
                    Title = $"Hợp đồng vận chuyển {model.ContractCode} v{model.Version}",
                    Author = CompanyName,
                    Subject = model.BookingCode,
                    CreationDate = issued,
                    ModifiedDate = issued
                })
                .GeneratePdf();
        }

        public static string Sha256(byte[] content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

        // ---------------------------------------------------------------------------------------------

        private static void Section(ColumnDescriptor col, string title) =>
            col.Item().PaddingTop(8).Text(title).Bold().FontSize(12);

        private static void HeaderRow(TableDescriptor table, params string[] titles)
        {
            table.Header(header =>
            {
                foreach (var title in titles)
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(title).Bold();
                }
            });
        }

        private static void Cell(TableDescriptor table, string text, bool right = false)
        {
            var cell = table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3);
            if (right)
            {
                cell.AlignRight().Text(text);
            }
            else
            {
                cell.Text(text);
            }
        }

        // Markdown đơn giản của văn bản chính sách: tiêu đề #, gạch đầu dòng, danh sách số, bảng |...|, **đậm**
        private static void RenderMarkdown(ColumnDescriptor col, string markdown)
        {
            var lines = markdown.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd();
                if (line.Length == 0 || line == "---")
                {
                    continue;
                }
                if (line.StartsWith('|'))
                {
                    var rows = new List<string[]>();
                    while (i < lines.Length && lines[i].TrimStart().StartsWith('|'))
                    {
                        var cells = lines[i].Trim().Trim('|').Split('|').Select(c => c.Trim()).ToArray();
                        if (!cells.All(c => Regex.IsMatch(c, "^:?-+:?$")))
                        {
                            rows.Add(cells);
                        }
                        i++;
                    }
                    i--;
                    RenderTable(col, rows);
                    continue;
                }

                var heading = Regex.Match(line, "^(#{1,4}) (.*)$");
                if (heading.Success)
                {
                    float size = heading.Groups[1].Length switch { 1 => 13, 2 => 12, _ => 11 };
                    col.Item().PaddingTop(6).Text(t => Inline(t, heading.Groups[2].Value, size, bold: true));
                    continue;
                }

                var bullet = Regex.Match(line, @"^(\s*)[-*] (.*)$");
                if (bullet.Success)
                {
                    float indent = 8 + bullet.Groups[1].Length * 4;
                    col.Item().PaddingLeft(indent).Row(row =>
                    {
                        row.ConstantItem(10).Text("•");
                        row.RelativeItem().Text(t => Inline(t, bullet.Groups[2].Value));
                    });
                    continue;
                }

                var numbered = Regex.Match(line, @"^(\d+)\. (.*)$");
                if (numbered.Success)
                {
                    col.Item().PaddingLeft(8).Row(row =>
                    {
                        row.ConstantItem(16).Text(numbered.Groups[1].Value + ".");
                        row.RelativeItem().Text(t => Inline(t, numbered.Groups[2].Value));
                    });
                    continue;
                }

                col.Item().Text(t => Inline(t, line));
            }
        }

        private static void RenderTable(ColumnDescriptor col, List<string[]> rows)
        {
            if (rows.Count == 0)
            {
                return;
            }
            int columns = rows.Max(r => r.Length);
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    for (int k = 0; k < columns; k++)
                    {
                        c.RelativeColumn();
                    }
                });
                for (int r = 0; r < rows.Count; r++)
                {
                    for (int k = 0; k < columns; k++)
                    {
                        string text = k < rows[r].Length ? rows[r][k] : string.Empty;
                        var cell = table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3);
                        if (r == 0)
                        {
                            cell = cell.Background(Colors.Grey.Lighten3);
                        }
                        bool header = r == 0;
                        cell.Text(t => Inline(t, text, 9, bold: header));
                    }
                }
            });
        }

        // **đậm** và `mã`: bỏ dấu backtick, tách phần đậm
        private static void Inline(TextDescriptor t, string text, float size = 10, bool bold = false)
        {
            text = text.Replace("`", string.Empty);
            var parts = text.Split("**");
            for (int k = 0; k < parts.Length; k++)
            {
                if (parts[k].Length == 0)
                {
                    continue;
                }
                var span = t.Span(parts[k]).FontSize(size);
                if (bold || k % 2 == 1)
                {
                    span.Bold();
                }
            }
        }

        private static string ReasonText(string reason) => reason switch
        {
            ContractReasons.PickupDropoffChanged => "đổi điểm đón / giao",
            ContractReasons.DetourAccepted => "đồng ý đi đường vòng",
            ContractReasons.RecipientChanged => "đổi người nhận",
            _ => "bản đầu theo báo giá chốt"
        };

        private static string StopTypeText(string stopType) => stopType switch
        {
            RouteStopTypes.Origin => "Điểm đón",
            RouteStopTypes.Destination => "Điểm giao",
            RouteStopTypes.BorderGate => "Cửa khẩu",
            _ => "Trạm nghỉ"
        };

        private static string Money(decimal amount, string currency) =>
            $"{amount.ToString("#,##0.00", VietnameseCulture)} {currency}";

        private static string Date(DateTimeOffset value) =>
            value.ToOffset(VietnamOffset).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        private static string DateTime(DateTimeOffset value) =>
            value.ToOffset(VietnamOffset).ToString("HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture);
    }
}
