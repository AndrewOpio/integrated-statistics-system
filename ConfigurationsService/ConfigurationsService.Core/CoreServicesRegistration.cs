using ConfigurationsService.Core.Repositories.Branches;
using ConfigurationsService.Core.Repositories.CommercialBankBranches;
using ConfigurationsService.Core.Repositories.CommercialBanks;
using ConfigurationsService.Core.Repositories.Districts;
using Microsoft.Extensions.DependencyInjection;
using VMSService.Core.Repositories.Branches;

namespace ConfigurationsService.Core;

public static class CoreServicesRegistration
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
	{ 
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<ICommercialBankRepository, CommercialBankRepository>();
        services.AddScoped<ICommercialBankBranchRepository, CommercialBankBranchRepository>();
        services.AddScoped<IDistrictRepository, DistrictRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();

        return services;
    }
}

