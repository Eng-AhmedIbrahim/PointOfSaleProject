using Pos.Repository.Identity;

namespace POS.API.Extensions;

public static class IdentityServicesExtensions
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddIdentity<AppUser, IdentityRole>(options =>
        {
            // Password settings (Uncomment if needed)
            //options.Password.RequireNonAlphanumeric = true;
            //options.Password.RequireUppercase = true;
            //options.Password.RequireLowercase = true;
        })
        .AddRoles<IdentityRole>() // Register Role Management
        .AddEntityFrameworkStores<AppIdentityDbContext>()
        .AddDefaultTokenProviders();


        //services.AddAuthentication(options =>
        //{
        //    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        //    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        //})
        //    .AddJwtBearer(options =>
        //    {
        //        var secretKey = Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"] ?? string.Empty);
        //        var requiredKeyLength = 256 / 8; // 256 bits
        //        if (secretKey.Length < requiredKeyLength)
        //        {
        //            // Pad the key to meet the required length
        //            Array.Resize(ref secretKey, requiredKeyLength);
        //        }

        //        // Configure authentication handler
        //        options.TokenValidationParameters = new TokenValidationParameters()
        //        {
        //            ValidateAudience = true,
        //            ValidAudience = configuration["JWT:ValidAudience"],
        //            ValidateIssuer = true,
        //            ValidIssuer = configuration["JWT:ValidIssuer"],
        //            ValidateIssuerSigningKey = true,
        //            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        //            ValidateLifetime = true,
        //            ClockSkew = TimeSpan.FromDays(double.Parse(configuration["JWT:DurationInDays"] ?? string.Empty))
        //        };
        //    });
        return services;
    }
}