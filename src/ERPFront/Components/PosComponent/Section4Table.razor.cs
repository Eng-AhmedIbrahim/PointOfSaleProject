using POS.Contract.Models;

namespace ERPFront.Components.PosComponent;

public partial class Section4Table
{
    public TableItem? _elementBeforeEdit;

    [Parameter] public List<TableItem>? Items { get; set; }
    [Parameter] public EventCallback OnItemsChanged { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InvokeAsync(StateHasChanged);
        }
    }
}