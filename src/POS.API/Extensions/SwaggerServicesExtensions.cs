namespace POS.API.Extensions;

public static class SwaggerServicesExtensions
{
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }

    public static IApplicationBuilder UseSwaggerServices(this IApplicationBuilder app) 
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        return app;
    }
}
