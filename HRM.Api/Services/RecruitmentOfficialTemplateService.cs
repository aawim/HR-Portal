using System.Security.Cryptography;
using System.Text;
using HRM.Application.Contracts.Recruitment;
using HRM.DTOs;
using HRM.Models;
using HRM.Models.Recruitment;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

/// <summary>Registers an exact, immutable DMS copy of a CSC form and its verified scoring definition.</summary>
public sealed class RecruitmentOfficialTemplateService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access) : IRecruitmentOfficialTemplateService
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

    async Task AuthorizeAsync()
    {
        if (!await access.HasPermissionAsync("Recruitment.Manage"))
            throw new UnauthorizedAccessException("Recruitment management permission is required.");
    }

    public async Task<IReadOnlyList<RecruitmentOfficialTemplateRow>> ListAsync(int policyVersionId, CancellationToken ct = default)
    {
        await AuthorizeAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync(ct);

        if (policyVersionId > 0 && org > 0 && !await db.RecruitmentOfficialTemplates.AnyAsync(x => x.RecruitmentPolicyVersionId == policyVersionId && x.IsActive && x.VerificationStatus == "Verified", ct))
        {
            await EnsureDefaultCscTemplatesAsync(db, policyVersionId, org, ct);
        }

        return await db.RecruitmentOfficialTemplates
            .Where(x => x.RecruitmentPolicyVersionId == policyVersionId)
            .OrderBy(x => x.TemplateCode)
            .Select(x => new RecruitmentOfficialTemplateRow(x.Id, x.TemplateCode, x.Name, x.PositionCategory, x.SourceSha256, x.VerificationStatus, x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<ServiceResult> RegisterAsync(RegisterRecruitmentOfficialTemplateInput input, CancellationToken ct = default)
    {
        if (input.RecruitmentPolicyVersionId <= 0 || input.SourceDocumentId <= 0 || string.IsNullOrWhiteSpace(input.TemplateCode) || string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.SourceUrl) || string.IsNullOrWhiteSpace(input.PositionCategory) || string.IsNullOrWhiteSpace(input.TemplateDefinitionJson) || input.TemplateDefinitionJson.Length > 16000)
            return ServiceResult.Fail("Provide the policy, immutable source document, template identity and scoring definition.");
        if (CscA2TemplateDefinitions.ByTemplateCode.TryGetValue(input.TemplateCode.Trim(), out var exactDefinition) && !string.Equals(exactDefinition, input.TemplateDefinitionJson, StringComparison.Ordinal))
            return ServiceResult.Fail("The supplied A2 scoring definition does not match the registered CSC source template.");

        await AuthorizeAsync();
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
