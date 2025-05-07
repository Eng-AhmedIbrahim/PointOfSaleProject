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
        column.Item()
        .AlignCenter()
        .Width(receipt.LogoWidth) 
        .Image(receipt.LogoPath)
        .FitWidth();

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
                    table.Cell().Element(CellStyle).Text(item.Total?.ToString("N2")).AlignCenter();
                    table.Cell().Element(CellStyle).Text(item.Price?.ToString("N2")).AlignCenter();
                    table.Cell().Element(CellStyle).Text(item.Name).AlignEnd();
                    table.Cell().Element(CellStyle).Text(item.Quantity.ToString("N0")).AlignCenter();

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

                table.Cell()
                    .ColumnSpan(2)
                    .PaddingTop(8)
                    .Text(receipt.TotalAmount.ToString())
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
            .PaddingTop(8)
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
            .PaddingTop(5)
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
