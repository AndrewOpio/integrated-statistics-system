# Rebuild WorkflowService's `WithdrawalRequest` transition config

Status: Resolved

## Context

The live `WorkflowTransitions` table for the `Withdrawal Request` entity type has drifted: dead
statuses (`Cancelled`, never reached by anything — there is no Cancel, Recall is the CB's only
withdrawal mechanism), an unintended `VerifyExceptional`/"Pending Exceptional Verification" step,
`ApproveExceptional`/`RejectExceptional` pointing at Currency Approver instead of Back Office, two
action-name typos (`ApproveToRTGSVefification`, `ExceptionalWithdrawaRequest`), and `RequestRecall`
missing a CB Maker row (CB Approver-only today). Confirmed by querying the local WorkflowService
database directly, not inferred.

Rather than patch it, clear the `WithdrawalRequest` rows and re-enter from scratch.

## Change

No code change — this is WorkflowService admin UI data entry (Workflow Transitions → Create), done
directly by the business owner for audit-trail reasons (each row's `CreatedBy` should reflect who
actually configured it).

1. Rename the three `StatusType` rows: Pending RTGS Verification → Pending RTGS Code, Pending
   Withdrawal Confirmation → Pending RTGS Confirmation, Verification Failed → RTGS Confirmation
   Failed. Also Reject Exceptional Approval → Exceptional Approval Rejected.
2. Delete the `Cancelled`/`Cancel Complete` statuses and the `Complete Cancel` transition — unused,
   nothing reaches them.
3. Delete `VerifyExceptional` and the `Pending Exceptional Verification` status.
4. Re-point `ApproveExceptional`/`RejectExceptional` at Back Office (role 32), sourced directly from
   Pending Exceptional Approval (no intermediate step).
5. Fix the two action-name typos.
6. Add the missing CB Maker row for `RequestRecall` at every status it currently has a CB Approver
   row for.
7. Add three new `ReturnForRevision` rows (RTGS Confirmation Failed → Returned for Internal
   Revision, Back Office) and confirm the existing two (Pending Internal Approval → …, CB Approver;
   Pending Verification → …, Panel B) still match.
8. Add `RejectRecall` from Recall Verified (Currency Approver) — target status doesn't matter here
   since the code-side logic (ticket 04) overrides it with a direct status mutation, but
   WorkflowService still needs a row to match against for the action to be recognized at all.

Full row-by-row detail: [`workflow-transitions.md`](../../../docs/withdrawals/workflow-transitions.md).

## Tests

No code here, so no tests written in this ticket — but this ticket's correctness is what tickets
02–09's integration tests actually exercise (role enforcement, origin routing, etc. all run against
whatever's live in WorkflowService). Treat those as this ticket's real verification.

## Acceptance criteria

- Querying `WorkflowTransitions` joined to `StatusTypes`/`EntityTypes` for `Withdrawal Request`
  matches every ✅/🔧 row in `workflow-transitions.md` exactly (ActionName, From, To, RequiredRole).
- No row references a status this ticket removed.

## Comments

Despite the "no code change" framing above, this was ultimately done as a migration rather than
literal admin-UI clicking, per direction during implementation:

- Added `WorkflowService/WorkflowService.Data/Migrations/20260821120000_RebuildWithdrawalRequestTransitions.cs`
  (+ `.Designer.cs`) — clears all `WithdrawalRequest` `WorkflowTransitions`, renames/drops/adds the
  `StatusTypes` rows per the doc, and re-inserts all 31 ✅/🔧 rows. Resolves From/To status ids and
  the `EntityTypeId` by name (portable across environments); `RequiredRole` ints are hardcoded
  (22/23/25/26/32/4 for CB Maker/CB Approver/Panel A/Panel B/Back Office/Currency Approver) since
  WorkflowService has no FK into IdentityService's `ApplicationRoles` — verify these match before
  applying to any environment other than local dev. `CreatedBy`/`ModifiedBy` on the new/renamed rows
  is `wtibingana` (git identity), matching the ticket's original audit-trail intent. `Down()` fully
  restores the prior (buggy) 25-row state, including the two typos and the wrong Currency Approver
  role, for symmetry.
- Applied directly to the local `Aswam_WorkflowDB` LocalDB and verified against
  `docs/withdrawals/workflow-transitions.md` — see `docs/withdrawals/verify-workflow-transitions.sql`,
  a standalone re-runnable check (0 unexpected rows, 0 missing rows, 0 references to removed
  statuses) usable against any other environment after the migration is applied there.
- The `Approve` transition's `TransitionAttachmentRequirements` row ("Withdrawal Request Slip", pdf)
  cascade-deletes when its parent `WorkflowTransitions` row is cleared — not mentioned in this
  ticket's text, found via direct FK inspection. The migration re-creates it against the new row.
- Real regression coverage lives in `VMSService.Tests/Withdrawals/` (new xUnit project, scaffolding
  ticket 21's stub) — see ticket 02's comments.
