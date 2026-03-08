using POS.Contract.Dtos.ReportingDtos;

namespace POS.Reports.ReportsMakerServices;

public enum ReportPageFormat
{
    Cashier, // 7.2cm / 80mm
    A4
}

public class SalesSummaryDocument : IDocument
{
    private readonly SalesSummaryDto _summary;
    private readonly List<SalesItemSummaryDto> _items;
    private readonly string _storeName;
    private readonly string _logoPath;
    private readonly ReportPageFormat _format;
    private readonly bool _isArabic;

    public SalesSummaryDocument(
        SalesSummaryDto summary,
        List<SalesItemSummaryDto> items,
        string storeName,
        string logoPath,
        ReportPageFormat format = ReportPageFormat.Cashier,
        bool isArabic = true)
    {
        _summary = summary;
        _items = items;
        _storeName = storeName;
        _logoPath = logoPath;
        _format = format;
        _isArabic = isArabic;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            if (_format == ReportPageFormat.Cashier)
            {
                page.ContinuousSize(7.2f, Unit.Centimetre);
                page.Margin(2, Unit.Millimetre);
            }
            else
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
            }

            page.PageColor(Colors.White);

            page.DefaultTextStyle(x =>
                x.FontFamily("Noto Sans", "Noto Sans Arabic")
                 .FontSize(_format == ReportPageFormat.Cashier ? 9 : 11));

            if (_format == ReportPageFormat.A4)
            {
                page.Header().Element(BuildHeaderA4);
            }

            page.Content().PaddingVertical(5).Column(column =>
            {
                if (_format == ReportPageFormat.Cashier)
                {
                    BuildHeaderCashier(column);
                }

                BuildFinancialSummary(column);
                
                if (_summary.DetailedOrders != null && _summary.DetailedOrders.Any())
                {
                    BuildDetailedOrdersTable(column);
                }
                else
                {
                    BuildItemsTable(column);
                }
            });

