using Microsoft.Extensions.DependencyInjection;

namespace StockService.Client
{
    public static class StockServiceClientExtension
    {
        /// <summary>
        /// Registers IStockServiceClient against the given Stock API base URL. Every request is
        /// authenticated automatically via client-credentials (see StockServiceAuthHandler) —
        /// callers never need to handle tokens themselves.
        /// </summary>
        public static IServiceCollection AddStockServiceClient(
            this IServiceCollection services, string baseurl, StockServiceAuthOptions authOptions)
        {
            services.AddSingleton(authOptions);

            services.AddHttpClient("StockServiceTokenClient", client =>
            {
                client.BaseAddress = new Uri(baseurl);
            });

            services.AddTransient<StockServiceAuthHandler>();

            services.AddHttpClient<IStockServiceClient, StockServiceClient>(client =>
            {
                client.BaseAddress = new Uri(baseurl);
            }).AddHttpMessageHandler<StockServiceAuthHandler>();

            return services;
        }
    }
}
