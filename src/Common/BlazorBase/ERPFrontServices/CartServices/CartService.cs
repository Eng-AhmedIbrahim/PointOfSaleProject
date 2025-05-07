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
            SelectedItem.Total = SelectedItem.Quantity * SelectedItem.Price;
            SelectedItem.TotalAmount = SelectedItem.Total;
            UpdateAmount();
            CalculateSection4Table();
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
        CalculateSection4Table();
        NotifyStateChanged();
    }

    private void CalculateTotalAmount(List<TableItem> items)
     => UpdateAmount();


    private void UpdateAmount()
    {
       _commonProperties!._financeSettingsList![0].Value =
       _commonProperties!.TableItems!.Sum(i => i.Total ?? 0) % 1 == 0
       ? Math.Truncate(_commonProperties!.TableItems!.Sum(i => i.Total ?? 0))
       : Math.Round(_commonProperties!.TableItems!.Sum(i => i.Total ?? 0), 2);

        _commonProperties.TotalAmountAfterDiscount = _commonProperties._financeSettingsList[0].Value ?? 0M;
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
        switch (_commonProperties!.CurrentPosMode)
        {
            case "TakeAway":
                {
                    _commonProperties.TotalOrderPrice = CalculateAmountAfterOrderSettings<TakeAwaySettings>
                        (_commonProperties!.TakeAwaySettings!, _commonProperties._financeSettingsList![0].Value ?? 0M);
                    break;
                }
            case "DineIn":
                {
                    _commonProperties.TotalOrderPrice = CalculateAmountAfterOrderSettings<DineInSettings>
                        (_commonProperties!.DineInSettings!, _commonProperties._financeSettingsList![0].Value ?? 0M);
                    break;
                }
            case "Delivery":
                {
                    _commonProperties.TotalOrderPrice = CalculateAmountAfterOrderSettings<DeliverySettings>
                        (_commonProperties!.DeliverySettings!, _commonProperties._financeSettingsList![0].Value ?? 0M);
                    break;
                }
        }

        ApplyDiscountAfterTotal();
        _commonProperties!._financeSettingsList![4].Value = _commonProperties.TotalAmountAfterDiscount;
    }

    public void ApplyDiscountAfterTotal()
    {
        decimal? amount = _commonProperties!.TotalOrderPrice;

        if (_commonProperties.OrderDiscount!.Percentage > 0)
        {
            amount -= (amount * _commonProperties.OrderDiscount.Percentage) / 100;
            _commonProperties._financeSettingsList![1].Value = _commonProperties.OrderDiscount.Percentage;
        }

        if (_commonProperties.OrderDiscount.Value > 0)
        {
            amount -= _commonProperties.OrderDiscount.Value;
            _commonProperties._financeSettingsList![1].Value = _commonProperties.OrderDiscount.Value;
        }

        _commonProperties.TotalAmountAfterDiscount = amount ?? 0M;
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

    public void AddItemDiscount(string discountType,decimal discountValue)
    {
        if (SelectedItem!.Id == 0)
            return;
            
        if (discountType == "Percentage")
        {
            SelectedItem!.HasDiscount = true;
            SelectedItem!.DiscountPercentage = discountValue;
            SelectedItem.TotalDiscountPrice = SelectedItem.Quantity * SelectedItem.Price - SelectedItem.Quantity * SelectedItem.Price * discountValue / 100;
            SelectedItem.TotalAfterDiscount = SelectedItem.Quantity * SelectedItem.Price - SelectedItem.TotalDiscountPrice;
            SelectedItem.TotalAmount = SelectedItem.TotalAfterDiscount;
            SelectedItem.Total = SelectedItem.Quantity * SelectedItem.Price - SelectedItem.Quantity * SelectedItem.Price * discountValue / 100;
        }

        if(discountType == "Value")
        {
            SelectedItem!.HasDiscount = true;
            SelectedItem!.DiscountAmount = discountValue;
            SelectedItem.TotalDiscountPrice = discountValue;
            SelectedItem.TotalAfterDiscount = SelectedItem.Quantity * SelectedItem.Price - discountValue;
            SelectedItem.TotalAmount = SelectedItem.TotalAfterDiscount;
            SelectedItem.Total = SelectedItem.Quantity * SelectedItem.Price - discountValue;
        }

        UpdateAmount();
        CalculateSection4Table();
        NotifyStateChanged();
    }
}