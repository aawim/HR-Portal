# HRM.ActivityLogs — Enterprise Activity Logging Architecture

## 1. Purpose & Separation from `HRM.AuditLogs`
`HRM.ActivityLogs` is an independent class library project inside `HRM.sln` dedicated to recording **operational user and system business activities** ("what the user did" — timelines, recent workspace activity, workflow transitions, approvals, sign-outs, payroll initiation).

It is intentionally separate from `HRM.AuditLogs` (which captures compliance-grade, field-by-field before/after entity diffs):
- **Separate Project:** `HRM.ActivityLogs` (no reference to `HRM.Api`, `HRM.UI`, or `HRM.AuditLogs`)
- **Separate Database / Schema:** Dedicated SQL Server database `HRMActivityLogs`, schema `[activity]` (`[activity].[ActivityLogs]` and `[activity].[ActivityOutbox]`)
- **Separate Lifecycle:** Bounded asynchronous channel + transactional outbox + configurable category retention cleanup (`ActivityRetentionProcessor`)

---

## 2. Architecture Overview

| Layer | Components |
|---|---|
| **Abstractions** | `IActivityLogService`, `IActivityLogQueryService`, `IActivityContextAccessor`, `IActivityClock` |
| **Models** | `ActivityLog`, `ActivityOutboxMessage`, `ActivityLogRequest`, `ActivityLogFilter`, `ActivityLogDto`, `ActivityLogPageResult`, `ActivityCategory`, `ActivityTypes`, `ActivityLogOptions` |
| **Security** | `ActivitySanitizer` (strips sensitive keys like salary/password/token, sanitizes display names, computes one-way SHA-256 opaque `SessionId`), `ActivityAccessPolicy` (enforces server-side Individual / Team / Division / Organisation / Cross-Org / SuperAdmin boundaries) |
| **Persistence** | `ActivityDbContext`, `ActivityLogConfiguration`, `ActivityOutboxMessageConfiguration`, EF Core migration `20261121110000_InitialActivityLogsSchema`, `ActivitySchemaBootstrapper` |
| **Processing** | `ActivityQueueChannel` (bounded `Channel<ActivityLog>`), `ActivityEventProcessor` (single shared `BackgroundService` draining queue + polling outbox with exponential backoff & `EventId` idempotency), `ActivityRetentionProcessor` (category & default retention cleanup) |
| **API Host** | `HrmActivityContextAccessor`, `ActivityLogsController` (`/api/v1/activity-logs/*`) |
| **UI Host** | `IActivityLogApiClient`, `ActivityLogApiClient`, `ActivityLogsPage.razor` (`/admin/settings/activity-logs`), `ContextualActivityFeed.razor` |

---

## 3. Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "ActivityLogsConnection": "Server=...;Database=HRMActivityLogs;..."
  },
  "ActivityLogs": {
    "Enabled": true,
    "UseAsynchronousQueue": true,
    "QueueCapacity": 2048,
    "BatchSize": 64,
    "OutboxPollIntervalSeconds": 5,
    "OutboxMaxAttempts": 5,
    "DefaultRetentionDays": 180,
    "OutboxRetentionDays": 14,
    "RetentionSweepIntervalHours": 6,
    "CategoryRetentionDays": {
      "Authentication": 90,
      "Navigation": 30,
      "Payroll": 365
    }
  }
}
```
If `ConnectionStrings:ActivityLogsConnection` is omitted, `HRM.Api/Program.cs` defaults to the same SQL Server instance as `DefaultConnection` with `InitialCatalog = "HRMActivityLogs"`.

---

## 4. Recording Business Activities

Inject `IActivityLogService` into any application service:

```csharp
await _activityLogs.RecordAsync(new ActivityLogRequest
{
    ActivityCategory = ActivityCategory.Payroll,
    ActivityType = ActivityTypes.PayrollRunInitiated,
    Module = "Payroll",
    Action = "INITIATE",
    Description = $"Initiated payroll run '{run.Title}'.",
    TargetEntityType = nameof(PayrollRun),
    TargetEntityId = run.PayrollRunId.ToString(),
    TargetDisplayName = run.Title,
    RequireGuaranteedDelivery = true
}, cancellationToken);
```

- **Never pass salary figures, credentials, or tokens** in `Description` or `Metadata`. `ActivitySanitizer` automatically drops sensitive metadata keys.
- **Server-derived identity:** `TenantId`, `OrganisationId`, `DivisionId`, `UserId`, `ActorType`, `CorrelationId`, and opaque `SessionId` are resolved on the server by `HrmActivityContextAccessor`.

---

## 5. Authorization & Tenant Isolation

| Endpoint | Required Scope |
|---|---|
| `GET /api/v1/activity-logs/my` | Authenticated user (own activities only) |
| `GET /api/v1/activity-logs` | Organisation / Division / Team / Own (server-filtered) |
| `GET /api/v1/activity-logs/{id}` | Must belong to caller's authorized scope |
| `GET /api/v1/activity-logs/employee/{userId}` | Self, or Admin/HR/Supervisor within authorized organisation |
| `GET /api/v1/activity-logs/organisation/{orgId}` | Authorised organisation member with Org Activity permission, or SuperAdmin |
| `GET /api/v1/activity-logs/team/{teamId}` | Authorised team supervisor/member with Team permission, or Org Admin/HR |
| `GET /api/v1/activity-logs/entity/{type}/{id}` | Scoped to caller's authorised organisation/division/team |
| `GET /api/v1/activity-logs/export` | `CanExportActivities` (Admin/HR Lead/`Activity.Export`), CSV formula-injection protected, export itself logged |

---

## 6. Deployment & Migrations

- **Automatic startup bootstrap:** `ActivitySchemaBootstrapper.EnsureSchemaAsync(app.Services)` ensures `HRMActivityLogs` exists and applies pending EF Core migrations on startup.
- **CLI migration:**
  ```powershell
  dotnet ef database update --project HRM.ActivityLogs/HRM.ActivityLogs.csproj --startup-project HRM.Api/HRM.Api.csproj --context HRM.ActivityLogs.Persistence.ActivityDbContext --configuration Release
  ```
- **SQL script:** `HRM.Api/Database/Scripts/20261121_ActivityLogsSchema.sql`
