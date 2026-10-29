# Pay frameworks and position matrices

1. **Settings → Positions → Pay Frameworks → Frameworks**: a superadministrator adds/edits shared pay frameworks. Existing LEGACY, NEW_PAY and CUSTOM codes/IDs are retained. Framework codes are permanent; names are editable.
2. **Pay Frameworks → Job Matrices**: select a framework and enter the matrix code/version, name, currency, basic salary and effective dates. Add allowed titles, unique numbered pay steps (job allowance and optional basic salary override), and other allowances/benefits. Currency is shared across the matrix amounts. Benefit frequencies are Monthly, SixMonthly or Annually; a null amount is displayed as non-cash, never converted to a guessed amount.
3. **Manage Positions → Create position matrix**: an authorised organisation officer selects a framework/Job Matrix, one allowed title and active step, approved staffing slots and effective dates. The form displays inherited pay/benefits. Saving atomically creates the organisation's `PF_PositionMatrix`, initial `PF_PositionMatrixHistory`, and the canonical `Positions` entry. Staff assignment continues to select from `Positions`.
4. **Staff workspace → Position** or staff onboarding: select the position and submit through the existing approval flow. The workspace displays its framework salary, step allowance and other benefits. Final approval rechecks the matrix and available staffing slots, then records the pay snapshot under the existing JobID.

## Permissions and history

Shared frameworks/Job Matrices are editable only by actual superadministrators. Current-organisation `Positions.Configure` enables local position-matrix creation; `Position.Details`/`Staff.Positions.Manage` allow catalogue reads. Organisation officers cannot alter a shared pay definition or another organisation's slots. The existing staff assignment/onboarding approval permissions are unchanged.

A Job Matrix is frozen once any position matrix uses it. Create a new code/version for changes rather than overwriting employment/pay history. Unused definitions can be edited, including their steps/titles/benefits. No destructive catalogue delete action is provided. A framework with active dependent matrices/positions cannot be disabled.

Both request submission and final assignment approval validate matrix ownership, active framework/title/step, and effective dates. Final approvals run within the existing serializable transaction and acquire a transaction-owned SQL matrix lock before rechecking slots. Capacity is maximum concurrent approved occupancy, rather than the count of all records overlapping a date range. This is shared across all position records linked to the matrix. Duplicate overlapping position matrices for a title/step are rejected.

An approved matrix assignment stores basic salary and job-allowance snapshots in `PF_JobStepHistory` and salary in `JobPositionBasicSalaries`. Position changes close the previous pay interval; effective-date changes synchronise the matching pay interval. No new JobID is created for the pay-step snapshot. There is no separate pay-step progression editor in this feature.

This feature configures the catalogue and assignments. It does not rewrite existing `Jobs.BasicSalary`, posted payroll, or legacy salary records. Matrix benefits are stored and inherited/displayed; this change does not automatically post those benefits to the allowance/deduction payroll subledger. Existing position allowance rules remain available separately.

## Database and deployment

Existing tables are reused: `PF_Frameworks`, `PF_JobMatrix`, `PF_JobMatrixTitles`, `PF_JobTitles`, `PF_JobMatrixSteps`, `PF_JobMatrixBenefits`, `PF_PositionMatrix`, `PF_PositionMatrixHistory`, `PF_JobStepHistory`, `Positions` and `JobPositionBasicSalaries`. Their existing keys and physical names are preserved. No new physical table is introduced.

The additive `PayFrameworkMatrixManagement` migration adds nullable `FrameworkID` and `CurrencyTypeID` to `PF_JobMatrix` with checked foreign keys. Existing rows are not assigned guessed values. A filtered unique index on `Positions.PositionMatrixID` ensures one canonical position per matrix; the migration stops on duplicates instead of deleting records. PF tables remain excluded from creation migrations and mapped against their existing schema. Apply the migration before restarting API/UI.
