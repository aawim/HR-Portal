using HRM.DTOs;
namespace HRM.Application.Contracts.Recruitment;
public sealed record RegisterRecruitmentOfficialTemplateInput(int RecruitmentPolicyVersionId, long SourceDocumentId, string TemplateCode, string Name, string SourceUrl, string PositionCategory, string TemplateDefinitionJson, string Language = "dv", string Direction = "rtl");
public sealed record RecruitmentOfficialTemplateRow(int Id, string TemplateCode, string Name, string PositionCategory, string SourceSha256, string VerificationStatus, bool IsActive);
public interface IRecruitmentOfficialTemplateService { Task<IReadOnlyList<RecruitmentOfficialTemplateRow>> ListAsync(int policyVersionId, CancellationToken ct = default); Task<ServiceResult> RegisterAsync(RegisterRecruitmentOfficialTemplateInput input, CancellationToken ct = default); }
