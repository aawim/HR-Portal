# Work handover rules

Configure rules at **Admin → Settings → Leaves → Work Handover Rules** (`/admin/settings/leaves/work-handover-rules`). This is the only handover-rule editor. The leave-policy editor links here.

## Organisation default

1. Select **New rule** and enter a descriptive name.
2. Choose **Organisation** under Applies to.
3. Keep **All leave types**, or uncheck it and choose one or more types. A rule can cover both the global and local annual type without duplicating its threshold.
4. Choose all Job Types or one Job Type.
5. Set **Require handover** and the threshold. **More than 2 days** means 2 does not require handover and 3 does; 0 requires handover for any positive leave duration.
6. Set the effective period, keep Enabled, and save.

Days are the days charged under the effective leave policy, including or excluding holidays according to that policy. The rule is resolved for the leave start date.

The existing NCIT annual requirement is imported as one organisation default with a 2-day threshold, covering annual types 1 and 66. Other existing active policy settings are preserved as type-specific defaults. Existing applications and balances are not rewritten.

## Department or team override

Create another rule, choose **Department / structure** or **Team**, and select the target. Only approved current-tenant structures and valid current-tenant teams are offered. Optionally restrict the rule to selected leave types and a Job Type.

For example, set an organisation annual default to more than 2 days, then a Support team override to more than 5 days. A member's 4-day annual leave needs handover under the organisation default but does not under the Support override. **Not required** explicitly exempts matching staff. To remove an override, edit it and choose **Inherit default**, or disable it; no new rule is needed simply to inherit.

Rule order is:

1. Matching team, then approved department/structure, then organisation.
2. Within that scope: selected leave types before all types, then a specific Job Type before all Job Types.
3. Within equal specificity: higher Priority wins.

For staff in multiple teams, use distinct priorities when conflicting rules can both apply. Equal winning rules are rejected rather than guessed. Overlapping rules with the same scope, filters, priority and period cannot be saved. Team membership and team validity are checked for the leave start date. Other tenants' rules and memberships cannot participate.

## Preview and staff flow

Open **Preview a staff member’s rule**, find and select the staff member, select their leave type and start date, then select Preview. The result names the effective rule and threshold; conflicting matching rules are explained to the administrator.

The staff/HR leave application displays the applied rule. Increasing the duration past its threshold displays the handover task editor. Staff must assign eligible recipients and task details; existing recipient verification, supervisor approval, request-state transitions and final HR approval gates remain in effect. The server independently re-evaluates the rule and day count, so hiding the form or changing client values cannot bypass it.

New submissions and returned applications being resubmitted store the applied rule ID, version, scope description, threshold and counted days in `Leaves.HandoverRuleSnapshot`. Updating settings does not rewrite submitted or approved records. Normal leave-change and approval workflows remain responsible for previously approved leave. A pending application can be returned as Incomplete through its allowed workflow and resubmitted to evaluate the new rules.

## Storage, permissions and deployment

- `LH_LeaveHandoverRules`: tenant, scope and target, optional Job Type, all/selected types, requirement, threshold, priority, effective period, Enabled, Version and audit reference.
- `LH_LeaveHandoverRuleTypes`: selected type links, retained and marked inactive when removed.
- `Leaves.HandoverRuleSnapshot`: nullable JSON; existing rows are retained unchanged.

The tables share the established `LH_` prefix. Migration `20261026071944_ScopedLeaveHandoverRules` creates them and imports existing settings. Old policy handover columns remain historical metadata and are no longer editable or consulted by application rule resolution.

Configuration requires Super Administrator, Admin or Leave.Configuration.Manage permission and access to the current organisation. Saves are serializable, audited and protected against stale edits. Super Administrators use the existing organisation context switch; other administrators remain in their current organisation.

Use `--verify-scoped-handover-upgrade` with the DatabaseBaseline tool to create a verified backup, restore an isolated copy, migrate and verify exact balance/leave preservation and the annual threshold. `--verify-empty-schema` checks fresh provisioning. `--apply-unified-leave-types` permits this reviewed migration alongside the prior leave migrations. None of these commands start the application or its accrual worker.

Tests cover threshold boundaries, staff/HR rule lookup, server enforcement before financial writes, default/department/team precedence, inheritance, exemptions, type/Job Type filters, dates, expired memberships, multiple-team ambiguity, priorities, snapshots, stale saves and tenant/permission boundaries.
