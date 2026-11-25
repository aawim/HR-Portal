using HRM.DTOs;
namespace HRM.Application.Contracts.Recruitment;
public sealed record RegisterRecruitmentOfficialTemplateInput(int RecruitmentPolicyVersionId, long SourceDocumentId, string TemplateCode, string Name, string SourceUrl, string PositionCategory, string TemplateDefinitionJson, string Language = "dv", string Direction = "rtl");
public sealed record UploadRecruitmentOfficialTemplateInput(int RecruitmentPolicyVersionId, string TemplateCode, string Name, string SourceUrl, string PositionCategory, string TemplateDefinitionJson, string FileName, string? ContentType, byte[] FileContent, string Language = "en", string Direction = "ltr", bool VerifyImmediately = true);
public sealed record RecruitmentOfficialTemplateFileResult(bool Success, string FileName, string ContentType, byte[] Content, string? Error = null);
public sealed record RecruitmentOfficialTemplateRow(
    int Id,
    string TemplateCode,
    string Name,
    string PositionCategory,
    string SourceSha256,
    string VerificationStatus,
    bool IsActive,
    long SourceDocumentId = 0,
    string? SourceUrl = null,
    string? TemplateDefinitionJson = null,
    string? SourceFileName = null,
    bool HasUploadedFile = false,
    string Language = "en",
    string Direction = "ltr");
public interface IRecruitmentOfficialTemplateService
{
    Task<IReadOnlyList<RecruitmentOfficialTemplateRow>> ListAsync(int policyVersionId, CancellationToken ct = default);
    Task<ServiceResult> RegisterAsync(RegisterRecruitmentOfficialTemplateInput input, CancellationToken ct = default);
    Task<ServiceResult> UploadAndRegisterAsync(UploadRecruitmentOfficialTemplateInput input, CancellationToken ct = default);
    Task<ServiceResult> AttachSourceFileAsync(int templateId, string fileName, string? contentType, byte[] content, CancellationToken ct = default);
    Task<ServiceResult> VerifyAsync(int templateId, CancellationToken ct = default);
    Task<RecruitmentOfficialTemplateFileResult> DownloadSourceFileAsync(int templateId, CancellationToken ct = default);
}
