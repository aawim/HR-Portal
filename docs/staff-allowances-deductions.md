# Staff allowances and deductions

## Entry points

- Staff: `/online/allowances-deductions`.
- HR: Payroll → Allowances and deductions (`/admin/payroll/finance`).
- Selected staff: staff workspace → Allowances & deductions.
- Allowance types and deduction limits use the Configuration tab on the HR page. Required position allowances have one canonical editor at Settings → Positions → Position Allowances (`/admin/settings/positions/position-allowances`).
- Received Requests and notifications open the finance request page. Assignment grants access to the assigned request, not the employee's other financial records.

## Setup

Apply migrations `20261029093000_StaffAllowancesAndDeductions` and `20261029094000_FinancePayrollCycleRequests`. They add the tables below, three permissions, six request types with numbering sequences, and database history locks. They do not grant permissions to organisations, groups or staff, copy legacy financial records, create allowance amounts or invent deduction limits.

Use existing organisation permission management, User Groups and Admin Portal Access to grant:

| Permission | Purpose |
| --- | --- |
| `StaffFinance.Configure` | Allowance catalogue, dated position rules and deduction limits |
| `StaffFinance.Manage` | Current-tenant staff finance records, HR submissions and supporting documents |
| `StaffFinance.Payroll` | Payroll preview and posting |
| `StaffFinance.Assess` | Independently set amount/currency for a pending staff allowance request |

Actual superadministrators can perform these functions in their current organisation. Ordinary employees can submit their own requests and see their own records. An officer cannot approve a request they submitted on behalf of someone else, and applicants cannot approve their own requests.

Configure tenant/Job Type approval stages and allowed state transitions for:

1. Staff Allowance Request
2. Salary Deduction Agreement Request
3. Deduction Deferral Request
4. Deduction External Payment Request
5. Allowance End Request
6. Finance Payroll Cycle Request

Use Settings → Configurations → Request approval flows and Request State Transitions. Missing workflow configuration uses the existing HR/developer escalation and staff-friendly error handling. There is no automatic approval fallback.

## Allowances

Position rules are mandatory entitlements derived directly from approved `JobPositions`, their effective dates, employment dates and the rule's effective dates. A separate manual staff assignment is unnecessary. A position's `ToDate` is exclusive; a rule/allowance's `ToDate` is the inclusive last entitlement date.

Rules specify an amount **per payment**, currency, monthly/six-monthly/annual frequency, effective period and multiple-position policy: Once (lowest stable assignment ID), Highest, or Stack. All rules for an allowance type use the same policy. Required position allowances appear in the staff view as the current month's prorated entitlement. They are independent of staff-requested allowances such as subscriptions.

Staff request an allowance type and effective period without setting an amount or currency. The API rejects staff-supplied amounts as well as hiding those fields in the form. Independent HR officers with `StaffFinance.Assess` enter an amount/currency during request review. Assessments are append-only, visible in request history and leave the original request payload unchanged. Final approval requires an assessment and snapshots its latest amount into the staff entitlement. Intermediate verification stages can proceed before assessment. Assessment does not itself approve the allowance. Officers cannot assess their own requests or requests they submitted for someone else.

Staff-requested allowances become entitlements only after final approval. They can have an optional end date. An approved Allowance End Request ends future entitlement; position rules are ended by configuration permission. Amounts and effective periods cannot overlap existing rules/entitlements. Proration uses calendar days in the payment month, to two decimal places. No currency conversion or tax calculation is performed.

Posted entries snapshot amounts, descriptions, source rule and actual `JobPositionId`. Changing catalogue labels or ending future entitlement does not rewrite posted entries.

## Deductions

An agreement has its own ID, owning Individual ID, originating Job ID, tenant, creditor/insurer BusinessEntity ID, category, reference, currency, total and instalment schedule. Job ID identifies the payroll employment; Individual ID retains ownership after employment ends. Multiple agreements are supported. Employment transfer is not automatic: an agreement is never silently moved to another job or tenant.

Enter a total, first due date, instalment count and frequency (1, 6 or 12 months). Preview displays the actual due dates and rounded amounts. The final instalment absorbs rounding. Insurance renewals require a new approved agreement; they do not silently increase the total. Agreement and external payment approvals require attached evidence. Counterparties must exist in the BusinessEntity registry; a payer does not need an employee or login account.

Policies are tenant-specific, effective-dated and use the organisation's basic-salary currency. The same currency is required across that tenant's deduction policies; there is no FX conversion. Limits include total monthly amount, percentage of basic salary, minimum take-home based on basic salary, maximum outstanding and maximum active agreements. All agreements are considered together at approval. Payroll checks the limits again and blocks excess deductions rather than silently deducting part or carrying an unapproved amount.

Deferral creates a request and, on approval, a new schedule version. The selected month must be unpaid/unprocessed; overdue earlier instalments must be resolved first. Only unpaid balances are redistributed across the remaining scheduled dates. Paid instalments, payments and previous schedule versions remain in history. If no later instalments remain or limits would be exceeded, HR must resolve the term instead of the application silently extending it.

