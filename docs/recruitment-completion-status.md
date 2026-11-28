# Recruitment completion status

This is a delivery tracker, not a declaration of CSC compliance.

| Area | Status | Remaining verification/work |
| --- | --- | --- |
| Applicant portal | In progress | Complete vacancy/apply UI, profile retrieval, drafts, corrections, withdrawals and document flow. Authenticated non-employee applicant boundary enabled. |
| Internal UI | Initial workspace | Complete screening, assessment, results, objections, offers and onboarding screens; width preference and localisation. |
| Scoring | In progress | Criterion bounds and calculated-total checks implemented against verified templates. Verify source transcription, panel membership, moderation, component aggregation, pass marks and statutory tie breaks. Existing average-score ranking is not a verified CSC merit calculation. |
| Deadlines | Pending | Effective-dated provision verification, Maldives working-day/holiday calendar, minimum announcement and objection periods, assessment notice and publication deadlines. |
| Official outputs | Pending | Import original form bytes, populate A1/A2/checklists without altering layouts, render and verify Dhivehi RTL exports. |
| Document security | Pending | Applicant ownership and organisation boundaries, approved file types, malware quarantine/scan results, immutable versions, retention and legal hold. |
| Notifications | Pending | Transactional outbox, retry/idempotency, templates, provider integration and delivery audit. |
| Testing | In progress | Criterion integrity tests added. Still requires real endpoint/organisation isolation, offer acceptance, onboarding linkage, migration and full journey tests. |
| Deployment | Pending | Staging rehearsal, configuration validation, backup/restore evidence, scanner and notification providers. |
| Permissions | Catalogue code added | Synchronise database catalogue and use independent authorised assignment for A747888. No self-grant bypass. |

Known integrity gaps to close before release: application-ID tie break is provisional; upheld objections must trigger corrected publication before finalisation; onboarding must return an exact request ID rather than look up the latest matching request; appointment authority needs explicit delegation validation.
