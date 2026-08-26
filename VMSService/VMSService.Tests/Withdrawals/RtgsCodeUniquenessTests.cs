using System.Security.Claims;
using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using VMSService.Core.Authorization;
using VMSService.Core.Models.CbWithdrawRequest;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.AppliedPenalties;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.PublicHolidays;
using VMSService.Core.Repositories.WithdrawalRequestAgents;
using VMSService.Core.Repositories.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalTransactionRefNumberGenerators;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Controllers;
using WorkflowService.Client;
using Xunit;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Issue 12: two live requests can't share an RTGS code. The filtered unique index
/// (UQ_WithdrawalRequest_RtgsCode_Live) is the real database-level guarantee — these tests cover
/// the app-level layer on top of it: <c>ExistsByRtgsCodeAsync</c>'s friendly pre-check in
/// <c>SaveDraft</c>/<c>UpdateRtgs</c>, and both actions turning a raced-past-the-precheck
/// <see cref="DbUpdateException"/> into the same friendly message instead of a raw 500.
/// </summary>
public class RtgsCodeUniquenessTests
{
    private static DbUpdateException RtgsCodeConstraintViolation()
        => new("Saving changes failed.", FakeSqlException(2601,
            "Cannot insert duplicate key row in object 'dbo.WithdrawalRequests' with unique index 'UQ_WithdrawalRequest_RtgsCode_Live'."));

