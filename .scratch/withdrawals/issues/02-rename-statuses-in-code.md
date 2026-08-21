# Rename renamed statuses throughout VMSService code

Status: Resolved
Blocked by: 01

## Context

Ticket 01 renames three statuses in WorkflowService's live data. `WithdrawConstants` and everything
that reads/displays status names must match exactly, or transition matching (exact string equality
on `FromStatusId`/status name) silently breaks — this is the same class of bug the `ApproveToRTGSVefification`
typo already caused.

## Change

- `WithdrawConstants.WithdrawalStatusNames`: rename `PendingRTGSVerification` → keep the C# member
  name or rename it too (`PendingRtgsCode`), but the **string value** must become `"Pending RTGS
  Code"`. Same for `PendingWithdrawalConfirmation` → `"Pending RTGS Confirmation"`,
  `VerificationFailed` → `"RTGS Confirmation Failed"`, `RejectExceptionalApproval` → `"Exceptional
  Approval Rejected"`.
- `WithdrawConstants.WithdrawalStatusActionNames`: fix `ApproveToRTGSVerification`'s value
  (`ApproveToRTGSVefification` → `ApproveToRTGSCode`, matching the renamed target status) and
  `ExceptionalWithdrawalRequest`'s value (`ExceptionalWithdrawaRequest` → `ExceptionalWithdrawalRequest`).
- Grep the whole `VMSService` tree for the old string literals (both status names and the two typo'd
  action names) — `workflow-transitions.md`'s "Fixes required" section already lists known raw-string
  call sites in `CbWithdrawRequestsController.cs` and `WithdrawalRequestsController.cs` that bypass
  the constants class entirely; those need the same fix, not just the constants file.
- Update any `.cshtml` views that hardcode the old display names (status pills, badges, filters —
  `bou-withdrawal-process.md` section 1 lists several: the six status pills on
  `WithdrawalRequests/Index.cshtml` reference status names directly).

## Tests

Write against the harness from ticket 21, alongside this ticket's implementation:

- An integration test drives a request through the full happy path (Draft → … → Approved) after the
  renames and confirms every status match still succeeds — this is the regression check that would
  have caught the original `ApproveToRTGSVefification` typo bug.
- A grep-style assertion (or a small reflection test) confirming none of the four old status strings
  or two old typo'd action names remain anywhere the constants class exposes.

## Acceptance criteria

- No occurrence of the four old status strings or the two typo'd action-name strings anywhere in
  `VMSService` (search, not just the files listed above).
- A request can still be driven through every transition end-to-end against the rebuilt
  WorkflowService config from ticket 01.

## Comments

- Kept the C# member names unchanged (`PendingRTGSVerification`, `PendingWithdrawalConfirmation`,
  `VerificationFailed`, `RejectExceptionalApproval`) per the ticket's stated option — only the
  string *values* changed, to keep the diff surgical.
- Fixed raw-string call sites in `CbWithdrawRequestsController.cs` (lines ~536/542/545) that
  bypassed the constants class — rewrote the `switch` as pattern-matched `when` guards against
  `WithdrawalStatusActionNames` so they can't drift from the constant again.
- Updated every free-text mention of the old names in comments, `TempData` messages, and status-
  history remarks across both withdrawal controllers, plus the `data-status` filter attributes and
  display text in `Index.cshtml`'s status pills (the `data-status` values are what client-side
  filtering matches against, so leaving them stale would have silently broken the pill filters).
- Found and left alone: a second, unrelated dead `switch` in `WithdrawalRequestsController.cs`
  (~line 556, `vm.AvailableTransitions` filter) with case labels like `"Exceptional Approval"`,
  `"Pending_Recommendation"`, `"Verified Exceptional"` that don't match any real WithdrawalRequest
  `ActionName` under either the old or new config — looks like copy-pasted logic from a different
  entity type. Its `"Verification Failed"` arm was deleted because renaming it to `"RTGS
  Confirmation Failed"` would have made it start actually matching (that's Back Office's real,
  live `RTGSConfirmationFailed` action name), silently adding a new filter condition never asked
  for. The rest of that dead switch is out of scope for this ticket and left as-is — worth its own
  cleanup ticket.
- New: `VMSService/VMSService.Tests` (xUnit, referenced by `VMSService.sln`), scaffolding ticket
  21's stub. `Withdrawals/CbSideWorkflowTransitionsTests.cs` and `BouSideWorkflowTransitionsTests.cs`
  query the live local `Aswam_WorkflowDB`/`IdentityDB` LocalDB instances directly (role names
  resolved to ids dynamically, not hardcoded) and assert VMSService's `WithdrawConstants` values
  match what's actually live — including regression checks for the exact
  `ApproveExceptional`/`RejectExceptional` role bug and the two typos this pair of tickets fixed.
  27 tests, all passing against the post-migration DB. These are config-consistency tests, not
  full controller/HTTP integration tests — driving a request through the controllers end-to-end
  is still open (tickets 03–09's own test sections).
