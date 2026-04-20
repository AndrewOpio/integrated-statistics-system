namespace VMSService.Core.Repositories.DepositUnitOfWorks
{
    public interface IDepositTransactionRefNumberGenerator
    {
        Task<string> GenerateAsync(
        int bouBranchId,
        int cbId,
        DateOnly depositDate,
        CancellationToken ct = default);
    }
}
