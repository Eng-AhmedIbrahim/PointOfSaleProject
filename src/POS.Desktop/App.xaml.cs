using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;
using System.Windows;
using BlazorBase;
using BlazorBase.ERPFrontServices.PrintOrderServices;
using ERPFront.Auth;
using ERPFront.Extensions;
using MudBlazor.Services;
using Blazored.LocalStorage;
using System.Text.Json;
using BlazorBase.ERPFrontServices.Section4ButtonsService;
using BlazorBase.ERPFrontServices.CartServices;
using BlazorBase.ERPFrontServices.CategoryServices;
using BlazorBase.ERPFrontServices.AppDateServices;
using BlazorBase.ERPFrontServices.DineInServices;
using BlazorBase.ERPFrontServices.OrderServices;
using BlazorBase.ERPFrontServices.DeliveryServices;
using BlazorBase.ERPFrontServices.BranchServices;
using BlazorBase.API;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using POS.Authorization.Models;
using POS.Desktop.Components;

namespace POS.Desktop;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Setup Serilog for file logging
        SetupSerilog();

        // Configure services
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Create and show main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void SetupSerilog()
    {
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        Directory.CreateDirectory(logPath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Error)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(logPath, "POS-Desktop-.txt"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Application starting up");
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddSerilog();
        });

        // Add WPF services
        services.AddSingleton<MainWindow>();

        // Add Blazor services
        services.AddBlazorWebView();
        services.AddMudServices();
        services.AddBlazorBootstrap();

        // Configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // API Settings
        services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));
        services.AddSingleton<ApiSettings>(sp =>
            sp.GetRequiredService<IOptions<ApiSettings>>().Value);

        // HTTP Client
        services.AddHttpClient(configuration["ApiSettings:ApiName"]!,
            client => { client.BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"]!); })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseCookies = true,
                AllowAutoRedirect = true
            });

        // Application services
        services.AddSingleton<CommonProperties>();
        services.AddSingleton<HandelDeliveryInvocation>();
        services.AddSingleton<CartService>();
        services.AddSingleton<Section4ButtonsServices>();
        services.AddScoped<CustomAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthenticationStateProvider>());
        services.AddScoped<DineInService>();
        services.AddScoped<AppDateService>();
        services.AddScoped<OrderSettingsService>();
        services.AddScoped<DeliveryServices>();
        services.AddScoped<BranchService>();
        services.AddScoped<PrintOrderService>();

        // Category services
        services.AddScoped<ICategoryServices, CategoryService>();
        services.AddScoped<ICustomizationSettingsService, CustomizationSettingsService>();

        // Local Storage
        services.AddBlazoredLocalStorage(config =>
        {
            config.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            config.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            config.JsonSerializerOptions.IgnoreReadOnlyProperties = true;
            config.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            config.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            config.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
            config.JsonSerializerOptions.WriteIndented = false;
        });

        // Authentication & Authorization
        services.AddAuthenticationCore();
        services.AddAuthorization(options =>
        {
            foreach (var policy in Permissions.RolePermissions)
            {
                options.AddPolicy(policy.Key, policyBuilder =>
                    policyBuilder.RequireClaim("Permission", policy.Value));
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application shutting down");
        Log.CloseAndFlush();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