    // SqlException has no public constructor — this reflection-built instance mirrors what EF
    // Core's SqlServer provider actually throws, so IsRtgsCodeUniqueViolation's SqlException.Number
    // check (added instead of message-text-only matching, per code review: SQL Server's message
    // wording/locale can vary, but the error number is stable) can be exercised for real here.
    private static SqlException FakeSqlException(int number, string message)
    {
        var errorCollection = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;

        var errorCtor = typeof(SqlError).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance, null,
            [typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string), typeof(string), typeof(int), typeof(int), typeof(Exception)],
            null)!;
        var error = errorCtor.Invoke([number, (byte)0, (byte)0, "test-server", message, "test-procedure", 0, 0, null]);

        typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(errorCollection, [error]);

        var exceptionCtor = typeof(SqlException).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance, null,
            [typeof(string), typeof(SqlErrorCollection), typeof(Exception), typeof(Guid)],
            null)!;
        return (SqlException)exceptionCtor.Invoke([message, errorCollection, null!, Guid.NewGuid()]);
    }

    private static WithdrawalRequest NewDraftRequest(string statusName) => new()
    {
        Id = Guid.NewGuid(),
        CbId = 1,
        CbCode = "TB",
        StatusTypeId = 1,
        StatusTypeName = statusName,
        CollectionDate = DateOnly.FromDateTime(DateTime.Now.AddDays(5)),
        BouBranchId = 1,
        BouBranchCode = "HO",
        IsLumpSum = true,
        TotalAmount = 10_000_000m,
    };

    private static SaveWithdrawalRequestViewModel ValidSaveDraftVm(WithdrawalRequest request) => new()
    {
        Id = request.Id,
        CbCode = request.CbCode,
        BouBranchId = request.BouBranchId,
        BouBranchCode = request.BouBranchCode,
        CollectionDate = request.CollectionDate,
        IsLumpSum = true,
        LumpSumAmount = 10_000_000m,
    };

    private static (CbWithdrawRequestsController Controller, WithdrawalRequest Request,
        Mock<IWithdrawalRequestRepository> RequestRepo, Mock<IWithdrawalUnitOfWork> Uow) BuildController(WithdrawalRequest request)
    {
        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.FindById(request.Id)).ReturnsAsync(request);

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests.FindById(request.Id)).ReturnsAsync(request);
        uow.Setup(u => u.StatusHistory.GetByRequestAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        uow.Setup(u => u.Denominations).Returns(Mock.Of<IWithdrawalRequestDenominationRepository>());
        uow.Setup(u => u.WithdrawalAgents).Returns(Mock.Of<IWithdrawalRequestAgentRepository>());

        var txnRefGenerator = new Mock<IWithdrawalTransactionRefNumberGenerator>();
        txnRefGenerator
            .Setup(g => g.GenerateAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("REF-TEST");

        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService
            .Setup(s => s.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        var controller = new CbWithdrawRequestsController(
            authorizationService.Object,
            Mock.Of<IAgentDetailRepository>(),
            Mock.Of<IConfigurationsServiceClient>(),
            Mock.Of<IDenominationRepository>(),
            txnRefGenerator.Object,
            uow.Object,
            requestRepo.Object,
            Mock.Of<IWithdrawalRequestAgentRepository>(),
            Mock.Of<IWithdrawalRequestDenominationRepository>(),
            Mock.Of<IWorkflowServiceClient>(),
            Mock.Of<IWorkflowService>(),
            Mock.Of<IWithdrawalStatusResolver>(),
            Mock.Of<IAttachmentRepository>(),
            Mock.Of<IPublicHolidayRepository>(),
            Mock.Of<IAppliedPenaltyRepository>(),
            Mock.Of<ILogger<CbWithdrawRequestsController>>(),
            Mock.Of<IMapper>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.Name, "cb-maker"),
                        new Claim(ClaimTypes.NameIdentifier, "cb-maker-id"),
                        new Claim(RolePolicy.CbIdClaim, request.CbId.ToString()),
                        new Claim(SystemPolicy.ClaimType.AppRole, $"{SystemPolicy.SystemSettings.AppAcronym}:{RolePolicy.CbMaker}"),
                    ])),
                },
            },
            Url = Mock.Of<IUrlHelper>(),
        };
        controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

        return (controller, request, requestRepo, uow);
    }

    // ── SaveDraft ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveDraft_Returns_BadRequest_When_RtgsCode_Already_Used_By_Another_Live_Request()
    {
        var (controller, request, requestRepo, uow) = BuildController(NewDraftRequest(WithdrawalStatusNames.Draft));
        var vm = ValidSaveDraftVm(request);
        vm.RtgsCode = "DUP-CODE";

        requestRepo.Setup(r => r.ExistsByRtgsCodeAsync("DUP-CODE", request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.SaveDraft(vm, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        uow.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveDraft_Proceeds_When_RtgsCode_Is_Unique()
    {
        var (controller, request, requestRepo, _) = BuildController(NewDraftRequest(WithdrawalStatusNames.Draft));
        var vm = ValidSaveDraftVm(request);
        vm.RtgsCode = "FRESH-CODE";

        requestRepo.Setup(r => r.ExistsByRtgsCodeAsync("FRESH-CODE", request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await controller.SaveDraft(vm, CancellationToken.None);

        Assert.IsNotType<BadRequestObjectResult>(result);
        Assert.Equal("FRESH-CODE", request.RtgsCode);
    }

    [Fact]
    public async Task SaveDraft_Treats_Blank_RtgsCode_As_Null_And_Skips_The_Uniqueness_Check()
    {
        var (controller, request, requestRepo, _) = BuildController(NewDraftRequest(WithdrawalStatusNames.Draft));
        var vm = ValidSaveDraftVm(request);
        vm.RtgsCode = "   ";

        var result = await controller.SaveDraft(vm, CancellationToken.None);

        Assert.IsNotType<BadRequestObjectResult>(result);
        Assert.Null(request.RtgsCode);
        requestRepo.Verify(
            r => r.ExistsByRtgsCodeAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveDraft_Converts_A_Raced_Unique_Constraint_Violation_Into_A_Friendly_BadRequest()
    {
        // The pre-check passed (no conflict seen), but another request landed first and the
        // database's filtered unique index rejects the save — this must not surface as a raw 500.
        var (controller, request, requestRepo, uow) = BuildController(NewDraftRequest(WithdrawalStatusNames.Draft));
        var vm = ValidSaveDraftVm(request);
        vm.RtgsCode = "RACE-CODE";

        requestRepo.Setup(r => r.ExistsByRtgsCodeAsync("RACE-CODE", request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(RtgsCodeConstraintViolation());

        var result = await controller.SaveDraft(vm, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ── UpdateRtgs ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRtgs_Sets_TempData_Error_And_Redirects_When_RtgsCode_Already_Used()
    {
        var (controller, request, _, uow) = BuildController(NewDraftRequest(WithdrawalStatusNames.PendingInternalApproval));

        uow.Setup(u => u.Requests.ExistsByRtgsCodeAsync("DUP-CODE", request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.UpdateRtgs(request.Id, null, "DUP-CODE", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("RTGS code 'DUP-CODE' is already in use by another request.", controller.TempData["Error"]);
        Assert.Null(request.RtgsCode); // rejected before mutation
        uow.Verify(u => u.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRtgs_Converts_A_Raced_Unique_Constraint_Violation_Into_A_Friendly_TempData_Error()
    {
        var (controller, request, _, uow) = BuildController(NewDraftRequest(WithdrawalStatusNames.PendingInternalApproval));

        uow.Setup(u => u.Requests.ExistsByRtgsCodeAsync("RACE-CODE", request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(RtgsCodeConstraintViolation());

        var result = await controller.UpdateRtgs(request.Id, null, "RACE-CODE", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("RTGS code 'RACE-CODE' is already in use by another request.", controller.TempData["Error"]);
    }
}
