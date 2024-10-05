namespace POS.API.Extensions;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {

        services.AddAutoMapper(typeof(MappingProfiles));

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = (actionContext) =>
            {
                var errors = actionContext.ModelState.Where(e => e.Value?.Errors.Count > 0)
                                    .SelectMany(e => e.Value?.Errors ?? [])
                                    .Select(e => e.ErrorMessage)
                                    .ToArray();

                var apiValidationResponse = new ApiValidationErrorResponse()
                {
                    Errors = errors
                };
                return new BadRequestObjectResult(apiValidationResponse);
            };

        });

        return services;
    }

    public static IServiceCollection AddFlexibleCaching(
        this IServiceCollection services, string? redisConnectionString)
    {
        services.AddSingleton(typeof(IFlexibleCacheService<>), typeof(FlexibleCacheService<>));

        if (string.IsNullOrEmpty(redisConnectionString))
        {
            Log.Warning("Redis connection string is null or empty. Falling back to in-memory caching.");
            services.AddDistributedMemoryCache();
            return services;
        }

        try
        {
            var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
            services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
            });
            Log.Information("Successfully connected to Redis.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to connect to Redis. Falling back to in-memory cache.");
            services.AddDistributedMemoryCache();
        }

        return services;
    }
}
