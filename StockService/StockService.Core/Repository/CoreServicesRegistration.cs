using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StockService.Core.Repository.BranchCategories;
using StockService.Core.Repository.Categories;
using StockService.Core.Repository.CounterPartyTypes;
using StockService.Core.Repository.Denominations;
using StockService.Core.Repository.DenominationTypes;
using StockService.Core.Repository.StockLedgers;
using StockService.Core.Repository.Auditable;
using StockService.Core.Repository.SubCategories;

namespace StockService.Core;

public static class CoreServicesRegistration
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddScoped<IBranchCategoryRepository, BranchCategoryRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICounterPartyTypeRepository, CounterPartyTypeRepository>();
        services.AddScoped<IStockLedgerRepository, StockLedgerRepository>();
        services.AddScoped<IDenominationRepository, DenominationRepository>();
        services.AddScoped<IDenominationTypeRepository, DenominationTypeRepository>();
        services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
        // Register HttpContextAccessor if not already registered

        return services;
    }

    // Add the new extension method in the same static class
    public static IServiceCollection AddDbContextWithAuditing<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction = null)
        where TContext : DbContext
    {
        return services.AddDbContext<TContext>((sp, options) =>
        {
            optionsAction?.Invoke(options);

            // Add the interceptor
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            options.AddInterceptors(new AuditSaveChangesInterceptor(httpContextAccessor));
        });
    }
}