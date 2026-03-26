using Microsoft.Extensions.DependencyInjection;

namespace WorkflowService.Client
{
    public static class WorkflowServiceClientExtensions
    {
        public static IServiceCollection AddWorkflowServiceClient(
            this IServiceCollection services, string baseurl)
        {
            services.AddHttpClient<IWorkflowServiceClient, WorkflowServiceClient>(client =>
            {
                client.BaseAddress = new Uri(baseurl);
            });
            return services;
        }
    }
}
