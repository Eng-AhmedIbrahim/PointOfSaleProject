namespace POS.API.Extensions;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork));
        services.AddScoped(typeof(ICompanyService), typeof(CompanyService));
        services.AddScoped(typeof(IBranchService), typeof(BranchService));
        services.AddScoped(typeof(ICategoryService), typeof(CategoryService));
        services.AddScoped(typeof(IMenuSalesItemService), typeof(MenuSalesItemService));
        services.AddScoped(typeof(IAttributeService), typeof(AttributeService));
        services.AddScoped(typeof(IUserSetting), typeof(UserSetting));
        services.AddScoped(typeof(IAuthService), typeof(AuthService));
        services.AddScoped(typeof(IOrderService), typeof(OrderService));

        services.AddAutoMapper(typeof(MappingProfiles));
        services.AddHttpClient();

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
        catch (RedisConnectionException)
        {
            Log.Warning("Failed to connect to Redis. Falling back to in-memory cache.");
            services.AddDistributedMemoryCache();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "cache error");
            services.AddDistributedMemoryCache();
        }

        return services;
    }
}
