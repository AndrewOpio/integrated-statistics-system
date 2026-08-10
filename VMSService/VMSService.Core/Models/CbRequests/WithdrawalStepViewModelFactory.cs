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
        ("CbMaker",          "ti-user-plus",     "Initiator",         "Initiate request"),
        ("CbApprover",       "ti-user-check",    "Approver",          "Internal approval"),
        ("RtgsVerification", "ti-clock-bolt",    "RTGS Verification", "Awaiting RTGS code"),
        ("BouProcessing",    "ti-settings",      "BOU Processing",    "Processing at BOU"),
    ];

    private static readonly (string Key, string Icon, string Label, string SubLabel)[] BouSteps =
    [
        ("CB",        "ti-clock-hour-4",  "CB Handling",   "Commercial Bank Processing"),
        ("Remittance",  "ti-adjustments",          "Remittance",       "Validate Checks"),
        ("PanelA",      "ti-shield-check",         "Panel A",          "Review and Adjust"),
        ("PanelB",      "ti-arrow-right-circle",   "Panel B",          "Collect and Approve"),
    ];

    private static readonly HashSet<string> BankStepKeys = BouSteps.Select(s => s.Key).ToHashSet();


    // Steps visible to commercial bank users (requestor + supervisor roles)
    private static readonly HashSet<string> CompanyVisibleSteps = ["requestor", "supervisor"];

    public static WithdrawalStepViewModel Build(
        string currentStepKey,
        bool isRequestApproved,
        bool requiresSpecialApproval,
        ClaimsPrincipal user)
    {
        //var userRole = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? string.Empty;
        var isBouUser = user.HasClaim(c => c.Type == "isStaff" && c.Value == "true");  //RolePolicy.BouRoles.Contains(userRole);


        var activeSteps = isBouUser ? BouSteps : CbSteps;

        if (!requiresSpecialApproval)
        {
            activeSteps = activeSteps.Where(s => s.Key != "Remittance").ToArray();
        }

        var currentIndex = Array.FindIndex(activeSteps, s => s.Key == currentStepKey);

        //var steps = activeSteps.Select((s, i) => new WithdrawalStep
        //{
        //    Key = s.Key,
        //    Icon = s.Icon,
        //    Label = s.Label,
        //    SubLabel = s.SubLabel,
        //    //State = i < currentIndex ? StepState.Done
        //    //                : i == currentIndex ? StepState.Active
        //    //                : StepState.InProcess,
        //    State = isRequestApproved ? StepState.Done
        //      : i < currentIndex ? StepState.Done
        //      : i == currentIndex ? StepState.Active
        //      : StepState.InProcess,
        //    IsVisible = true,
        //    IsConnectorDone = i > 0 && (i - 1) < currentIndex,
        //}).ToList();
        var steps = activeSteps.Select((s, i) => new WithdrawalStep
        {
            Key = s.Key,
            Icon = s.Icon,
            Label = s.Label,
            SubLabel = s.SubLabel,
            State = isRequestApproved ? StepState.Done
             : i < currentIndex ? StepState.Done
             : i == currentIndex ? StepState.Active
             : StepState.InProcess,
            IsVisible = true,
            IsConnectorDone = isRequestApproved || (i > 0 && (i - 1) < currentIndex),
        }).ToList();

        //var isInProgress = !isBouUser && currentStepKey == "bou" && !isRequestApproved;
        //var isApproved = !isBouUser && isRequestApproved;
        //var isInProgress = !isBouUser && currentStepKey == "bou" && !isRequestApproved;
        var isApproved = !isBouUser && isRequestApproved;
        var isInProgress = !isBouUser && BankStepKeys.Contains(currentStepKey) && !isRequestApproved;

        // ── debug ──
        Console.WriteLine($"[Stepper] isBankUser={isBouUser}");
        Console.WriteLine($"[Stepper] currentStepKey={currentStepKey}");
        Console.WriteLine($"[Stepper] isRequestApproved={isRequestApproved}");
        Console.WriteLine($"[Stepper] activeSteps=[{string.Join(", ", activeSteps.Select(s => s.Key))}]");
        Console.WriteLine($"[Stepper] currentIndex={currentIndex}");
        foreach (var step in steps)
        {
            Console.WriteLine($"[Stepper]   {step.Key,-12} State={step.State,-10} ConnectorDone={step.IsConnectorDone}");
        }
        // ───────────


        return new WithdrawalStepViewModel
        {
            Steps = steps,
            IsInProgress = isInProgress,
            IsApproved = isApproved,
            ShowBouPill = false,
        };
    }
}
