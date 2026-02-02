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
            _commonProperties._financeSettingsList[0].Value = grossTotal % 1 == 0
                ? Math.Truncate(grossTotal)
                : Math.Round(grossTotal, 2);
        }

        // We'll store the net-after-line price for Tax/Service calculation
        _commonProperties.SubTotal = grossTotal - totalLineDiscount;
    }

    public void CalculateTotalAmountFromTableItems(List<TableItem> items)
    {
        CalculateTotalAmount(items);
        NotifyStateChanged();
    }

    public void UpdateFinanceSettingsByMode(string posMode)
    {
        if (_commonProperties == null || _commonProperties._financeSettingsList == null)
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
            new FinanceSettings
            {
                Label = "Account",
                Value = 0M
            },
            new FinanceSettings
            {
                Label = "Discount",
                Value = _commonProperties.TotalDiscount % 1 == 0
                    ? Math.Truncate(_commonProperties.TotalDiscount ?? 0M)
                    : Math.Round(_commonProperties.TotalDiscount ?? 0M, 2)
            },
            new FinanceSettings
            {
                Label = "Tax",
                Value = orderSettings.Tax % 1 == 0
                    ? Math.Truncate(orderSettings.Tax ?? 0M)
                    : Math.Round(orderSettings.Tax ?? 0M, 2)
            },
            new FinanceSettings
            {
                Label = "Service",
                Value = orderSettings.Service % 1 == 0
                    ? Math.Truncate(orderSettings.Service ?? 0M)
                    : Math.Round(orderSettings.Service ?? 0M, 2)
            },
            new FinanceSettings
            {
                Label = "Total",
                Value = _commonProperties.TableItems?.Sum(i => i.Total ?? 0M) % 1 == 0
                    ? Math.Truncate(_commonProperties.TableItems?.Sum(i => i.Total ?? 0M) ?? 0M)
                    : Math.Round(_commonProperties.TableItems?.Sum(i => i.Total ?? 0M) ?? 0M, 2)
            }
        };

        NotifyStateChanged();
    }


    public void CalculateTotalOrderPrice()
    {
        // Net price after items discount but before order-level discount
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
            _commonProperties._financeSettingsList[4].Value = _commonProperties.TotalAmountAfterDiscount;
        }
    }

    public void ApplyDiscountAfterTotal()
    {
        // Apply order discount on the total (which includes tax/service on top of net item prices)
        decimal originalTotalWithTax = _commonProperties!.TotalOrderPrice ?? 0M;
        decimal amount = originalTotalWithTax;

        if (_commonProperties.OrderDiscount!.Percentage > 0)
        {
            amount -= (amount * _commonProperties.OrderDiscount.Percentage) / 100;
        }

        if (_commonProperties.OrderDiscount.Value > 0)
        {
            amount -= _commonProperties.OrderDiscount.Value;
        }

        decimal orderDiscountValue = originalTotalWithTax - amount;
        
        // TotalDiscount (Index 1) will show sum of Line + Order discounts
        decimal totalCombinedDiscount = (_commonProperties.TotalLineDiscount ?? 0M) + orderDiscountValue;
        
        _commonProperties.TotalDiscount = totalCombinedDiscount;
        if (_commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 1)
        {
            _commonProperties._financeSettingsList[1].Value = totalCombinedDiscount % 1 == 0 
                ? Math.Truncate(totalCombinedDiscount) 
                : Math.Round(totalCombinedDiscount, 2);
        }

        _commonProperties.TotalAmountAfterDiscount = amount;
    }

    public void CalculateSection4Table()
    {
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
        var totalOrderSetting = settings.Tax + settings.Service ;

        var amount = Amount + (totalOrderSetting * Amount) / 100;
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

    public void ResetDiscount()
    {
        _commonProperties!.DiscountPercentage = 0M;
        _commonProperties!.DiscountValue = 0M;
        _commonProperties.TotalDiscount = 0M;
        _commonProperties.OrderDiscount = new();
        if(_commonProperties._financeSettingsList is not null && _commonProperties._financeSettingsList.Count > 1)
        {
            _commonProperties._financeSettingsList![1].Value = 0M;
        }
        CalculateSection4Table();
        NotifyStateChanged();
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
}