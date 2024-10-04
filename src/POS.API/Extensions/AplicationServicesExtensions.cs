using POS.API.Helpers;

namespace POS.API.Extensions;

public static class AplicationServicesExtensions
{
    public static IServiceCollection AddAplicationServices(this IServiceCollection services)
    {

        services.AddAutoMapper(a=>a.AddProfile(new MappingProfiles()));

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = (actionContext) =>
            {
                var errors = actionContext.ModelState.Where(e=>e.Value?.Errors.Count()> 0)
                                    .SelectMany(e=>e.Value?.Errors??new())
                                    .Select(e=>e.ErrorMessage)
                                    .ToArray();

                var apiValidationResponse =  new ApiValidationErrorResponse() 
                {
                    Errors = errors 
                };
                return new BadRequestObjectResult(apiValidationResponse);
            };

        });

        return services;
    }
}
