namespace HRM.Application.Contracts.Recruitment;
public sealed record MyRecruitmentApplicationRow(string Reference,string VacancyReference,string Status,DateTime? SubmittedAtUtc);
public sealed record MyRecruitmentOfferRow(int Id,string VacancyReference,string Status,string TermsSnapshotJson);
public sealed record MyRecruitmentResultRow(int PublicationId,int ApplicationId,string ApplicationReference,string VacancyReference,string? PositionName,int Rank,decimal Score,bool IsFinal,DateTime ObjectionClosesAtUtc,int? ObjectionId,string? ObjectionStatus,string? ObjectionGrounds,string? ObjectionResolution);
public sealed record MyRecruitmentWorkspace(IReadOnlyList<MyRecruitmentApplicationRow> Applications,IReadOnlyList<MyRecruitmentOfferRow> Offers,int? ApplicantId=null,IReadOnlyList<MyRecruitmentResultRow>? Results=null);
public sealed record ApplyToRecruitmentVacancyInput(int VacancyId,string Qualifications,string Experience,string SupportingStatement);
