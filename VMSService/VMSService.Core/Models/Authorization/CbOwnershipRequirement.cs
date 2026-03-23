using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using VMSService.Core.Models.CbRequests;

namespace VMSService.Core.Authorization;
    /// <summary>
    /// Ensures a commercial bank user can only access withdrawal requests that belong to their own company.
    ///
    /// Bank users bypass this check entirely — they can see all requests.
    ///
    /// Usage in controller:
    ///   var result = await _authorizationService
    ///       .AuthorizeAsync(User, request, CbOwnershipRequirement.Instance);
    ///   if (!result.Succeeded) return Forbid();
    /// </summary>
    public class CbOwnershipRequirement : IAuthorizationRequirement
    {
        public static readonly CbOwnershipRequirement Instance = new();
    }
