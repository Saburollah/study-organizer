# Moodle End-to-End Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a deterministic, local Moodle course flow from registration through one shared manual scan to deduplicated personal study tasks and visible review states.

**Architecture:** Keep the external course and normalized contents shared, connect each user through a personal subscription and study module, and materialize source-controlled tasks through an idempotent link table. Application ports define discovery and complete snapshots; EF Core and a network-free mock adapter implement them, while authenticated Minimal APIs and a Vue view expose the flow.

**Tech Stack:** .NET 8, C# 12, ASP.NET Core Minimal APIs, Entity Framework Core 8.0.29, PostgreSQL/Npgsql, xUnit, Vue 3, TypeScript 6, Vue Router, Vue I18n, Vitest.

**Spec:** `Docs/superpowers/specs/2026-08-27-moodle-end-to-end-design.md`

## Global Constraints

- Work only in the isolated `experiment/superpowers` worktree; do not copy Matt issues, ADRs, plans, or implementation artifacts.
- Follow red-green-refactor for every behavior. Never write production code before observing the focused test fail for the expected reason.
- The mock provider accepts only the two fixture aliases `https://mock-moodle.local/courses/software-engineering-2026` and `https://mock-moodle.local/course/view.php?id=se-2026`; it performs no network calls.
- Canonical course identity is exactly `mock-moodle + software-engineering-2026`.
- Canonical content identity is `ExternalCourse.Id + ProviderContentId`; titles, links, positions, and deadlines are mutable attributes.
- The initial fixture uses `exercise-1` due `2026-09-15T12:00:00Z`; the changed fixture moves it to `2026-09-17T12:00:00Z` and adds `exercise-2` due `2026-09-20T12:00:00Z`.
- Only an `Assignment` with a structured UTC deadline is task-eligible. Freitext dates are never parsed.
- Shared external state and personal tasks must be atomically persisted for a successful scan.
- Failed or invalid scans never alter the last successful `ExternalContent` state or linked `StudyTask` rows.
- A linked Moodle task permits status changes only; title, description, deadline, edit, and delete remain source-controlled.
- A linked personal module remains editable but cannot be deleted while its subscription exists.
- Use `TimeProvider` for deadline relevance and scan timestamps in all new workflows.
- Do not add a scheduler, notification channel, real Moodle authentication, HTML parser, LLM call, unsubscribe flow, or browser E2E framework.
- Add all new user-facing copy in both `frontend/src/i18n/locales/de.ts` and `frontend/src/i18n/locales/en.ts`.
- Preserve the S0 baseline record: 17 Domain tests passed; 43 pre-existing API tests failed at host startup because of JWT configuration. Do not describe that known failure as a Moodle regression.

---

## Planned File Structure

### Backend domain and application

- `backend/src/Domain/ExternalCourses/ExternalCourse.cs` — shared course identity and scan lease state.
- `backend/src/Domain/ExternalCourses/CourseSubscription.cs` — personal user/course/module association.
- `backend/src/Domain/ExternalCourses/ExternalContent.cs` — normalized current content state and visibility transitions.
- `backend/src/Domain/ExternalCourses/ExternalTaskLink.cs` — idempotent content/subscription/task association.
- `backend/src/Domain/ExternalCourses/ScanRun.cs` — safe scan audit lifecycle.
- `backend/src/Domain/ExternalCourses/ExternalCourseEnums.cs` — content, review, visibility, and scan enums.
- `backend/src/Application/ExternalCourses/ExternalCourseProviderContracts.cs` — discovery and complete-snapshot port.
- `backend/src/Application/ExternalCourses/CourseSnapshotDiffer.cs` — pure stable-ID comparison.
- `backend/src/Application/ExternalCourses/ExternalCourseResults.cs` — registration, query, and scan results.
- `backend/src/Application/ExternalCourses/IExternalCourseRegistrationHandler.cs` — registration use case.
- `backend/src/Application/ExternalCourses/IExternalCourseQueryHandler.cs` — subscription/content queries.
- `backend/src/Application/ExternalCourses/IExternalCourseScanHandler.cs` — shared manual scan use case.

### Backend infrastructure and API

- `backend/src/Infrastructure/ExternalCourses/MockMoodleProvider.cs` — allowlisted network-free provider.
- `backend/src/Infrastructure/ExternalCourses/ExternalCourseRegistrationHandler.cs` — atomic registration and late materialization.
- `backend/src/Infrastructure/ExternalCourses/ExternalCourseQueryHandler.cs` — owner-scoped projections.
- `backend/src/Infrastructure/ExternalCourses/ExternalCourseScanHandler.cs` — lease, fetch, validation, diff, transaction, and materialization.
- `backend/src/Infrastructure/Persistence/Configurations/ExternalCourseConfiguration.cs` — canonical course uniqueness.
- `backend/src/Infrastructure/Persistence/Configurations/CourseSubscriptionConfiguration.cs` — owner/course and module constraints.
- `backend/src/Infrastructure/Persistence/Configurations/ExternalContentConfiguration.cs` — content identity and field limits.
- `backend/src/Infrastructure/Persistence/Configurations/ExternalTaskLinkConfiguration.cs` — idempotent task links.
- `backend/src/Infrastructure/Persistence/Configurations/ScanRunConfiguration.cs` — scan audit persistence.
- `backend/src/Api/ExternalCourses/ExternalCourseModels.cs` — HTTP request/response contracts.
- `backend/src/Api/ExternalCourses/ExternalCourseEndpoints.cs` — authenticated subscription and scan endpoints.

### Backend tests

- `backend/tests/Domain.Tests/ExternalCourses/ExternalCourseTests.cs` — course and subscription invariants.
- `backend/tests/Domain.Tests/ExternalCourses/ExternalContentTests.cs` — content state transitions.
- `backend/tests/Domain.Tests/ExternalCourses/ScanRunTests.cs` — audit lifecycle.
- `backend/tests/Application.Tests/ExternalCourses/CourseSnapshotDifferTests.cs` — pure diff behavior.
- `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCourseTestDatabase.cs` — isolated relational SQLite fixture.
- `backend/tests/Infrastructure.Tests/ExternalCourses/TestTimeProvider.cs` — deterministic test clock.
- `backend/tests/Infrastructure.Tests/ExternalCourses/ControlledExternalCourseProvider.cs` — controllable provider and fetch counter.
- `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCourseSnapshots.cs` — exact initial, changed, missing, and invalid snapshots.
- `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCourseScenario.cs` — reusable registered/scanned integration scenario.
- `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCoursePersistenceTests.cs` — relational constraints.
- `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCourseRegistrationHandlerTests.cs` — registration and late subscribers.
- `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCourseScanHandlerTests.cs` — happy path, updates, failures, and concurrency.
- `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalSourceProtectionTests.cs` — linked task/module write protection.
- `backend/tests/Api.Tests/ExternalCourses/ExternalCourseEndpointsTests.cs` — HTTP mapping and authorization.

### Frontend

- `frontend/src/features/externalCourses/externalCourseModels.ts` — typed API models.
- `frontend/src/features/externalCourses/externalCourseService.ts` — HTTP client boundary.
- `frontend/src/features/externalCourses/CourseRegistrationForm.vue` — URL input and validation.
- `frontend/src/views/externalCourses/MoodleCoursesView.vue` — registration, list, scan, and contents.
- `frontend/src/features/externalCourses/__tests__/externalCourseService.spec.ts` — route and payload tests.
- `frontend/src/features/externalCourses/__tests__/CourseRegistrationForm.spec.ts` — form tests.
- `frontend/src/views/externalCourses/__tests__/MoodleCoursesView.spec.ts` — visible state tests.
- Existing router, navigation, module, task, models, tests, and locale files are modified only where named in the tasks below.

---

### Task 1: Model Shared Courses, Contents, Subscriptions, Links, and Scan Runs

**Files:**
- Create: `backend/src/Domain/ExternalCourses/ExternalCourseEnums.cs`
- Create: `backend/src/Domain/ExternalCourses/ExternalCourse.cs`
- Create: `backend/src/Domain/ExternalCourses/CourseSubscription.cs`
- Create: `backend/src/Domain/ExternalCourses/ExternalContent.cs`
- Create: `backend/src/Domain/ExternalCourses/ExternalTaskLink.cs`
- Create: `backend/src/Domain/ExternalCourses/ScanRun.cs`
- Create: `backend/tests/Domain.Tests/ExternalCourses/ExternalCourseTests.cs`
- Create: `backend/tests/Domain.Tests/ExternalCourses/ExternalContentTests.cs`
- Create: `backend/tests/Domain.Tests/ExternalCourses/ScanRunTests.cs`

