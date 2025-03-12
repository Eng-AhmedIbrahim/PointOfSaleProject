namespace ERPFront.Components.DineInComponents;

public partial class MenuButtons
{
    private void CreateDineInOrder()
    {
        if(_commonProperties!.CurrentDineInOrder!.CaptainName is null)
        {
            _snackbar.Add("Choice a Captain", Severity.Error);
        }


        _commonProperties!.CurrentDineInOrder!.BasicOrderDetails!.CashierName = _commonProperties.CurrentUser;
        
        _commonProperties.CurrentDineInOrder!.BasicOrderDetails!.OrderDataTime =
            _commonProperties.PosDate.HasValue
          ? _commonProperties.PosDate.Value.ToDateTime(TimeOnly.FromTimeSpan(DateTime.Now.TimeOfDay))
          : DateTime.Now;

        _commonProperties!.DineOrdersDetails!.Add(_commonProperties.CurrentDineInOrder.RelatedTableId, _commonProperties.CurrentDineInOrder);


        _navigationManager.NavigateTo("/pos");
    }
}