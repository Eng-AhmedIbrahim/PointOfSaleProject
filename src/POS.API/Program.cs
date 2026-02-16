using POS.API.Hubs;
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogService();

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
var databaseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Decryption temporarily disabled - connection string is plain text
// var encryptedConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// var databaseConnectionString = POS.API.Helpers.EncryptionHelper.DecryptString(encryptedConnectionString!);


builder.Services.AddControllers();
builder.Services.AddSwaggerServices();
builder.Services.AddApplicationServices();
builder.Services.AddSignalR();

var callCenterSettings = builder.Configuration.GetSection("CallCenterSettings").Get<CallCenterSettings>() ?? new CallCenterSettings();
builder.Services.AddSingleton(callCenterSettings);

if (callCenterSettings.IsCentralCallCenter)
{
    builder.Services.AddHostedService<OrderRetryBackgroundService>();
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", options =>
    {
        options.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ => true) // Allow any origin for SignalR
            .AllowCredentials(); // Required for SignalR
    });
});


#region Database connections


builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(databaseConnectionString);
});

builder.Services.AddDbContext<AppIdentityDbContext>(options =>
{
    options.UseSqlServer(databaseConnectionString);
});

#endregion

builder.Services.AddIdentityServices(builder.Configuration);

var app = builder.Build();

#region Database Migrate

using var scope = app.Services.CreateScope();

var services = scope.ServiceProvider;

var dbContext = services.GetRequiredService<AppDbContext>();
var identityDbContext = services.GetRequiredService<AppIdentityDbContext>();
var userManager = services.GetRequiredService<UserManager<AppUser>>();
var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

var loggerFactory = services.GetRequiredService<ILoggerFactory>();

try
{
    await dbContext.Database.MigrateAsync();
    await identityDbContext.Database.MigrateAsync();
    await PosDbContextDataSeed.SeedAsync(dbContext);
    await AppIdentityDbContextSeed.SeedAsync(userManager, roleManager, identityDbContext);
}
catch (Exception ex)
{
    var logger = loggerFactory.CreateLogger<Program>();
    logger.LogError(ex, "An error occurred during migration");
}

#endregion

#region Configure Kestrel Middelewares

app.UseMiddleware<ExeptionMiddleWare>();

app.UseSwaggerServices();

app.UseStatusCodePagesWithReExecute("/errors/{0}");

app.UseHttpsRedirection();
app.UseCors("MyPolicy");
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<DeliveryHub>("/callcenterhub");
#endregion

await app.RunAsync();
