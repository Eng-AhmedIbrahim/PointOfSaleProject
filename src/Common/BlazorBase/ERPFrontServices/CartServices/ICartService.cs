using POS.Contract.Models;

namespace BlazorBase.ERPFrontServices.CartServices;
public interface ICartService
{
    public void SetSelectedItem(TableItem item);

    public void AppendNumberToQuantity(string number);
    public void OnClickBS();
    public void IncrementQuantity();
    public void DecrementQuantity();
    public void RemoveItem(List<TableItem> items);
    public void UpdateFinanceSettingsByMode(string posMode);
    //public void AppendSpecialNumberToQuantity(string number);
}
