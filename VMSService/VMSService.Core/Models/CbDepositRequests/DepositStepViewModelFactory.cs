using System.Security.Claims;
using VMSService.Core.Authorization;
using VMSService.Core.Models.CbRequests;
using VMSService.Core.Static;

namespace VMSService.Core.Models.CbDepositRequests;
public class DepositStepViewModelFactory
{

    private static readonly (string Key, string Icon, string Label, string SubLabel)[] CbSteps =
    [
        ("CbMaker",     "ti-user-plus",     "Initiator",   "Initiate request"),
        ("CbApprover",  "ti-building-bank", "Approver",    "Approve deposit"),
        ("BOU",        "ti-clock-hour-4",  "BOU",   "Processing"),
    ];

    private static readonly (string Key, string Icon, string Label, string SubLabel)[] BouSteps =
    [
        ("cb",          "ti-building",             "Commercial Bank",          "Submit request"),
        ("PanelA",      "ti-shield-check",         "Panel A",          "Review and recommend"),
        ("PanelB",      "ti-arrow-right-circle",   "Panel B",          "Review and Approve"),
    ];

    private static readonly (string Key, string Icon, string Label, string SubLabel)[] BouStepsExceptional =
    [
        ("cb",          "ti-building",             "Commercial Bank",          "Submit request"),
        ("Remittance",  "ti-adjustments",          "Remittance",       "Exceptional Approval"),
        ("PanelA",      "ti-shield-check",         "Panel A",          "Review and Adjust"),
        ("PanelB",      "ti-arrow-right-circle",   "Panel B",          "Confirm and Approve"),
    ];


    // Steps visible to commercial bank users (requestor + supervisor roles)
    private static readonly HashSet<string> CompanyVisibleSteps = ["requestor", "supervisor"];

    public static DepositStepViewModel Build(
        string currentStepKey,
        bool isRequestApproved,
        bool isSpecialApprovalRequired,
        ClaimsPrincipal user)
    {
        var userRole = user.FindAll("appRole").Where(c => c.Value.StartsWith($"{SystemPolicy.SystemSettings.AppAcronym}:")).Select(c => c.Value.Split(":")[1]).ToList();
        var isBouUser = false;
        foreach (var role in userRole)
        {
            if (RolePolicy.BouRoles.Contains(role))
            {
                isBouUser = true;
                break;
            }
        }

        var activeSteps = isBouUser ? BouSteps : CbSteps;
        if (isBouUser && isSpecialApprovalRequired) activeSteps = BouStepsExceptional;
        var currentIndex = Array.FindIndex(activeSteps, s => s.Key == currentStepKey);

        var steps = activeSteps.Select((s, i) => new DepositStep
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

        var isInProgress = !isBouUser && currentStepKey == "bank" && !isRequestApproved;
        var isApproved = !isBouUser && isRequestApproved;

        if(isRequestApproved)
        {
            // Rebuild the steps list with all states set to Done
            steps = activeSteps.Select((s, i) => new DepositStep
            {
                Key = s.Key,
                Icon = s.Icon,
                Label = s.Label,
                SubLabel = s.SubLabel,
                State = StepState.Done,
                IsVisible = true,
                IsConnectorDone = i > 0 && (i - 1) < currentIndex,
            }).ToList();
        }

        return new DepositStepViewModel
        {
            Steps = steps,
            IsInProgress = isInProgress,
            IsApproved = isApproved,
            ShowBouPill = false,
        };
    }
}