Third-party payments record payer BusinessEntity ID, actual date, reference, amount and receipt evidence. HR verifies receipt of funds through the configured workflow. Approval allocates to the oldest unpaid instalments, reducing future deductions. Overpayment and duplicate agreement/payment references are rejected. Uploading evidence alone does not reduce debt or prove bank settlement.

## Payroll

HR selects staff, an existing tenant payroll cycle for the selected month and a payroll reference. Preview lists allowances and unpaid due deductions, including overdue instalments. Posting recalculates the preview inside a serializable transaction and verifies its fingerprint. Changed amounts require a new preview.

If no cycle exists, Payroll → Request a new payroll cycle submits a request for the selected active organisation payroll period. Final independent workflow approval creates the cycle; submission creates no cycle. Cycle tenant access comes from `OrganisationPayrollPeriods.OrganisationId`, including historical cycles whose Request organisation is empty. Period end day is in the selected payroll month; a start day other than 1 comes from the preceding month, matching the existing cross-month payroll convention. Days beyond the month's length are clamped to the last day.

Posting creates immutable payroll entries linked to the existing `PayrollCycles.RequestID`; deduction entries create payments and allocations in the same transaction. Staff receive a payroll notification. A client batch key and unique month/source indexes prevent repeated posting. A payroll officer cannot post their own entries.

These entries are the allowance/deduction subledger for the selected payroll cycle; this feature does not run bank disbursements, calculate tax or replace the entire salary engine. Approved requests are final. Changes use a separate approved request and future postings. Database triggers reject changes/deletion of posted batches, entries, payments, allocations, documents and request payloads. There is deliberately no destructive rollback migration. A financial reversal requires a separately reviewed accounting operation; editing a posted row is never offered.

Former staff have historical read access through authorised finance queries; mutations and payroll posting require active approved employment, including for superadministrators. General staff finance management is limited to the current organisation.

## Tables

Every new table uses its feature prefix. Existing tables and balances are preserved.

| Table | Key / main columns |
| --- | --- |
| `AL_AllowanceTypes` | Id, OrganisationId, Name, IsActive |
| `AL_PositionAllowanceRules` | Id, OrganisationId, PositionId, AllowanceTypeId, CurrencyTypeId, Amount, FrequencyMonths, FromDate, ToDate, MultiplePositionPolicy, CreatedByUserId, CreatedAtUtc |
| `AL_StaffAllowances` | Id, OrganisationId, IndividualId, JobId, RequestId, AllowanceTypeId, CurrencyTypeId, Amount, FrequencyMonths, FromDate, ToDate |
| `AL_AllowanceAssessments` | Id, RequestId, RequestStateId, Amount, CurrencyTypeId, AssessedByUserId, AssessedAtUtc |
| `DA_DeductionPolicies` | Id, OrganisationId, CurrencyTypeId, MaximumPerMonth, MaximumPercentOfBasic, MinimumTakeHome, MaximumOutstanding, MaximumAgreements, FromDate, CreatedByUserId |
| `DA_DeductionAgreements` | Id, OrganisationId, IndividualId, JobId, RequestId, CreditorBusinessEntityId, Reference, Category, CurrencyTypeId, TotalAmount, FirstDueDate, InstalmentCount, FrequencyMonths, ScheduleVersion |
| `DA_Instalments` | Id, AgreementId, Version, DueDate, Amount, IsSuperseded, SourceRequestId |
| `DA_Payments` | Id, AgreementId, RequestId, PayrollEntryId, PayerBusinessEntityId, Amount, PaidDate, Reference, RecordedByUserId |
| `DA_PaymentAllocations` | Id, PaymentId, InstalmentId, Amount |
| `AD_FinanceRequests` | RequestId (link to shared Requests), OrganisationId, IndividualId, JobId, Kind, PayloadJson, ClientRequestId, CreatedByUserId |
| `AD_RequestDocuments` | Id, RequestId, Name, ContentType, Content, UploadedByUserId |
| `AD_PayrollBatches` | Id, OrganisationId, Month, PayrollCycleRequestId, ClientBatchId, Reference, PostedByUserId, PostedAtUtc, SelectionJson, Fingerprint |
| `AD_PayrollEntries` | Id, BatchId, OrganisationId, JobId, IndividualId, Month, SourceKey, Kind, Description, CurrencyTypeId, Amount, InstalmentId, AllowanceId, PositionRuleId, JobPositionId |

Money uses `decimal(18,2)`. Shared Requests, workflow stages/actions, operation logs, staff notifications, positions and business entity records are reused. Existing `CurrencyTypes` is mapped read-only for this feature and excluded from migrations. Documents are authenticated downloads, restricted to PDF/PNG/JPEG, signature-checked, at most 5 MB each and ten per request. They are never served from a public web directory. File signature checks do not replace an organisation's malware scanning controls.
