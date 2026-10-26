# System gap implementation — 25 October 2026

Scope: the thirteen findings in `system-gap-review-20261025.md`. DNR lookup is deliberately deferred at the user's request. No sample identity is returned. The web application was not started during this work, avoiding its live accrual workers.

## Delivered changes

| Gap | Resolution |
| --- | --- |
| G01 | Leave configuration uses a permission-checked, tenant-scoped service for reads, saves and deletes. Ownership, reference values and duplicates are validated server-side; changes are audited. |
| G02 | Shared Job Type/Request Type catalogue changes require Super Admin authority and are audited. Tenant policy settings remain separate. |
| G03 | Final position assignment/change approvals retain the resulting JobPosition–Request link. Attendance resolves actual approval dates for both old and new request types and does not backdate absence. |
| G04 | Manual individual registration creates BusinessEntity/Individual identity records with duplicate checks and optional addresses/contacts through existing address logic. It creates no employment or login account and does not falsely mark identities verified. DNR lookup is unavailable until the real API is supplied. |
| G05 | Explicit audited legacy-baseline tooling verifies existing tables/columns/keys before recording migration history. Standard EF upgrades now succeed. A separate guarded provisioner supports an empty database, and isolated SQL Server tests exercise fresh deployment, additive upgrade, preservation of a catalogue record and repeated upgrade. |
| G06 | Emergency admin revoke closes outstanding grants/workflow stages, invalidates access and records audit/notifications without waiting for another approver. An outstanding grant cannot later reactivate access. |
| G07 | One approval-flow editor remains under Settings → Configurations → Request Approval Flows. The former Job Type writer refuses writes. The explicit legacy upgrade copies existing routing to canonical tenant/Job Type definitions without changing in-flight instances. |
| G08 | Position periods include FromDate and exclude ToDate. Attendance, context, profile, overview, selectors and management use that boundary. Changing position ends the predecessor on the successor's start date; overlap validation follows the same boundary. |
| G09 | OT claims enforce tenant/Job Type eligibility and cumulative daily/weekly limits against pending plus approved claims in serializable transactions. Completed paired attendance outside official time supports the claim. Assigned workflow stages approve/reject; applicants cannot approve themselves. Approval retains a policy snapshot; authorised staff can record an external payroll/payment reference once. Raw attendance remains intact. |
| G10 | Generic request reads require tenant/resource authority. Generic state mutation is retired; business actions must use their domain workflow validation. |
| G11 | Added gap/domain/search regression cases and Windows CI for builds, dependency checks, regression coverage, source inventory and isolated SQL Server deployment. Existing duty/dashboard fixtures now reflect multiple duties, official defaults and the current UI. |
| G12 | Canonical staff search debounces input, cancels superseded queries, rejects stale query/organisation results, normalises whitespace and preserves tenant/active-position restrictions. |
| G13 | Removed unused JobStore, LeaveDataLoader, the old unscoped shared search component and unused unimplemented service contracts/DI registrations. Unsupported navigation is hidden; direct unsupported pages show an unavailable state. |

## Configuration and feature inventory

- Manual registry registration: implemented under HR → Individuals → Add New. DNR: deferred/unavailable. Addresses and contacts remain optional; employment/account creation follows onboarding approval.
- Approval flows: canonical editor under `/admin/settings/config/approval-flows`. Explicit tenant/Job Type/Request Type configuration and permitted transitions remain required. Level 0 remains reserved. No default approval authority was invented for overtime.
- Overtime: implemented under `/online/ot` and `/online/overtime-requests`; pending actions also appear in the workspace. Potential worked time, pending claims, approved claims and paid records remain distinct. Recording a payment reference does not transfer funds or calculate salary rates.
- CSC fetch/classification/designation/office integration, HR Requests Overview, Attendance Clients and unimplemented Budget/Payroll settings: unavailable navigation is hidden. This cleanup does not invent the unfinished external integrations or full payroll product.
- Position Overview continues to separate the implemented legacy catalogue from the explicitly unavailable future position-management subsystem.

