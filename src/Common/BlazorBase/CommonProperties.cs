namespace BlazorBase;

public class CommonProperties
{
    public int CurrentOrderId { get; set; } = 1;
    public double CategorySpacing { get; set; } = 4.0;
    public double CategoryPadding => CategorySpacing * 2;
    public double CategoryFontSize => CategorySpacing + 16;
    public double SalesItemsHorizontalSlider = 4;
    public double SalesItemsVerticalSlider = 4;


    public decimal? SubTotal { get; set; }
    public decimal? TotalOrderPrice { get; set; }
    public List<TableItem>? TableItems { get; set; } = [];
    public List<TableItem>? AppendedTableItems { get; set; } = [];
    public bool TableItemDisabled { get; set; } = false;

    public event Action? OnChange;
    private string _currentPosMode = "TakeAway";

    public Receipt? OrderReceipt { get; set; }
    public string? CurrentUser { get; set; }
    public string? CurrentUserId { get; set; }
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


    /// <Discount>
    public OrderDiscount? OrderDiscount { get; set; } = new();
    public decimal? TotalDiscount { get; set; } = 0M;
    public decimal TotalAmountAfterDiscount { get; set; } = 0M;
    public decimal DiscountPercentage { get; set; } = 0M;
    public decimal DiscountValue { get; set; } = 0M;
    /// </Discount>

    // <DineIn>
    public DineInOrderDetails? CurrentDineInOrder { get; set; } = new();
    public Dictionary<int, DineInOrderDetails>? DineInOrdersDetails { get; set; } = new();
    public DineInOrderValues? DineInOrderValues { get; set; } = new();
    public int TableId { get; set; }
    public bool UpdateDineInOrder { get; set; } = false;
    /////////


    #region Delivery
    public CustomerDetails? CustomerDetails { get; set; } = new();

    #endregion
    public DateOnly? PosDate { get; set; } = new();

    public ICollection<OrderSettingToReturnDto> OrderSettings { get; set; } = new List<OrderSettingToReturnDto>();
    public TakeAwaySettings? TakeAwaySettings { get; set; }
    public DineInSettings? DineInSettings { get; set; }
    public DeliverySettings? DeliverySettings { get; set; }

    public IDialogReference? DialogReference { get; set; }
    public IDialogReference? ItemCommentDialogReference { get; set; }
    public IDialogReference? ItemDiscountDialogReference { get; set; }

    public OrderDto? OrderDto { get; set; } = new();
    public BranchToReturnDto? BranchDetails { get; set; } = new();

    public string? OrderNote { get; set; }
}