# HRM.AuditLogs — Enterprise Audit Logging Subsystem

## Overview
`HRM.AuditLogs` is an independent .NET 8 class library providing automatic EF Core change-tracking auditing, an explicit audit API, server-enforced multi-tenant query scoping, sensitive-field redaction, and append-only SQL Server storage in the dedicated `[audit]` schema.

## Architecture
- **No circular dependencies:** `HRM.AuditLogs` does not reference `HRM.Api`, `HRM.UI`, or `HRM.Core`.
- **Host adapter:** `HRM.Api/Infrastructure/HrmAuditContextAccessor.cs` implements `IAuditContextAccessor` using `IUserAccessService`, `IHttpContextAccessor`, and `OrganisationalAccessScopeResolver`. Tenant and organisation context is always resolved server-side.
- **Dedicated Database & DbContext:** `HRM.AuditLogs.Persistence.AuditDbContext` targets the dedicated **`HRMAuditLogs`** database (`ConnectionStrings:AuditLogsConnection`, defaulting automatically to `InitialCatalog = HRMAuditLogs` on the same SQL Server as `DefaultConnection`) with schema `audit` (`audit.AuditLogs` and `audit.AuditLogDetails`) and migration history table `[audit].[__EFMigrationsHistory_AuditLogs]`.
- **Transaction safety:** When sharing the same database, `AuditSaveChangesInterceptor` enlists in the same SQL Server `DbTransaction`; when targeting the dedicated `HRMAuditLogs` database, mandatory audit failures propagate and roll back the business `HrmTeContext` transaction so protected business writes never silently commit without their audit trail.

## Configuration (`appsettings.json`)
```json
{
  "AuditLogs": {
    "ConnectionStringName": "DefaultConnection",
    "Schema": "audit",
    "EnableAutomaticAuditing": true,
    "FailOnMandatoryAuditError": true,
    "DefaultSource": "HRM.Api",
    "DefaultRetentionDays": 2555,
    "MaxValueLength": 4000,
    "MaxMetadataLength": 8000,
    "MaxPageSize": 200,
    "MaxExportRows": 5000,
    "CaptureIpAddress": true
  }
}
```

## Database Migration & Immutability
Apply either the EF Core migration or the idempotent SQL deployment script:
```powershell
# Option 1: EF Core migration (dedicated AuditDbContext)
dotnet ef database update --project HRM.AuditLogs --startup-project HRM.Api --context HRM.AuditLogs.Persistence.AuditDbContext

# Option 2: SQL script
# Run HRM.Api/Database/Scripts/20261121_AuditLogsSchema.sql
```
Both options create `[audit].[AuditLogs]`, `[audit].[AuditLogDetails]`, composite indexes, `DeleteBehavior.Restrict` FK, and `INSTEAD OF UPDATE, DELETE` append-only triggers (`TR_AuditLogs_AppendOnly` and `TR_AuditLogDetails_AppendOnly`).

For production least-privilege hardening, run as DBA:
```sql
GRANT INSERT, SELECT ON SCHEMA::[audit] TO [dhr_app_user];
DENY UPDATE, DELETE ON SCHEMA::[audit] TO [dhr_app_user];
```

## API Endpoints (`/api/v1/audit-logs`)
- `GET /api/v1/audit-logs` — Paginated search with server-side tenant/division scoping, filters, and sorting
- `GET /api/v1/audit-logs/{id}` — Record details with Old/New diffs (confidential fields masked unless authorised)
- `GET /api/v1/audit-logs/entity/{entityType}/{entityId}` — Entity change history
- `GET /api/v1/audit-logs/user/{userId}` — User activity history
- `GET /api/v1/audit-logs/organisation/{organisationId}` — Organisation history (cross-org requires explicit permission and is itself audited)
- `GET /api/v1/audit-logs/correlation/{correlationId}` — Correlation trace search
- `GET /api/v1/audit-logs/export` — Authorised CSV export (with formula-injection neutralization and export auditing)

## UI
- Admin page: `/admin/settings/audit-logs` (`HRM.UI/Components/Admin/Settings/Audit/AuditLogsPage.razor`)
- Reusable contextual history panel: `<ContextualAuditHistoryPanel EntityType="..." EntityId="..." />` (`HRM.UI/Components/Shared/ContextualAuditHistoryPanel.razor`)
