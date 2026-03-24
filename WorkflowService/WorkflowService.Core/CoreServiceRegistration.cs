using Microsoft.Extensions.DependencyInjection;
using WorkflowService.Core.Repositories.StatusTypes;
using WorkflowService.Core.Repositories.EntityTypes;
using WorkflowService.Core.Repositories.WorkflowTransitions;
using WorkflowService.Core.Services.Identity;

namespace WorkflowService.Core
{
    public static class CoreServicesRegistration
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            services.AddScoped<IIdentityService, IdentityServices>();
            services.AddScoped<IEntityTypeRepository, EntityTypeRepository>();
            services.AddScoped<IStatusTypeRepository, StatusTypeRepository>();
            services.AddScoped<IWorkflowTransitionRepository, WorkflowTransitionRepository>();

            return services;
        }
    }
}