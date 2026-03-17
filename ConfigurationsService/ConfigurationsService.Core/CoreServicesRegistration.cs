using ConfigurationsService.Core.Repositories.Branches;
using Microsoft.Extensions.DependencyInjection;
using VMSService.Core.Repositories.Branches;

namespace ConfigurationsService.Core;

public static class CoreServicesRegistration
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
	{ 
        services.AddScoped<IBranchRepository, BranchRepository>();

        return services;
    }
}

