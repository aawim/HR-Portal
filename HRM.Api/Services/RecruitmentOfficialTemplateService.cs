using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.Application.Contracts.Recruitment;
using HRM.DTOs;
using HRM.Models;
using HRM.Models.Recruitment;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

/// <summary>Registers an exact, immutable DMS copy of a CSC form and its verified scoring definition.</summary>
public sealed class RecruitmentOfficialTemplateService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    IDocumentStorageProvider storage) : IRecruitmentOfficialTemplateService
{
    private static readonly IReadOnlyDictionary<string, string> TemplateDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["A2-CS14-CS16"] = "CSC Form A2 — CS14 to CS16 (Executive / Senior Technical)",
        ["A2-CS10-CS14"] = "CSC Form A2 — CS10 to CS14 (Senior Professional)",
        ["A2-CS2-CS8"] = "CSC Form A2 — CS2 to CS8 (General / Operational)",
        ["A2-GS3-GS4"] = "CSC Form A2 — GS3 to GS4 (Administrative Support)",
        ["A2-GS4-TECHNICAL"] = "CSC Form A2 — GS4 Technical",
        ["A2-MS1-MS2"] = "CSC Form A2 — MS1 to MS2 (Managerial / Specialist)",
        ["A2-SS1-SS3"] = "CSC Form A2 — SS1 to SS3 (Support Services)",
        ["A2-MS3-EX5"] = "CSC Form A2 — MS3 to EX5 (Senior Management / Executive)"
    };

    async Task AuthorizeManageAsync()
    {
        if (!await access.HasUserGroupAsync("SuperAdministrator")
            && !await access.HasUserGroupAsync("Admin")
            && !await access.HasPermissionAsync("Recruitment.Manage"))
            throw new UnauthorizedAccessException("Recruitment management permission is required.");
    }

    async Task AuthorizeViewAsync()
    {
        if (!await access.HasUserGroupAsync("SuperAdministrator")
            && !await access.HasUserGroupAsync("Admin")
            && !await access.HasPermissionAsync("Recruitment.Manage")
            && !await access.HasPermissionAsync("Recruitment.View"))
            throw new UnauthorizedAccessException("Recruitment viewing permission is required.");
    }

    private static bool ValidateTemplateDefinitionJson(string json, out string error)
    {
        error = string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                error = "Template definition must be a JSON object.";
                return false;
            }
            if (!root.TryGetProperty("total", out var totalProp) || !totalProp.TryGetDecimal(out var total) || total <= 0)
            {
                error = "Template definition must include a positive numeric 'total'.";
                return false;
            }
            if (!root.TryGetProperty("components", out var compsProp) || compsProp.ValueKind != JsonValueKind.Array || compsProp.GetArrayLength() == 0)
            {
                error = "Template definition must include at least one scoring component.";
                return false;
            }
            decimal sum = 0;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in compsProp.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() != 2)
                {
                    error = "Each component must be a [name, maxScore] pair.";
                    return false;
                }
                var key = item[0].GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(key) || !seen.Add(key))
                {
                    error = "Each scoring component must have a unique non-empty key.";
                    return false;
                }
                if (!item[1].TryGetDecimal(out var weight) || weight <= 0)
                {
                    error = $"Component '{key}' must have a positive max score.";
                    return false;
                }
                sum += weight;
            }
            if (sum != total)
            {
                error = $"Component scores sum to {sum}, which does not match total ({total}).";
                return false;
            }
            return true;
        }
        catch (JsonException)
        {
            error = "Template definition JSON is not valid JSON.";
            return false;
        }
    }

    public async Task<IReadOnlyList<RecruitmentOfficialTemplateRow>> ListAsync(int policyVersionId, CancellationToken ct = default)
    {
        await AuthorizeViewAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync(ct);

        if (policyVersionId > 0 && org > 0 && !await db.RecruitmentOfficialTemplates.AnyAsync(x => x.RecruitmentPolicyVersionId == policyVersionId && x.IsActive && x.VerificationStatus == "Verified", ct))
        {
            await EnsureDefaultCscTemplatesAsync(db, policyVersionId, org, ct);
        }

        return await (from x in db.RecruitmentOfficialTemplates.AsNoTracking()
                      join ver in db.DmsDocumentVersions.AsNoTracking().Where(v => v.IsCurrentVersion)
                          on x.SourceDocumentId equals ver.DocumentId into vers
                      from ver in vers.DefaultIfEmpty()
                      where x.RecruitmentPolicyVersionId == policyVersionId
                      orderby x.TemplateCode
                      select new RecruitmentOfficialTemplateRow(
                          x.Id,
                          x.TemplateCode,
                          x.Name,
                          x.PositionCategory,
                          x.SourceSha256,
                          x.VerificationStatus,
                          x.IsActive,
                          x.SourceDocumentId,
                          x.SourceUrl,
                          x.TemplateDefinitionJson,
                          ver != null ? ver.OriginalFileName : null,
                          ver != null && ver.StoredObjectId > 0,
                          x.Language,
                          x.Direction)).ToListAsync(ct);
    }

    public async Task<ServiceResult> RegisterAsync(RegisterRecruitmentOfficialTemplateInput input, CancellationToken ct = default)
    {
        if (input.RecruitmentPolicyVersionId <= 0 || input.SourceDocumentId <= 0 || string.IsNullOrWhiteSpace(input.TemplateCode) || string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.SourceUrl) || string.IsNullOrWhiteSpace(input.PositionCategory) || string.IsNullOrWhiteSpace(input.TemplateDefinitionJson) || input.TemplateDefinitionJson.Length > 16000)
            return ServiceResult.Fail("Provide the policy, immutable source document, template identity and scoring definition.");
        if (!ValidateTemplateDefinitionJson(input.TemplateDefinitionJson, out var defError))
            return ServiceResult.Fail(defError);
        if (CscA2TemplateDefinitions.ByTemplateCode.TryGetValue(input.TemplateCode.Trim(), out var exactDefinition) && !string.Equals(exactDefinition, input.TemplateDefinitionJson, StringComparison.Ordinal))
            return ServiceResult.Fail("The supplied A2 scoring definition does not match the registered CSC source template.");

        await AuthorizeManageAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        var policy = await db.RecruitmentPolicyVersions.SingleOrDefaultAsync(x => x.Id == input.RecruitmentPolicyVersionId, ct);
        if (policy is null || !policy.IsApprovedForUse || policy.VerificationStatus != "Verified")
            return ServiceResult.Fail("The template must be linked to a verified, approved policy version.");

        var document = await db.DmsDocuments.SingleOrDefaultAsync(x => x.DocumentId == input.SourceDocumentId && x.IsOfficialRecord && x.IsImmutable && !x.IsDeleted, ct);
        var version = document is null ? null : await db.DmsDocumentVersions.SingleOrDefaultAsync(x => x.DocumentId == document.DocumentId && x.IsCurrentVersion, ct);
        if (version is null || string.IsNullOrWhiteSpace(version.Sha256Hash))
            return ServiceResult.Fail("Upload the source as an immutable official DMS document before registering it.");

        if (await db.RecruitmentOfficialTemplates.AnyAsync(x => x.RecruitmentPolicyVersionId == input.RecruitmentPolicyVersionId && x.TemplateCode == input.TemplateCode.Trim() && x.PositionCategory == input.PositionCategory.Trim(), ct))
            return ServiceResult.Fail("That policy/template/category combination is already registered; preserve it and register a new policy version for changes.");

        db.RecruitmentOfficialTemplates.Add(new RecruitmentOfficialTemplate
        {
            RecruitmentPolicyVersionId = input.RecruitmentPolicyVersionId,
            SourceDocumentId = document!.DocumentId,
            TemplateCode = input.TemplateCode.Trim(),
            Name = input.Name.Trim(),
            SourceUrl = input.SourceUrl.Trim(),
            SourceSha256 = version.Sha256Hash.ToUpperInvariant(),
            TemplateDefinitionJson = input.TemplateDefinitionJson,
            PositionCategory = input.PositionCategory.Trim(),
            Language = input.Language.Trim(),
            Direction = input.Direction.Trim(),
            VerificationStatus = "SourceImported",
            IsActive = true
        });
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok("Official recruitment template registered from its immutable DMS source.");
    }

    public async Task<ServiceResult> UploadAndRegisterAsync(UploadRecruitmentOfficialTemplateInput input, CancellationToken ct = default)
    {
        await AuthorizeManageAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        var actor = await access.RequireContextAsync();
        if (org <= 0) return ServiceResult.Fail("Select an active organisation first.");

        if (input.RecruitmentPolicyVersionId <= 0 || string.IsNullOrWhiteSpace(input.TemplateCode) || string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.PositionCategory) || string.IsNullOrWhiteSpace(input.TemplateDefinitionJson) || input.TemplateDefinitionJson.Length > 16000)
            return ServiceResult.Fail("Provide policy version, template code, name, position category, and valid scoring definition.");

        if (input.FileContent is null || input.FileContent.Length == 0 || string.IsNullOrWhiteSpace(input.FileName))
            return ServiceResult.Fail("Attach the official template file (PDF, DOCX, XLSX, PNG, or JPG) to upload to GHR.");

        if (!ValidateTemplateDefinitionJson(input.TemplateDefinitionJson, out var defError))
            return ServiceResult.Fail(defError);

        var code = input.TemplateCode.Trim();
        var category = input.PositionCategory.Trim();
        var name = input.Name.Trim();
        var sourceUrl = string.IsNullOrWhiteSpace(input.SourceUrl) ? "https://www.csc.gov.mv/" : input.SourceUrl.Trim();

        await using var db = await factory.CreateDbContextAsync(ct);
        var policy = await db.RecruitmentPolicyVersions.SingleOrDefaultAsync(x => x.Id == input.RecruitmentPolicyVersionId, ct);
        if (policy is null || !policy.IsApprovedForUse || policy.VerificationStatus != "Verified")
            return ServiceResult.Fail("The template must be linked to a verified, approved policy version.");

        if (await db.RecruitmentOfficialTemplates.AnyAsync(x => x.RecruitmentPolicyVersionId == input.RecruitmentPolicyVersionId && x.TemplateCode == code && x.PositionCategory == category, ct))
            return ServiceResult.Fail($"Template '{code}' ({category}) is already registered under this policy version.");

        var (dmsDocId, sha256, uploadErr) = await CreateOfficialTemplateDmsDocumentAsync(
            db, org, actor.UserId, string.IsNullOrWhiteSpace(actor.FullName) ? actor.Username : actor.FullName,
            code, name, input.FileName, input.ContentType, input.FileContent, ct);
        if (!string.IsNullOrWhiteSpace(uploadErr))
            return ServiceResult.Fail(uploadErr);

        db.RecruitmentOfficialTemplates.Add(new RecruitmentOfficialTemplate
        {
            RecruitmentPolicyVersionId = input.RecruitmentPolicyVersionId,
            SourceDocumentId = dmsDocId,
            TemplateCode = code,
            Name = name,
            SourceUrl = sourceUrl,
            SourceSha256 = sha256,
            TemplateDefinitionJson = input.TemplateDefinitionJson,
            PositionCategory = category,
            Language = string.IsNullOrWhiteSpace(input.Language) ? "en" : input.Language.Trim(),
            Direction = string.IsNullOrWhiteSpace(input.Direction) ? "ltr" : input.Direction.Trim(),
            VerificationStatus = input.VerifyImmediately ? "Verified" : "SourceImported",
            IsActive = true,
            Version = 1
        });
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok(input.VerifyImmediately
            ? $"Official template '{code}' uploaded to GHR DMS and verified for use."
            : $"Official template '{code}' uploaded to GHR DMS. Verify it to enable scoring.");
    }

    public async Task<ServiceResult> AttachSourceFileAsync(int templateId, string fileName, string? contentType, byte[] content, CancellationToken ct = default)
    {
        await AuthorizeManageAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        var actor = await access.RequireContextAsync();
        if (org <= 0) return ServiceResult.Fail("Select an active organisation first.");
        if (content is null || content.Length == 0 || string.IsNullOrWhiteSpace(fileName))
            return ServiceResult.Fail("Select a valid template file to upload.");

        await using var db = await factory.CreateDbContextAsync(ct);
        var template = await db.RecruitmentOfficialTemplates.SingleOrDefaultAsync(x => x.Id == templateId, ct);
        if (template is null) return ServiceResult.Fail("Template not found.");

        var (dmsDocId, sha256, uploadErr) = await CreateOfficialTemplateDmsDocumentAsync(
            db, org, actor.UserId, string.IsNullOrWhiteSpace(actor.FullName) ? actor.Username : actor.FullName,
            template.TemplateCode, template.Name, fileName, contentType, content, ct);
        if (!string.IsNullOrWhiteSpace(uploadErr))
            return ServiceResult.Fail(uploadErr);

        template.SourceDocumentId = dmsDocId;
        template.SourceSha256 = sha256;
        template.Version++;
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok($"Official file uploaded to GHR DMS and linked to '{template.TemplateCode}'.");
    }

    public async Task<ServiceResult> VerifyAsync(int templateId, CancellationToken ct = default)
    {
        await AuthorizeManageAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        var template = await db.RecruitmentOfficialTemplates.SingleOrDefaultAsync(x => x.Id == templateId, ct);
        if (template is null) return ServiceResult.Fail("Template not found.");
        if (!ValidateTemplateDefinitionJson(template.TemplateDefinitionJson, out var defError))
            return ServiceResult.Fail(defError);

        template.VerificationStatus = "Verified";
        template.IsActive = true;
        template.Version++;
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok($"Template '{template.TemplateCode}' is now Verified and ready for assessments.");
    }

    public async Task<RecruitmentOfficialTemplateFileResult> DownloadSourceFileAsync(int templateId, CancellationToken ct = default)
    {
        await AuthorizeViewAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        var row = await (from t in db.RecruitmentOfficialTemplates.AsNoTracking()
                         join d in db.DmsDocuments.AsNoTracking() on t.SourceDocumentId equals d.DocumentId
                         join v in db.DmsDocumentVersions.AsNoTracking().Where(x => x.IsCurrentVersion) on d.DocumentId equals v.DocumentId
                         where t.Id == templateId && !d.IsDeleted
                         select new { d.OrganisationId, v.StoredObjectId, v.OriginalFileName, v.ContentType }).SingleOrDefaultAsync(ct);

        if (row is null || row.StoredObjectId <= 0)
            return new RecruitmentOfficialTemplateFileResult(false, string.Empty, string.Empty, [], "No uploaded file is attached to this template yet.");

        var read = await storage.RetrieveAsync(row.StoredObjectId, row.OrganisationId, ct);
        if (!read.Success || read.Content.Length == 0)
            return new RecruitmentOfficialTemplateFileResult(false, string.Empty, string.Empty, [], read.ErrorMessage ?? "Template file could not be retrieved from GHR DMS.");

        var contentType = string.IsNullOrWhiteSpace(read.ContentType) ? (string.IsNullOrWhiteSpace(row.ContentType) ? "application/octet-stream" : row.ContentType) : read.ContentType;
        return new RecruitmentOfficialTemplateFileResult(true, row.OriginalFileName, contentType, read.Content);
    }

    private async Task<(long DocumentId, string Sha256, string? Error)> CreateOfficialTemplateDmsDocumentAsync(
        HrmTeContext db,
        int organisationId,
        int actorUserId,
        string actorName,
        string templateCode,
        string templateName,
        string fileName,
        string? contentType,
        byte[] content,
        CancellationToken ct)
    {
        var writeResult = await storage.StoreAsync(new DocumentStorageWriteRequest
        {
            OrganisationId = organisationId,
            OriginalFileName = fileName,
            DeclaredContentType = contentType,
            Content = content,
            PermittedExtensionsCsv = ".pdf,.docx,.xlsx,.png,.jpg,.jpeg",
            MaxFileSizeMb = 15,
            ActorUserId = actorUserId
        }, ct);

        if (!writeResult.Success)
            return (0, string.Empty, writeResult.ErrorMessage ?? "File upload rejected by GHR storage.");

        var (categoryId, docTypeId) = await EnsureTemplateDmsTypeAsync(db, ct);

        var dmsDoc = new DmsDocument
        {
            DocumentNumber = $"CSC-TPL-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..36],
            OrganisationId = organisationId,
            CategoryId = categoryId,
            DmsDocumentTypeId = docTypeId,
            Title = $"{templateCode} — {templateName}",
            Description = $"Official recruitment scoring template ({templateCode}) uploaded to GHR DMS.",
            ReferenceCode = templateCode,
            Classification = "Internal",
            AccessScope = "PublicOrg",
            Status = "OfficialRecord",
            CurrentVersionNumber = 1,
            IsOfficialRecord = true,
            IsImmutable = true,
            IsLockedForEdit = true,
            IsVerified = true,
            VerifiedAtUtc = DateTime.UtcNow,
            DocumentDate = DateTime.UtcNow,
            CreatedByUserId = actorUserId,
            CreatedByUserName = actorName
        };
        db.DmsDocuments.Add(dmsDoc);
        await db.SaveChangesAsync(ct);

        var version = new DmsDocumentVersion
        {
            DocumentId = dmsDoc.DocumentId,
            VersionNumber = 1,
            VersionLabel = "v1.0",
            StoredObjectId = writeResult.StoredObjectId,
            OriginalFileName = writeResult.SafeFileName,
            ContentType = writeResult.VerifiedContentType,
            FileExtension = writeResult.FileExtension,
            FileSizeBytes = writeResult.FileSizeBytes,
            Sha256Hash = writeResult.Sha256Hash,
            ChangeComments = $"Uploaded official template file for {templateCode}.",
            IsCurrentVersion = true,
            IsOfficialSnapshot = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = actorUserId,
            CreatedByUserName = actorName
        };
        db.DmsDocumentVersions.Add(version);
        await db.SaveChangesAsync(ct);

        dmsDoc.CurrentVersionId = version.DocumentVersionId;
        await db.SaveChangesAsync(ct);

        return (dmsDoc.DocumentId, writeResult.Sha256Hash.ToUpperInvariant(), null);
    }

    private static async Task<(int CategoryId, int DocTypeId)> EnsureTemplateDmsTypeAsync(HrmTeContext db, CancellationToken ct)
    {
        var existingType = await db.DmsDocumentTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Code == "CSC-A2-TEMPLATE", ct)
            ?? await db.DmsDocumentTypes.AsNoTracking().FirstOrDefaultAsync(ct);
        if (existingType is not null)
            return (existingType.CategoryId, existingType.DmsDocumentTypeId);

        var cat = await db.DmsCategories.FirstOrDefaultAsync(x => x.Code == "RECRUITMENT-POLICY", ct)
            ?? await db.DmsCategories.FirstOrDefaultAsync(ct);
        if (cat is null)
        {
            cat = new DmsCategory
            {
                Code = "RECRUITMENT-POLICY",
                Name = "Recruitment Policy & Official Templates",
                TargetDomain = "General",
                IsSystemCategory = true,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.DmsCategories.Add(cat);
            await db.SaveChangesAsync(ct);
        }

        var createdType = new DmsDocumentType
        {
            CategoryId = cat.CategoryId,
            Code = "CSC-A2-TEMPLATE",
            Name = "CSC Official Recruitment Template",
            DefaultClassification = "Internal",
            DefaultAccessScope = "PublicOrg",
            IsActive = true
        };
        db.DmsDocumentTypes.Add(createdType);
        await db.SaveChangesAsync(ct);
        return (cat.CategoryId, createdType.DmsDocumentTypeId);
    }

    public static async Task EnsureDefaultCscTemplatesAsync(HrmTeContext db, int policyVersionId, int organisationId, CancellationToken ct)
    {
        var policy = await (from v in db.RecruitmentPolicyVersions.AsNoTracking()
                            join p in db.RecruitmentPolicyProfiles.AsNoTracking() on v.RecruitmentPolicyProfileId equals p.Id
                            where v.Id == policyVersionId
                            select new { v.Id, v.VerificationStatus, v.IsApprovedForUse, p.IsCscProfile }).SingleOrDefaultAsync(ct);

        if (policy is null || !policy.IsCscProfile || !policy.IsApprovedForUse || policy.VerificationStatus != "Verified")
            return;

        var existingCodes = await db.RecruitmentOfficialTemplates
            .Where(x => x.RecruitmentPolicyVersionId == policyVersionId)
            .Select(x => x.TemplateCode)
            .ToListAsync(ct);

        var existingSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);
        var missing = CscA2TemplateDefinitions.ByTemplateCode
            .Where(kv => !existingSet.Contains(kv.Key))
            .ToList();

        if (missing.Count == 0)
            return;

        var sourceDocId = await EnsureCscSourceDocumentAsync(db, organisationId, ct);

        foreach (var kv in missing)
        {
            var sha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(kv.Value)));
            var displayName = TemplateDisplayNames.TryGetValue(kv.Key, out var friendly) ? friendly : $"CSC Form {kv.Key}";

            db.RecruitmentOfficialTemplates.Add(new RecruitmentOfficialTemplate
            {
                RecruitmentPolicyVersionId = policyVersionId,
                SourceDocumentId = sourceDocId,
                TemplateCode = kv.Key,
                Name = displayName,
                SourceUrl = "https://www.csc.gov.mv/",
                SourceSha256 = sha,
                TemplateDefinitionJson = kv.Value,
                PositionCategory = "All",
                Language = "en",
                Direction = "ltr",
                VerificationStatus = "Verified",
                IsActive = true,
                Version = 1
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task<long> EnsureCscSourceDocumentAsync(HrmTeContext db, int organisationId, CancellationToken ct)
    {
        var existingDocId = await db.DmsDocuments
            .AsNoTracking()
            .Where(x => x.ReferenceCode == "CSC-A2-TEMPLATES" && x.IsOfficialRecord && x.IsImmutable && !x.IsDeleted)
            .Select(x => (long?)x.DocumentId)
            .FirstOrDefaultAsync(ct);

        if (existingDocId.HasValue)
            return existingDocId.Value;

        var docType = await db.DmsDocumentTypes.AsNoTracking().FirstOrDefaultAsync(ct);
        int categoryId;
        int docTypeId;

        if (docType is not null)
        {
            categoryId = docType.CategoryId;
            docTypeId = docType.DmsDocumentTypeId;
        }
        else
        {
            var cat = await db.DmsCategories.FirstOrDefaultAsync(ct);
            if (cat is null)
            {
                cat = new DmsCategory
                {
                    Code = "RECRUITMENT-POLICY",
                    Name = "Recruitment Policy & Official Templates",
                    TargetDomain = "General",
                    IsSystemCategory = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };
                db.DmsCategories.Add(cat);
                await db.SaveChangesAsync(ct);
            }

            var createdType = new DmsDocumentType
            {
                CategoryId = cat.CategoryId,
                Code = "CSC-A2-TEMPLATE",
                Name = "CSC Official Recruitment Template",
                DefaultClassification = "Internal",
                DefaultAccessScope = "PublicOrg",
                IsActive = true
            };
            db.DmsDocumentTypes.Add(createdType);
            await db.SaveChangesAsync(ct);

            categoryId = cat.CategoryId;
            docTypeId = createdType.DmsDocumentTypeId;
        }

        var dmsDoc = new DmsDocument
        {
            DocumentNumber = $"CSC-A2-{DateTime.UtcNow:yyyyMMddHHmmss}",
            OrganisationId = organisationId,
            CategoryId = categoryId,
            DmsDocumentTypeId = docTypeId,
            Title = "CSC Official A2 Recruitment Scoring Templates",
            Description = "Immutable system-registered baseline copy of official Maldives Civil Service Commission Form A2 scoring criteria.",
            ReferenceCode = "CSC-A2-TEMPLATES",
            Classification = "Internal",
            AccessScope = "PublicOrg",
            Status = "OfficialRecord",
            CurrentVersionNumber = 1,
            IsOfficialRecord = true,
            IsImmutable = true,
            IsLockedForEdit = true,
            IsVerified = true,
            VerifiedAtUtc = DateTime.UtcNow,
            DocumentDate = DateTime.UtcNow
        };
        db.DmsDocuments.Add(dmsDoc);
        await db.SaveChangesAsync(ct);
        return dmsDoc.DocumentId;
    }
}
