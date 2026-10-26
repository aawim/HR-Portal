# Organisation Document Types

Canonical management page: `/admin/settings/org/document-types` (Settings → Organisation → Document Types). The old `/admin/settings/documents` URL redirects here.

Uses existing `DocumentTypes`: DocumentTypeID, TypeName, TypeNameDhivehi, Description, OrganisationID, IsActive, IsSystemType, IsImage, ShouldBeColor, ImageMinimumResolution and ServiceTypeID. No schema changes or new tables.

Types owned by the current organisation can be created, renamed, edited and disabled. Image settings use the existing fields. Shared types (OrganisationID null) and system types are read-only. Types used by Documents or RequestTypeSpecificDocumentTypes cannot be deleted; disable them to retain history. An unused organisation-owned type can be deleted after confirmation.

Access follows existing organisation settings permissions: Admin, Organisation.Edit or UserOrganisations.Edit for writes; existing organisation view permissions for read-only access. Super Admin authority is retained. Every operation checks the current organisation server-side. Saves/deletes use serializable transactions, stale-edit fingerprints and operation logs; organisation ownership and system flags cannot be changed through the form.

Supervisor assignments are managed under Admin → HR → Supervisors Management (`/admin/hr/supervisors/management`). Legacy organisation team supervisor links redirect here, preserving organisation and department selection. Organisation → Manage → Teams retains team CRUD and links to HR for supervisor assignments. Existing eligibility and assignment permissions apply.

Only Super Administrators can select another organisation in supervisor management. Other admin-portal staff see their current organisation as a fixed value. Organisation lists, details, links and supervisor changes enforce this server-side, even when a user has broad Admin/organisation permissions or membership of multiple organisations. A forged organisation URL does not grant access.
