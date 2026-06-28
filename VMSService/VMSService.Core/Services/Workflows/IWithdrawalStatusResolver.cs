namespace VMSService.Core.Services.Workflows
{
    public interface IWithdrawalStatusResolver
    {
        Task<int> GetStatusIdAsync(string statusName);
    }
}
