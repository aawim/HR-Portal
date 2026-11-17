using HRM.DTOs;namespace HRM.Application.Contracts.Recruitment;
public sealed record RecordScreeningInput(int ApplicationId,bool IsEligible,bool IsShortlisted,string DecisionReason,string EvidenceSnapshotJson);
public sealed record DeclareRecruitmentConflictInput(int VacancyId,bool HasConflict,string Declaration,string? Mitigation);
public interface IRecruitmentScreeningService{Task<ServiceResult> RecordAsync(RecordScreeningInput input,CancellationToken ct=default);Task<ServiceResult> DeclareConflictAsync(DeclareRecruitmentConflictInput input,CancellationToken ct=default);}
