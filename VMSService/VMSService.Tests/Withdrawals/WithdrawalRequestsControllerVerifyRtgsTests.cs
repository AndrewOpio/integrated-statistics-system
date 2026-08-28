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
using Xunit;
using static VMSService.Core.Static.SystemPolicy;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// VerifyRtgs had no status check at all — only role. Unlike every other mutating action on this
/// controller, a direct POST could overwrite RtgsVerificationStatus/RtgsVerifiedDate on a request
/// at any status, including one already fully processed. The guard added mirrors the exact
/// condition the "Verify RTGS" button itself is gated on
/// (BouWithdrawalRequestDetailsViewModel.CanVerifyRtgs): status is Pending RTGS Confirmation or
/// Recall Requested, verification isn't already past Pending, and an RTGS code is on file.
/// </summary>
public class WithdrawalRequestsControllerVerifyRtgsTests
{
    private static WithdrawalRequest NewRequest(
        string statusName, string? rtgsCode = "RTGS-TEST", string rtgsVerificationStatus = RtgsVerification.Pending)
        => new()
        {
            Id = Guid.NewGuid(),
            StatusTypeId = 10,
            StatusTypeName = statusName,
            RtgsCode = rtgsCode,
            RtgsVerificationStatus = rtgsVerificationStatus,
            CollectionDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            TotalAmount = 10_000_000m,
        };

    private static WithdrawalRequestsController BuildController(WithdrawalRequest request)
    {
        var requestRepo = new Mock<IWithdrawalRequestRepository>();
        requestRepo.Setup(r => r.FindById(request.Id)).ReturnsAsync(request);

        var uow = new Mock<IWithdrawalUnitOfWork>();
        uow.Setup(u => u.Requests).Returns(requestRepo.Object);
        uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var controller = new WithdrawalRequestsController(
            requestRepo.Object,
            Mock.Of<IWorkflowServiceClient>(),
            Mock.Of<IWorkflowService>(),
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
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimType.AppRole, $"{SystemSettings.AppAcronym}:{Roles.BackOffice}")])),
                },
            },
        };
        controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

        return controller;
    }

    [Fact]
    public async Task VerifyRtgs_On_Already_Approved_Request_Is_Rejected()
    {
        var request = NewRequest(WithdrawalStatusNames.Approved);
        var controller = BuildController(request);

        var result = await controller.VerifyRtgs(request.Id, RtgsVerification.Confirmed, null, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.Equal(RtgsVerification.Pending, request.RtgsVerificationStatus);
    }

    [Fact]
    public async Task VerifyRtgs_When_Already_Confirmed_Is_Rejected()
    {
        var request = NewRequest(WithdrawalStatusNames.PendingWithdrawalConfirmation, rtgsVerificationStatus: RtgsVerification.Confirmed);
        var controller = BuildController(request);

        var result = await controller.VerifyRtgs(request.Id, RtgsVerification.AmountMismatch, null, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.Equal(RtgsVerification.Confirmed, request.RtgsVerificationStatus);
    }

    [Fact]
    public async Task VerifyRtgs_With_No_RtgsCode_On_File_Is_Rejected()
    {
        var request = NewRequest(WithdrawalStatusNames.PendingWithdrawalConfirmation, rtgsCode: null);
        var controller = BuildController(request);

        var result = await controller.VerifyRtgs(request.Id, RtgsVerification.Confirmed, null, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.Equal(RtgsVerification.Pending, request.RtgsVerificationStatus);
    }

    [Fact]
    public async Task VerifyRtgs_On_PendingWithdrawalConfirmation_Succeeds()
    {
        var request = NewRequest(WithdrawalStatusNames.PendingWithdrawalConfirmation);
        var controller = BuildController(request);

        var result = await controller.VerifyRtgs(request.Id, RtgsVerification.Confirmed, null, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(RtgsVerification.Confirmed, request.RtgsVerificationStatus);
    }

    [Fact]
    public async Task VerifyRtgs_On_RecallRequested_Succeeds()
    {
        var request = NewRequest(WithdrawalStatusNames.RecallRequested);
        var controller = BuildController(request);

        var result = await controller.VerifyRtgs(request.Id, RtgsVerification.Confirmed, null, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(RtgsVerification.Confirmed, request.RtgsVerificationStatus);
    }
}
