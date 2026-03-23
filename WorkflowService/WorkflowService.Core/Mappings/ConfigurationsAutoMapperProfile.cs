using AutoMapper;
using WorkflowService.Core.Models.EntityTypes;
using WorkflowService.Core.Models.StatusTypes;
using WorkflowService.Core.Models.WorkflowTransitions;
using WorkflowService.Data.Entities;

namespace WorkflowService.Core.Mappings
{
    public class ConfigurationsAutoMapperProfile : Profile
    {
        public ConfigurationsAutoMapperProfile()
        {
            CreateMap<StatusType, StatusTypeViewModel>().ReverseMap();
            CreateMap<EntityType, EntityTypeViewModel>().ReverseMap();
            CreateMap<WorkflowTransition, WorkflowTransitionViewModel>().ReverseMap();
        }
    }
}
