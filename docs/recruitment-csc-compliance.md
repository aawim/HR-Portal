# Maldives CSC Recruitment Compliance Foundation

The recruitment module begins with a source-traceable compliance registry. It does not encode unverified recruitment deadlines, assessment weights, form layouts, panel composition, or appointment powers in application code.

## Official sources to verify before activation

- CSC recruitment policy page: <https://www.csc.gov.mv/Policy/Index/51>
- Maldives Civil Service Regulation 2014 page: <https://www.csc.gov.mv/En/Regulation2014>

The CSC policy page currently lists a recruitment standard effective 1 November 2025, a Recruitment SOP, assessment guidance, screening sheets, and A2 sheets. The official Dhivehi text controls where it conflicts with a translation.

## Implemented persistence foundation

| Requirement | Storage | Safety control |
| --- | --- | --- |
| Policy framework | `RM_RecruitmentPolicyProfiles` | CSC and non-CSC profiles remain distinct. |
| Applicable policy version | `RM_RecruitmentPolicyVersions` | Effective dates, official source, verifier, approval state, and concurrency token. |
| Individual requirement | `RM_RecruitmentComplianceRules` | Official provision, rule scope, enforcement level, validation definition, responsible authority, and verification status. |
| Organisation applicability | `RM_RecruitmentPolicyAssignments` | Effective-dated organisation/type/category assignment with audit-log reference. |

Only a verified rule under an approved policy version may later be activated as a mandatory workflow gate. The next recruitment-domain phase must snapshot the assigned policy version onto each requisition and vacancy so later policy updates cannot silently alter an in-flight process.

## Deliberately pending CSC verification

The official forms and Dhivehi legal source documents must be obtained and reviewed before adding enforced values for eligibility, publication/objection deadlines, scoring, A1/A2 template mapping, panel requirements, or appointment delegations. This implementation therefore creates no legally guessed rule data and makes no claim of CSC certification.
