namespace HRM.Application.Contracts.Recruitment;
public sealed record SaveRecruitmentDraftInput(int VacancyId,int ExpectedVersion,string Qualifications,string Experience,string SupportingStatement);
public sealed record RecruitmentLifecycleActionInput(int ExpectedVersion,bool Confirmed);
public sealed record RecruitmentDocumentRow(int Id,string Category,string FileName);
public sealed record RecruitmentEventRow(string Kind,string Message,DateTime CreatedAtUtc);
public sealed record RecruitmentDraftDetail(int Id,int VacancyId,string Reference,string Status,int Version,string SnapshotJson,IReadOnlyList<RecruitmentDocumentRow> Documents,IReadOnlyList<RecruitmentEventRow> Notifications);
