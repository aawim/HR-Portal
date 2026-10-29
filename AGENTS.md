# Repository instructions

Codex Instructions — HRM Solution

Primary Goal

Minimize token consumption and unnecessary file reads while maintaining code correctness.

File Reading Rules

Do not scan or read the entire solution unless explicitly requested.

Start by checking git status --short and identifying relevant changed files.

Use filenames, directory listings, symbol searches, and targeted text searches before opening files.

Read only files directly relevant to the requested task.

For large files, read only relevant methods, classes, or line ranges.

Do not repeatedly read a file if its relevant content is already available in the current session.

Do not read unchanged files unless they are required to understand dependencies, interfaces, or affected behavior.

Avoid reading generated code, build output, binaries, or unrelated project files.

Do not automatically inspect every controller, service, interface, or DTO.

Expand the search scope only when the current information is insufficient.

Git-Based Workflow

Use git status --short to identify uncommitted changes.

Use git diff --name-only to identify unstaged modified files.

Use git diff --cached --name-only to identify staged changes.

Use git diff --name-only <base>...HEAD when reviewing a branch against a specified base.

Never assume unchanged files are irrelevant to the task.

Do not modify unrelated files.

HRM Project Architecture

HRM.UI communicates with HRM.Api through HTTP clients.

HRM.UI references Application interfaces.

HRM.Api implements backend application services.

HRM.Infrastructure handles persistence.

HRM.Domain contains domain models.

Preserve existing architecture unless changes are explicitly requested.

Code Modification

Make minimal, targeted changes.

Preserve public interfaces unless necessary.

Prefer existing services and utilities.

Avoid unnecessary refactoring.

Do not regenerate entire files for small modifications.

Run targeted tests for the affected project.

Run full-solution tests only when necessary or explicitly requested.

Response Style

Keep explanations concise.

Summarize changed files and verification results.

Do not repeatedly explain unchanged architecture.

Report any relevant validation that was not performed.

Context Efficiency

Reuse information already discovered in the current session.

Avoid repeated repository-wide searches.

Avoid reading large documentation files unless needed.

Provide git comments after every change, implementation.

Ask before undertaking a broad architectural review not required by the task.












## Database table names

- Every new physical table must have an uppercase feature-initial prefix, an underscore, and a descriptive PascalCase plural name, for example `LC_LateCheckInRequests`.
- Related tables share their feature prefix. Use the established prefixes documented in `Database/Scripts/TableNaming.md` and `WF_` for workflow tables.
- Keep EF mappings, raw SQL, migrations, deployment scripts and documentation consistent with the physical table name.
- Preserve already-applied migrations; introduce a data-preserving rename migration for existing tables. Do not create replacement tables or discard existing records to change a name.
