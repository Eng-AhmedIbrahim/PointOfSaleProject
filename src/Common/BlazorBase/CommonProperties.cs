namespace BlazorBase;

public class CommonProperties
{
    public double CategorySpacing { get; set; } = 4.0;
    public double CategoryPadding => CategorySpacing * 2;
    public double CategoryFontSize => CategorySpacing + 16;
    public double SalesItemsHorizontalSlider = 4;
    public double SalesItemsVerticalSlider = 4;
    public List<TableItem>? TableItems { get; set; } = [];

    public Task ClearTableItems()
    {
        TableItems?.Clear();
        return Task.CompletedTask;
    }

}
