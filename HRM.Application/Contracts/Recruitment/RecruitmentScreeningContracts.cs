using HRM.DTOs;namespace HRM.Application.Contracts.Recruitment;
public sealed record RecordScreeningInput(int ApplicationId,bool IsEligible,bool IsShortlisted,string DecisionReason,string EvidenceSnapshotJson);
public sealed record RecruitmentScreeningApplicationRow(int Id,string Reference,string ApplicantName,string Status,DateTime? SubmittedAtUtc);
public sealed record HrRecruitmentApplicationDetail(int ApplicationId,string Reference,string Status,DateTime? SubmittedAtUtc,string ApplicantName,string? ApplicantEmail,string? ApplicantMobile,string? EfaasIdNumber,string ProfileSnapshotJson,IReadOnlyList<RecruitmentDocumentRow> Documents,IReadOnlyList<RecruitmentEventRow> Events,string? ReturnComment=null,DateTime? CorrectionDeadlineUtc=null);
public sealed record ReturnRecruitmentApplicationInput(int ApplicationId,string Comment,DateTime CorrectionDeadlineUtc);
public sealed record ScreeningResultSheetRow(int RowNumber,string CodeNumber,bool MeetsCriteria,bool DoesNotMeetCriteria,string Status);
public sealed record ScreeningResultSheetDto(int VacancyId,string AnnouncementNumber,string PositionTitle,decimal? BasicSalary,DateTime? ExpiryDateUtc,string AgencyName,string? AgencyNameDhivehi,string? AgencyEmail,string? AgencyPhone,IReadOnlyList<ScreeningResultSheetRow> Rows);
public sealed record DeclareRecruitmentConflictInput(int VacancyId,bool HasConflict,string Declaration,string? Mitigation);
public sealed record RecruitmentConflictDeclarationRow(int Id,int DeclaredByUserId,string DeclaredByName,bool HasConflict,string Declaration,string? Mitigation,DateTime DeclaredAtUtc,bool IsMine);
public interface IRecruitmentScreeningService{Task<ServiceResult> RecordAsync(RecordScreeningInput input,CancellationToken ct=default);Task<ServiceResult> DeclareConflictAsync(DeclareRecruitmentConflictInput input,CancellationToken ct=default);}
