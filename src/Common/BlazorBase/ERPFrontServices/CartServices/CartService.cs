using POS.Contract.Models;

namespace BlazorBase.ERPFrontServices.CartServices;

public class CartService : ICartService
{
    public TableItem? SelectedItem { get; private set; }
    public event Action? OnChange;
    private readonly CommonProperties? _commonProperties;

    public CartService(CommonProperties commonProperties)
        => _commonProperties = commonProperties;

    public void SetSelectedItem(TableItem item)
       => SelectedItem = item;

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
            RecalculateItemTotals(SelectedItem);
            UpdateAmount();
            CalculateSection4Table();
            NotifyStateChanged();
        }
    }

    private void RecalculateItemTotals(TableItem item)
    {
        decimal originalTotal = item.Quantity * (item.Price ?? 0M);
        
        if (item.HasDiscount)
        {
            if (item.DiscountPercentage.HasValue && item.DiscountPercentage.Value > 0)
            {
                decimal discountAmount = originalTotal * (item.DiscountPercentage.Value / 100);
                item.TotalDiscountPrice = discountAmount;
                item.TotalAfterDiscount = originalTotal - discountAmount;
            }
            else if (item.DiscountAmount.HasValue && item.DiscountAmount.Value > 0)
            {
                item.TotalDiscountPrice = item.DiscountAmount.Value;
                item.TotalAfterDiscount = originalTotal - item.DiscountAmount.Value;
            }
            else
            {
                item.TotalDiscountPrice = 0;
                item.TotalAfterDiscount = originalTotal;
            }
            
            item.TotalAmount = item.TotalAfterDiscount;
            item.Total = item.TotalAfterDiscount;
        }
        else
        {
            item.TotalDiscountPrice = 0;
            item.TotalAfterDiscount = originalTotal;
            item.TotalAmount = originalTotal;
            item.Total = originalTotal;
        }
    }

    public void RemoveItem(List<TableItem> items)
    {
        if (SelectedItem != null && items.Contains(SelectedItem))
        {
            items.Remove(SelectedItem);
        }

        CalculateTotalAmount(items);
        CalculateSection4Table();
        NotifyStateChanged();
    }

    private void CalculateTotalAmount(List<TableItem> items)
     => UpdateAmount();


    private void UpdateAmount()
    {
        decimal grossTotal = _commonProperties!.TableItems?.Sum(i => i.Quantity * (i.Price ?? 0M)) ?? 0M;
        decimal totalLineDiscount = _commonProperties.TableItems?.Sum(i => i.TotalDiscountPrice ?? 0M) ?? 0M;
        
        _commonProperties.TotalLineDiscount = totalLineDiscount;
        
        // Account (Index 0) will show Gross Total
        if (_commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 0)
        {
            _commonProperties._financeSettingsList[0].Value = FormatValue(grossTotal);
        }

        // We'll store the net-after-line price for Tax/Service calculation
        _commonProperties.SubTotal = grossTotal - totalLineDiscount;
    }

    private decimal FormatValue(decimal value)
    {
        return value % 1 == 0 ? Math.Truncate(value) : Math.Round(value, 2);
    }

    public void CalculateTotalAmountFromTableItems(List<TableItem> items)
    {
        CalculateTotalAmount(items);
        NotifyStateChanged();
    }

    public void UpdateFinanceSettingsByMode(string posMode)
    {
        if (_commonProperties == null || _commonProperties.OrderSettings == null)
            return;

        var orderType = posMode switch
        {
            nameof(PosModes.TakeAway) => PosModes.TakeAway.ToString(),
            nameof(PosModes.Delivery) => PosModes.Delivery.ToString(),
            nameof(PosModes.DineIn) => PosModes.DineIn.ToString(),
            _ => null
        };

        if (orderType == null) return;

        var orderSettings = _commonProperties.OrderSettings
            .FirstOrDefault(x => x.OrderType == orderType);

        if (orderSettings == null) return;

        _commonProperties._financeSettingsList = new()
        {
            new FinanceSettings { Label = "Account", Value = 0M },
            new FinanceSettings { Label = "Discount", Value = 0M },
            new FinanceSettings { Label = "Tax", Value = 0M },
            new FinanceSettings { Label = "Service", Value = 0M },
            new FinanceSettings { Label = "Total", Value = 0M }
        };

        UpdateAmount();
        CalculateSection4Table();
        NotifyStateChanged();
    }


    public void CalculateTotalOrderPrice()
    {
        decimal netAmountAfterLine = _commonProperties.SubTotal ?? 0M;

        switch (_commonProperties!.CurrentPosMode)
        {
            case "TakeAway":
                _commonProperties.TotalOrderPrice = CalculateAmountAfterOrderSettings(_commonProperties.TakeAwaySettings!, netAmountAfterLine);
                break;
            case "DineIn":
                _commonProperties.TotalOrderPrice = CalculateAmountAfterOrderSettings(_commonProperties.DineInSettings!, netAmountAfterLine);
                break;
            case "Delivery":
                _commonProperties.TotalOrderPrice = CalculateAmountAfterOrderSettings(_commonProperties.DeliverySettings!, netAmountAfterLine);
                break;
        }

        ApplyDiscountAfterTotal();
        
        if (_commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 4)
        {
            decimal finalTotal = _commonProperties.TotalAmountAfterDiscount;
            _commonProperties._financeSettingsList[4].Value = FormatValue(finalTotal);
        }
    }

    public void ApplyDiscountAfterTotal()
    {
        decimal originalTotalWithTax = _commonProperties!.TotalOrderPrice ?? 0M;
        decimal amount = originalTotalWithTax;

        if (_commonProperties.OrderDiscount != null)
        {
            if (_commonProperties.OrderDiscount.Percentage > 0)
            {
                amount -= (amount * _commonProperties.OrderDiscount.Percentage) / 100;
            }

            if (_commonProperties.OrderDiscount.Value > 0)
            {
                amount -= _commonProperties.OrderDiscount.Value;
            }
        }

        if (amount < 0) amount = 0;

        decimal orderDiscountValue = originalTotalWithTax - amount;
        decimal totalCombinedDiscount = (_commonProperties.TotalLineDiscount ?? 0M) + orderDiscountValue;
        
        _commonProperties.TotalDiscount = totalCombinedDiscount;
        if (_commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 1)
        {
            _commonProperties._financeSettingsList[1].Value = FormatValue(totalCombinedDiscount);
        }

        _commonProperties.TotalAmountAfterDiscount = amount;
    }

    public void CalculateSection4Table()
    {
        UpdateAmount();
        CalculateTotalOrderPrice();
        NotifyStateChanged();
    }

    public decimal CalculateAmountAfterOrderSettings<T>(T orderSettings, decimal Amount)
    {
        if (orderSettings is TakeAwaySettings takeawaySettings)
        {
            return CalculateTotalAmountHelper(takeawaySettings, Amount);
        }

        if (orderSettings is DeliverySettings deliverySettings)
        {
            return CalculateTotalAmountHelper(deliverySettings, Amount);
        }

        if (orderSettings is DineInSettings dinInSettings)
        {
            return CalculateTotalAmountHelper(dinInSettings, Amount);
        }

        return 0M;
    }

    private decimal CalculateTotalAmountHelper(dynamic settings, decimal Amount)
    {
        decimal taxRate = settings.Tax ?? 0M;
        decimal serviceRate = settings.Service ?? 0M;

        decimal taxAmount = (taxRate * Amount) / 100;
        decimal serviceAmount = (serviceRate * Amount) / 100;

        // Store these in finance settings if they exist
        if (_commonProperties._financeSettingsList != null)
        {
            if (_commonProperties._financeSettingsList.Count > 2)
                _commonProperties._financeSettingsList[2].Value = FormatValue(taxAmount);
            if (_commonProperties._financeSettingsList.Count > 3)
                _commonProperties._financeSettingsList[3].Value = FormatValue(serviceAmount);
        }

        var amount = Amount + taxAmount + serviceAmount;
        return Math.Round(amount * 2, MidpointRounding.AwayFromZero) / 2;
    }
    public async void NotifyStateChanged()
     => OnChange?.Invoke();

    public void ClearDineInOrderAttributes()
    {
        _commonProperties!.TotalDiscount = 0M;
        _commonProperties._financeSettingsList![0].Value = 0M;
        _commonProperties.TotalOrderPrice = 0M;
        _commonProperties.TableItems!.Clear();
        _commonProperties!.CurrentDineInOrder = new();
        _commonProperties.DineInOrderValues = new();
        _commonProperties!.OrderDiscount = new();
        _commonProperties.CustomerName = "";
        _commonProperties.CustomerPhone = "";
        _commonProperties.SelectedPaymentMethod = PaymentMethod.Cash;
    }

    public void ClearTakeAwayOrderAttributes()
    {
        _commonProperties!.TotalDiscount = 0M;
        _commonProperties._financeSettingsList![0].Value = 0M;
        _commonProperties.TotalOrderPrice = 0M;
        _commonProperties.TableItems!.Clear();
        _commonProperties!.OrderDto = new();
        _commonProperties.OrderNote = string.Empty ;
        _commonProperties!.OrderDiscount = new();
        _commonProperties.CustomerName = "";
        _commonProperties.CustomerPhone = "";
        _commonProperties.SelectedPaymentMethod = PaymentMethod.Cash;
    }

    public void RemoveItemDiscount()
    {
        if (SelectedItem == null) return;
        SelectedItem.HasDiscount = false;
        SelectedItem.DiscountPercentage = null;
        SelectedItem.DiscountAmount = null;
        SelectedItem.TotalDiscountPrice = 0;
        RecalculateItemTotals(SelectedItem);
        UpdateAmount();
        CalculateSection4Table();
        NotifyStateChanged();
    }

    public string? AddItemComment(string comment)
    {
        if (SelectedItem is null )
            return null;

        var tableItem = _commonProperties!.TableItems!
            .FirstOrDefault(i => i.Id == SelectedItem.Id);

        var random = new Random();
        var commentAttribute = new AttributeDto()
        {
            Id = random.Next(5000, 100000),
            Name = comment
        };

        tableItem!.Attributes!.Add(commentAttribute);

        NotifyStateChanged();

        return "success";
    }

    public string? EditItemComment(string oldComment, string newComment)
    {
        if (SelectedItem!.Id == 0)
            return null;

        var tableItem = _commonProperties!.TableItems!
            .FirstOrDefault(i => i.Id == SelectedItem.Id);

        var existingComment = tableItem?.Attributes?
            .FirstOrDefault(attr => attr.Name == oldComment);

        if (existingComment == null)
            return "comment not found";

        existingComment.Name = newComment;

        NotifyStateChanged();

        return "success";
    }

    public string? DeleteItemComment(string comment)
    {
        if (SelectedItem!.Id == 0)
            return null;

        var tableItem = _commonProperties!.TableItems!
            .FirstOrDefault(i => i.Id == SelectedItem.Id);

        var commentToRemove = tableItem?.Attributes?
            .FirstOrDefault(attr => attr.Name == comment);

        if (commentToRemove == null)
            return "comment not found";

        tableItem!.Attributes!.Remove(commentToRemove);

        NotifyStateChanged();

        return "success";
    }

    public void AddItemDiscount(string discountType, decimal discountValue)
    {
        if (SelectedItem == null || SelectedItem.Id == 0)
            return;

        SelectedItem.HasDiscount = true;
        if (discountType == "Percentage")
        {
            SelectedItem.DiscountPercentage = discountValue;
            SelectedItem.DiscountAmount = null;
        }
        else if (discountType == "Value")
        {
            SelectedItem.DiscountAmount = discountValue;
            SelectedItem.DiscountPercentage = null;
        }

        RecalculateItemTotals(SelectedItem);

        UpdateAmount();
        CalculateSection4Table();
        NotifyStateChanged();
    }

    public void ApplyOrderDiscount(decimal discountValue, bool isPercentage, DiscountReason reason)
    {
        if (_commonProperties!.OrderDiscount == null)
            _commonProperties.OrderDiscount = new();

        if (isPercentage)
        {
            _commonProperties.OrderDiscount.Percentage = discountValue;
            _commonProperties.OrderDiscount.Value = 0;
            _commonProperties.OrderDiscount.DiscountType = "Percentage";
        }
        else
        {
            _commonProperties.OrderDiscount.Value = discountValue;
            _commonProperties.OrderDiscount.Percentage = 0;
            _commonProperties.OrderDiscount.DiscountType = "Value";
        }

        _commonProperties.OrderDiscount.DiscountReason = reason.ToString();

        CalculateSection4Table();
        NotifyStateChanged();
    }

    public void ResetDiscount()
    {
        if (_commonProperties!.OrderDiscount != null)
        {
            _commonProperties.OrderDiscount.Percentage = 0;
            _commonProperties.OrderDiscount.Value = 0;
            _commonProperties.OrderDiscount.DiscountType = null;
            _commonProperties.OrderDiscount.DiscountReason = null;
        }

        CalculateSection4Table();
        NotifyStateChanged();
    }
}