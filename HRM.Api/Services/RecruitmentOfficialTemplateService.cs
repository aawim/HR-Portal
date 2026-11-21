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
    async Task AuthorizeAsync() { if (!await access.HasPermissionAsync("Recruitment.Manage")) throw new UnauthorizedAccessException("Recruitment management permission is required."); }
    public async Task<IReadOnlyList<RecruitmentOfficialTemplateRow>> ListAsync(int policyVersionId, CancellationToken ct = default) { await AuthorizeAsync(); await using var db = await factory.CreateDbContextAsync(ct); return await db.RecruitmentOfficialTemplates.Where(x => x.RecruitmentPolicyVersionId == policyVersionId).OrderBy(x => x.TemplateCode).Select(x => new RecruitmentOfficialTemplateRow(x.Id, x.TemplateCode, x.Name, x.PositionCategory, x.SourceSha256, x.VerificationStatus, x.IsActive)).ToListAsync(ct); }
    public async Task<ServiceResult> RegisterAsync(RegisterRecruitmentOfficialTemplateInput input, CancellationToken ct = default)
    {
        if (input.RecruitmentPolicyVersionId <= 0 || input.SourceDocumentId <= 0 || string.IsNullOrWhiteSpace(input.TemplateCode) || string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.SourceUrl) || string.IsNullOrWhiteSpace(input.PositionCategory) || string.IsNullOrWhiteSpace(input.TemplateDefinitionJson) || input.TemplateDefinitionJson.Length > 16000) return ServiceResult.Fail("Provide the policy, immutable source document, template identity and scoring definition.");
        if (CscA2TemplateDefinitions.ByTemplateCode.TryGetValue(input.TemplateCode.Trim(), out var exactDefinition) && !string.Equals(exactDefinition, input.TemplateDefinitionJson, StringComparison.Ordinal)) return ServiceResult.Fail("The supplied A2 scoring definition does not match the registered CSC source template.");
        await AuthorizeAsync(); await using var db = await factory.CreateDbContextAsync(ct);
        var policy = await db.RecruitmentPolicyVersions.SingleOrDefaultAsync(x => x.Id == input.RecruitmentPolicyVersionId, ct);
        if (policy is null || !policy.IsApprovedForUse || policy.VerificationStatus != "Verified") return ServiceResult.Fail("The template must be linked to a verified, approved policy version.");
        var document = await db.DmsDocuments.SingleOrDefaultAsync(x => x.DocumentId == input.SourceDocumentId && x.IsOfficialRecord && x.IsImmutable && !x.IsDeleted, ct);
        var version = document is null ? null : await db.DmsDocumentVersions.SingleOrDefaultAsync(x => x.DocumentId == document.DocumentId && x.IsCurrentVersion, ct);
        if (version is null || string.IsNullOrWhiteSpace(version.Sha256Hash)) return ServiceResult.Fail("Upload the source as an immutable official DMS document before registering it.");
        if (await db.RecruitmentOfficialTemplates.AnyAsync(x => x.RecruitmentPolicyVersionId == input.RecruitmentPolicyVersionId && x.TemplateCode == input.TemplateCode.Trim() && x.PositionCategory == input.PositionCategory.Trim(), ct)) return ServiceResult.Fail("That policy/template/category combination is already registered; preserve it and register a new policy version for changes.");
        db.RecruitmentOfficialTemplates.Add(new RecruitmentOfficialTemplate { RecruitmentPolicyVersionId = input.RecruitmentPolicyVersionId, SourceDocumentId = document!.DocumentId, TemplateCode = input.TemplateCode.Trim(), Name = input.Name.Trim(), SourceUrl = input.SourceUrl.Trim(), SourceSha256 = version.Sha256Hash.ToUpperInvariant(), TemplateDefinitionJson = input.TemplateDefinitionJson, PositionCategory = input.PositionCategory.Trim(), Language = input.Language.Trim(), Direction = input.Direction.Trim(), VerificationStatus = "SourceImported", IsActive = true });
        await db.SaveChangesAsync(ct); return ServiceResult.Ok("Official recruitment template registered from its immutable DMS source.");
    }
}
