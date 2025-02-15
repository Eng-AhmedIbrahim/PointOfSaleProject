namespace BlazorBase;

public class CommonProsperities
{
    public double CategorySpacing { get; set; } = 4.0;
    public double CategoryPadding => CategorySpacing * 2;
    public double CategoryFontSize => CategorySpacing + 16;
    public double _salesItemsHorizontalSider = 4;
    public double _salesItemsVerticalSlider = 4;
    public List<TableItem>? _tableItems { get; set; } = new List<TableItem>();

    public Task ClearTableItems()
    {
        _tableItems?.Clear();
        return Task.CompletedTask;
    }

}
