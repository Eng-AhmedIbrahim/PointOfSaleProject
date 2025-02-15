using BlazorBase;
using Microsoft.JSInterop;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMudServices();
builder.Services.AddBlazorBootstrap();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.AddSerilogService();
builder.Services.AddSingleton<CommonProsperities>();


builder.Services.AddHttpClient(ConstantStrings.ApiUrlName, client =>
{
     client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("BaseApiUrl")!);
});


builder.Services.AddAuthenticationCore();
//builder.Services.AddHttpClient

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStatusCodePagesWithRedirects("/404");

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
