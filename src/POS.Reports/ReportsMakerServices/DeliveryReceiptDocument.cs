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
        column.Item()
            .AlignCenter()
            .Text(_receipt.StoreName)
            .Bold()
            .FontSize(18);

        if (!string.IsNullOrEmpty(_receipt.LogoPath) && File.Exists(_receipt.LogoPath))
        {
            column.Item()
            .PaddingTop(5)
            .AlignCenter()
            .MaxHeight(100)
            .MaxWidth(180)
            .Image(_receipt.LogoPath)
            .FitArea();
        }

        column.Item()
            .PaddingTop(5)
            .AlignCenter()
            .Text(text =>
            {
                if (_receipt.IsCopy)
                {
                    text.Span("*********COPY**********")
                        .Bold()
                        .FontSize(11)
                        .FontColor(Colors.Red.Medium);
                    text.EmptyLine();
                }

                text.Span(_receipt.ReceiptType ?? "توصيل طلبات")
                    .Bold()
                    .FontSize(12);

                if (_receipt.IsCopy)
                {
                    text.EmptyLine();
                    text.Span("*********COPY**********")
                        .Bold()
                        .FontSize(11)
                        .FontColor(Colors.Red.Medium);
                }
            });

        column.Item()
            .AlignCenter()
            .PaddingTop(2)
            .Text(text =>
            {
                text.Span(_receipt.Id.ToString("0.##")).Bold().FontSize(18);
                
                if (_receipt.IsFollowUp)
                {
                    text.Span(" - تابع")
                        .Bold()
                        .FontSize(12)
                        .FontColor(Colors.Grey.Medium);
                }
            });

        if (_receipt.ParentOrderId.HasValue)
        {
            column.Item()
                .AlignCenter()
                .Text($"تكملة لرقم {_receipt.ParentOrderId}")
                .Bold()
                .FontSize(11)
                .FontColor(Colors.Grey.Medium);
        }

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
                columns.RelativeColumn(5.5f); // Value column
                columns.RelativeColumn(2.5f); // Label column
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
                    DiscountPercentage = first.DiscountPercentage
                };
            }).ToList();

        column.Item()
            .PaddingTop(6)
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2.5f); // اجمالي 
                    columns.RelativeColumn(2.5f); // السعر
                    columns.RelativeColumn(3.5f); // الصنف 
                    columns.RelativeColumn(1.5f); // كمية
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

                foreach (TableItem item in groupedItems)
                {
                    table.Cell().Element(CellStyle).Text(item.TotalAmount?.ToString("0.##")).AlignCenter();
                    table.Cell().Element(CellStyle).Text(item.Price?.ToString("0.##")).AlignCenter();
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

            table.Cell().Text(_receipt.TotalAmount?.ToString("0.##")).FontSize(13).Bold().AlignCenter();
            table.Cell().Text("حساب الاصناف").Bold().FontSize(15).Bold().AlignCenter();

            table.Cell().Text(_receipt.DeliveryFees?.ToString("0.##")).FontSize(13).Bold().AlignCenter();
            table.Cell().Text("خدمة توصيل").Bold().FontSize(15).Bold().AlignCenter();

            table.Cell().Text(_receipt.TotalOrder?.ToString("0.##")).FontSize(13).Bold().AlignCenter();
            table.Cell().Text("الاجمالي").FontSize(13).Bold().AlignCenter();
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