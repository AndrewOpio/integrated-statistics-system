using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using VMSService.Core.Mappings;
using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Data.Entities;
using Xunit;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// EditWithdrawViewModel.Request used to be the raw WithdrawalRequest EF entity. It's now a
/// dedicated projection mapped through the real AutoMapper profile — this exercises that actual
/// profile (not a mock) so a renamed/removed source property fails here instead of silently
/// mapping to a null/default field on the view.
/// </summary>
public class WithdrawalRequestEditViewModelMappingTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<VMSServiceAutoMapperProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void Maps_All_Fields_Edit_Cshtml_Reads()
    {
        var request = new WithdrawalRequest
        {
            Id = Guid.NewGuid(),
            BouBranchId = 3,
            CreatedDate = new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc),
            FundSettlementValueDate = new DateOnly(2026, 1, 20),
            RtgsCode = "RTGS-2026-001",
            RequiresSpecialApproval = true,
            SpecialApprovalReason = "Same-day exceptional",
        };

        var vm = Mapper.Map<WithdrawalRequestEditViewModel>(request);

        Assert.Equal(request.Id, vm.Id);
        Assert.Equal(request.BouBranchId, vm.BouBranchId);
        Assert.Equal(request.CreatedDate, vm.CreatedDate);
        Assert.Equal(request.FundSettlementValueDate, vm.FundSettlementValueDate);
        Assert.Equal(request.RtgsCode, vm.RtgsCode);
        Assert.Equal(request.RequiresSpecialApproval, vm.RequiresSpecialApproval);
        Assert.Equal(request.SpecialApprovalReason, vm.SpecialApprovalReason);
    }
}
