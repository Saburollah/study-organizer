# Task 6 Report: Robust External Course Scans

## Status

Complete. Task 6 implements change synchronization, completed-task preservation,
late-subscriber snapshot materialization, missing-content visibility, safe scan
failure auditing, cancellation/unexpected-failure cleanup, and database-backed
concurrent scan exclusion without changing the public scan signatures.

## TDD evidence

### RED

Tests were added before the corresponding production behavior and executed with:

```text
dotnet test backend/tests/Domain.Tests/StudyOrganizer.Domain.Tests.csproj --no-restore --filter FullyQualifiedName~ScanRun -m:1 --disable-build-servers
```

Result: **2 failed, 10 passed, 12 total**. Both new cases failed because
`scan_cancelled` and `scan_failed` were rejected by `ScanRun`'s closed allowlist.

```text
dotnet test backend/tests/Infrastructure.Tests/StudyOrganizer.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~ExternalCourse -m:1 --disable-build-servers
```

After correcting test-only SQLite `DateTimeOffset` ordering, the clean feature RED
was **9 failed, 51 passed, 60 total**. Failures demonstrated missing open-task
synchronization, missing late-subscriber materialization, unhandled timeout/auth
provider failures, absent invalid-snapshot audit codes, uncleared cancellation and
unexpected-failure leases, and absent persistence-failure cleanup. The completed,
lost-deadline, missing-content, and conditional concurrent-lease tests already
passed because Task 5 behavior preserved those invariants.

The late-registration transaction test was mutation-checked by removing the
materialization call. Its targeted RED was **1 failed, 0 passed** with `Assert.Throws`
reporting no `DbUpdateException`; restoring materialization made the trigger fire.

### GREEN

Focused results after minimal implementation:

- ScanRun domain tests: **12 passed, 0 failed**.
- ExternalCourse infrastructure tests before the final transaction test: **60 passed, 0 failed**.
- Late-registration rollback test: **1 passed, 0 failed**.

## Final verification

Fresh commands and results immediately before completion:

```text
dotnet test backend/tests/Domain.Tests/StudyOrganizer.Domain.Tests.csproj --no-restore -m:1 --disable-build-servers
55 passed, 0 failed

dotnet test backend/tests/Application.Tests/StudyOrganizer.Application.Tests.csproj --no-restore -m:1 --disable-build-servers
6 passed, 0 failed

dotnet test backend/tests/Infrastructure.Tests/StudyOrganizer.Infrastructure.Tests.csproj --no-restore -m:1 --disable-build-servers
61 passed, 0 failed
```

Total: **122 passed, 0 failed, 0 skipped**. Application tests emitted NU1900
because NuGet vulnerability metadata could not be reached; test execution passed.

## Behavioral evidence

- Changed `exercise-1` keeps its task ID and synchronizes title/deadline only while
  open; `exercise-2` creates one new task per subscriber.
- A completed linked task retains its completed status, title, and deadline.
- A task-eligible item that loses its structured deadline becomes review-required
  without changing or deleting its linked task.
- Late registration reuses visible, future, task-eligible persisted content without
  another provider fetch. Invisible, past, and review-required content is excluded.
- A forced late task-insert failure rolls back the new subscription, module, task,
  and link together, proving registration materialization remains atomic.
- A complete snapshot omission marks content `NotVisible` and preserves task/link.
- Timeout/auth failures preserve existing contents/tasks, store only mapped safe
  codes, fail the run, and clear the lease. Wrong identity and canonical duplicate
  IDs store only `invalid_external_response`; raw payload/title/query fragments are
  never written to `ScanRun.ErrorCode`.
- Cancellation records `scan_cancelled` and rethrows. Unexpected provider and forced
  content-persistence failures record `scan_failed`, clear the lease best-effort,
  and rethrow; the persistence trigger also proves content/task/link rollback.
- Two independently scoped handlers and DbContexts share one SQLite test database.
  A blocked first fetch allows the second scope to contend: exactly one provider
  fetch occurs, with outcomes `Succeeded` and `AlreadyRunning`, one succeeded run,
  and a cleared final lease.

## Files changed

- `backend/src/Domain/ExternalCourses/ScanRun.cs`
- `backend/src/Infrastructure/ExternalCourses/ExternalCourseRegistrationHandler.cs`
- `backend/src/Infrastructure/ExternalCourses/ExternalCourseScanHandler.cs`
- `backend/tests/Domain.Tests/ExternalCourses/ScanRunTests.cs`
- `backend/tests/Infrastructure.Tests/ExternalCourses/ControlledExternalCourseProvider.cs`
- `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCourseRegistrationHandlerTests.cs`
- `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCourseScanHandlerTests.cs`
- `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCourseScenario.cs`
- `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCourseTestDatabase.cs`
- `.superpowers/sdd/2026-08-28-moodle-end-to-end/task-6-report.md`

## Self-review and concerns

- Public registration/scan interfaces and Task 7 API/write-protection surfaces were
  not changed.
- Failure auditing clears the EF tracker before saving only the run and lease state;
  cancellation-independent tokens prevent request cancellation from skipping cleanup.
- Unexpected cleanup is intentionally best-effort: a database that remains unavailable
  cannot guarantee audit persistence, but the original exception is preserved.
- Existing `Docs/skill-evaluation/superpowers-observations.md` changes were left
  untouched and unstaged.

## Review fix round 1

The independent review found no production defect and three Important test gaps.
The existing tests were strengthened to keep an expired item visible and
task-eligible during late registration, use and assert a changed non-null task
description, cover all four typed provider failures, and compare the persisted
last-success/content/task state across provider, cancellation, unexpected, and
persistence failures.

The first fix agent stopped at its usage limit after editing the tests. A fresh
verification agent was interrupted when the controller switched Tasks 7–12 to an
inline workflow. At that moment it had temporarily removed the production due-date
filter for a mutation check; the controller restored exactly that committed line.
No Task 6 production behavior was otherwise changed in this round.

Fresh inline verification of the inherited worktree:

```text
dotnet test backend/tests/Infrastructure.Tests/StudyOrganizer.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~ExternalCourse -m:1 --disable-build-servers
63 passed, 0 failed

dotnet test backend/tests/Infrastructure.Tests/StudyOrganizer.Infrastructure.Tests.csproj --no-restore -m:1 --disable-build-servers
63 passed, 0 failed
```

Review findings: **3 addressed, 0 open**. Per the updated execution constraint,
there was no per-task re-review; one complete implementation review remains after
Tasks 7–12.
