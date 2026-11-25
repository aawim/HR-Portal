namespace HRM.Application.Contracts.Recruitment;
public sealed record RecruitmentResultRow(int ApplicationId,decimal Score,int Rank,string? ApplicationReference=null,string? ApplicantName=null);
public sealed record RecruitmentObjectionRow(int Id,int ApplicationId,string Grounds,string Status,string? Resolution);
public sealed record RecruitmentResultWorkspace(int? PublicationId,bool IsFinal,DateTime? ObjectionClosesAtUtc,IReadOnlyList<RecruitmentResultRow> Results,IReadOnlyList<RecruitmentObjectionRow> Objections);
