namespace POS.Reports.ReportsMakerServices;

public class ReceiptDocument(Receipt receipt) : IDocument
{
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
                    BuildBarcodeOrNoteSection(ref column);
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
        page.MarginTop(40);
        page.MarginRight(20);
        page.MarginBottom(20);
        page.MarginLeft(20);
        page.DefaultTextStyle(
            TextStyle.Default.FontFamily("Noto Sans", "Noto Sans Arabic"));
    }

    private void AddHeader(ref ColumnDescriptor column)
    {
        /*Header * Name */
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
                    .Padding(-6)
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
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(60);
                    cols.ConstantColumn(60);
                    cols.RelativeColumn();
                    cols.ConstantColumn(30);
                });

                table.Header(header =>
                {
                    header.Cell()
                        .Text(ArabicConstStrings.Total)
                        .AlignCenter();

                    header.Cell()
                        .Text(ArabicConstStrings.Price)
                        .AlignCenter();

                    header.Cell()
                        .Text(ArabicConstStrings.Name)
                        .AlignCenter();

                    header.Cell()
                        .Text(ArabicConstStrings.Quantity)
                        .AlignCenter();
                });
// Table Rows
                foreach (var item in receipt.Items)
                {
                    table.Cell()
                        .Element(CellStyle)
                        .Text(item.Total.ToString("N2"))
                        .AlignCenter();

                    table.Cell()
                        .Element(CellStyle)
                        .Text(item.Price.ToString("N2"))
                        .AlignCenter();

                    table.Cell()
                        .Element(CellStyle)
                        .Text(item.Name)
                        .AlignEnd();

                    table.Cell()
                        .Element(CellStyle)
                        .Text(item.Quantity.ToString("N0"))
                        .AlignCenter();
                }

                table.Cell()
                    .ColumnSpan(2)
                    .Padding(4)
                    .Text(receipt.TotalAmount.ToString())
                    .FontSize(20)
                    .Bold()
                    .AlignCenter();

                table.Cell()
                    .ColumnSpan(2)
                    .PaddingTop(-4)
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
            .FontSize(20)
            .AlignCenter();
    }

    private void BuildBarcodeOrNoteSection(ref ColumnDescriptor column)
    {
        column.Item()
            .Table(table =>
            {
                table.ColumnsDefinition(cols => { cols.RelativeColumn(); });

                if (string.IsNullOrEmpty(receipt.ReceiptNote))
                {
                    table.Cell()
                        .PaddingTop(4)
                        .Element(CellStyle)
                        .Text("Empty Note Item")
                        .FontSize(15)
                        .AlignCenter();
                }
                else
                {
                    table.Cell()
                        .PaddingTop(4)
                        .Element(CellStyle)
                        .Text(receipt.ReceiptNote)
                        .FontSize(15)
                        .AlignCenter();
                }
            });
    }

    private void BuildFooter(ref ColumnDescriptor column)
    {
        column.Item()
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
                    .PaddingTop(4)
                    .Text("FB:New Tech")
                    .FontSize(14)
                    .AlignLeft();

                table.Cell()
                    .PaddingTop(4)
                    .Text("www.newtech.com")
                    .FontSize(14)
                    .AlignRight();
            });
    }

    private static IContainer CellStyle(IContainer container)
        => container.Border(1)
            .Padding(2);
}