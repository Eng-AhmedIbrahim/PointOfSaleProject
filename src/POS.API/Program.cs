using POS.API.MiddleWare;

namespace POS.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        builder.Services.AddSwaggerServices();
        builder.Services.AddAplicationServices();


        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        });

        builder.Services.AddDbContext<AppIdentityDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        });

        builder.Services.AddIdentityServices(builder.Configuration);


        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
           app.UseSwaggerServices();
        }



        app.UseMiddleware<ExeptionMiddleWare>();

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        
        app.UseAuthentication();
        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
