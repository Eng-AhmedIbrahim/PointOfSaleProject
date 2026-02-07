namespace POS.Reports.ReportsMakerServices;

public class KitchenReceiptDocument : IDocument
{
    private readonly List<TableItem> _items;
    private readonly KitchenReceipt receipt;

    public KitchenReceiptDocument(KitchenReceipt _receipt, List<TableItem> items)
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
                    BuildKitchenNotes(ref column);
                });
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
        column.Item()
           .Text(text =>
           {
               text.Span(receipt.KitchenType.ToString())
                   .Bold()
                   .FontSize(15);
               text.AlignCenter();
           });

        column.Item()
            .PaddingTop(6)
            .Text(text =>
            {
                text.Span(receipt.Id.ToString())
                    .Bold()
                    .FontSize(15);
                text.AlignCenter();
            });

        column.Item()
            .PaddingTop(6)
            .Text(text =>
            {
                text.Span(receipt.OrderType)
                    .Bold()
                    .FontSize(15);
                text.AlignCenter();
            });

        if (!string.IsNullOrEmpty(receipt.TableName))
        {
            column.Item()
                .PaddingTop(6)
                .Text(text =>
                {
                    text.Span($"{receipt.TableName} : {ArabicConstStrings.Table}")
                        .Bold()
                        .FontSize(18);
                    text.AlignCenter();
                });
        }
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
                    columns.RelativeColumn(10);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
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
                    table.Cell().Element(CellStyle).Text(item.Name).AlignEnd();
                    table.Cell().Element(CellStyle).Text(item.Quantity.ToString("N0")).AlignCenter();

                    if (item.Attributes?.Any() == true)
                    {
                        foreach (var attribute in item.Attributes)
                        {
                            table.Cell().ColumnSpan(2)
                                .Element(CellStyle)
                                .PaddingRight(45)
                                .Text(attribute.Name + "<==")
                                .FontSize(10)
                                .AlignEnd();
                        }
                    }
                }
            });
    }

    private void BuildKitchenNotes(ref ColumnDescriptor column)
    {
        if (!string.IsNullOrEmpty(receipt.KitchenNote))
        {
            column.Item()

                .PaddingTop(10)
                .AlignCenter()
                .Text(text =>
                {
                    text.Span("ملاحظات المطبخ").FontSize(13).Bold();
                });

            column.Item()
                .PaddingTop(4)
                .AlignCenter()
                .Text(text =>
                {
                    var lines = receipt.KitchenNote.Split('\n');
                    foreach (var line in lines)
                    {
                        text.Span(line);
                        if (line != lines.Last())
                            text.EmptyLine()
                            .FontSize(10);
                    }
                });
        }
    }

    private static IContainer CellStyle(IContainer container)
        => container.Border(1)
            .Padding(2);
}