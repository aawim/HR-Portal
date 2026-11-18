namespace HRM.Application.Contracts.Recruitment;
public sealed record MyRecruitmentApplicationRow(string Reference,string VacancyReference,string Status,DateTime? SubmittedAtUtc);
public sealed record MyRecruitmentOfferRow(int Id,string VacancyReference,string Status,string TermsSnapshotJson);
public sealed record MyRecruitmentWorkspace(IReadOnlyList<MyRecruitmentApplicationRow> Applications,IReadOnlyList<MyRecruitmentOfferRow> Offers);
