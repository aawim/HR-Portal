# Requested legacy table usage audit

Scope: the eighteen tables named by the user on 29 October 2026. Application searches covered runtime services, application contracts, UI, Core models, DbContext mappings and tests. Database checks covered row counts, incoming/outgoing foreign keys, SQL expression dependencies, SQL module text and target triggers. Historical applied migrations were preserved.

## Decisions

| Original table | Rows at audit | Action / final table | Evidence |
| --- | ---: | --- | --- |
| AdditionDeductionTypeDependencyTimePeriodAmounts | 0 | Retired | No model, runtime consumer, foreign key or SQL module dependency |
| AdditionOrDeductionTypes | 2 | `PY_AdditionOrDeductionTypes` | Referenced by legacy payroll item definitions and processing-code FKs; 43 PayrollItemTypes records exist |
| BreakTimes | 36 | `BT_BreakTimes` | Legacy shift history: 78 WorkShiftsBreakTimes and 14 AttachedBreakTimesDays records reference this catalogue |
| BudgetItems | 228 | `BG_BudgetItems` | Existing budget-code catalogue and parent of OrganisationBudgets; preserve the codes and the database relationship |
| BudgetTransactions | 0 | Retired | No model/runtime consumer or incoming dependency; no transactions exist |
| BudgetTransactionTypes | 4 | Retired after transactions | Only the retired BudgetTransactions table referenced it; all four lookup records archived |
| BusinessEntitiesDocuments | 2 | `BE_BusinessEntityDocuments` | Organisation documents, former-staff history and document ownership protection |
| BusinessEntityCalendars | 1 | `BE_BusinessEntityCalendars` | Organisation calendar settings and leave-planning calendar ownership |
| BusinessEntityLocations | 927 | `BE_BusinessEntityLocations` | Staff/organisation address management, registry and former-staff protections |
| BusinessEntityLocationTypes | 4 | `BE_BusinessEntityLocationTypes` | Address usage options and address validation; also referenced by OnlineBusinessEntityLocations |
| BusinessEntityRelatedLocationTypes | 0 | Retired | No model/runtime consumer or outside incoming dependency |
| BusinessEntityRelationAssignedRoles | 0 | Retired | No model/runtime consumer or outside incoming dependency |
| BusinessEntityRelations | 0 | Retired after its two children | No model/runtime consumer; only retired requests/role-assignment tables referenced it |
| BusinessEntityRelationStates | 11 | Retired after relations | Only the retired BusinessEntityRelations table referenced it; all eleven lookup records archived |
| BusinessEntityRequests | 0 | Retired | No model/runtime consumer or outside incoming dependency |
| BusinessEntitySchedules | 392 | `BE_BusinessEntitySchedules` | Active leave-planning calendar schedules |
| BusinessEntityTypes | 2 | `BE_BusinessEntityTypes` | Business entity registration, individual creation and organisation creation; parent of the live registry |
| BussinessEntityContactInformations | 641 | `BE_BusinessEntityContacts` | Staff/organisation contacts, profiles, registration and former-staff history |

Counts are an audit snapshot, not hard-coded future assumptions. No new payroll, budget or shift CRUD was invented as part of cleanup. The three retained legacy catalogues have a different status from the seven actively used business entity tables: their linked data/schema is preserved rather than pretending those historical features are unused or replacing their data with the new financial implementation.

## Code changes

The eight retired tables already had no Core models, DbSets, model-builder entities or navigations in the current application. There were no remaining corresponding files or navigations to delete.

The eight retained mapped tables now use explicit prefixed physical mappings in `HrmTeContext.RetainedLegacyTables.cs`. Existing C# model/DbSet names and application interfaces remain stable. Existing keys, columns, foreign keys and index names are preserved. The other two retained catalogues remain legacy database dependencies outside the current EF model; their in-place rename keeps all references intact.

## Recovery evidence and safeguards

Migration `20261029155305_RetireUnusedLegacyTables` runs the embedded `20261029_RetireUnusedLegacyTables.sql`. Its transaction:

1. Refuses source/destination name collisions, missing retained tables, outside incoming FKs, SQL module references and target triggers.
2. Takes transaction-held table locks before inspecting/counting data.
3. Refuses retirement if any of the six operational candidates contains records.
4. Archives every retirement candidate into `SC_TableRetirementAudits`: original table name, record count, full row JSON, column/type/default/computed/identity metadata, indexes, foreign keys, checks and permission metadata. JSON validity and archived row count are verified before any drop.
5. Renames ten retained tables with `sp_rename`, checking row counts and object IDs after each rename. Existing FK and index object identities are retained.
6. Drops eight unused tables in dependency order. All fifteen retired lookup rows remain in recovery evidence.
7. Protects retirement evidence from UPDATE/DELETE with an immutable-history trigger.

The script is transactional and repeatable. A reappearing previously archived source table requires review instead of silently overwriting recovery evidence. `Down` refuses destructive rollback; restoration uses a reviewed forward recovery migration based on the archived metadata/data, respecting the retained-table rename manifest above.

For recovery inspection, authorised database administrators can read `SC_TableRetirementAudits` using `MigrationId='RetireUnusedLegacyTables'`. It is not exposed as staff-facing CRUD or through public application endpoints. This archive is supplementary recovery evidence, not a replacement for normal database backups.

Only this repository and this connected database were audited; independently deployed applications, external reporting tools or jobs outside the database are not discoverable from those sources. Coordinate their table-name changes before deploying elsewhere. Restart the API/UI together after the schema rename; older binaries still expect the original physical names.
