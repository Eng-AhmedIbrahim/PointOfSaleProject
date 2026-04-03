using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using POS.Contract.Dtos.OrderDtos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace POS.Reports.ReportsMakerServices;

public class DriverSettlementDocument : IDocument
{
    private readonly DriverSettlementDto _settlement;
    private readonly DateTime _posDate;
    private readonly string _branchName;
    private readonly string _logoPath;
    private readonly ReportPageFormat _format;
    private readonly bool _isArabic;

    public DriverSettlementDocument(DriverSettlementDto settlement, DateTime posDate, string branchName, string logoPath, ReportPageFormat format, bool isArabic = true)
    {
        _settlement = settlement;
        _posDate = posDate;
        _branchName = branchName;
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
                page.ContinuousSize(8f, Unit.Centimetre);
                page.Margin(2, Unit.Millimetre);
            }
            else
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
            }

            page.PageColor(Colors.White);

            var baseStyle = TextStyle.Default
                .FontFamily("Arial", "Noto Sans Arabic")
                .FontSize(_format == ReportPageFormat.Cashier ? 9 : 11);

            page.DefaultTextStyle(baseStyle);

            if (_isArabic)
                page.ContentFromRightToLeft();

            page.Content().Column(col =>
            {
                col.Spacing(3);

                // 1. Title
                col.Item().AlignCenter().Text(_isArabic ? "تسوية الطيار" : "Driver Settlement").FontSize(14).Bold();
                col.Item().AlignCenter().Text(_posDate.ToString("yyyy-MM-dd")).FontSize(10);

                // 2. Info section
                col.Item().PaddingTop(5).Table(t =>
                {
                    t.ColumnsDefinition(cd => { cd.RelativeColumn(1); cd.RelativeColumn(2); });
                    
                    AddRow(t, _isArabic ? "اسم الطيار" : "Driver", _settlement.DriverDisplayName ?? _settlement.DriverName, true);
                    AddRow(t, _isArabic ? "الفرع" : "Branch", _branchName);
                });

                // 3. Financial Summary
                col.Item().PaddingVertical(5).Table(t =>
                {
                    t.ColumnsDefinition(cd => { cd.RelativeColumn(1); cd.RelativeColumn(1); });
                    
                    AddSummaryCell(t, _isArabic ? "عدد الطلبات" : "Orders", _settlement.OrderCount.ToString());
                    AddSummaryCell(t, _isArabic ? "إجمالي المبلغ" : "Total Amount", _settlement.TotalAmount.ToString("0.##"));
                    
                    if (_settlement.TotalBonus > 0)
                    {
                        AddSummaryCell(t, _isArabic ? "إجمالي البونص" : "Total Bonus", _settlement.TotalBonus.ToString("0.##"), Colors.Orange.Medium);
                    }
                });

                // 4. Detailed Orders
                if (_settlement.Orders != null && _settlement.Orders.Any())
                {
                    col.Item().PaddingTop(5).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text(_isArabic ? "تفاصيل الطلبات" : "Order Details").Bold();
                    
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(1.2f); // ID
                            cd.RelativeColumn(2.5f); // Customer
                            cd.RelativeColumn(1.8f); // Payment
                            cd.RelativeColumn(1.5f); // Total
                        });

                        t.Header(h =>
                        {
                            h.Cell().BorderBottom(1).Padding(1).AlignCenter().Text(_isArabic ? "رقم" : "No").Bold();
                            h.Cell().BorderBottom(1).Padding(1).Text(_isArabic ? "العميل" : "Customer").Bold();
                            h.Cell().BorderBottom(1).Padding(1).AlignCenter().Text(_isArabic ? "دفع" : "Pay").Bold();
                            h.Cell().BorderBottom(1).Padding(1).AlignRight().Text(_isArabic ? "إجمالي" : "Total").Bold();
                        });

                        foreach (var o in _settlement.Orders)
                        {
                            t.Cell().Padding(1).AlignCenter().Text($"#{o.OrderId}");
                            t.Cell().Padding(1).Text(o.CustomerName ?? "—");
                            t.Cell().Padding(1).AlignCenter().Text(o.PaymentMethod.ToString());
                            t.Cell().Padding(1).AlignRight().Text(o.GrandTotal?.ToString("0.##"));
                        }

                        t.Cell().ColumnSpan(3).PaddingTop(2).BorderTop(1).AlignRight().Text(_isArabic ? "الإجمالي الكلي" : "Grand Total").Bold();
                        t.Cell().PaddingTop(2).BorderTop(1).AlignRight().Text(_settlement.Orders.Sum(x => x.GrandTotal ?? 0).ToString("0.##")).Bold();
                    });
                }

                // 5. Footer
                col.Item().PaddingTop(10).AlignCenter().Text($"{DateTime.Now:HH:mm yyyy-MM-dd}").FontSize(8).Italic();
            });
        });
    }

    private void AddRow(TableDescriptor t, string lbl, string val, bool bold = false)
    {
        t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(lbl).Bold();
        var cell = t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(val);
        if (bold) cell.Bold();
    }

    private void AddSummaryCell(TableDescriptor t, string lbl, string val, string color = null)
    {
        t.Cell().Border(0.5f).Padding(3).Column(c =>
        {
            c.Item().AlignCenter().Text(lbl).FontSize(10).Bold();
            c.Item().AlignCenter().Text(val).FontSize(12).Bold().FontColor(color ?? Colors.Black);
        });
    }
}
