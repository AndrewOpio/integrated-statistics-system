using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Authorization;

namespace VMSService.Core.Models.CbRequests;
public class WithdrawalStepViewModelFactory
{

    private static readonly (string Key, string Icon, string Label, string SubLabel)[] CbSteps =
    [
        ("CbMaker",     "ti-user-plus",     "Initiator",   "Initiate request"),
        ("CbApprover",  "ti-building-bank", "Approver",    "Approve withdrawal"),
        ("BOU",        "ti-clock-hour-4",  "BOU Handling",   "For processing"),
    ];

    private static readonly (string Key, string Icon, string Label, string SubLabel)[] BouSteps =
    [
        ("Cb",          "ti-building",             "Commercial Bank",  "Commercial Bank Request"),
        ("Remittance",  "ti-adjustments",          "Remittance",       "Validate Checks"),
        ("PanelA",      "ti-shield-check",         "Panel A",          "Review and Adjust"),
        ("PanelB",      "ti-arrow-right-circle",   "Panel B",          "Collect and Approve"),
    ];


    // Steps visible to commercial bank users (requestor + supervisor roles)
    private static readonly HashSet<string> CompanyVisibleSteps = ["requestor", "supervisor"];

    public static WithdrawalStepViewModel Build(
        string currentStepKey,
        bool isRequestApproved,
        ClaimsPrincipal user)
    {
        var userRole = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? string.Empty;
        var isBouUser = RolePolicy.BouRoles.Contains(userRole);

        var activeSteps = isBouUser ? BouSteps : CbSteps;
        var currentIndex = Array.FindIndex(activeSteps, s => s.Key == currentStepKey);

        var steps = activeSteps.Select((s, i) => new WithdrawalStep
        {
            Key = s.Key,
            Icon = s.Icon,
            Label = s.Label,
            SubLabel = s.SubLabel,
            State = i < currentIndex ? StepState.Done
                            : i == currentIndex ? StepState.Active
                            : StepState.InProcess,
            IsVisible = true,
            IsConnectorDone = i > 0 && (i - 1) < currentIndex,
        }).ToList();

        var isInProgress = !isBouUser && currentStepKey == "bou" && !isRequestApproved;
        var isApproved = !isBouUser && isRequestApproved;

        return new WithdrawalStepViewModel
        {
            Steps = steps,
            IsInProgress = isInProgress,
            IsApproved = isApproved,
            ShowBouPill = false,
        };
    }
}
