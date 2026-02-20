using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VMSService.Data.Configurations;

namespace VMSService.Data
{
    public static class DataServicesRegistration
    {
        public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            services.AddDbContext<VMSServiceDbContext>(options => options.UseSqlServer(connectionString));
            //services.AddDatabaseDeveloperPageExceptionFilter();

            return services;
        }
    }
}
