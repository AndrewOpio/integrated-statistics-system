
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigurationsService.Client
{
    public static class ConfigurationsServiceClientExtensions
    {
        public static IServiceCollection AddConfigurationsServiceClient(
            this IServiceCollection services, string baseurl)
        {
            services.AddHttpClient<IConfigurationsServiceClient, ConfigurationsServiceClient>(client =>
            {
                client.BaseAddress = new Uri(baseurl);
            });
            return services;
        }
    }
}
