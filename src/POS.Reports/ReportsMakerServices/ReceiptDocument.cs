namespace POS.Reports.ReportsMakerServices;

public class ReceiptDocument : IDocument
{
    private readonly List<TableItem> _items;
    private readonly Receipt receipt;

    public ReceiptDocument(Receipt _receipt, List<TableItem> items)
    {
        receipt = _receipt;
        _items = items;
    }
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            ConfigurePage(ref page);
            
            if (receipt.IsCopy)
            {
                page.Foreground()
                    .AlignCenter()
                    .AlignMiddle()
                    .Rotate(-45)
                    .Text("COPY")
                    .FontSize(60)
                    .FontColor(Colors.Grey.Lighten3);
            }

            page.Content()
                .Column(column =>
                {
                    AddHeader(ref column);
                    BuildDateAndCashierInfo(ref column);
                    BuildItemsTable(ref column);
                    BuildPaymentMethod(ref column);
                    //BuildBarcodeOrNoteSection(ref column);
                });

            page.Footer()
                .Column(column => { BuildFooter(ref column); });
        });
    }

    private void ConfigurePage(ref PageDescriptor page)
    {
        page.ContinuousSize(7.2f, Unit.Centimetre); // 72mm printable width
        page.PageColor(Colors.White);
        page.MarginTop(5);
        page.MarginRight(2);
        page.MarginBottom(5);
        page.MarginLeft(2);
        page.DefaultTextStyle(
            TextStyle.Default
                .FontFamily("Noto Sans", "Noto Sans Arabic")
                .FontSize(12)
                .Bold()); // Bold for better darkness on thermal printers
    }

    private void AddHeader(ref ColumnDescriptor column)
    {
        // Store Name at the top
        column.Item()
            .Text(text =>
            {
                text.Span(receipt.StoreName)
                    .Bold()
                    .FontSize(18);
                text.AlignCenter();
            });

        // 🖼️ Logo section underneath Store Name
        if (!string.IsNullOrEmpty(receipt.LogoPath) && File.Exists(receipt.LogoPath))
        {
            column.Item()
            .PaddingTop(5)
            .AlignCenter()
            .MaxHeight(100)
            .MaxWidth(180)
            .Image(receipt.LogoPath)
            .FitArea();
        }

        column.Item()
            .PaddingTop(8)
            .Text(text =>
            {
                text.Span(receipt.Id.ToString("0.##"))
                    .Bold()
                    .FontSize(18);
                
                if (receipt.IsFollowUp)
                {
                    text.Span(" - تابع")
                        .Bold()
                        .FontSize(18)
                        .FontColor(Colors.Grey.Medium);
                }
                
                text.AlignCenter();
            });

        /*Header * Type */
        column.Item()
            .PaddingTop(1)
            .Text(text =>
            {
                if (receipt.IsCopy)
                {
                    text.Span("*********COPY**********")
                        .Bold()
                        .FontSize(20)
                        .FontColor(Colors.Grey.Medium);
                    text.EmptyLine();
                }

                text.Span(receipt.ReceiptType)
                    .Bold()
                    .FontSize(18);
                
                if (receipt.IsCopy)
                {
                    text.EmptyLine();
                    text.Span("*********COPY**********")
                        .Bold()
                        .FontSize(20)
                        .FontColor(Colors.Grey.Medium);
                }

                text.AlignCenter();
            });
    }

    private void BuildDateAndCashierInfo(ref ColumnDescriptor column)
    {
        column.Item()
            .PaddingTop(5)
            .Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                });

                table.Cell()
                    .Element(CellStyle)
                    .Text(receipt.DateCreated.ToString("d/MM/yyyy"))
                    .AlignCenter();

                table.Cell()
                    .Element(CellStyle)
                    .Text(receipt.DateCreated.ToString("hh:mm:ss tt"))
                    .AlignCenter();

                table.Cell()
                    .Element(CellStyle)
                    .Text(receipt.CashierName)
                    .AlignCenter();

                table.Cell()
                    .Element(CellStyle)
                    .Text(ArabicConstStrings.From)
                    .AlignCenter();
            });
    }

    private void BuildItemsTable(ref ColumnDescriptor column)
    {
        // Group items before printing
        var groupedItems = _items
            .GroupBy(i => new
            {
                i.Id,
                i.Name,
                i.Price,
                i.IsVoided,
                i.HasDiscount,
                i.DiscountPercentage,
                AttributesHash = string.Join("|", i.Attributes?.OrderBy(a => a.Id).Select(a => a.Id) ?? Enumerable.Empty<int?>())
            })
            .Select(g =>
            {
                var first = g.First();
                return new TableItem
                {
                    Id = first.Id,
                    Name = first.Name,
                    Price = first.Price,
                    Quantity = g.Sum(x => x.Quantity),
                    TotalAmount = g.Sum(x => x.TotalAmount ?? (x.Price * x.Quantity) ?? 0),
                    Attributes = first.Attributes,
                    IsVoided = first.IsVoided,
                    HasDiscount = first.HasDiscount,
                    DiscountPercentage = first.DiscountPercentage,
                    TotalDiscountPrice = g.Sum(x => x.TotalDiscountPrice ?? 0)
                };
            }).ToList();

        column.Item()
            .PaddingTop(3)
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.5f);  // اجمالي
                    columns.RelativeColumn(2.5f);  // السعر
                    columns.RelativeColumn(3.5f);  // الصنف
                    columns.RelativeColumn(1.5f);  // كمية
                });

                // Table Header
                table.Header(header =>
                {
                    header.Cell()
                        .Element(CellStyle)
                        .Text(ArabicConstStrings.Total)
                        .AlignCenter()
                        .Bold();

                    header.Cell()
                        .Element(CellStyle)
                        .Text(ArabicConstStrings.Price)
                        .AlignCenter()
                        .Bold();

                    header.Cell()
                        .Element(CellStyle)
                        .Text(ArabicConstStrings.Name)
                        .AlignCenter()
                        .Bold();

                    header.Cell()
                        .Element(CellStyle)
                        .Text(ArabicConstStrings.Quantity)
                        .AlignCenter()
                        .Bold();
                });

                foreach (TableItem item in groupedItems)
                {
                    table.Cell().Element(CellStyle).Text(item.TotalAmount?.ToString("0.##")).AlignCenter();
                    table.Cell().Element(CellStyle).Text(item.Price?.ToString("0.##")).AlignCenter();
                    table.Cell().Element(CellStyle).Text(item.Name).AlignEnd();
                    table.Cell().Element(CellStyle).Text(item.Quantity.ToString("N0")).AlignCenter();

                    // Add Discount if available
                    if (item.HasDiscount)
                    {
                        var discountText = item.DiscountPercentage > 0 
                            ? $"{item.DiscountPercentage}%" 
                            : item.TotalDiscountPrice?.ToString("0.##");

                        table.Cell().ColumnSpan(4)
                            .Element(CellStyle)
                            .PaddingRight(45)
                            .Text($"➤ {ArabicConstStrings.Discount}: {discountText} (-{item.TotalDiscountPrice?.ToString("0.##")})")
                            .FontSize(10)
                            .FontColor(QuestPDF.Helpers.Colors.Red.Medium)
                            .AlignEnd();
                    }

                    // Add attributes if available
                    if (item.Attributes?.Any() == true)
                    {
                        foreach (var attribute in item.Attributes)
                        {
                            table.Cell().ColumnSpan(4)
                            .Element(CellStyle)
                                .PaddingRight(45)
                                .Text(attribute.Name + "<==")
                                .FontSize(10)
                                .AlignEnd();
                        }
                    }

                }

                if (receipt.SubTotal != 0 && receipt.SubTotal.HasValue)
                {
                    table.Cell().ColumnSpan(2).PaddingTop(8).Text(receipt.SubTotal.Value.ToString("0.##")).FontSize(12).Bold().AlignCenter();
                    table.Cell().ColumnSpan(2).PaddingTop(8).Text(ArabicConstStrings.SubTotal).FontSize(12).Bold().AlignCenter();
                }

                if (receipt.Services != 0 && receipt.Services.HasValue)
                {
                    table.Cell().ColumnSpan(2).PaddingTop(8).Text(receipt.Services.Value.ToString("0.##")).FontSize(12).Bold().AlignCenter();
                    table.Cell().ColumnSpan(2).PaddingTop(8).Text(ArabicConstStrings.Service).FontSize(12).Bold().AlignCenter();
                }

                if (receipt.Tax != 0 && receipt.Tax.HasValue)
                {
                    table.Cell().ColumnSpan(2).PaddingTop(8).Text(receipt.Tax.Value.ToString("0.##")).FontSize(12).Bold().AlignCenter();
                    table.Cell().ColumnSpan(2).PaddingTop(8).Text(ArabicConstStrings.Tax).FontSize(12).Bold().AlignCenter();
                }

                if (receipt.Discount != 0 && receipt.Discount.HasValue)
                {
                    table.Cell().ColumnSpan(2).PaddingTop(8).Text(receipt.Discount.Value.ToString("0.##")).FontSize(14).Bold().AlignCenter();
                    table.Cell().ColumnSpan(2).PaddingTop(8).Text(ArabicConstStrings.Discount).FontSize(12).Bold().AlignCenter();
                }

                table.Cell()
                    .ColumnSpan(2)
                    .PaddingTop(8)
                    .Text((receipt.TotalAmount ?? 0).ToString("0.##"))
                    .FontSize(20)
                    .Bold()
                    .AlignCenter();

                table.Cell()
                    .ColumnSpan(2)
                    .PaddingTop(8)
                    .Text(ArabicConstStrings.Total)
                    .FontSize(18)
                    .Bold()
                    .AlignCenter();
            });
    }

    private void BuildPaymentMethod(ref ColumnDescriptor column)
    {
        column.Item()
            .PaddingTop(13)
            .PaddingBottom(-2)
            .Text(receipt.PaymentMethod)
            .AlignCenter()
            .Bold()
            .FontSize(15)
            .AlignCenter();
    }

    private void BuildFooter(ref ColumnDescriptor column)
    {
        column.Item()
            .PaddingTop(8)
            .Text(receipt.FooterMessage)
            .Bold()
            .FontSize(15)
            .AlignCenter();

        column.Item()
            .AlignCenter()
            .PaddingTop(5)
            .Text(text =>
            {
                text.Line("New Tech Company for Software Development").FontSize(9);
                text.Line("Contact: 01033964899").FontSize(9);
            });

    }

    private static IContainer CellStyle(IContainer container)
        => container.Border(1)
            .Padding(2);
}
