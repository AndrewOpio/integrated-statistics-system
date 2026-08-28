using System.Security.Claims;
using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
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
using VMSService.Core.Services.Identity;
using VMSService.Core.Services.Workflows;
using VMSService.Core.Static;
using VMSService.Data.Entities;
using VMSService.Web.Controllers;
using WorkflowService.Client;
using WorkflowService.Client.Dto;
using Xunit;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Fixing ExecuteAction to reject an unflagged same-day submission (see
/// CbWithdrawRequestsControllerSameDayExceptionalSubmitTests) left the Submit button itself still
/// visible — clicking it just bounced back with the rejection error instead of a failed round
/// trip being avoided. This covers hiding SubmitForInternalApproval/ResubmitForApproval from
/// Details' AvailableTransitions in exactly the same condition ExecuteAction rejects on, so the
/// Maker is routed to Edit instead.
/// </summary>
public class CbWithdrawRequestsControllerSameDayExceptionalTransitionVisibilityTests
{
    private const int RoleId = 1;

    private static WithdrawalRequest NewRequest(
        DateOnly collectionDate, string statusName, bool requiresSpecialApproval, string? reason)
        => new()
        {
            Id = Guid.NewGuid(),
            CbId = 1,
            StatusTypeId = 3,
            StatusTypeName = statusName,
            CollectionDate = collectionDate,
            RequiresSpecialApproval = requiresSpecialApproval,
            SpecialApprovalReason = reason,
            TransactionRefNumber = null, // skips the applied-penalties branch, irrelevant here
            TotalAmount = 10_000_000m,
        };

    private static CbWithdrawRequestsController BuildController(
        WithdrawalRequest request, List<WorkflowTransitionDto> transitions)
    {
        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.GetWithDetailsAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests).Returns(requestRepo.Object);

        var workflowServiceClient = new Mock<IWorkflowServiceClient>();
        workflowServiceClient
            .Setup(c => c.FindStatusById(It.IsAny<int>()))
            .ReturnsAsync((int id) => new StatusTypeDto { Id = id, Name = request.StatusTypeName });
        workflowServiceClient
            .Setup(c => c.GetAvailableTransitions(It.IsAny<GetAvailableTransitionsRequestDto>()))
            .ReturnsAsync(transitions);

        var configServiceClient = new Mock<IConfigurationsServiceClient>();
        configServiceClient.Setup(c => c.GetCbIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new CommercialBankDto { Id = 1, Name = "Test Bank", Code = "TB" });
        configServiceClient.Setup(c => c.GetBouBranchByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new BranchDto { Id = 1, Name = "Head Office", BranchCode = "HO" });

        var attachmentRepository = new Mock<IAttachmentRepository>();
        attachmentRepository.Setup(r => r.FindByEntityId(It.IsAny<string>())).ReturnsAsync(new List<Attachment>());

        var publicHolidayRepository = new Mock<IPublicHolidayRepository>();
        publicHolidayRepository.Setup(r => r.FindByDateAsync(It.IsAny<DateOnly>())).ReturnsAsync((PublicHoliday?)null);

        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService
            .Setup(s => s.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        var controller = new CbWithdrawRequestsController(
            authorizationService.Object,
            Mock.Of<IAgentDetailRepository>(),
            configServiceClient.Object,
            Mock.Of<IDenominationRepository>(),
            Mock.Of<IWithdrawalTransactionRefNumberGenerator>(),
            uow.Object,
            Mock.Of<IWithdrawalRequestRepository>(),
            Mock.Of<IWithdrawalRequestAgentRepository>(),
            Mock.Of<IWithdrawalRequestDenominationRepository>(),
            workflowServiceClient.Object,
            Mock.Of<IWorkflowService>(),
            Mock.Of<IWithdrawalStatusResolver>(),
            attachmentRepository.Object,
            publicHolidayRepository.Object,
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
                        new Claim("appRole", $"{SystemPolicy.SystemSettings.AppAcronym}:{RolePolicy.CbMaker}"),
                        new Claim("appRoleId", $"{SystemPolicy.SystemSettings.AppAcronym}:{RoleId}"),
                        new Claim("cbId", "1"),
                    ])),
                },
            },
        };

        return controller;
    }

    private static List<WorkflowTransitionDto> SubmitTransition(string actionName) =>
        [new WorkflowTransitionDto { ActionName = actionName, DisplayLabel = "Submit for Approval" }];

    [Fact]
    public async Task Unflagged_Same_Day_Draft_Hides_SubmitForInternalApproval()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = NewRequest(today, WithdrawalStatusNames.Draft, requiresSpecialApproval: false, reason: null);
        var controller = BuildController(request, SubmitTransition(WithdrawalStatusActionNames.SubmitForInternalApproval));

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<WithdrawalRequestDetailsViewModel>(view.Model);

        // Same-day-ness itself is time-of-day dependent (see the ExecuteAction tests) — but the
        // presence/absence of the transition must always match whether the button would actually
        // be rejected, which is exactly ShowSameDayMakerWarning's own condition.
        if (vm.ShowSameDayMakerWarning && !request.RequiresSpecialApproval)
            Assert.DoesNotContain(vm.AvailableTransitions, t => t.ActionName == WithdrawalStatusActionNames.SubmitForInternalApproval);
        else
            Assert.Contains(vm.AvailableTransitions, t => t.ActionName == WithdrawalStatusActionNames.SubmitForInternalApproval);
    }

    [Fact]
    public async Task Properly_Flagged_Same_Day_Draft_Still_Shows_SubmitForInternalApproval()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = NewRequest(today, WithdrawalStatusNames.Draft, requiresSpecialApproval: true, reason: "Urgent same-day withdrawal");
        var controller = BuildController(request, SubmitTransition(WithdrawalStatusActionNames.SubmitForInternalApproval));

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<WithdrawalRequestDetailsViewModel>(view.Model);

        Assert.Contains(vm.AvailableTransitions, t => t.ActionName == WithdrawalStatusActionNames.SubmitForInternalApproval);
    }

    [Fact]
    public async Task Future_Dated_Draft_Always_Shows_SubmitForInternalApproval()
    {
        // Fully deterministic regardless of real time — a future collection date is never
        // same-day, so the hide condition can never fire here.
        var future = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
        var request = NewRequest(future, WithdrawalStatusNames.Draft, requiresSpecialApproval: false, reason: null);
        var controller = BuildController(request, SubmitTransition(WithdrawalStatusActionNames.SubmitForInternalApproval));

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<WithdrawalRequestDetailsViewModel>(view.Model);

        Assert.Contains(vm.AvailableTransitions, t => t.ActionName == WithdrawalStatusActionNames.SubmitForInternalApproval);
        Assert.False(vm.ShowSameDayMakerWarning);
    }

    [Fact]
    public async Task Unflagged_Same_Day_ReturnedForRevision_Hides_ResubmitForApproval()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var request = NewRequest(today, WithdrawalStatusNames.ReturnedForRevision, requiresSpecialApproval: false, reason: null);
        var controller = BuildController(request, SubmitTransition(WithdrawalStatusActionNames.ResubmitForApproval));

        var view = Assert.IsType<ViewResult>(await controller.Details(request.Id, CancellationToken.None));
        var vm = Assert.IsType<WithdrawalRequestDetailsViewModel>(view.Model);

        if (vm.ShowSameDayMakerWarning && !request.RequiresSpecialApproval)
            Assert.DoesNotContain(vm.AvailableTransitions, t => t.ActionName == WithdrawalStatusActionNames.ResubmitForApproval);
        else
            Assert.Contains(vm.AvailableTransitions, t => t.ActionName == WithdrawalStatusActionNames.ResubmitForApproval);
    }
}