**Interfaces:**
- Consumes: existing `StudyModule` and `StudyTask` IDs as `Guid` values only.
- Produces: the exact domain types used by all later tasks: `ExternalCourse`, `CourseSubscription`, `ExternalContent`, `ExternalTaskLink`, `ScanRun`, `ExternalContentKind`, `ExternalContentProcessingState`, `ExternalContentReviewReason`, `ExternalContentVisibility`, and `ScanRunStatus`.

- [ ] **Step 1: Write failing course and subscription invariant tests**

```csharp
[Fact]
public void Constructor_WithCanonicalIdentity_TrimsValues()
{
    var now = new DateTimeOffset(2026, 8, 28, 8, 0, 0, TimeSpan.Zero);

    var course = new ExternalCourse(
        " mock-moodle ",
        " software-engineering-2026 ",
        " Software Engineering ",
        now);

    Assert.Equal("mock-moodle", course.ProviderKey);
    Assert.Equal("software-engineering-2026", course.ExternalCourseId);
    Assert.Equal("Software Engineering", course.Name);
    Assert.Null(course.ActiveScanRunId);
}

[Fact]
public void CourseSubscription_WithEmptyModuleId_Throws()
{
    Assert.Throws<ArgumentException>(() => new CourseSubscription(
        Guid.NewGuid(), Guid.NewGuid(), Guid.Empty,
        DateTimeOffset.UtcNow));
}
```

- [ ] **Step 2: Write failing content and scan lifecycle tests**

```csharp
[Fact]
public void ApplySnapshot_PreservesIdentityAndUpdatesMutableFields()
{
    var content = ExternalContent.Create(
        Guid.NewGuid(), "exercise-1", ExternalContentKind.Assignment,
        "Exercise 1", null, "https://mock-moodle.local/content/exercise-1",
        new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero),
        ExternalContentProcessingState.TaskEligible,
        ExternalContentReviewReason.None,
        DateTimeOffset.UtcNow);
    var originalId = content.Id;

    content.ApplySnapshot(
        ExternalContentKind.Assignment, "Exercise 1 revised", "New text",
        "https://mock-moodle.local/content/exercise-1-v2",
        new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero),
        ExternalContentProcessingState.TaskEligible,
        ExternalContentReviewReason.None,
        DateTimeOffset.UtcNow);

    Assert.Equal(originalId, content.Id);
    Assert.Equal("exercise-1", content.ProviderContentId);
    Assert.Equal("Exercise 1 revised", content.Title);
    Assert.Equal(ExternalContentVisibility.Visible, content.Visibility);
}

[Fact]
public void Fail_StoresSafeCodeAndCompletesRun()
{
    var run = new ScanRun(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
    run.Fail("external_timeout", DateTimeOffset.UtcNow.AddSeconds(1));

    Assert.Equal(ScanRunStatus.Failed, run.Status);
    Assert.Equal("external_timeout", run.ErrorCode);
    Assert.NotNull(run.FinishedAtUtc);
}
```

- [ ] **Step 3: Run the focused domain tests and observe the expected compile failure**

Run:

```bash
dotnet test backend/tests/Domain.Tests/StudyOrganizer.Domain.Tests.csproj --filter FullyQualifiedName~ExternalCourses
```

Expected: FAIL because `StudyOrganizer.Domain.ExternalCourses` and its types do not exist.

- [ ] **Step 4: Add the enums and exact public entity surface**

```csharp
public enum ExternalContentKind { Assignment = 0, Announcement = 1, Resource = 2 }
public enum ExternalContentProcessingState { TaskEligible = 0, ReviewRequired = 1 }
public enum ExternalContentReviewReason { None = 0, NotAnAssignment = 1, MissingStructuredDeadline = 2 }
public enum ExternalContentVisibility { Visible = 0, NotVisible = 1 }
public enum ScanRunStatus { InProgress = 0, Succeeded = 1, Failed = 2 }

public sealed class ExternalCourse
{
    public Guid Id { get; private set; }
    public string ProviderKey { get; private set; } = null!;
    public string ExternalCourseId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Guid? ActiveScanRunId { get; private set; }
    public DateTimeOffset? LastSuccessfulScanAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void Rename(string name);
    public void MarkScanStarted(Guid scanRunId);
    public void MarkScanSucceeded(Guid scanRunId, DateTimeOffset finishedAtUtc);
    public void MarkScanFailed(Guid scanRunId);
}
```

Implement constructor guards for empty GUIDs and blank required strings. Normalize all strings with `Trim()`. `ExternalContent.ApplySnapshot` resets visibility to `Visible`; `MarkNotVisible` changes visibility only. `ScanRun.Succeed` and `Fail` may execute only while `InProgress` and reject blank error codes.

- [ ] **Step 5: Run the domain tests and verify green**

Run:

```bash
dotnet test backend/tests/Domain.Tests/StudyOrganizer.Domain.Tests.csproj --filter FullyQualifiedName~ExternalCourses
```

Expected: PASS for all new ExternalCourses tests.

- [ ] **Step 6: Commit the domain model**

```bash
git add backend/src/Domain/ExternalCourses backend/tests/Domain.Tests/ExternalCourses
git commit -m "feat: model external course domain"
```

---

### Task 2: Define Provider Contracts and Deterministic Snapshot Diffing

**Files:**
- Create: `backend/src/Application/ExternalCourses/ExternalCourseProviderContracts.cs`
- Create: `backend/src/Application/ExternalCourses/CourseSnapshotDiffer.cs`
- Create: `backend/tests/Application.Tests/StudyOrganizer.Application.Tests.csproj`
- Create: `backend/tests/Application.Tests/ExternalCourses/CourseSnapshotDifferTests.cs`
- Modify: `backend/StudyOrganizer.sln`

**Interfaces:**
- Consumes: `ExternalContentKind` from Task 1.
- Produces: `IExternalCourseProvider`, `CourseDiscovery`, `CourseSnapshot`, `CourseSnapshotItem`, `ExistingContentState`, `CourseSnapshotDiff`, `CourseContentChange`, `CourseContentChangeKind`, `ExternalCourseProviderException`, and `ExternalCourseProviderError`.

- [ ] **Step 1: Add the Application test project and failing diff tests**

Use this project file and add it to the solution:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.5.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
    <ProjectReference Include="..\..\src\Application\StudyOrganizer.Application.csproj" />
  </ItemGroup>
</Project>
```

Run:

```bash
dotnet sln backend/StudyOrganizer.sln add backend/tests/Application.Tests/StudyOrganizer.Application.Tests.csproj --solution-folder tests
```

Write tests named:

```csharp
[Fact] public void Compare_NewStableId_ReturnsNew();
[Fact] public void Compare_SameValues_ReturnsUnchanged();
[Fact] public void Compare_SameIdWithChangedTitleLinkAndDeadline_ReturnsChanged();
[Fact] public void Compare_MissingStableId_ReturnsMissing();
[Fact] public void Compare_DuplicateIncomingIds_ThrowsInvalidSnapshot();
```

The changed test must assert that the incoming item retains `ProviderContentId == "exercise-1"` while `Kind == Changed`.

- [ ] **Step 2: Run the comparer test and observe the expected failure**

```bash
dotnet test backend/tests/Application.Tests/StudyOrganizer.Application.Tests.csproj --filter FullyQualifiedName~CourseSnapshotDifferTests
```

Expected: FAIL because the provider contracts and differ do not exist.

- [ ] **Step 3: Implement the exact provider and snapshot contracts**

```csharp
public sealed record CourseDiscovery(
    string ProviderKey,
    string ExternalCourseId,
    string Name);

public sealed record CourseSnapshot(
    string ProviderKey,
    string ExternalCourseId,
    bool IsComplete,
    IReadOnlyList<CourseSnapshotItem> Contents);

public sealed record CourseSnapshotItem(
    string ProviderContentId,
    ExternalContentKind Kind,
    string Title,
    string? Description,
    Uri SourceUri,
    DateTimeOffset? StructuredDueDateUtc);

public interface IExternalCourseProvider
{
    string ProviderKey { get; }
    bool CanHandle(Uri courseUri);
    Task<CourseDiscovery> DiscoverAsync(Uri courseUri, CancellationToken cancellationToken = default);
    Task<CourseSnapshot> FetchSnapshotAsync(string externalCourseId, CancellationToken cancellationToken = default);
}

public enum ExternalCourseProviderError
{
    UnsupportedUrl,
    Timeout,
    AuthenticationRequired,
    InvalidResponse
}
```

`ExternalCourseProviderException` carries only the enum and a safe message. It must not carry raw HTML, tokens, or complete content payloads.

- [ ] **Step 4: Implement stable-ID diffing**

```csharp
public enum CourseContentChangeKind { New, Changed, Unchanged, Missing }

public sealed record ExistingContentState(
    Guid Id,
    string ProviderContentId,
    ExternalContentKind Kind,
    string Title,
    string? Description,
    string SourceUrl,
    DateTimeOffset? StructuredDueDateUtc);

