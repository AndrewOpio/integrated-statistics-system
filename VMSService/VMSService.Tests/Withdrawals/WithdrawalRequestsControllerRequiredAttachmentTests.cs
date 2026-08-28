using System.Security.Claims;
using AutoMapper;
using ConfigurationsService.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.AppliedPenalties;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.Categories;
using VMSService.Core.Repositories.CounterPartyTypes;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.PenaltyWaivers;
using VMSService.Core.Repositories.Penalties;
using VMSService.Core.Repositories.PublicHolidays;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Repositories.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Workflows;
using VMSService.Data.Entities;
using VMSService.Web.Controllers;
using WorkflowService.Client;
using WorkflowService.Client.Dto;
using Xunit;
using static VMSService.Core.Static.SystemPolicy;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Deposits' ExecuteAction already rejects a transition server-side when a required attachment
/// wasn't actually uploaded — Withdrawals' ExecuteAction uploaded whatever was given but never
/// checked a required one was present, so a bypassed/failed client-side check (JS disabled, direct
/// POST) let the transition through with zero attachments. This covers the ported fix: a missing
/// required attachment now blocks the transition with an error instead of silently proceeding.
/// </summary>
public class WithdrawalRequestsControllerRequiredAttachmentTests
{
    private static WithdrawalRequest NewRequest()
        => new()
        {
            Id = Guid.NewGuid(),
            StatusTypeId = 50,
            StatusTypeName = WithdrawalStatusNames.PendingApproval,
            RtgsCode = "RTGS-TEST",
            TotalAmount = 10_000_000m,
            CreatedDate = DateTime.Now,
        };

    private static WorkflowTransitionDto ApproveTransition(bool requiresAttachments, bool isRequired)
        => new()
        {
            Id = 1094,
            ActionName = WithdrawalStatusActionNames.Approve,
            ToStatusId = 51,
            RequiresAttachments = requiresAttachments,
            AttachmentRequirements = requiresAttachments
                ?
                [
                    new TransitionAttachmentRequirementDto
                    {
                        Id = 3,
                        AttachmentName = "Withdrawal Request Slip",
                        AllowedFormats = "pdf",
                        IsRequired = isRequired,
                    },
                ]
                : [],
        };

    private static (WithdrawalRequestsController Controller, Mock<IWorkflowService> WorkflowService) BuildController(
        WithdrawalRequest request, WorkflowTransitionDto transition)
    {
        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.FindById(request.Id)).ReturnsAsync(request);
        requestRepo.Setup(r => r.GetWithDetailsAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        requestRepo.Setup(r => r.Update(It.IsAny<WithdrawalRequest>())).ReturnsAsync(true);

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests).Returns(requestRepo.Object);
        uow.Setup(u => u.Denominations).Returns(Mock.Of<IWithdrawalRequestDenominationRepository>());
        uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var workflowServiceClient = new Mock<IWorkflowServiceClient>();
        workflowServiceClient
            .Setup(c => c.FindByTransitionKey(It.IsAny<GetTransitionByKeyRequestDto>()))
            .ReturnsAsync(transition);

        var workflowService = new Mock<IWorkflowService>();
        workflowService
            .Setup(w => w.ExecuteTransition(
                It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()))
            .ReturnsAsync(true);

        // HandleAttachmentUploadsAsync reads Request.Form.Files — a bare DefaultHttpContext with no
        // Content-Type throws synchronously on that getter, so it needs an explicit (empty) form
        // primed up front, same as a real multipart POST with no files attached would produce.
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimType.AppRole, $"{SystemSettings.AppAcronym}:{Roles.PanelA}")])),
        };
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

        var controller = new WithdrawalRequestsController(
            requestRepo.Object,
            workflowServiceClient.Object,
            workflowService.Object,
            Mock.Of<IAgentDetailRepository>(),
            Mock.Of<IConfigurationsServiceClient>(),
            Mock.Of<IDenominationRepository>(),
            Mock.Of<IWithdrawalRequestDenominationRepository>(),
            Mock.Of<IAttachmentRepository>(),
            uow.Object,
            Mock.Of<IWebHostEnvironment>(),
            Mock.Of<IStockMovementRepository>(),
            Mock.Of<IStockLedgerRepository>(),
            Mock.Of<ICategoryRepository>(),
            Mock.Of<ICounterPartyTypeRepository>(),
            Mock.Of<IPenaltyRepository>(),
            Mock.Of<IAppliedPenaltyRepository>(),
            Mock.Of<IPenaltyWaiverRepository>(),
            Mock.Of<ILogger<WithdrawalRequestsController>>(),
            Mock.Of<IMapper>(),
            Mock.Of<IPublicHolidayRepository>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
            },
        };
        controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

        return (controller, workflowService);
    }

    [Fact]
    public async Task Approve_Missing_Required_Attachment_Is_Rejected_Without_Executing_Transition()
    {
        var request = NewRequest();
        var (controller, workflowService) = BuildController(
            request, ApproveTransition(requiresAttachments: true, isRequired: true));

        // No files added to Request.Form — simulates a bypassed client-side check.
        var result = await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.Approve, "approved", CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(WithdrawalRequestsController.Details), redirect.ActionName);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.Contains("Withdrawal Request Slip", (string)controller.TempData["Error"]!);
        workflowService.Verify(w => w.ExecuteTransition(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()), Times.Never);
    }

    [Fact]
    public async Task Approve_With_No_Attachment_Requirements_Configured_Executes_Normally()
    {
        var request = NewRequest();
        var (controller, workflowService) = BuildController(
            request, ApproveTransition(requiresAttachments: false, isRequired: false));

        var result = await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.Approve, "approved", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(controller.TempData["Error"]);
        workflowService.Verify(w => w.ExecuteTransition(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()), Times.Once);
    }

    [Fact]
    public async Task Approve_With_Optional_Attachment_Requirement_Executes_Normally_When_Missing()
    {
        var request = NewRequest();
        var (controller, workflowService) = BuildController(
            request, ApproveTransition(requiresAttachments: true, isRequired: false));

        var result = await controller.ExecuteAction(request.Id, WithdrawalStatusActionNames.Approve, "approved", CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(controller.TempData["Error"]);
        workflowService.Verify(w => w.ExecuteTransition(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>(), It.IsAny<List<long>?>()), Times.Once);
    }
}
