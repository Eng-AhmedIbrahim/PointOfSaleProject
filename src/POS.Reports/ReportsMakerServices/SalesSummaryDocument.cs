using POS.Contract.Dtos.ReportingDtos;

public class SalesSummaryDocument : IDocument
{
    private readonly SalesSummaryDto _summary;
    private readonly List<SalesItemSummaryDto> _items;
    private readonly string _storeName;
    private readonly string _logoPath;

    public SalesSummaryDocument(
        SalesSummaryDto summary,
        List<SalesItemSummaryDto> items,
        string storeName,
        string logoPath)
    {
        _summary = summary;
        _items = items;
        _storeName = storeName;
        _logoPath = logoPath;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.ContinuousSize(7.2f, Unit.Centimetre);
            page.Margin(5);
            page.PageColor(Colors.White);

            page.DefaultTextStyle(x =>
                x.FontFamily("Noto Sans", "Noto Sans Arabic")
                 .FontSize(10));

            page.Content().Column(column =>
            {
                BuildHeader(column);
                BuildFinancialSummary(column);
                BuildItemsTable(column);
            });

            page.Footer()
                .AlignCenter()
                .Text($"Printed at: {DateTime.Now:g}")
                .FontSize(8);
        });
    }

    // ================= HEADER =================

    private void BuildHeader(ColumnDescriptor column)
    {
        column.Item().AlignCenter().Text(_storeName)
            .FontSize(15)
            .Bold();

        if (!string.IsNullOrWhiteSpace(_logoPath) && File.Exists(_logoPath))
        {
            column.Item()
                .PaddingVertical(4)
                .AlignCenter()
                .Height(45)
                .Image(_logoPath)
                .FitArea();
        }

        column.Item().AlignCenter().Text("ملخص المبيعات")
            .FontSize(12)
            .Bold();

        column.Item().PaddingVertical(5).LineHorizontal(1);

        column.Item().Row(row =>
        {
            row.RelativeItem()
                .AlignLeft()
                .Text(_summary.PosDate.ToString("yyyy-MM-dd"))
                .Bold();

            row.RelativeItem()
                .AlignRight()
                .Text("التاريخ")
                .Bold();
        });

        column.Item().PaddingVertical(5).LineHorizontal(1);
    }

    // ================= FINANCIAL =================

    private void BuildFinancialSummary(ColumnDescriptor column)
    {
        column.Item().PaddingTop(5).Column(fin =>
        {
            AddMoneyRow(fin, _summary.TakeAway.Total , "تيك أواي");
            AddMoneyRow(fin, _summary.DineIn.Total , "صالة");
            AddMoneyRow(fin, _summary.Delivery.Total, "دليفري");

            fin.Item().PaddingVertical(4).LineHorizontal(0.7f);

            AddMoneyRow(fin, _summary.Overall.TotalRevenue, "إجمالي الإيراد", true);
            
            if (_summary.Overall.PendingAmount > 0)
            {
                AddMoneyRow(fin, _summary.Overall.PendingAmount, "إجمالي المبيعات المعلقة");
                if (_summary.TakeAway.UncollectedAmount > 0)
                    AddMoneyRow(fin, _summary.TakeAway.UncollectedAmount, "- معلق (تيك أواي)");
                if (_summary.DineIn.UncollectedAmount > 0)
                    AddMoneyRow(fin, _summary.DineIn.UncollectedAmount, "- معلق (صالة)");
                if (_summary.Delivery.UncollectedAmount > 0)
                    AddMoneyRow(fin, _summary.Delivery.UncollectedAmount, "- معلق (دليفري)");
            }

            AddMoneyRow(fin, _summary.Overall.TotalSales, "إجمالي المحصل ", true);
            AddMoneyRow(fin, _summary.Overall.CashAmount, "نقدي");
            AddMoneyRow(fin, _summary.Overall.CreditAmount, "فيزا");
            //AddMoneyRow(fin, _summary.Overall.OnAccountAmount, "مبيعات آجلة (آجل)");

            fin.Item().PaddingVertical(4).LineHorizontal(0.7f);

            AddMoneyRow(fin, _summary.Overall.VoidAmount,
                $"فويد ({_summary.Overall.VoidCount})");
        });

        column.Item().PaddingVertical(8).LineHorizontal(1);
    }

    private void AddMoneyRow(ColumnDescriptor column, decimal value, string label, bool highlight = false)
    {
        column.Item().Row(row =>
        {
            row.RelativeItem()
                .AlignLeft()
                .Text(value.ToString("0.##"))
                .FontSize(highlight ? 12 : 10)
                .Bold();

            row.RelativeItem()
                .AlignRight()
                .Text(label)
                .FontSize(highlight ? 12 : 10)
                .Bold();
        });
    }

    // ================= ITEMS =================

    private void BuildItemsTable(ColumnDescriptor column)
    {
        column.Item().AlignCenter().Text("بيان الأصناف")
            .Bold()
            .FontSize(11);

        column.Item().PaddingTop(5).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2); // Total
                cols.RelativeColumn(3); // Name
                cols.RelativeColumn(1); // Qty
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).AlignCenter().Text("إجمالي").Bold();
                header.Cell().Element(CellStyle).AlignCenter().Text("الصنف").Bold();
                header.Cell().Element(CellStyle).AlignCenter().Text("كمية").Bold();
            });

            foreach (var item in _items)
            {
                table.Cell().Element(CellStyle).AlignCenter()
                    .Text(item.TotalAmount.ToString("0.##"));

                table.Cell().Element(CellStyle).AlignRight()
                    .Text(item.ItemName);

                table.Cell().Element(CellStyle).AlignCenter()
                    .Text(item.Quantity.ToString("0.##"));
            }
        });
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container
            .BorderBottom(0.5f)
            .PaddingVertical(2);
    }
}