public sealed record CourseContentChange(
    CourseContentChangeKind Kind,
    ExistingContentState? Existing,
    CourseSnapshotItem? Incoming);

public sealed record CourseSnapshotDiff(
    IReadOnlyList<CourseContentChange> Changes);

public sealed class InvalidCourseSnapshotException(string message)
    : Exception(message);
```

`CourseSnapshotDiffer.Compare` must use `StringComparer.Ordinal` for provider content IDs, compare every mutable field, reject duplicate incoming IDs, and return changes ordered by provider content ID for deterministic tests.

- [ ] **Step 5: Run Application and Domain tests**

```bash
dotnet test backend/tests/Application.Tests/StudyOrganizer.Application.Tests.csproj
dotnet test backend/tests/Domain.Tests/StudyOrganizer.Domain.Tests.csproj
```

Expected: PASS.

- [ ] **Step 6: Commit contracts and diffing**

```bash
git add backend/StudyOrganizer.sln backend/src/Application/ExternalCourses backend/tests/Application.Tests
git commit -m "feat: define external course snapshots"
```

---

### Task 3: Persist External Course State with Relational Constraints

**Files:**
- Modify: `backend/src/Infrastructure/Persistence/ApplicationDbContext.cs`
- Create: `backend/src/Infrastructure/Persistence/Configurations/ExternalCourseConfiguration.cs`
- Create: `backend/src/Infrastructure/Persistence/Configurations/CourseSubscriptionConfiguration.cs`
- Create: `backend/src/Infrastructure/Persistence/Configurations/ExternalContentConfiguration.cs`
- Create: `backend/src/Infrastructure/Persistence/Configurations/ExternalTaskLinkConfiguration.cs`
- Create: `backend/src/Infrastructure/Persistence/Configurations/ScanRunConfiguration.cs`
- Create: `backend/tests/Infrastructure.Tests/StudyOrganizer.Infrastructure.Tests.csproj`
- Create: `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCourseTestDatabase.cs`
- Create: `backend/tests/Infrastructure.Tests/ExternalCourses/TestTimeProvider.cs`
- Create: `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCoursePersistenceTests.cs`
- Modify: `backend/StudyOrganizer.sln`
- Create generated migration files under: `backend/src/Infrastructure/Persistence/Migrations/` with migration name `AddExternalCourses`
- Modify generated snapshot: `backend/src/Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs`

**Interfaces:**
- Consumes: all Task 1 entities.
- Produces: `ApplicationDbContext` sets named `ExternalCourses`, `CourseSubscriptions`, `ExternalContents`, `ExternalTaskLinks`, and `ScanRuns`, plus a reusable SQLite test fixture.

- [ ] **Step 1: Create the Infrastructure test project and failing uniqueness test**

Use this project file, then add it to the solution under `tests`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.29" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.5.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
    <ProjectReference Include="..\..\src\Infrastructure\StudyOrganizer.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

```bash
dotnet sln backend/StudyOrganizer.sln add backend/tests/Infrastructure.Tests/StudyOrganizer.Infrastructure.Tests.csproj --solution-folder tests
```

```csharp
[Fact]
public async Task SaveChanges_DuplicateCanonicalCourse_Throws()
{
    await using var database = await ExternalCourseTestDatabase.CreateAsync();
    database.Context.ExternalCourses.AddRange(
        new ExternalCourse("mock-moodle", "software-engineering-2026", "SE", database.Now),
        new ExternalCourse("mock-moodle", "software-engineering-2026", "SE copy", database.Now));

    await Assert.ThrowsAsync<DbUpdateException>(() =>
        database.Context.SaveChangesAsync());
}
```

Add parallel tests for duplicate `OwnerId + ExternalCourseId`, duplicate `ExternalCourseId + ProviderContentId`, duplicate `CourseSubscriptionId + ExternalContentId`, and duplicate linked `TaskId`.

- [ ] **Step 2: Run the persistence tests and observe the expected compile failure**

```bash
dotnet test backend/tests/Infrastructure.Tests/StudyOrganizer.Infrastructure.Tests.csproj --filter FullyQualifiedName~ExternalCoursePersistenceTests
```

Expected: FAIL because DbSets and configurations do not exist.

- [ ] **Step 3: Add DbSets and exact relational mappings**

```csharp
public DbSet<ExternalCourse> ExternalCourses => Set<ExternalCourse>();
public DbSet<CourseSubscription> CourseSubscriptions => Set<CourseSubscription>();
public DbSet<ExternalContent> ExternalContents => Set<ExternalContent>();
public DbSet<ExternalTaskLink> ExternalTaskLinks => Set<ExternalTaskLink>();
public DbSet<ScanRun> ScanRuns => Set<ScanRun>();
```

Use table names `external_courses`, `course_subscriptions`, `external_contents`, `external_task_links`, and `scan_runs`. Use snake_case column names, `timestamp with time zone` timestamps, integer enum conversions, and these indexes:

```csharp
builder.HasIndex(x => new { x.ProviderKey, x.ExternalCourseId }).IsUnique();
builder.HasIndex(x => new { x.OwnerId, x.ExternalCourseId }).IsUnique();
builder.HasIndex(x => new { x.ExternalCourseId, x.ProviderContentId }).IsUnique();
builder.HasIndex(x => new { x.CourseSubscriptionId, x.ExternalContentId }).IsUnique();
builder.HasIndex(x => x.TaskId).IsUnique();
```

Configure subscription-to-module deletion with `DeleteBehavior.Restrict`; course-to-subscription and course-to-content deletion also use `Restrict` because unsubscribe/course removal is outside scope.

- [ ] **Step 4: Implement the reusable SQLite fixture**

```csharp
public sealed class ExternalCourseTestDatabase : IAsyncDisposable
{
    public SqliteConnection Connection { get; }
    public ApplicationDbContext Context { get; }
    public DateTimeOffset Now { get; } = new(2026, 8, 28, 8, 0, 0, TimeSpan.Zero);
    public TimeProvider TimeProvider { get; }

    public static async Task<ExternalCourseTestDatabase> CreateAsync();
    public Task<Guid> CreateUserAsync(string email);
    public ValueTask DisposeAsync();
}
```

`CreateAsync` opens one `Data Source=:memory:` connection, builds `DbContextOptions` with `UseSqlite`, creates the context, and calls `EnsureCreatedAsync`. Keep the connection open until disposal. `TestTimeProvider` derives from `TimeProvider`, returns `Now` from `GetUtcNow`, and exposes `Advance(TimeSpan)`. `CreateUserAsync` inserts an `ApplicationUser` with `Email`, `NormalizedEmail`, `UserName`, and `NormalizedUserName` all derived from the supplied address.

- [ ] **Step 5: Run persistence tests and verify green**

```bash
dotnet test backend/tests/Infrastructure.Tests/StudyOrganizer.Infrastructure.Tests.csproj --filter FullyQualifiedName~ExternalCoursePersistenceTests
```

Expected: PASS.

- [ ] **Step 6: Generate and inspect the PostgreSQL migration**

```bash
dotnet ef migrations add AddExternalCourses --project backend/src/Infrastructure --startup-project backend/src/Api
dotnet build backend/StudyOrganizer.sln
```

Expected: the migration creates the five tables, foreign keys, and five unique indexes listed above; build succeeds with no new warnings.

- [ ] **Step 7: Commit persistence**

```bash
git add backend/StudyOrganizer.sln backend/src/Infrastructure/Persistence backend/tests/Infrastructure.Tests
git commit -m "feat: persist external course state"
```

---

### Task 4: Register and Query Mock Courses Idempotently

**Files:**
- Create: `backend/src/Application/ExternalCourses/ExternalCourseResults.cs`
- Create: `backend/src/Application/ExternalCourses/IExternalCourseRegistrationHandler.cs`
- Create: `backend/src/Application/ExternalCourses/IExternalCourseQueryHandler.cs`
- Create: `backend/src/Infrastructure/ExternalCourses/MockMoodleProvider.cs`
- Create: `backend/src/Infrastructure/ExternalCourses/ExternalCourseRegistrationHandler.cs`
- Create: `backend/src/Infrastructure/ExternalCourses/ExternalCourseQueryHandler.cs`
- Create: `backend/tests/Infrastructure.Tests/ExternalCourses/ControlledExternalCourseProvider.cs`
- Create: `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCourseRegistrationHandlerTests.cs`

**Interfaces:**
- Consumes: provider contracts from Task 2 and DbSets from Task 3.
- Produces: registration/query handler interfaces and result records consumed by Tasks 5, 6, and 8.

- [ ] **Step 1: Write failing registration tests**

```csharp
[Fact]
public async Task RegisterAsync_TwoAliases_CreateOneSharedCourseAndOneSubscription()
{
    await using var database = await ExternalCourseTestDatabase.CreateAsync();
    var ownerId = await database.CreateUserAsync("student@example.com");
    var provider = ControlledExternalCourseProvider.ForSoftwareEngineering();
    var handler = new ExternalCourseRegistrationHandler(
        database.Context, [provider], database.TimeProvider);

    var first = await handler.RegisterAsync(ownerId,
        "https://mock-moodle.local/courses/software-engineering-2026");
    var second = await handler.RegisterAsync(ownerId,
        "https://mock-moodle.local/course/view.php?id=se-2026");

    Assert.Equal(CourseRegistrationOutcome.Created, first.Outcome);
    Assert.Equal(CourseRegistrationOutcome.Existing, second.Outcome);
    Assert.Equal(first.Subscription!.Id, second.Subscription!.Id);
    Assert.Single(database.Context.ExternalCourses);
    Assert.Single(database.Context.CourseSubscriptions);
    Assert.Single(database.Context.Modules);
}
```

Add exact tests for invalid URI, unsupported host/path, two owners sharing one course but receiving different modules, and rollback when module/subscription persistence fails.

- [ ] **Step 2: Run the registration tests and observe the expected failure**

```bash
dotnet test backend/tests/Infrastructure.Tests/StudyOrganizer.Infrastructure.Tests.csproj --filter FullyQualifiedName~ExternalCourseRegistrationHandlerTests
```

Expected: FAIL because registration interfaces, result types, provider, and handlers do not exist.

- [ ] **Step 3: Add the registration and query contracts**

```csharp
public enum CourseRegistrationOutcome { Created, Existing, InvalidUrl, UnsupportedUrl }