            page.Footer().Element(BuildFooter);
        });
    }

    private void BuildHeaderCashier(ColumnDescriptor column)
    {
        column.Item().AlignCenter().Text(_storeName).FontSize(14).Bold();
        
        if (!string.IsNullOrWhiteSpace(_logoPath) && File.Exists(_logoPath))
        {
            column.Item().AlignCenter().Height(40).Image(_logoPath).FitArea();
        }

        string title = _isArabic ? "ملخص مبيعات الأصناف" : "Sales Items Summary";
        column.Item().AlignCenter().Text(title).FontSize(10).Bold();
        if (!string.IsNullOrEmpty(_summary.StaffName))
        {
            column.Item().Row(row =>
            {
                if (_isArabic)
                {
                    row.RelativeItem().AlignLeft().Text(_summary.StaffName);
                    row.RelativeItem().AlignRight().Text("الكاشير");
                }
                else
                {
                    row.RelativeItem().AlignLeft().Text("Staff");
                    row.RelativeItem().AlignRight().Text(_summary.StaffName);
                }
            });
        }
        column.Item().PaddingVertical(2).LineHorizontal(1);
    }

    private void BuildHeaderA4(IContainer container)
    {
        container.Row(row =>
        {
            if (_isArabic)
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(_storeName).FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                    string titleStr = string.IsNullOrEmpty(_summary.StaffName) ? "ملخص مبيعات الأصناف" : $"تقرير مبيعات: {_summary.StaffName}";
                    column.Item().Text(titleStr).FontSize(14).Bold();
                    column.Item().Text($"{_summary.PosDate:yyyy-MM-dd}").FontSize(10);
                });

                if (!string.IsNullOrWhiteSpace(_logoPath) && File.Exists(_logoPath))
                    row.ConstantItem(80).Height(80).Image(_logoPath).FitArea();
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(_logoPath) && File.Exists(_logoPath))
                    row.ConstantItem(80).Height(80).Image(_logoPath).FitArea();

                row.RelativeItem().Column(column =>
                {
                    column.Item().AlignRight().Text(_storeName).FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                    string titleStr = string.IsNullOrEmpty(_summary.StaffName) ? "Sales Items Summary" : $"Sales Report: {_summary.StaffName}";
                    column.Item().AlignRight().Text(titleStr).FontSize(14).Bold();
                    column.Item().AlignRight().Text($"{_summary.PosDate:yyyy-MM-dd}").FontSize(10);
                });
            }
        });
    }

    private void BuildFinancialSummary(ColumnDescriptor column)
    {
        if (_format == ReportPageFormat.A4)
        {
            column.Item().PaddingVertical(5).LineHorizontal(1);
            column.Item().Row(row =>
            {
                if (_isArabic)
                {
                    row.RelativeItem().Column(col => {
                        AddMoneyRow(col, _summary.TakeAway.Total, "تيك أواي");
                        AddMoneyRow(col, _summary.DineIn.Total, "صالة");
                        AddMoneyRow(col, _summary.Delivery.Total, "دليفري");
                        AddMoneyRow(col, _summary.Overall.TotalDiscount, "إجمالي الخصم");
                        AddMoneyRow(col, _summary.Overall.VoidAmount, "إجمالي الفويد");
                    });
                    row.ConstantItem(30);
                    row.RelativeItem().Column(col => {
                        AddMoneyRow(col, _summary.Overall.TotalSales, "صافي المبيعات", true);
                        AddMoneyRow(col, _summary.Overall.CashAmount, "نقدي");
                        AddMoneyRow(col, _summary.Overall.CreditAmount, "فيزا");
                    });
                }
                else
                {
                    row.RelativeItem().Column(col => {
                        AddMoneyRow(col, _summary.Overall.TotalSales, "Net Sales", true);
                        AddMoneyRow(col, _summary.Overall.CashAmount, "Cash");
                        AddMoneyRow(col, _summary.Overall.CreditAmount, "Visa");
                    });
                    row.ConstantItem(30);
                    row.RelativeItem().Column(col => {
                        AddMoneyRow(col, _summary.TakeAway.Total, "Take Away");
                        AddMoneyRow(col, _summary.DineIn.Total, "Dine In");
                        AddMoneyRow(col, _summary.Delivery.Total, "Delivery");
                        AddMoneyRow(col, _summary.Overall.TotalDiscount, "Total Discount");
                        AddMoneyRow(col, _summary.Overall.VoidAmount, "Total Void");
                    });
                }
            });
            column.Item().PaddingVertical(5).LineHorizontal(1);
        }
        else
        {
            column.Item().Column(fin =>
            {
                if (_isArabic)
                {
                    AddMoneyRow(fin, _summary.Overall.TotalSales, "صافي المبيعات", true);
                    AddMoneyRow(fin, _summary.Overall.CashAmount, "نقدي");
                    AddMoneyRow(fin, _summary.Overall.CreditAmount, "فيزا");
                    fin.Item().PaddingVertical(2).LineHorizontal(0.5f);
                    AddMoneyRow(fin, _summary.Overall.TotalDiscount, "إجمالي الخصم");
                    AddMoneyRow(fin, _summary.Overall.VoidAmount, "إجمالي الفويد");
                }
                else
                {
                    AddMoneyRow(fin, _summary.Overall.TotalSales, "Net Sales", true);
                    AddMoneyRow(fin, _summary.Overall.CashAmount, "Cash");
                    AddMoneyRow(fin, _summary.Overall.CreditAmount, "Visa");
                    fin.Item().PaddingVertical(2).LineHorizontal(0.5f);
                    AddMoneyRow(fin, _summary.Overall.TotalDiscount, "Total Discount");
                    AddMoneyRow(fin, _summary.Overall.VoidAmount, "Total Void");
                }
                fin.Item().PaddingVertical(2).LineHorizontal(0.5f);
            });
        }
    }

    private void AddMoneyRow(ColumnDescriptor column, decimal value, string label, bool highlight = false)
    {
        column.Item().Row(row =>
        {
            if (_isArabic)
            {
                row.RelativeItem().AlignLeft().Text(value.ToString("0.##"))
                    .FontSize(highlight ? (_format == ReportPageFormat.A4 ? 12 : 10) : (_format == ReportPageFormat.A4 ? 10 : 9))
                    .Bold();

                row.RelativeItem().AlignRight().Text(label)
                    .FontSize(highlight ? (_format == ReportPageFormat.A4 ? 12 : 10) : (_format == ReportPageFormat.A4 ? 10 : 9))
                    .Bold();
            }
            else
            {
                row.RelativeItem().AlignLeft().Text(label)
                    .FontSize(highlight ? (_format == ReportPageFormat.A4 ? 12 : 10) : (_format == ReportPageFormat.A4 ? 10 : 9))
                    .Bold();

                row.RelativeItem().AlignRight().Text(value.ToString("0.##"))
                    .FontSize(highlight ? (_format == ReportPageFormat.A4 ? 12 : 10) : (_format == ReportPageFormat.A4 ? 10 : 9))
                    .Bold();
            }
        });
    }

    private void BuildItemsTable(ColumnDescriptor column)
    {
        string sectionTitle = _isArabic ? "تفاصيل الأصناف المباعة" : "Sold Items Details";
        column.Item().PaddingVertical(2).Text(sectionTitle).FontSize(10).Bold().Underline();
        
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                if (_isArabic)
                {
                    cols.RelativeColumn(2); // Total
                    cols.RelativeColumn(_format == ReportPageFormat.A4 ? 4 : 3); // Name
                    cols.RelativeColumn(1); // Qty
                }
                else
                {
                    cols.RelativeColumn(1); // Qty
                    cols.RelativeColumn(_format == ReportPageFormat.A4 ? 4 : 3); // Name
                    cols.RelativeColumn(2); // Total
                }
            });

            table.Header(header =>
            {
                if (_isArabic)
                {
                    header.Cell().Element(CellStyle).AlignCenter().Text("إجمالي").Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("الصنف").Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("كمية").Bold();
                }
                else
                {
                    header.Cell().Element(CellStyle).AlignCenter().Text("Qty").Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("Item").Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("Total").Bold();
                }
            });

            foreach (var item in _items)
            {
                if (_isArabic)
                {
                    table.Cell().Element(CellStyle).AlignCenter().Text(item.TotalAmount.ToString("0.##"));
                    table.Cell().Element(CellStyle).AlignRight().Text(item.ItemName);
                    table.Cell().Element(CellStyle).AlignCenter().Text(item.Quantity.ToString("0.##"));
                }
                else
                {
                    table.Cell().Element(CellStyle).AlignCenter().Text(item.Quantity.ToString("0.##"));
                    table.Cell().Element(CellStyle).AlignLeft().Text(item.ItemName);
                    table.Cell().Element(CellStyle).AlignCenter().Text(item.TotalAmount.ToString("0.##"));
                }
            }
        });
    }

    private void BuildDetailedOrdersTable(ColumnDescriptor column)
    {
        string sectionTitle = _isArabic ? "تفاصيل الأوردرات" : "Orders Details";
        column.Item().PaddingVertical(2).Text(sectionTitle).FontSize(10).Bold().Underline();

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                if (_isArabic)
                {
                    cols.RelativeColumn(2); // Total
                    cols.RelativeColumn(1.5f); // Void
                    cols.RelativeColumn(1.5f); // Discount
                    cols.RelativeColumn(3); // Order ID / Type
                }
                else
                {
                    cols.RelativeColumn(3); // Order ID / Type
                    cols.RelativeColumn(1.5f); // Discount
                    cols.RelativeColumn(1.5f); // Void
                    cols.RelativeColumn(2); // Total
                }
            });

            table.Header(header =>
            {
                if (_isArabic)
                {
                    header.Cell().Element(CellStyle).AlignCenter().Text("الإجمالي").Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("فويد").Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("خصم").Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("الأوردر").Bold();
                }
                else
                {
                    header.Cell().Element(CellStyle).AlignCenter().Text("Order").Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("Disc").Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("Void").Bold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("Total").Bold();
                }
            });

            foreach (var order in _summary.DetailedOrders!)
            {
                if (_isArabic)
                {
                    table.Cell().Element(CellStyle).AlignCenter().Text(order.GrandTotal?.ToString("0.##") ?? "0.00");
                    table.Cell().Element(CellStyle).AlignCenter().Text(order.VoidAmount?.ToString("0.##") ?? "0.00").FontColor(Colors.Red.Medium);
                    table.Cell().Element(CellStyle).AlignCenter().Text(order.TotalDiscount?.ToString("0.##") ?? "0.00").FontColor(Colors.Green.Medium);
                    table.Cell().Element(CellStyle).AlignRight().Text($"{order.OrderType} #{order.OrderId}");
                }
                else
                {
                    table.Cell().Element(CellStyle).AlignLeft().Text($"{order.OrderType} #{order.OrderId}");
                    table.Cell().Element(CellStyle).AlignCenter().Text(order.TotalDiscount?.ToString("0.##") ?? "0.00").FontColor(Colors.Green.Medium);
                    table.Cell().Element(CellStyle).AlignCenter().Text(order.VoidAmount?.ToString("0.##") ?? "0.00").FontColor(Colors.Red.Medium);
                    table.Cell().Element(CellStyle).AlignCenter().Text(order.GrandTotal?.ToString("0.##") ?? "0.00");
                }
            }
        });
    }

    private IContainer CellStyle(IContainer container)
    {
        return container
            .Border(_format == ReportPageFormat.Cashier ? 0.3f : 0.5f)
            .BorderColor(Colors.Grey.Darken1)
            .PaddingVertical(1)
            .PaddingHorizontal(2);
    }

    private void BuildFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Printed at: ").FontSize(8);
            x.Span($"{DateTime.Now:g}").FontSize(8);
            if (_format == ReportPageFormat.A4)
            {
                x.EmptyLine();
                x.CurrentPageNumber().FontSize(8);
                x.Span(" / ").FontSize(8);
                x.TotalPages().FontSize(8);
            }
        });
    }
}