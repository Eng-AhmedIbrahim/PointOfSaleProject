namespace POS.Reports.ReportsMakerServices;

public class DeliveryReceiptDocument : IDocument
{
    private readonly DeliveryReceipt _receipt;
    private readonly List<TableItem> _items;

    public DeliveryReceiptDocument(DeliveryReceipt receipt, List<TableItem> items)
    {
        _receipt = receipt;
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
                    BuildOrderInfo(ref column);
                    BuildItemsTable(ref column);
                    BuildTotals(ref column);
                    BuildPaymentMethod(ref column);
                });

            page.Footer()
                .Column(column => BuildFooter(ref column));
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
            .AlignCenter()
            .Text(_receipt.StoreName)
            .Bold()
            .FontSize(12);

        // 🖼️ Logo section
        if (!string.IsNullOrEmpty(_receipt.LogoPath) && File.Exists(_receipt.LogoPath))
        {
            column.Item()
            .PaddingTop(5)
            .AlignCenter()
            .Width(_receipt.LogoWidth)
            .Image(_receipt.LogoPath)
            .FitWidth();
        }

        column.Item()
            .PaddingTop(5)
            .AlignCenter()
            .Text("توصيل طلبات")
            .Bold()
            .FontSize(12);

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            table.Cell().PaddingTop(5).Text(_receipt.DateCreated.ToString("d/M/yyyy")).AlignCenter();
            table.Cell().PaddingTop(5).Text(_receipt.DateCreated.ToString("h:mm:ss tt")).AlignCenter();
        });
    }

    private void BuildOrderInfo(ref ColumnDescriptor column)
    {
        column.Item().PaddingTop(5).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(6); // Label column (40%)
                columns.RelativeColumn(2); // Value column (60%)
            });

            table.Cell().Element(CellStyle).Text(_receipt.CashierName).AlignRight();
            table.Cell().Element(CellStyle).Text("الكاشير").Bold().AlignRight();

            table.Cell().Element(CellStyle).Text(FormatPhoneNumbers()).AlignRight();
            table.Cell().Element(CellStyle).Text("تليفون").Bold().AlignRight();

            table.Cell().Element(CellStyle).Text(_receipt.CustomerName).AlignRight();
            table.Cell().Element(CellStyle).Text("العميل").Bold().AlignRight();


            table.Cell().Element(CellStyle).Text(_receipt.CustomerAddress).AlignRight();
            table.Cell().Element(CellStyle).Text("الشارع").Bold().AlignRight();

            table.Cell().Element(CellStyle).AlignRight().Text(text =>
            {
                text.Span("مبنى: ").Bold();
                text.Span($"{_receipt.Building} $ ");
                text.Span("الدور: ").Bold();
                text.Span($"{_receipt.FloorNumber} $ ");
                text.Span("الشقة: ").Bold();
                text.Span(_receipt.FlatNumber);
            });
            table.Cell().Element(CellStyle).Text("العنوان").Bold().AlignRight();

            table.Cell().Element(CellStyle).Text(_receipt.ZoneName).AlignRight();
            table.Cell().Element(CellStyle).Text("المنطقة").Bold().AlignRight();

            table.Cell().Element(CellStyle).Text(_receipt.AddressNote ?? "-").AlignRight();
            table.Cell().Element(CellStyle).Text("ملاحظات").Bold().AlignRight();

            table.Cell().Element(CellStyle).Text(_receipt.DeliveryName).AlignRight();
            table.Cell().Element(CellStyle).Text("الطيار").Bold().AlignRight();
        });
    }

    private string FormatPhoneNumbers()
    {
        if (string.IsNullOrEmpty(_receipt.CustomerSecondPhone))
            return _receipt!.CustomerFirstPhone!;

        return $"{_receipt.CustomerFirstPhone} - {_receipt.CustomerSecondPhone}";
    }

    private void BuildItemsTable(ref ColumnDescriptor column)
    {
        column.Item()
            .PaddingTop(6)
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2); 
                    columns.RelativeColumn(2); 
                    columns.RelativeColumn(4.5f);  
                    columns.RelativeColumn(1.5f);
                });

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
            });
    }
    private void BuildTotals(ref ColumnDescriptor column)
    {
        column.Item().PaddingTop(8).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            table.Cell().Text(_receipt.TotalAmount.ToString()).FontSize(13).Bold().AlignCenter();
            table.Cell().Text("حساب الاصناف").Bold().FontSize(15).Bold().AlignCenter();

            table.Cell().Text(_receipt.DeliveryFees.ToString()).FontSize(13).Bold().AlignCenter();
            table.Cell().Text("خدمة توصيل").Bold().FontSize(15).Bold().AlignCenter();

            table.Cell().Text(_receipt.TotalOrder.ToString()).FontSize(13).Bold().AlignCenter();
            table.Cell().Text("الإجمالي").FontSize(15).Bold().AlignCenter();
        });
    }
    private void BuildPaymentMethod(ref ColumnDescriptor column)
    {
        column.Item()
            .PaddingTop(8)
            .PaddingBottom(-2)
            .Text(_receipt.PaymentMethod)
            .AlignCenter()
            .Bold()
            .FontSize(15)
            .AlignCenter();
    }
    private void BuildFooter(ref ColumnDescriptor column)
    {
        column.Item()
            .PaddingTop(7)
            .Text(_receipt.FooterMessage)
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