public sealed record CourseSubscriptionResult(
    Guid Id,
    Guid ModuleId,
    string CourseName,
    string ProviderKey,
    string ExternalCourseId,
    string LastScanStatus,
    DateTimeOffset? LastSuccessfulScanAtUtc);

public sealed record CourseRegistrationResult(
    CourseRegistrationOutcome Outcome,
    CourseSubscriptionResult? Subscription);

public enum ExternalContentDisplayStatus { TaskCreated, ReviewRequired, NotVisible }

public sealed record ExternalContentResult(
    Guid Id,
    string ProviderContentId,
    string Title,
    string? Description,
    string SourceUrl,
    DateTimeOffset? DueDateUtc,
    ExternalContentDisplayStatus Status,
    string? ReviewReason,
    Guid? TaskId);
```

`IExternalCourseRegistrationHandler.RegisterAsync(Guid ownerId, string courseUrl, CancellationToken)` returns `CourseRegistrationResult`. `IExternalCourseQueryHandler` exposes `GetByOwnerAsync` and owner-scoped `GetContentsAsync`; the latter returns `null` for a foreign or missing subscription.

- [ ] **Step 4: Implement the allowlisted mock provider**

```csharp
public sealed class MockMoodleProvider : IExternalCourseProvider
{
    public const string Key = "mock-moodle";
    public const string CourseId = "software-engineering-2026";
    public string ProviderKey => Key;

    public bool CanHandle(Uri courseUri);
    public Task<CourseDiscovery> DiscoverAsync(Uri courseUri, CancellationToken cancellationToken = default);
    public Task<CourseSnapshot> FetchSnapshotAsync(string externalCourseId, CancellationToken cancellationToken = default);
}
```

`CanHandle` returns true only for the two exact HTTPS fixture aliases. `DiscoverAsync` returns `Software Engineering`; `FetchSnapshotAsync` returns `exercise-1` with structured deadline `2026-09-15T12:00:00Z` and `announcement-1` without a deadline. Use the exact source links from `ExternalCourseSnapshots.Initial`. Do not instantiate `HttpClient`.

The test double has this public control surface:

```csharp
public sealed class ControlledExternalCourseProvider : IExternalCourseProvider
{
    public int FetchCount { get; private set; }
    public static ControlledExternalCourseProvider ForSoftwareEngineering();
    public void SetSnapshot(CourseSnapshot snapshot);
    public void SetFailure(ExternalCourseProviderError error);
    public void BlockNextFetch();
    public void ReleaseBlockedFetch();
}
```

- [ ] **Step 5: Implement atomic registration and owner-scoped queries**

Registration must parse an absolute HTTPS URI, select exactly one provider by `CanHandle`, discover canonical identity, reuse or create `ExternalCourse`, create a `StudyModule`, and create `CourseSubscription` in one transaction. Re-query on a unique conflict and return `Existing` only for the same owner and canonical course.

Query projections must derive `TaskCreated` from `ExternalTaskLink`, prefer `NotVisible` over every processing state, and otherwise expose `ReviewRequired` for non-task-eligible content.

- [ ] **Step 6: Run registration, persistence, and provider tests**

```bash
dotnet test backend/tests/Infrastructure.Tests/StudyOrganizer.Infrastructure.Tests.csproj --filter FullyQualifiedName~ExternalCourse
```

Expected: PASS; the controlled provider reports zero fetches during registration.

- [ ] **Step 7: Commit registration and query handlers**

```bash
git add backend/src/Application/ExternalCourses backend/src/Infrastructure/ExternalCourses backend/tests/Infrastructure.Tests/ExternalCourses
git commit -m "feat: register mock Moodle courses"
```

---

### Task 5: Scan Once and Materialize Tasks for Every Subscriber

**Files:**
- Create: `backend/src/Application/ExternalCourses/IExternalCourseScanHandler.cs`
- Modify: `backend/src/Application/ExternalCourses/ExternalCourseResults.cs`
- Create: `backend/src/Infrastructure/ExternalCourses/ExternalCourseScanHandler.cs`
- Modify: `backend/src/Domain/Tasks/StudyTask.cs`
- Modify: `backend/tests/Domain.Tests/Tasks/StudyTaskTests.cs`
- Create: `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCourseSnapshots.cs`
- Create: `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCourseScenario.cs`
- Create: `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCourseScanHandlerTests.cs`

**Interfaces:**
- Consumes: all Task 1–4 types and handlers.
- Produces: `CourseScanOutcome`, `CourseScanSummary`, `CourseScanResult`, `IExternalCourseScanHandler`, and `StudyTask.SynchronizeFromExternalSource`.

- [ ] **Step 1: Write the failing three-subscriber and idempotency tests**

```csharp
[Fact]
public async Task ScanAsync_ThreeSubscribers_FetchesOnceAndCreatesThreeTasks()
{
    var setup = await ExternalCourseScenario.CreateAsync(subscriberCount: 3);
    setup.Provider.SetSnapshot(ExternalCourseSnapshots.Initial);

    var result = await setup.Handler.ScanAsync(setup.OwnerIds[0], setup.SubscriptionIds[0]);

    Assert.Equal(CourseScanOutcome.Succeeded, result.Outcome);
    Assert.Equal(1, setup.Provider.FetchCount);
    Assert.Equal(2, await setup.Database.Context.ExternalContents.CountAsync());
    Assert.Equal(3, await setup.Database.Context.Tasks.CountAsync());
    Assert.Equal(3, await setup.Database.Context.ExternalTaskLinks.CountAsync());
    Assert.Equal(1, result.Summary!.NewTaskEligibleCount);
    Assert.Equal(1, result.Summary.ReviewRequiredCount);
}

[Fact]
public async Task ScanAsync_SameSnapshotTwice_DoesNotCreateDuplicates()
{
    var setup = await ExternalCourseScenario.CreateAsync(subscriberCount: 1);
    setup.Provider.SetSnapshot(ExternalCourseSnapshots.Initial);

    await setup.Handler.ScanAsync(setup.OwnerIds[0], setup.SubscriptionIds[0]);
    await setup.Handler.ScanAsync(setup.OwnerIds[0], setup.SubscriptionIds[0]);

    Assert.Equal(2, setup.Provider.FetchCount);
    Assert.Single(setup.Database.Context.Tasks);
    Assert.Single(setup.Database.Context.ExternalTaskLinks);
}
```

- [ ] **Step 2: Write a failing source synchronization domain test**

```csharp
[Fact]
public void SynchronizeFromExternalSource_OpenTask_UpdatesSourceFieldsAndKeepsId()
{
    var task = new StudyTask(Guid.NewGuid(), "Old", DateTimeOffset.UtcNow.AddDays(1));
    var id = task.Id;
    var updatedAt = DateTimeOffset.UtcNow.AddHours(1);

    task.SynchronizeFromExternalSource("New", DateTimeOffset.UtcNow.AddDays(2), "Updated", updatedAt);

    Assert.Equal(id, task.Id);
    Assert.Equal("New", task.Title);
    Assert.Equal(updatedAt, task.UpdatedAt);
}
```

- [ ] **Step 3: Run focused tests and observe failure**

```bash
dotnet test backend/tests/Domain.Tests/StudyOrganizer.Domain.Tests.csproj --filter FullyQualifiedName~SynchronizeFromExternalSource
dotnet test backend/tests/Infrastructure.Tests/StudyOrganizer.Infrastructure.Tests.csproj --filter FullyQualifiedName~ExternalCourseScanHandlerTests
```

Expected: FAIL because scan contracts, handler, and synchronization method do not exist.

- [ ] **Step 4: Add exact scan results and interface**

```csharp
public enum CourseScanOutcome
{
    Succeeded,
    NotFound,
    AlreadyRunning,
    ExternalFailure,
    InvalidSnapshot
}