## Database and permissions

Applied additive migrations:

- `20261025120600_SystemGapHardening`: new `OT_OvertimeRequests`, OT request type/numbering and explicit permissions, compatible legacy schema repair.
- `20261025120700_PreserveTransferHistory`: restrictive transfer relationships, including installations with differently named legacy constraints. No history deletion.

New table `OT_OvertimeRequests` columns: `RequestId` (PK/FK), `OrganisationId`, `IndividualId`, `JobId`, `WorkDate`, `WeekStart`, `Minutes`, `Reason`, `ApprovedPolicyJson`, `CreatedAtUtc`, `PaidAtUtc`, `PaidByUserId`, `PaymentReference`. Minutes have a database check; supporting tenant/person/date and FK indexes are included. Existing `JobPositionsRequests` retains position approval linkage; no replacement table was created.

New permissions: `Leave.Configuration.Manage`, `Individual.Add`, `Overtime.Review`, `Overtime.Pay`. They are catalogue entries only: organisation permission ceilings, User Group grants and direct grants still apply. Payment recording requires `Overtime.Pay` or Super Admin, and cannot be performed for one's own claim.

Legacy flow precedence: a canonical explicit tenant/Job Type/Request Type flow wins. Compatibility resolution remains read-only for unmigrated/future-effective legacy definitions, using tenant before global, Job Type before generic, then highest version; ambiguity blocks rather than guessing. Currently applicable legacy routes are copied by the deployment utility. Original definitions and active request instances are preserved. Imported named-person approvers retain their existing routing; editing/replacing a flow uses User Groups in the canonical editor.

## Deployment procedure

Use the same protected connection configuration as the application's design-time factory; never print credentials. Tools do not start the application or hosted workers.

1. Build the application and `tools/DatabaseBaseline/DatabaseBaseline.csproj`. Ensure the tool loads the newly built HRM assembly when using a separate output directory.
2. Existing legacy database: run the baseline tool without arguments to verify tables/columns/keys. Use `--repair-compatible-schema --apply` to perform reviewed additive compatibility repairs and record the verified baseline with audit. A mismatch refuses the baseline. Preserve an operational database backup before deployment.
3. Apply normal EF migrations through `20261025120700_PreserveTransferHistory`.
4. Run the tool with `--canonicalize-legacy-flows` to copy applicable legacy routes. Repeating it adds no duplicate canonical flows. Ambiguous source routing rolls the operation back.
5. New empty database: run `--provision-empty`. This refuses any database containing tables, creates the current model and installs feature metadata/migration history. Organisation/reference catalogues and the first privileged account still require normal deployment provisioning; no fictional staff are seeded.
6. Run `--verify-empty-schema` for an isolated SQL Server rehearsal. Only a generated `HRM_Integration_<GUID>` database is created/deleted, with an exact-name guard. Tests never run the application against the live tenant database.

## Verification

- Application warnings-as-errors build and CSS compilation passed.
- Targeted tests include tenant configuration/catalogue denial, global identity duplicates, group history/stale edits, actual position final approval/attendance linkage, exclusive end dates, generic request bypass denial, OT submission/caps/self-approval/final approval/payment idempotency, inherited shared-schedule precedence, and search race/selector checks.
- Isolated SQL Server fresh installation, data-preserving additive upgrade and repeated upgrade passed. The configured legacy database also completed normal EF upgrade; one applicable legacy flow was copied without modifying active instances.
- Final `tools/verify-project.ps1` passed: 29 regression groups, zero build warnings/errors, CSS build, npm audit (zero vulnerabilities), NuGet advisory audit (no vulnerable packages reported), and source inventory (no removal candidates). `git diff --check` passed. A successful source/build/regression audit is not a penetration-test certification.
