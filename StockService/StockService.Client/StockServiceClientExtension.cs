using Microsoft.Extensions.DependencyInjection;

namespace StockService.Client
{
    public static class StockServiceClientExtension
    {
        public static IServiceCollection AddStockServiceClient(
            this IServiceCollection services, string baseurl)
        {
            services.AddHttpClient<IStockServiceClient, StockServiceClient>(client =>
            {
                client.BaseAddress = new Uri(baseurl);
            });
            return services;
        }
    }
}
