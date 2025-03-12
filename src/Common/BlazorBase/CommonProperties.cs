using POS.Reports.Models;
using System.Security.Claims;
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
    public ClaimsPrincipal AuthUser { get; set; } = new();

    //DineIN
    public OrderDiscount? OrderDiscount { get; set; } = new();
    public DineInOrderDetails? CurrentDineInOrder { get; set; } = new();
    public Dictionary<int, DineInOrderDetails>? DineOrdersDetails { get; set; } = new();
    /////////
   
    public decimal DiscountOrderValue { get; set; } = 0M;

    public decimal DiscountPercentage { get; set; } = 0M;
    public DateOnly? PosDate { get; set; } = new();
    //public TimeOnly? PosTime { get; set; }

    public ICollection<OrderSettingToReturnDto> OrderSettings { get; set; } = new List<OrderSettingToReturnDto>();

    public string? TakeAwayStatment { get; set; }
    public string? DineInStatment { get; set; }
    public string? DeliveryStatment { get; set; }
}