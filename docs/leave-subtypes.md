# Leave sub-types

Configure under **Settings → Leaves → Leave Type Management**: select a parent leave type, then edit **Leave sub-types**. The existing `Leave.Configuration.Manage` permission controls changes. Sub-types belong to the current organisation, including when their parent is shared. No duplicate parent leave type or new approval workflow is needed.

- Active allocations must add up to the parent's configured days. They partition that entitlement and never increase a staff member's parent balance.
- Manual sub-types use the **Optional / manual selection** rule.
- Certificate routing requires two sub-types: **Required** and **Without**. The server chooses between these from the actual attachment, irrespective of a submitted ID or typed certificate number.
- NCIT Sick Leave is initially configured with 15 days for each certificate category, within its 30-day parent allocation.
- If the without-certificate allocation is insufficient, the applicant must attach a certificate. PDF, PNG and JPEG uploads up to 2 MB are accepted after extension/signature checks. Files are encrypted with the application's persistent Data Protection keys. Certificate downloads require the same tenant and existing leave-request view access; they are not public files.
- Pending applications reserve both parent days and sub-type days. Cancellation/rejection releases the corresponding reservation through the existing leave state/refund flow. Early returns and date corrections release days; extensions and corrections must fit the original allocation and entitlement year.
- Allocation periods follow the current parent assignment's effective-from anniversary (employment joining date when absent). Dates must remain in the current entitlement year; selecting another year cannot reset a quota. Existing parent renewal/carry-forward checks continue to apply. Persisted parent balance IDs and quota snapshots ensure later changes cannot charge a different balance or silently expand an existing request's allocation.
- Used allocations/certificate rules cannot be rewritten. Names remain editable; existing requests retain the saved name. Unused configurations can be changed, disabled or removed from selection.

New tables: `LT_LeaveSubtypes` (tenant configuration) and `LT_LeaveSubtypeUsages` (one reservation and optional encrypted certificate per leave). The migration updates the model and DbContext without changing existing balances or classifying historical leaves. Historical applications lack reliable subtype evidence and remain unclassified; their existing deductions remain in the parent balance, which always caps new requests.

Migration: `20261030100000_LeaveSubtypes`, applied to the development database. Restart both API and UI after deployment. Keep Data Protection keys persistent and backed up alongside the database so stored certificates remain readable.
