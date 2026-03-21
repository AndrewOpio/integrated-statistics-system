using ConfigurationsService.Core.Repository.Common;
using ConfigurationsService.Data.Entities;
namespace ConfigurationsService.Core.Repositories.Districts
{
    public interface IDistrictRepository : IRepositoryBase<District, int>
    {
        Task<District> FindByName(string districtName);
    }
}