
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddBlazorBootstrap();

builder.AddSerilogService();

builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));
builder.Services.AddSingleton<ApiSettings>(sp =>
    sp.GetRequiredService<IOptions<ApiSettings>>()
        .Value);

builder.Services.AddSingleton<CommonProperties>();
builder.Services.AddSingleton<CartService>();

//DevExpress Configurations
builder.Services.AddDevExpressBlazor();
builder.Services.AddDevExpressServerSideBlazorReportViewer();
builder.Services.Configure<DevExpress.Blazor.Configuration.GlobalOptions>(options =>
{
    options.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5;
});
builder.WebHost.UseWebRoot("wwwroot");
builder.WebHost.UseStaticWebAssets();


builder.Services.AddHttpClient(builder.Configuration["ApiSettings:ApiName"]!,
    client => { client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]!); });

builder.Services.AddScoped<ICategoryServices, CategoryService>();

builder.Services.AddAuthenticationCore();


var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection();
//app.UsedevExpressBlazorWepServerReportViewer();

app.UseStatusCodePagesWithRedirects("/404");

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();
await app.RunAsync();