using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Authorization;

namespace VMSService.Web.Controllers;
    public class BaseWithdrawController(IAuthorizationService authorizationService) : Controller
    {
        protected readonly IAuthorizationService AuthorizationService = authorizationService;

        /// <summary>
        /// Returns false if the current user is a commercial bank user attempting to access a request that does not belong to their company.
        /// BOU users always pass this check.
        /// </summary>
        protected async Task<bool> CanAccessRequestAsync(Guid cbId)
        {
            var authContext = new WithdrawalRequestAuthContext { CbId = cbId };
            var result = await AuthorizationService
                .AuthorizeAsync(User, authContext, CbOwnershipRequirement.Instance);
            return result.Succeeded;
        }
    }