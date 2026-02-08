namespace POS.Desktop.Components.DineInComponents;

public partial class TableReservationDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public int TableId { get; set; }
    [Parameter] public string TableName { get; set; } = string.Empty;
    [Parameter] public string CurrentUser { get; set; } = string.Empty;
    [Parameter] public string CurrentUserId { get; set; } = string.Empty;

    private string CustomerName { get; set; } = string.Empty;
    private string CustomerPhone { get; set; } = string.Empty;
    private DateTime? ReservationDate { get; set; } = DateTime.Today;
    private TimeSpan? ReservationTime { get; set; } = new TimeSpan(18, 0, 0); // Default 6:00 PM
    private int GuestCount { get; set; } = 2;
    private decimal DepositAmount { get; set; } = 0;
    private string SpecialRequests { get; set; } = string.Empty;

    private void Cancel() => MudDialog.Cancel();

    private async Task Reserve()
    {
        // Validation
        if (string.IsNullOrWhiteSpace(CustomerName))
        {
            Snackbar.Add("Customer name is required", Severity.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(CustomerPhone))
        {
            Snackbar.Add("Phone number is required", Severity.Error);
            return;
        }

        if (!ReservationDate.HasValue || !ReservationTime.HasValue)
        {
            Snackbar.Add("Reservation date and time are required", Severity.Error);
            return;
        }

        if (GuestCount < 1)
        {
            Snackbar.Add("Guest count must be at least 1", Severity.Error);
            return;
        }

        // Combine date and time
        var scheduledDateTime = ReservationDate.Value.Date.Add(ReservationTime.Value);

        // Check if reservation is in the past
        if (scheduledDateTime < DateTime.Now)
        {
            Snackbar.Add("Reservation time cannot be in the past", Severity.Error);
            return;
        }

        // Create reservation DTO
        var reservation = new DineInOrderDto
        {
            TableId = TableId,
            TableName = TableName,
            CustomerName = CustomerName,
            CustomerPhone = CustomerPhone,
            ScheduleDateTime = scheduledDateTime,
            CustomerCount = GuestCount,
            ReservationPaid = DepositAmount,
            OrderNotice = SpecialRequests,
            CashierId = CurrentUserId,
            CashierName = CurrentUser,
            OrderState = "Reserved"
        };

        var result = await DineInOrderFrontService.ReserveTableAsync(TableId, reservation);
        
        if (result)
        {
            Snackbar.Add($"Table {TableName} reserved successfully for {CustomerName}", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            Snackbar.Add("Failed to create reservation. Table may already be reserved or occupied.", Severity.Error);
        }
    }
}
