# Individual identity and image

HR → Staff → Add Individual has a Foreigner switch. Local registration requires National ID. Foreign registration requires Passport number and nationality; passport duplicate checks use the existing country-scoped passport identity. Switching clears the hidden identity field; the service also ignores the unused field so stale form values cannot create the wrong identity record.

An optional PNG/JPEG image can be selected, previewed and removed before saving. The server validates the actual file structure/type, size (2 MB maximum) and dimensions (1–4096 pixels). SVG/HTML and arbitrary URLs are not accepted. Image uploads are stored in the database, with no public upload directory or new unauthorised image endpoint. Profiles use a validated data URL from their existing profile result.

Individuals gains nullable IsForeigner (bit), ImageData (varbinary(max)) and ImageContentType (nvarchar(20)). The SQL constraint requires matching nullable image/type fields, PNG/JPEG MIME types and a nonempty image no larger than 2 MB. Existing identities remain unchanged with unknown classification/null images; no citizenship is guessed from historical records. Former-staff immutability also protects these columns.

Individual registration still creates only the shared registry identity. Employment and user accounts require the existing approved onboarding flow. Verification covers local/passport selection, duplicate identity prevention, invalid/missing identity fields, image validation/storage, permissions and a restored SQL Server upgrade rehearsal.
