namespace ERPFront.Components.PosComponent;

public partial class Section4Buttons
{

    private void PrintOrder()
    {
        if(_commonProperties!.TableItems!.Any())
        {
            _navigationManager.NavigateTo("/printOrder");
        }else
        {
            _snackbar.Add("No Order to print", Severity.Info);
        }
    }
}