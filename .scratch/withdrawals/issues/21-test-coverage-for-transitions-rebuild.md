# Set up the test project

Status: Open

## Context

No test project exists anywhere in this solution yet (confirmed — the only near-miss,
`NotificationService.Tests`, is an empty stub not even referenced in its own `.sln`). This ticket is
scaffolding only: standing up the harness so every other ticket can write its own tests alongside its
own implementation, rather than one large test-writing pass happening after everything else is done.

Test shape, decided earlier in this effort: unit tests with mocked repositories for pure logic that
doesn't touch the DB; integration tests against a real database for anything involving transition
execution or authorization — that's exactly the class of bug a mock can't catch (the live
`ApproveExceptional`/`RejectExceptional` role misconfiguration found this session was only visible by
querying the real WorkflowService database).

## Change

- Create an xUnit test project, referenced by the `.sln` (unlike the existing
  `NotificationService.Tests` stub).
- Set up the integration-test harness: `Microsoft.EntityFrameworkCore.InMemory` or a LocalDB test
  instance — either works, pick whichever is faster to stand up.
- As the first tests in the new project (don't depend on ticket 01 — this pre-existing logic isn't
  touched by the transitions rebuild, so these can be written immediately and prove the harness
  works):
  - `SubmissionClassifier` — standard/exceptional/grace-window boundaries: exactly-11:00, Monday vs.
    weekday, day-after-public-holiday grace.
  - Amount/bundle validation rules.

## Downstream: where the rest of the tests live

Every other implementation ticket in this batch (01–09, 24, 27–29) carries its own "Tests" section
in its acceptance criteria, written against this harness as part of that ticket's own work — not
collected here. See each ticket for its specific scenarios.

## Acceptance criteria

- Test project exists, runs locally, and is referenced by the `.sln`.
- `SubmissionClassifier` and amount/bundle validation both have passing tests.

## Comments

Partially scaffolded as a side effect of ticket 02: `VMSService/VMSService.Tests` now exists
(xUnit, LocalDB-based integration harness, referenced by `VMSService.sln`) with 27 passing tests
in `Withdrawals/` covering ticket 01/02's transition rebuild. Still open for this ticket
specifically: `SubmissionClassifier` and amount/bundle validation unit tests haven't been written —
this ticket isn't fully resolved yet.
