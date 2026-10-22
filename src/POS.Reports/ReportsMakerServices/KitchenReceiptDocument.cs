using POS.Contract.Models.ReceiptModels;
using POS.Contract.Models.ReceiptModels.Kitchen;

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
        page.ContinuousSize(72, Unit.Millimetre); // Match printer internal width (72mm)
        page.PageColor(Colors.White);
        page.Margin(2); // Minimal marginsatched" look on thermal printers
        page.DefaultTextStyle(
            TextStyle.Default
                .FontFamily("Noto Sans", "Noto Sans Arabic")
                .FontSize(12) // Increased from 10 to 12 for better readability
                .Bold()); // Bold for better darkness on thermal printers
    }

    private void AddHeader(ref ColumnDescriptor column)
    {
        if (receipt.IsCopy)
        {
            column.Item()
                .AlignCenter()
                .Text("COPY - نسخة")
                .Bold()
                .FontSize(20)
                .FontColor(Colors.Red.Medium);
        }

        column.Item()
           .Text(text =>
           {
               text.Span(receipt.KitchenType.ToString())
                   .Bold()
                   .FontSize(18);
               text.AlignCenter();
           });

        column.Item()
            .PaddingTop(6)
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
                        .FontColor(Colors.Red.Medium);
                }
                
                text.AlignCenter();
            });

        column.Item()
            .PaddingTop(6)
            .Text(text =>
            {
                text.Span(receipt.OrderType)
                    .Bold()
                    .FontSize(18);
                text.AlignCenter();
            });

        if (receipt.IsVoid)
        {
            column.Item()
                .PaddingTop(6)
                .Border(3)
                .BorderColor(Colors.Red.Medium)
                .AlignCenter()
                .Text(text =>
                {
                    text.Span("X  ملغي  X")
                        .Bold()
                        .FontSize(40)
                        .FontColor(Colors.Red.Medium);
                });
        }

        if (receipt.ParentOrderId.HasValue)
        {
            column.Item()
                .AlignCenter()
                .Text($"تكملة لرقم {receipt.ParentOrderId}")
                .Bold()
                .FontSize(11);
        }

        if (!string.IsNullOrEmpty(receipt.TableName))
        {
            column.Item()
                .PaddingTop(6)
                .Text(text =>
                {
                    text.Span($"{receipt.TableName} : {ArabicConstStrings.Table}")
                        .Bold()
                        .FontSize(20);
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
        // Group items before printing
        var groupedItems = _items
            .GroupBy(i => new
            {
                i.Id,
                i.Name,
                i.IsVoided,
                LineComment = !string.IsNullOrWhiteSpace(i.LineComment)
                    ? i.LineComment
                    : (i.Attributes?.Any(a => a.Id >= 5000) == true
                        ? string.Join(", ", i.Attributes.Where(a => a.Id >= 5000).Select(a => a.Name))
                        : null),
                AttributesHash = string.Join("|", i.Attributes?.Where(a => a.Id < 5000).OrderBy(a => a.Id).Select(a => a.Id) ?? Enumerable.Empty<int?>())
            })
            .Select(g =>
            {
                var first = g.First();
                decimal totalQty = g.Sum(x => x.Quantity);
                decimal totalVoidQty = g.Sum(x => x.VoidAmount ?? 0);
                decimal displayQty = totalQty > 0 ? totalQty : (totalVoidQty > 0 ? totalVoidQty : totalQty);
                return new TableItem
                {
                    Id = first.Id,
                    Name = first.Name,
                    Quantity = displayQty,
                    VoidAmount = totalVoidQty,
                    Attributes = first.Attributes?.Where(a => a.Id < 5000).ToList(),
                    IsVoided = first.IsVoided || receipt.IsVoid,
                    ByWeight = first.ByWeight,
                    LineComment = g.Key.LineComment
                };
            }).ToList();

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

                foreach (TableItem item in groupedItems)
                {
                    table.Cell().Element(CellStyle).AlignRight().Text(text => {
                        text.Span(item.Name);
                        // Only show (ملغي) if the entire receipt is a void receipt OR the item record is explicitly fully voided
                        if (receipt.IsVoid || (item.IsVoided == true && item.Quantity == 0)) {
                            text.Span(" (ملغي)").FontColor(Colors.Red.Medium);
                        }
                    });
                    
                    var qtyText = item.Quantity.ToString("0.##");
                    if (item.ByWeight) qtyText += " كجم";
                    table.Cell().Element(CellStyle).AlignCenter().Text(qtyText);

                    if (item.Attributes?.Any(a => a.Id < 5000) == true)
                    {
                        var groupedAttrs = item.Attributes
                            .Where(a => a.Id < 5000)
                            .GroupBy(a => a.Name)
                            .Select(g => new { Name = g.Key, Count = g.Count() * item.Quantity });

                        foreach (var attr in groupedAttrs)
                        {
                            table.Cell().ColumnSpan(2)
                                .Element(CellStyle)
                                .PaddingRight(45)
                                .Text($"{attr.Name} {(attr.Count > 1 ? $"({attr.Count:N0})" : "")}")
                                .FontSize(10)
                                .AlignEnd();
                        }
                    }

                    // Print per-item comment (LineComment or manual comments in Attributes)
                    var lineComment = !string.IsNullOrWhiteSpace(item.LineComment)
                        ? item.LineComment
                        : (item.Attributes?.Any(a => a.Id >= 5000) == true
                            ? string.Join(", ", item.Attributes.Where(a => a.Id >= 5000).Select(a => a.Name))
                            : null);

                    if (!string.IsNullOrWhiteSpace(lineComment))
                    {
                        table.Cell().ColumnSpan(2)
                            .Element(CellStyle)
                            .PaddingRight(20)
                            .Text($"{lineComment} ⟵")
                            .FontSize(11)
                            .Bold()
                            .AlignEnd();
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