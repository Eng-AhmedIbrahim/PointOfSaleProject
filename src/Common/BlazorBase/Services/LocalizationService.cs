using System.Globalization;
using Microsoft.Extensions.Localization;
using BlazorBase;

namespace BlazorBase.Services;

public class LocalizationService
{
    private readonly IStringLocalizer<AppResources> _localizer;
    private readonly CommonProperties _commonProperties;
    private readonly ILocalStorageService _localStorage;

    public event Action? OnLanguageChanged;

    public LocalizationService(IStringLocalizer<AppResources> localizer, CommonProperties commonProperties, ILocalStorageService localStorage)
    {
        _localizer = localizer;
        _commonProperties = commonProperties;
        _localStorage = localStorage;
    }

    public string this[string key] => _localizer[key];

    public async Task SetLanguage(string culture)
    {
        var cultureInfo = new CultureInfo(culture);
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;
        
        _commonProperties.Language = culture;
        _commonProperties.IsRtl = culture == "ar";
        
        await _localStorage.SetItemAsync("culture", culture);

        OnLanguageChanged?.Invoke();
    }

    public async Task InitializeAsync()
    {
        var culture = await _localStorage.GetItemAsync<string>("culture");
        if (string.IsNullOrEmpty(culture))
        {
            culture = "ar"; // Default
        }
        await SetLanguage(culture);
    }
    
    public string GetCurrentLanguage() => _commonProperties.Language;
    
    public bool IsRtl() => _commonProperties.IsRtl;
}

// Dummy class for resource generation
