namespace POS.API;

public class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddSerilogService();

        var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
        var databaseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");


        builder.Services.AddControllers();
        builder.Services.AddSwaggerServices();
        builder.Services.AddApplicationServices();

        builder.Services.AddFlexibleCaching(redisConnectionString);

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

        var _dbContext = services.GetRequiredService<AppDbContext>();

        var loggerFactory = services.GetRequiredService<ILoggerFactory>();

        try
        {
            //await _dbContext.Database.MigrateAsync();
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

        if (app.Environment.IsDevelopment())
            app.UseSwaggerServices();

        app.UseStatusCodePagesWithReExecute("/errors/{0}");

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        #endregion

        await app.RunAsync();
    }
}