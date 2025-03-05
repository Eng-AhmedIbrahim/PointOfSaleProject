using BlazorBase.Models;
using POS.Contract.Models;
using POS.Reports.Models;
namespace BlazorBase;

public class CommonProperties
{
    public double CategorySpacing { get; set; } = 4.0;
    public double CategoryPadding => CategorySpacing * 2;
    public double CategoryFontSize => CategorySpacing + 16;
    public double SalesItemsHorizontalSlider = 4;
    public double SalesItemsVerticalSlider = 4;

    public decimal? TotalOrderPrice { get; set; }
    public decimal? TotalDiscount { get; set; }
    public decimal? TotalItemsBeforeOrder { get; set; }
    public List<TableItem>? TableItems { get; set; } = [];
    public event Action? OnChange;
    private string _currentPosMode = "TakeAway";

    public Receipt? OrderReceipt { get; set; }
    public string? CurrentUser { get; set; }
    public string? StoreName { get; set; }
    public string? PaymentMethod { get; set; } = "Cash";

    public int CurrentOrderCount { get; set; }

    public string CurrentPosMode
    {
        get => _currentPosMode;
        set
        {
            if (_currentPosMode != value)
            {
                _currentPosMode = value ?? "TakeAway";
                OnChange?.Invoke();
            }
        }
    }
    public Task ClearTableItems()
    {
        TableItems?.Clear();
        return Task.CompletedTask;
    }

    public int SelectedItemCount { get; set; }

    public List<FinanceSettings>? _financeSettingsList = new();

    public WaitingQueue? WaitingQueue { get; set; } = new();

}