public sealed record CourseScanSummary(
    int NewContentCount,
    int ChangedContentCount,
    int ReviewRequiredCount,
    int NotVisibleCount,
    int NewTaskEligibleCount);

public sealed record CourseScanResult(
    CourseScanOutcome Outcome,
    CourseScanSummary? Summary,
    string? ErrorCode);

public interface IExternalCourseScanHandler
{
    Task<CourseScanResult> ScanAsync(
        Guid ownerId,
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 5: Implement the minimal happy-path scan**

Acquire a course lease with one conditional `ExecuteUpdateAsync` that sets `ActiveScanRunId` only when it is null. Add an `InProgress` `ScanRun`, fetch once, validate the canonical course identity, uniqueness of provider content IDs, non-empty titles, absolute HTTP/HTTPS source URLs, and complete snapshot structure, compare through `CourseSnapshotDiffer`, and process in one EF transaction.

Classify exactly as follows:

```csharp
private static (ExternalContentProcessingState State, ExternalContentReviewReason Reason)
    Classify(CourseSnapshotItem item) =>
        item.Kind != ExternalContentKind.Assignment
            ? (ExternalContentProcessingState.ReviewRequired, ExternalContentReviewReason.NotAnAssignment)
            : item.StructuredDueDateUtc is null
                ? (ExternalContentProcessingState.ReviewRequired, ExternalContentReviewReason.MissingStructuredDeadline)
                : (ExternalContentProcessingState.TaskEligible, ExternalContentReviewReason.None);
```

For each task-eligible content and subscription, create a `StudyTask` only when no `ExternalTaskLink` exists. Mark the run succeeded and clear the lease in the same transaction.

`ExternalCourseSnapshots` exposes `Initial`, `Changed`, `WithoutExerciseOne`, `WrongCourse`, and `DuplicateContentIds`. Give `ExternalCourseScenario` this exact public test surface so every later test uses one declared fixture rather than inventing setup:

```csharp
public sealed class ExternalCourseScenario : IAsyncDisposable
{
    public ExternalCourseTestDatabase Database { get; }
    public ControlledExternalCourseProvider Provider { get; }
    public ExternalCourseRegistrationHandler RegistrationHandler { get; }
    public ExternalCourseScanHandler Handler { get; }
    public StudyTaskHandler TaskHandler { get; }
    public IReadOnlyList<Guid> OwnerIds { get; }
    public IReadOnlyList<Guid> SubscriptionIds { get; }
    public IReadOnlyList<Guid> ModuleIds { get; }
    public IReadOnlyList<Guid> TaskIds { get; }
    public DateTimeOffset DueDate { get; }

    public static Task<ExternalCourseScenario> CreateAsync(int subscriberCount);
    public static Task<ExternalCourseScenario> CreateScannedAsync(int subscriberCount);
    public Task<IReadOnlyList<StudyTask>> TasksForAsync(Guid ownerId);
    public Task<StudyTask> ReloadTaskAsync(Guid taskId);
    public ValueTask DisposeAsync();
}
```

`CreateAsync` fixes the test clock before every fixture deadline, registers the primary URL for every owner, and leaves the provider unfetched. `CreateScannedAsync` additionally installs `ExternalCourseSnapshots.Initial`, performs one scan, and refreshes `ModuleIds`, `TaskIds`, and `DueDate` from persisted state.

- [ ] **Step 6: Run focused and aggregate backend tests**

```bash
dotnet test backend/tests/Domain.Tests/StudyOrganizer.Domain.Tests.csproj
dotnet test backend/tests/Application.Tests/StudyOrganizer.Application.Tests.csproj
dotnet test backend/tests/Infrastructure.Tests/StudyOrganizer.Infrastructure.Tests.csproj
```

Expected: PASS.

- [ ] **Step 7: Commit the shared happy path**

```bash
git add backend/src/Application/ExternalCourses backend/src/Infrastructure/ExternalCourses backend/src/Domain/Tasks backend/tests
git commit -m "feat: scan shared courses idempotently"
```

---

### Task 6: Handle Changes, Late Subscribers, Failures, Missing Contents, and Concurrent Scans

**Files:**
- Modify: `backend/src/Infrastructure/ExternalCourses/ExternalCourseRegistrationHandler.cs`
- Modify: `backend/src/Infrastructure/ExternalCourses/ExternalCourseScanHandler.cs`
- Modify: `backend/tests/Infrastructure.Tests/ExternalCourses/ControlledExternalCourseProvider.cs`
- Modify: `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCourseRegistrationHandlerTests.cs`
- Modify: `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalCourseScanHandlerTests.cs`

**Interfaces:**
- Consumes: Task 5 scan surface unchanged.
- Produces: complete spec behavior without changing public signatures.

- [ ] **Step 1: Add failing change and completed-task tests**

Use the initial snapshot, store the generated task ID, switch the provider to a changed snapshot with the same `exercise-1` ID and a new `exercise-2`, then assert:

```csharp
Assert.Equal(originalTaskId, updatedExerciseOne.Id);
Assert.Equal("Exercise 1 revised", updatedExerciseOne.Title);
Assert.Equal(changedDueDate, updatedExerciseOne.DueDate);
Assert.Equal(2, tasksForFirstSubscriber.Count);
```

Complete `exercise-1` before a third changed snapshot and assert its title, deadline, and completed status remain unchanged.

- [ ] **Step 2: Add failing late-subscriber and missing-content tests**

```csharp
[Fact]
public async Task RegisterAsync_AfterSuccessfulScan_ReusesSnapshotWithoutFetch()
{
    var setup = await ExternalCourseScenario.CreateAsync(subscriberCount: 1);
    setup.Provider.SetSnapshot(ExternalCourseSnapshots.Initial);
    await setup.Handler.ScanAsync(setup.OwnerIds[0], setup.SubscriptionIds[0]);
    var fetchesBeforeRegistration = setup.Provider.FetchCount;

    var secondOwner = await setup.Database.CreateUserAsync("second@example.com");
    var result = await setup.Registration.RegisterAsync(
        secondOwner,
        "https://mock-moodle.local/courses/software-engineering-2026");

    Assert.Equal(fetchesBeforeRegistration, setup.Provider.FetchCount);
    Assert.Single(await setup.TasksForAsync(secondOwner));
}
```

Add a scan whose valid complete snapshot omits `exercise-1`; assert `Visibility == NotVisible` and the linked task still exists.

- [ ] **Step 3: Add failing invalid, failure, and concurrency tests**

Add exact tests for timeout, authentication-required, wrong course identity, duplicate content IDs, and a provider blocked by `TaskCompletionSource`. For each failure, assert the previous content values and tasks are unchanged. For concurrency, start two scan tasks, release one provider call, and assert `FetchCount == 1` plus outcomes `Succeeded` and `AlreadyRunning`.

Also assert that `ScanRun.ErrorCode` contains only the mapped safe code and never the provider exception message, URI query, or snapshot payload.

- [ ] **Step 4: Run focused tests and observe the expected failures**

```bash
dotnet test backend/tests/Infrastructure.Tests/StudyOrganizer.Infrastructure.Tests.csproj --filter FullyQualifiedName~ExternalCourse
```

Expected: FAIL in change synchronization, late materialization, failure preservation, missing visibility, and concurrent lease cases.

- [ ] **Step 5: Implement change and late-materialization rules**

For a changed visible content, call `ApplySnapshot`. Synchronize linked tasks only when `Status == StudyTaskStatus.Open`. Create newly eligible task links exactly once. For late registration, query visible task-eligible contents where `StructuredDueDateUtc > timeProvider.GetUtcNow()` and materialize them inside the registration transaction without fetching.

- [ ] **Step 6: Implement safe failure and missing-content behavior**

Map provider errors to safe codes:

```csharp
ExternalCourseProviderError.Timeout => "external_timeout",
ExternalCourseProviderError.AuthenticationRequired => "external_auth_required",
ExternalCourseProviderError.InvalidResponse => "invalid_external_response",
ExternalCourseProviderError.UnsupportedUrl => "unsupported_url"
```

On every provider/validation failure, fail `ScanRun`, clear the lease, and save only those audit changes. Mark missing existing contents `NotVisible` only after a complete valid snapshot. Never delete a task or link.

Clear the lease in a `finally`-equivalent failure path for cancellation and unexpected persistence exceptions as well; a failed request must not leave a permanently active scan in normal process execution.

- [ ] **Step 7: Run all new backend tests**

```bash
dotnet test backend/tests/Domain.Tests/StudyOrganizer.Domain.Tests.csproj
dotnet test backend/tests/Application.Tests/StudyOrganizer.Application.Tests.csproj
dotnet test backend/tests/Infrastructure.Tests/StudyOrganizer.Infrastructure.Tests.csproj
```

Expected: PASS.

- [ ] **Step 8: Commit robustness behavior**

```bash
git add backend/src/Infrastructure/ExternalCourses backend/tests/Infrastructure.Tests/ExternalCourses
git commit -m "feat: preserve course state across scan changes"
```

---

### Task 7: Protect Moodle Tasks and Linked Modules While Exposing Source Metadata

**Files:**
- Modify: `backend/src/Application/Tasks/IStudyTaskHandler.cs`
- Modify: `backend/src/Application/Tasks/StudyTaskResult.cs`
- Create: `backend/src/Application/Tasks/StudyTaskMutationResult.cs`
- Modify: `backend/src/Application/Modules/IModuleHandler.cs`
- Modify: `backend/src/Application/Modules/ModuleResult.cs`
- Create: `backend/src/Application/Modules/ModuleDeleteOutcome.cs`
- Modify: `backend/src/Infrastructure/Tasks/StudyTaskHandler.cs`
- Modify: `backend/src/Infrastructure/Modules/ModuleHandler.cs`
- Modify: `backend/src/Api/Tasks/StudyTaskModels.cs`
- Modify: `backend/src/Api/Tasks/StudyTaskEndpoints.cs`
- Modify: `backend/src/Api/Modules/ModuleModels.cs`
- Modify: `backend/src/Api/Modules/ModuleEndpoints.cs`
- Modify: `backend/tests/Api.Tests/Tasks/StudyTaskEndpointsTests.cs`
- Modify: `backend/tests/Api.Tests/Modules/ModuleEndpointsTests.cs`
- Create: `backend/tests/Infrastructure.Tests/ExternalCourses/ExternalSourceProtectionTests.cs`

**Interfaces:**
- Consumes: `ExternalTaskLink` and `CourseSubscription` persistence.
- Produces: `StudyTaskMutationOutcome`, `StudyTaskMutationResult`, `ExternalTaskSourceResult`, `ModuleDeleteOutcome`, `ModuleResult.IsExternalCourseLinked`, and optional task source response fields used by frontend Tasks 9–11.

- [ ] **Step 1: Write failing handler protection tests**

```csharp
[Fact]
public async Task UpdateAsync_LinkedTask_ReturnsExternallyManagedAndKeepsValues()
{
    var setup = await ExternalCourseScenario.CreateScannedAsync(subscriberCount: 1);

    var result = await setup.TaskHandler.UpdateAsync(
        setup.OwnerIds[0], setup.ModuleIds[0], setup.TaskIds[0],
        "Local override", setup.DueDate.AddDays(1), null);

    Assert.Equal(StudyTaskMutationOutcome.ExternallyManaged, result.Outcome);
    Assert.Equal("Exercise 1", (await setup.ReloadTaskAsync(setup.TaskIds[0])).Title);
}
```

Add tests that delete is rejected, status completion succeeds, manual task update/delete still succeed, linked module deletion returns `LinkedToExternalCourse`, and query results include provider/title/source URL.

- [ ] **Step 2: Run protection tests and observe the expected failure**

```bash
dotnet test backend/tests/Infrastructure.Tests/StudyOrganizer.Infrastructure.Tests.csproj --filter FullyQualifiedName~ExternalSourceProtectionTests
```

Expected: FAIL because mutation outcomes and source projections do not exist.

- [ ] **Step 3: Introduce exact mutation and source result types**

```csharp
public enum StudyTaskMutationOutcome { Succeeded, NotFound, ExternallyManaged }

public sealed record ExternalTaskSourceResult(
    string ProviderKey,
    string CourseName,
    string SourceUrl);

public sealed record StudyTaskMutationResult(
    StudyTaskMutationOutcome Outcome,
    StudyTaskResult? Task);

public enum ModuleDeleteOutcome { Deleted, NotFound, LinkedToExternalCourse }
```

Change task `UpdateAsync` and `DeleteAsync` to return `StudyTaskMutationResult`. Add `ExternalTaskSourceResult? ExternalSource` to `StudyTaskResult`. Change module `DeleteAsync` to return `ModuleDeleteOutcome`; add `bool IsExternalCourseLinked` to `ModuleResult`.

- [ ] **Step 4: Enforce protection in Infrastructure**

Before task update/delete, query `ExternalTaskLinks.AnyAsync(link => link.TaskId == taskId)`. Return `ExternallyManaged` without mutation. Keep status changes unchanged. Project source through link → content → course. Before module delete, query `CourseSubscriptions.AnyAsync(subscription => subscription.ModuleId == moduleId)`.

- [ ] **Step 5: Update Minimal API mappings and existing stubs**

Map `ExternallyManaged` and `LinkedToExternalCourse` to HTTP 409 Problem Details with `detail` exactly `externally_managed_task` and `linked_external_course_module`. Preserve 404 and success mappings. Add optional `ExternalTaskSourceResponse` and `isExternalCourseLinked` fields to API responses.

Update every existing `StubStudyTaskHandler` and `StubModuleHandler` method signature and constructor value so the whole Api.Tests project compiles.

- [ ] **Step 6: Run focused protection and API mapping tests**

```bash
dotnet test backend/tests/Infrastructure.Tests/StudyOrganizer.Infrastructure.Tests.csproj --filter FullyQualifiedName~ExternalSourceProtectionTests
dotnet test backend/tests/Api.Tests/StudyOrganizer.Api.Tests.csproj --filter "FullyQualifiedName~StudyTaskEndpointsTests|FullyQualifiedName~ModuleEndpointsTests"
```

Expected: new protection tests PASS. If the second command still hits the documented JWT baseline host-start failure, record that unchanged result and verify compilation separately with `dotnet build backend/StudyOrganizer.sln`.

- [ ] **Step 7: Commit source protection**

```bash
git add backend/src/Application/Tasks backend/src/Application/Modules backend/src/Infrastructure/Tasks backend/src/Infrastructure/Modules backend/src/Api/Tasks backend/src/Api/Modules backend/tests
git commit -m "feat: protect Moodle-managed study data"
```

---

### Task 8: Expose Authenticated Course Subscription APIs

**Files:**
- Create: `backend/src/Api/ExternalCourses/ExternalCourseModels.cs`
- Create: `backend/src/Api/ExternalCourses/ExternalCourseEndpoints.cs`
- Modify: `backend/src/Api/Program.cs`
- Create: `backend/tests/Api.Tests/ExternalCourses/ExternalCourseEndpointsTests.cs`

**Interfaces:**
- Consumes: three Application handlers and result types from Tasks 4–6.
- Produces: the four HTTP routes and JSON shapes consumed by frontend Task 9.

- [ ] **Step 1: Write failing endpoint tests with handler stubs**

Write tests for unauthorized access to every route, 201 new registration, 200 existing registration, 400 invalid/unsupported URL, owner-scoped list/content, 404 foreign subscription, 200 successful scan summary, 409 `scan_in_progress`, and 502 safe external failure.

```csharp
[Fact]
public async Task Scan_WhenAlreadyRunning_ReturnsConflict()
{
    var handler = new StubScanHandler(new CourseScanResult(
        CourseScanOutcome.AlreadyRunning, null, "scan_in_progress"));
    using var factory = CreateFactory(scanHandler: handler);
    using var client = CreateAuthorizedClient(factory, Guid.NewGuid());

    var response = await client.PostAsync(
        $"/api/course-subscriptions/{Guid.NewGuid()}/scan", null);

    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    Assert.Equal("scan_in_progress", problem!.Detail);
}
```

The test file defines these exact private helpers and configures all required settings through `ConfigureAppConfiguration` with an in-memory dictionary:

```csharp
private static WebApplicationFactory<Program> CreateFactory(
    IExternalCourseRegistrationHandler? registrationHandler = null,
    IExternalCourseQueryHandler? queryHandler = null,
    IExternalCourseScanHandler? scanHandler = null);

private static HttpClient CreateAuthorizedClient(
    WebApplicationFactory<Program> factory,
    Guid ownerId);
```

- [ ] **Step 2: Run the endpoint tests and observe the expected failure**

```bash
dotnet test backend/tests/Api.Tests/StudyOrganizer.Api.Tests.csproj --filter FullyQualifiedName~ExternalCourseEndpointsTests
```

Expected: FAIL because routes and HTTP contracts do not exist. Ensure the factory adds complete in-memory `Jwt:Issuer`, `Jwt:Audience`, `Jwt:SigningKey`, `Jwt:ExpiresInMinutes`, and `ConnectionStrings:DefaultConnection` settings so these new tests do not inherit the baseline configuration defect.

- [ ] **Step 3: Add exact request and response models**

```csharp
public sealed class RegisterCourseSubscriptionRequest
{
    [Required, StringLength(2048), Url]
    public string CourseUrl { get; init; } = string.Empty;
}

public sealed record CourseSubscriptionResponse(
    Guid Id,
    Guid ModuleId,
    string CourseName,
    string ProviderKey,
    string ExternalCourseId,
    string LastScanStatus,
    DateTimeOffset? LastSuccessfulScanAtUtc);

public sealed record ExternalCourseContentResponse(
    Guid Id,
    string ProviderContentId,
    string Title,
    string? Description,
    string SourceUrl,
    DateTimeOffset? DueDateUtc,
    string Status,
    string? ReviewReason,
    Guid? TaskId);

public sealed record CourseScanResponse(
    string Status,
    int NewContentCount,
    int ChangedContentCount,
    int ReviewRequiredCount,
    int NotVisibleCount,
    int NewTaskEligibleCount);
```

- [ ] **Step 4: Implement and register endpoints and dependencies**

Map exactly:

```csharp
POST /api/course-subscriptions
GET  /api/course-subscriptions
GET  /api/course-subscriptions/{subscriptionId:guid}/contents
POST /api/course-subscriptions/{subscriptionId:guid}/scan
```

Create one authorized route group tagged `Moodle Courses`. Register `MockMoodleProvider` as `IExternalCourseProvider` and the three handlers as scoped services. Call `app.MapExternalCourseEndpoints()` after existing endpoint mappings.

- [ ] **Step 5: Run endpoint tests and backend build**

```bash
dotnet test backend/tests/Api.Tests/StudyOrganizer.Api.Tests.csproj --filter FullyQualifiedName~ExternalCourseEndpointsTests
dotnet build backend/StudyOrganizer.sln
```

Expected: new endpoint tests PASS and build succeeds with zero new warnings.

- [ ] **Step 6: Commit the API**

```bash
git add backend/src/Api/ExternalCourses backend/src/Api/Program.cs backend/tests/Api.Tests/ExternalCourses
git commit -m "feat: expose Moodle course APIs"
```

---

### Task 9: Add Typed Frontend Course Service

**Files:**
- Create: `frontend/src/features/externalCourses/externalCourseModels.ts`
- Create: `frontend/src/features/externalCourses/externalCourseService.ts`
- Create: `frontend/src/features/externalCourses/__tests__/externalCourseService.spec.ts`

**Interfaces:**
- Consumes: Task 8 JSON shapes and existing `apiRequest`.
- Produces: `CourseSubscription`, `ExternalCourseContent`, `CourseScanSummary`, `RegisterCourseRequest`, `ExternalCourseService`, and singleton `externalCourseService`.

- [ ] **Step 1: Write failing service route tests**

```typescript
it('registers a course link', async () => {
  const request = { courseUrl: 'https://mock-moodle.local/courses/software-engineering-2026' }
  const fetchMock = stubFetch(subscription, 201)

  const result = await new HttpExternalCourseService().register(request)

  expect(fetchMock).toHaveBeenCalledExactlyOnceWith(
    'http://localhost:5101/api/course-subscriptions',
    expect.objectContaining({ method: 'POST', body: JSON.stringify(request) }),
  )
  expect(result).toEqual(subscription)
})
```

Add tests for `getAll`, `getContents(subscriptionId)`, and `scan(subscriptionId)` with encoded IDs and exact paths from Task 8.

- [ ] **Step 2: Run the service test and observe failure**

```bash
cd frontend
pnpm exec vitest run src/features/externalCourses/__tests__/externalCourseService.spec.ts
```

Expected: FAIL because the models and service do not exist.

- [ ] **Step 3: Implement exact TypeScript contracts and service**

```typescript
export type ExternalContentStatus = 'TaskCreated' | 'ReviewRequired' | 'NotVisible'

export interface CourseSubscription {
  id: string
  moduleId: string
  courseName: string
  providerKey: string
  externalCourseId: string
  lastScanStatus: string
  lastSuccessfulScanAtUtc: string | null
}

export interface ExternalCourseContent {
  id: string
  providerContentId: string
  title: string
  description: string | null
  sourceUrl: string
  dueDateUtc: string | null
  status: ExternalContentStatus
  reviewReason: string | null
  taskId: string | null
}

export interface CourseScanSummary {
  status: string
  newContentCount: number
  changedContentCount: number
  reviewRequiredCount: number
  notVisibleCount: number
  newTaskEligibleCount: number
}
```

Implement `register`, `getAll`, `getContents`, and `scan` with `apiRequest` and the exact Task 8 paths.

- [ ] **Step 4: Run service tests and type-check**

```bash
cd frontend
pnpm exec vitest run src/features/externalCourses/__tests__/externalCourseService.spec.ts
pnpm type-check
```

Expected: PASS.

- [ ] **Step 5: Commit frontend contracts**

```bash
git add frontend/src/features/externalCourses
git commit -m "feat: add Moodle course client"
```

---

### Task 10: Build the Moodle Courses View and Navigation

**Files:**
- Create: `frontend/src/features/externalCourses/CourseRegistrationForm.vue`
- Create: `frontend/src/features/externalCourses/__tests__/CourseRegistrationForm.spec.ts`
- Create: `frontend/src/views/externalCourses/MoodleCoursesView.vue`
- Create: `frontend/src/views/externalCourses/__tests__/MoodleCoursesView.spec.ts`
- Modify: `frontend/src/router/index.ts`
- Modify: `frontend/src/router/__tests__/router.spec.ts`
- Modify: `frontend/src/App.vue`
- Modify: `frontend/src/i18n/locales/de.ts`
- Modify: `frontend/src/i18n/locales/en.ts`

**Interfaces:**
- Consumes: `externalCourseService` and models from Task 9.
- Produces: protected route named `moodle-courses`, registration form event `register`, and the visible end-to-end course page.

- [ ] **Step 1: Write failing form tests**

Test that an empty URL shows the localized required message, a non-URL shows the localized invalid message, a valid fixture URL emits exactly `{ courseUrl }`, cancel is not present, and the submit button shows a loading label while disabled.

```typescript
expect(wrapper.emitted('register')).toEqual([[
  { courseUrl: 'https://mock-moodle.local/courses/software-engineering-2026' },
]])
```

- [ ] **Step 2: Write failing view tests**

Cover loading, empty state, successful registration, scan invocation, summary counts, content statuses, module link, safe API error, and English copy. The scan test must assert `externalCourseService.scan(subscription.id)` followed by a contents reload.

- [ ] **Step 3: Run component tests and observe failure**

```bash
cd frontend
pnpm exec vitest run src/features/externalCourses/__tests__/CourseRegistrationForm.spec.ts src/views/externalCourses/__tests__/MoodleCoursesView.spec.ts
```

Expected: FAIL because components and translations do not exist.

- [ ] **Step 4: Implement the registration form**

Use `<input id="course-url" type="url" maxlength="2048">`, a native submit event, trimmed values, and localized validation. Public props and emits are exactly:

```typescript
const props = defineProps<{ isSubmitting: boolean }>()
const emit = defineEmits<{
  register: [request: RegisterCourseRequest]
}>()
```

- [ ] **Step 5: Implement the course view**

On mount, load subscriptions; load each subscription's contents when rendering its card. Each card contains:

```html
<RouterLink class="course-module-link" :to="{ name: 'module-tasks', params: { moduleId: subscription.moduleId } }">
  {{ t('externalCourses.actions.openModule') }}
</RouterLink>
<button class="scan-course-button" type="button" :disabled="scanningSubscriptionId === subscription.id">
  {{ t('externalCourses.actions.scan') }}
</button>
```

Render status elements with classes `.content-status-task-created`, `.content-status-review-required`, and `.content-status-not-visible`; render external links with `target="_blank"` and `rel="noopener noreferrer"`.

- [ ] **Step 6: Add protected route, authenticated navigation, and translations**

Add route `/moodle-courses`, name `moodle-courses`, `requiresAuth: true`. Add the navigation link inside the authenticated template. Add the complete `externalCourses` translation tree for page headings, form labels/validation, loading/empty/error/success text, scan summary, statuses, review reasons, and actions in both locales.

- [ ] **Step 7: Run view, router, type, and lint checks**

```bash
cd frontend
pnpm exec vitest run src/features/externalCourses src/views/externalCourses src/router/__tests__/router.spec.ts
pnpm type-check
pnpm lint
```

Expected: PASS and no tracked formatting changes outside named files.

- [ ] **Step 8: Commit the course UI**

```bash
git add frontend/src/features/externalCourses frontend/src/views/externalCourses frontend/src/router frontend/src/App.vue frontend/src/i18n/locales
git commit -m "feat: add Moodle course workflow"
```

---

### Task 11: Show Provenance and Lock Source-Controlled Task and Module Actions

**Files:**
- Modify: `frontend/src/features/tasks/taskModels.ts`
- Modify: `frontend/src/features/tasks/__tests__/taskService.spec.ts`
- Modify: `frontend/src/features/dashboard/__tests__/dashboardService.spec.ts`
- Modify: `frontend/src/views/tasks/StudyTasksView.vue`
- Modify: `frontend/src/views/tasks/__tests__/StudyTasksView.spec.ts`
- Modify: `frontend/src/features/modules/moduleModels.ts`
- Modify: `frontend/src/features/modules/__tests__/moduleService.spec.ts`
- Modify: `frontend/src/views/modules/ModulesView.vue`
- Modify: `frontend/src/views/modules/__tests__/ModulesView.spec.ts`
- Modify: `frontend/src/i18n/locales/de.ts`
- Modify: `frontend/src/i18n/locales/en.ts`

**Interfaces:**
- Consumes: optional backend fields `externalSource` and `isExternalCourseLinked` from Task 7.
- Produces: visible provenance and action locks while preserving manual-task/module behavior.

- [ ] **Step 1: Extend test fixtures and write failing task-source tests**

Add this optional model:

```typescript
export interface ExternalTaskSource {
  providerKey: string
  courseName: string
  sourceUrl: string
}
```

Add `externalSource: ExternalTaskSource | null` to `StudyTask`. Update all existing task fixtures with `externalSource: null`. Add a view test with a non-null source and assert source label/link is visible, `.edit-task-button` and `.delete-task-button` are absent for that card, and `.status-button` remains present.

- [ ] **Step 2: Write failing linked-module tests**

Add `isExternalCourseLinked: boolean` to `StudyModule` and all fixtures. Assert a linked module still has an edit button, has a disabled delete button, and shows the localized explanation. Assert a manual module retains its delete dialog.

- [ ] **Step 3: Run focused view tests and observe failure**

```bash
cd frontend
pnpm exec vitest run src/views/tasks/__tests__/StudyTasksView.spec.ts src/views/modules/__tests__/ModulesView.spec.ts
```

Expected: FAIL because conditional provenance and action locks are absent.

- [ ] **Step 4: Implement task provenance and per-card action guards**

Render this only when `task.externalSource` is non-null:

```html
<p class="external-task-source">
  {{ t('tasks.externalSource.label', { course: task.externalSource.courseName }) }}
  <a :href="task.externalSource.sourceUrl" target="_blank" rel="noopener noreferrer">
    {{ t('tasks.externalSource.open') }}
  </a>
</p>
```

Guard edit and delete buttons with `v-if="!task.externalSource"`. Do not guard the status button.

- [ ] **Step 5: Implement linked-module delete protection**

Keep edit available. Disable delete when `module.isExternalCourseLinked`; do not open the confirmation dialog for a linked module. Render `.linked-module-help` with localized text explaining that deletion requires a future unsubscribe flow.

- [ ] **Step 6: Run all frontend tests and static checks**

```bash
cd frontend
pnpm exec vitest run
pnpm type-check
pnpm lint
pnpm build
```

Expected: 17 existing test files plus the new external-course tests PASS; exact total is recorded rather than predicted in the observation log.

- [ ] **Step 7: Commit existing-view integration**

```bash
git add frontend/src/features/tasks frontend/src/features/modules frontend/src/features/dashboard/__tests__/dashboardService.spec.ts frontend/src/views/tasks frontend/src/views/modules frontend/src/i18n/locales
git commit -m "feat: identify Moodle-managed tasks"
```

---

### Task 12: Document the Fixture Flow and Run Complete Acceptance Checks

**Files:**
- Modify: `README.md`
- Modify: `Docs/skill-evaluation/superpowers-observations.md`

**Interfaces:**
- Consumes: the completed backend and frontend behavior.
- Produces: reproducible local instructions, final command evidence, and experiment measurements.

- [ ] **Step 1: Add exact local fixture instructions to README**

Document the route `/moodle-courses`, the primary fixture URL, the alias URL, the fact that both resolve to one course, the manual scan action, expected `exercise-1` task, expected `announcement-1` review state, and the absence of all external network calls.

- [ ] **Step 2: Run all focused new backend suites**

```bash
dotnet test backend/tests/Domain.Tests/StudyOrganizer.Domain.Tests.csproj
dotnet test backend/tests/Application.Tests/StudyOrganizer.Application.Tests.csproj
dotnet test backend/tests/Infrastructure.Tests/StudyOrganizer.Infrastructure.Tests.csproj
dotnet test backend/tests/Api.Tests/StudyOrganizer.Api.Tests.csproj --filter FullyQualifiedName~ExternalCourseEndpointsTests
```

Expected: PASS for every new and previously green focused suite.

- [ ] **Step 3: Run the protocol backend baseline commands**

```bash
dotnet build backend/StudyOrganizer.sln
dotnet test backend/StudyOrganizer.sln
```

Expected: build PASS. Record the exact full-test result. If the same 43 JWT host-start failures remain, label them as the unchanged S0 baseline defect. Any additional failure blocks completion.

- [ ] **Step 4: Run the protocol frontend commands**

```bash
cd frontend
pnpm type-check
pnpm lint
pnpm exec vitest run
pnpm build
```

Expected: PASS for all four commands. Confirm `git diff` contains no unintended formatter changes.

- [ ] **Step 5: Perform the visible acceptance walkthrough**

Using a test account and the primary fixture link, verify and record:

```text
Moodle-Kurse opens only when authenticated.
Registration creates one course card and one linked personal module.
Now scan creates exercise-1 and marks announcement-1 as review required.
The module link opens the generated task with Moodle provenance.
The generated task status changes, while edit and delete are unavailable.
The linked module remains editable, while delete is unavailable.
Switching German/English updates the complete flow.
Registering the alias returns the existing subscription.
```

- [ ] **Step 6: Complete experiment evidence**

Update `superpowers-observations.md` with task commits, elapsed time, user corrections, failed attempts, test counts, review findings, changed-file count, line counts, and the exact command outputs. Mark personal-learning text as pending until the user supplies it.

- [ ] **Step 7: Commit documentation and evidence**

```bash
git add README.md Docs/skill-evaluation/superpowers-observations.md
git commit -m "docs: record Moodle slice verification"
```

---

## Execution Notes

- Execute tasks in order because every task consumes public types or persisted state created earlier.
- For each task, keep the red test output and final green output available to the parent agent for the experiment log.
- Run a specification-compliance review and a code-quality review after every task when using subagent-driven development.
- Do not begin Task 2 while Task 1 has unresolved review findings.
- Do not weaken database uniqueness or API authorization to make a test pass.
- Stop and return to the user if a required behavior contradicts the approved spec; do not silently broaden scope.

## Plan Self-Review

- Spec sections `Architektur` through `Fehlerverhalten` map to Tasks 1–7: explicit relational state, stable external identities, deterministic mock snapshots, conservative task creation, synchronization, review states, late subscribers, failure retention, and concurrency.
- Spec sections `API` through `Sicherheit und Datenschutz` map to Tasks 7–11: owner-scoped authenticated APIs, the complete local registration/scan UI, provenance, source-controlled action locks, allowlisted URLs, and safe error details.
- Spec sections `Deterministische Testdaten` through `Abschlussprüfungen` map to Tasks 1–12: fixed fixtures, backend/frontend test layers, documentation, the visible acceptance walkthrough, and protocol-wide verification.
- Acceptance criteria map without gaps: AC1 → Tasks 4, 10, 12; AC2 → Task 4; AC3 → Task 5; AC4 → Task 5; AC5 → Task 5; AC6 → Task 6; AC7 → Task 6; AC8 → Task 6; AC9 → Task 6; AC10 → Task 6; AC11 → Task 8; AC12 → Tasks 7 and 11; AC13 → Tasks 7 and 11; AC14 → Tasks 4, 6, 8, and 12; AC15 → Tasks 10 and 12.
- Every named production type is introduced before use. Shared test helpers have declared files and public surfaces. Commands use repository-root paths or explicitly enter `frontend`.
- No task contains an unresolved placeholder, optional product decision, real Moodle integration, scheduler, notification delivery, text parsing, or manual review approval flow.
