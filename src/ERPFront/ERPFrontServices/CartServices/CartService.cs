namespace ERPFront.ERPFrontServices.CartServices;

public class CartService : ICartService
{
    public TableItem? SelectedItem { get; private set; }
    public event Action? OnChange;
    public string? Quantity { get; set; }

    public void SetSelectedItem(TableItem item)
       => SelectedItem = item;

    private async void NotifyStateChanged()
     => OnChange?.Invoke();

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
            NotifyStateChanged();
        }
    }

    public void RemoveItem(List<TableItem> items)
    {
        items.Remove(SelectedItem ?? new TableItem());
        NotifyStateChanged();
    }
}