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
        page.Size(PageSizes.A6);
        page.ContinuousSize(10.5f, Unit.Centimetre);
        page.PageColor(Colors.White);
        page.MarginTop(15);
        page.MarginRight(5);
        page.MarginBottom(10);
        page.MarginLeft(5);
        page.DefaultTextStyle(
            TextStyle.Default.FontFamily("Noto Sans", "Noto Sans Arabic"));
    }

    private void AddHeader(ref ColumnDescriptor column)
    {
        // 🖼️ Logo section
        if (!string.IsNullOrEmpty(receipt.LogoPath) && File.Exists(receipt.LogoPath))
        {
            column.Item()
            .AlignCenter()
            .Width(receipt.LogoWidth)
            .Image(receipt.LogoPath)
            .FitWidth();
        }

        column.Item()
            .Text(text =>
            {
                text.Span(receipt.StoreName)
                    .Bold()
                    .FontSize(25);
                text.AlignCenter();
            });

        /*Header * Id */
        column.Item()
            .PaddingTop(8)
            .Text(text =>
            {
                text.Span(receipt.Id.ToString())
                    .Bold()
                    .FontSize(25);
                text.AlignCenter();
            });

        /*Header * Type */
        column.Item()
            .PaddingTop(1)
            .Text(text =>
            {
                text.Span(receipt.ReceiptType)
                    .Bold()
                    .FontSize(22);
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
        column.Item()
            .PaddingTop(3)
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);     // Total
                    columns.RelativeColumn(2);     // Price
                    columns.RelativeColumn(4.5f);  // Name
                    columns.RelativeColumn(1.5f);  // Quantity
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

                foreach (TableItem item in _items)
                {
                    table.Cell().Element(CellStyle).Text(item.TotalAmount?.ToString("N2")).AlignCenter();
                    table.Cell().Element(CellStyle).Text(item.Price?.ToString("N2")).AlignCenter();
                    table.Cell().Element(CellStyle).Text(item.Name).AlignEnd();
                    table.Cell().Element(CellStyle).Text(item.Quantity.ToString("N0")).AlignCenter();

                    // Add Discount if available
                    if (item.HasDiscount)
                    {
                        var discountText = item.DiscountPercentage > 0 
                            ? $"{item.DiscountPercentage}%" 
                            : item.TotalDiscountPrice?.ToString("N2");

                        table.Cell().ColumnSpan(4)
                            .Element(CellStyle)
                            .PaddingRight(45)
                            .Text($"➤ {ArabicConstStrings.Discount}: {discountText} (-{item.TotalDiscountPrice?.ToString("N2")})")
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
                    table.Cell().ColumnSpan(2).PaddingTop(8).Text(receipt.SubTotal.Value.ToString("N2")).FontSize(12).Bold().AlignCenter();
                    table.Cell().ColumnSpan(2).PaddingTop(8).Text(ArabicConstStrings.SubTotal).FontSize(12).Bold().AlignCenter();
                }

                if (receipt.Services != 0 && receipt.Services.HasValue)
                {
                    table.Cell().ColumnSpan(2).PaddingTop(8).Text(receipt.Services.Value.ToString("N2")).FontSize(12).Bold().AlignCenter();
                    table.Cell().ColumnSpan(2).PaddingTop(8).Text(ArabicConstStrings.Service).FontSize(12).Bold().AlignCenter();
                }

                if (receipt.Tax != 0 && receipt.Tax.HasValue)
                {
                    table.Cell().ColumnSpan(2).PaddingTop(8).Text(receipt.Tax.Value.ToString("N2")).FontSize(12).Bold().AlignCenter();
                    table.Cell().ColumnSpan(2).PaddingTop(8).Text(ArabicConstStrings.Tax).FontSize(12).Bold().AlignCenter();
                }

                if (receipt.Discount != 0 && receipt.Discount.HasValue)
                {
                    table.Cell().ColumnSpan(2).PaddingTop(8).Text(receipt.Discount.Value.ToString("N2")).FontSize(14).Bold().AlignCenter();
                    table.Cell().ColumnSpan(2).PaddingTop(8).Text(ArabicConstStrings.Discount).FontSize(12).Bold().AlignCenter();
                }

                table.Cell()
                    .ColumnSpan(2)
                    .PaddingTop(8)
                    .Text((receipt.TotalAmount ?? 0).ToString("N2"))
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
            .Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn();
                    cols.RelativeColumn();
                });

                table.Cell()
                    .PaddingTop(5)
                    .Text("FB:New Tech")
                    .FontSize(9)
                    .AlignLeft();

                table.Cell()
                    .PaddingTop(5)
                    .Text("www.NewTech.com")
                    .FontSize(9)
                    .AlignRight();
            });
    }

    private static IContainer CellStyle(IContainer container)
        => container.Border(1)
            .Padding(2);
}
