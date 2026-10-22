# Repository instructions

## Database table names

- Every new physical table must have an uppercase feature-initial prefix, an underscore, and a descriptive PascalCase plural name, for example `LC_LateCheckInRequests`.
- Related tables share their feature prefix. Use the established prefixes documented in `Database/Scripts/TableNaming.md` and `WF_` for workflow tables.
- Keep EF mappings, raw SQL, migrations, deployment scripts and documentation consistent with the physical table name.
- Preserve already-applied migrations; introduce a data-preserving rename migration for existing tables. Do not create replacement tables or discard existing records to change a name.
