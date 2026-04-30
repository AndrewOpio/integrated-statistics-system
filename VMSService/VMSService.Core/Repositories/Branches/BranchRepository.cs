using ConfigurationsService.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VMSService.Core.Services.Configurations;

namespace VMSService.Core.Repositories.Branches
{
    public class BranchRepository(IConfigurationService _configService, ILogger<BranchRepository> _logger, 
        IHttpContextAccessor _httpContextAccessor) : IBranchRepository
    {
        public async Task<ICollection<BranchDto>> FindAll()
        {
            try
            {
                var item =  await _configService.GetAllBranches();
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all branches");
                return new List<BranchDto>();
            }
        }

        public async Task<BranchDto> FindById(int id)
        {
            try
            {
                var branch = await _configService.GetBranchById(id);
                if (branch == null)
                    return null;

                return branch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch with id {BranchId}", id);
                return null;
            }
        }

        public async Task<bool> IsExists(int id)
        {
            try
            {
                var branch = await FindById(id);
                return branch != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if branch exists with id {BranchId}", id);
                return false;
            }
        }

        public async Task<bool> Save()
        {
            return true;
        }

        public async Task<BranchDto> FindByName(string branchName)
        {
            return (await FindAll()).Where(b => b.Name == branchName).FirstOrDefault();
        }

        public Task<bool> Create(BranchDto entity)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Update(BranchDto entity)
        {
            throw new NotImplementedException();
        }
    }
}