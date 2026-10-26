# Unified leave types

The application has one leave-type catalogue at `/admin/settings/leaves/leave-types`, implemented by `LeaveTypeCatalogueService`. The policy editor, staff entitlements, leave sets, Job Type accrual, carry-forward, applications, changes, attendance and receipts use the same `LeaveTypeId`.

## Data model and upgrade

Migration `20261026054857_UnifiedLeaveTypes` renames the existing `LeaveTypes` table in place to `LT_LeaveTypes`. Existing type IDs, names, ownership, defaults, foreign-key references, leave history and balance amounts are preserved. The canonical catalogue adds `Code`, `Description`, `IsActive`, `CreatedDate` and `UpdatedDate` from the policy-based design. Existing explicitly mapped definitions retain their codes and descriptions; other existing types receive a stable `LT-{id}` code. Names always come from the existing types. A new definition without an existing mapping is retained as an independent canonical type, rather than silently discarded or matched by name.

The retired `LeaveDefinitions`, `LeaveTypeMappings` and `LeaveFrameworkConfigurations` tables are removed after their records have been captured in `LT_TypeMigrationAudits`. That table contains `Id`, `SourceEntity`, `SourceId`, nullable canonical `LeaveTypeId`, `SnapshotJson` and `MigratedAtUtc`. It also holds the original type, affected assignment and policy snapshots. No runtime mapping or framework switch remains. Historical applied migrations are unchanged.

`JobLeaveTypes.LeaveDefinitionID` is converted to the canonical `LeaveTypeID` and removed. `LeavePolicies.LeaveDefinitionID` becomes `LeaveTypeID`. Unchanged existing balance rows are checked inside the upgrade transaction, including their exact amounts and validity flags; historical leave rows are also checked. Ambiguous definition mappings or conflicting codes abort the migration and roll back. Same-name types and separate entitlement rows are never automatically merged or added together.

The old type CRUD service and editor, mapping service, framework resolver, and legacy staff-assignment panel are removed. Former editor URLs redirect to the single Settings editor. Inactive types remain visible for management and history, but cannot be newly assigned. New types receive a General reason so they can be used once their policy and approval flow are configured.

## Rule ownership

- Type catalogue: identity, name, scope and active state; preserves existing rule defaults for dates without an effective organisation policy.
- Organisation policy: effective-dated application rules, including holiday counting, pay, location, eligibility, request routing and work handover.
- Job Type configuration: accrual, renewal, caps and carry-forward. The policy editor links here instead of presenting a second, non-posting accrual editor. Historical policy-accrual metadata is retained, with no user-facing writer.
- Leave sets: membership by canonical type ID; assigning a set to a Job Type does not itself grant days.

Applications resolve rules for their start date. The form reloads rules when the date changes; the server resolves and validates them again on submission. Multiple applicable policies are rejected rather than silently choosing one. Resolving a policy never writes its values back onto the type record. Financial processing uses exact type IDs; a same-name different type can trigger a reconciliation warning, never an automatic conversion.

## Deployment and verification

Do not start the web app to run schema checks: its hosted accrual worker can post grants.

1. `dotnet run --project tools/DatabaseBaseline -- --verify-leave-type-upgrade` creates a SQL Server COPY_ONLY backup with checksum, verifies it, restores a uniquely named isolated database, rehearses the migration, checks preservation and an unmapped-definition fixture, and repeats the upgrade. Only the isolated database is dropped; the verified backup is retained. For repeat rehearsals, `HRM_LEAVE_UPGRADE_BACKUP` may point to that pre-upgrade backup.
2. `dotnet run --project tools/DatabaseBaseline -- --verify-empty-schema` checks current-model fresh provisioning and repeat upgrades in a separate isolated database.
3. `dotnet run --project tools/DatabaseBaseline -- --apply-unified-leave-types` applies only the reviewed consolidation migration; it refuses unrelated pending migrations and verifies that no retired tables remain.
4. Restart the application using the rebuilt code after the schema upgrade. Automatic Down is disabled because reverting the single-identity model requires restoring the verified backup with the matching earlier application version.

Regression coverage includes canonical identity, scope, global types, duplicate codes/assignments, inactive types, audit creation, effective dates, policy ambiguity, and existing leave, attendance, workflow, accrual, carry-forward and receipt tests.

## Related policy table naming

The following naming migration renames `LeavePolicies` to `LT_LeavePolicies` and `LeavePolicyAccrualRules` to `LT_LeavePolicyAccrualRules` in place. All four type-configuration tables share the established `LT_` feature prefix. EF mappings, the model snapshot and the idempotent standalone script `20261026_PrefixLeavePolicyTables.sql` agree. Table object IDs, row counts, foreign keys, constraints and indexes are retained. Previously applied consolidation scripts and migrations are unchanged.
