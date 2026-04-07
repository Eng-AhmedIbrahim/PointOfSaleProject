namespace POS.Desktop.Components.POSLayoutComponents;

public partial class SettingsDrawer
{
    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    [Parameter] public Anchor Anchor { get; set; } = Anchor.Start;
    [Parameter] public bool OverlayAutoClose { get; set; } = true;
    [Parameter] public string Title { get; set; } = "Settings";
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string _buttonText = "Start";

    private async Task OnOpenChanged(bool value)
    {
        if (Open != value)
        {
            Open = value;
            await OpenChanged.InvokeAsync(value);
        }
    }

    private async Task SetAnchor(Anchor anchor)
    {
        Anchor = anchor;
        _buttonText = anchor.ToString();
        // Since anchor change doesn't necessarily close the drawer, no need to notify Open changed here
    }

    private async Task CloseDrawer()
    {
        await OnOpenChanged(false);
    }

    private string _currentPageName = "";
    private string CurrentPageName => _currentPageName;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await AttemptToInitializePageNameAsync();
        }
    }

    private async Task AttemptToInitializePageNameAsync()
    {
        int maxRetries = 5;
        int currentRetry = 0;
        int delayMs = 100;

        while (currentRetry < maxRetries)
        {
            try
            {
                // Accessing Navigation.Uri too early in Blazor Hybrid can throw
                if (Navigation != null && !string.IsNullOrEmpty(Navigation.Uri))
                {
                    _currentPageName = GetCurrentPageName();
                    StateHasChanged();
                    return;
                }
                else
                {
                    throw new InvalidOperationException("NavigationManager not yet initialized");
                }
            }
            catch (InvalidOperationException)
            {
                currentRetry++;
                if (currentRetry >= maxRetries)
                {
                    _currentPageName = "Unknown";
                    StateHasChanged();
                    return;
                }
                
                await Task.Delay(delayMs);
                delayMs *= 2;
            }
        }
    }

    private string GetCurrentPageName()
    {
        try
        {
            if (Navigation == null || string.IsNullOrEmpty(Navigation.Uri) || string.IsNullOrEmpty(Navigation.BaseUri))
                return "Unknown";

            var currentRoute = Navigation.Uri.Replace(Navigation.BaseUri, "")
                .Trim('/');

            return currentRoute.ToLower() switch
            {
                "" or "index" => PagesNames.Home,
                "login" => PagesNames.Login,
                "register" => PagesNames.Register,
                "pos" => PagesNames.POS,
                _ => "Unknown"
            };
        }
        catch
        {
            return "Unknown";
        }
    }
}