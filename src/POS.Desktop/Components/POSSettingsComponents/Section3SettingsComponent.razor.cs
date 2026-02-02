namespace POS.Desktop.Components.POSSettingsComponents;

public partial class Section3SettingsComponent
{
    private const string UpdateFontSize = "updateFontSize";
    private const string ClearSec3FontSizeStorage = "clearSec3FontSizeStorage";
    private const string SpecialKeypadButton = "special-keypad-button";
    private const string SpecialQuantityButton = "special-quantity-button";
    private const string SpecialIconBtn = "special-icon-btn";

    private async Task IncreaseSize(string className)
    {
        switch (className)
        {
            // case SpecialQuantityButton:
            //     await JS.InvokeVoidAsync(UpdateFontSize, SpecialQuantityButton, 0.7);
            //     break;
            case SpecialKeypadButton:
                await Js.InvokeVoidAsync(UpdateFontSize, SpecialKeypadButton, 2);
                break;
            case SpecialIconBtn:
                await Js.InvokeVoidAsync(UpdateFontSize, SpecialIconBtn, 4);
                break;
            default:
                return;
        }
    }

    private async Task DecreaseSize(string className)
    {
        switch (className)
        {
            // case SpecialQuantityButton:
            //     await JS.InvokeVoidAsync(UpdateFontSize, SpecialQuantityButton, -0.7);
            //     break;
            case SpecialKeypadButton:
                await Js.InvokeVoidAsync(UpdateFontSize, SpecialKeypadButton, -2);
                break;
            case SpecialIconBtn:
                await Js.InvokeVoidAsync(UpdateFontSize, SpecialIconBtn, -4);
                break;
            default:
                return;
        }
    }

    private async Task Reset()
        => await Js.InvokeVoidAsync(ClearSec3FontSizeStorage);
}