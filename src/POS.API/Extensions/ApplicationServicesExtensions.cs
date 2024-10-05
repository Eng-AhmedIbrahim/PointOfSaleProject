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
}
