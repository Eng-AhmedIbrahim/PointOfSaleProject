QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogService();

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
var databaseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");


builder.Services.AddControllers();
builder.Services.AddSwaggerServices();
builder.Services.AddApplicationServices();

builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", options =>
    {
        options.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});


#region Database connections

builder.Services.AddDbContext<AppDbContext>(options => { options.UseSqlServer(databaseConnectionString); });

builder.Services.AddDbContext<AppIdentityDbContext>(options => { options.UseSqlServer(databaseConnectionString); });

#endregion

builder.Services.AddIdentityServices(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", options =>
    {
        options.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

var app = builder.Build();

#region Database Migrate

using var scope = app.Services.CreateScope();

var services = scope.ServiceProvider;

var dbContext = services.GetRequiredService<AppDbContext>();
var identityDbContext = services.GetRequiredService<AppIdentityDbContext>();

var loggerFactory = services.GetRequiredService<ILoggerFactory>();

try
{
    await dbContext.Database.MigrateAsync();
    await identityDbContext.Database.MigrateAsync();
    //await PosDbContextDataSeed.SeedAsync(_dbContext);
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

#endregion

await app.RunAsync();
