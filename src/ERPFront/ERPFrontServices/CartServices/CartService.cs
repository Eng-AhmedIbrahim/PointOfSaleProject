using POS.Contract.Models;

namespace ERPFront.ERPFrontServices.CartServices;

public class CartService : ICartService
{
    public TableItem? SelectedItem { get; private set; }
    public event Action? OnChange;
    public string? Quantity { get; set; }

    private readonly CommonProperties? _commonProperties;
    public CartService(CommonProperties commonProperties)
    {
        _commonProperties = commonProperties;
    }

    public void SetSelectedItem(TableItem item)
       => SelectedItem = item;

    private async void NotifyStateChanged()
     =>  OnChange?.Invoke();

    public void AppendNumberToQuantity(string number)
    => UpdateQuantity(_ => int.Parse(number));

    public void OnClickBS()
     => UpdateQuantity(current => current > 9 ?
                        int.Parse(current.ToString().Substring(0, current.ToString().Length - 1)) : 0);

    public void IncrementQuantity()
    => UpdateQuantity(current => current + 1);

    public void DecrementQuantity()
    => UpdateQuantity(current => current - 1 <= 0 ? 1 : current - 1);

    public void UpdateQuantity(Func<int, int> updateFunc)
    {
        if (SelectedItem != null)
        {
            SelectedItem.Quantity = updateFunc(SelectedItem.Quantity);
            SelectedItem.Total = SelectedItem.Quantity * SelectedItem.Price;
            UpdateAmount();
            NotifyStateChanged();
        }
    }

    public void RemoveItem(List<TableItem> items)
    {
        if (SelectedItem != null && items.Contains(SelectedItem))
        {
            items.Remove(SelectedItem);
        }

        CalculateTotalAmount(items);
        NotifyStateChanged();
    }

    private void CalculateTotalAmount(List<TableItem> items)
    => UpdateAmount();

    private void UpdateAmount()
        => _commonProperties!._financeSettingsList![0].Value = _commonProperties!.TableItems!.Sum(i => i.Total ?? 0);


    public void CalculateTotalAmountFromTableItems(List<TableItem> items)
    {
        CalculateTotalAmount(items);
        NotifyStateChanged();
    }

    //public void UpdateTotalAmount()
    //{
    //    NotifyStateChanged();
    //}
}