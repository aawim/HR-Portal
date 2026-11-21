namespace HRM.Application.Contracts.Recruitment;
public sealed record RecruitmentOfferCandidateRow(int MeritResultId,string ApplicationReference,string ApplicantName,int Rank);
public sealed record RecruitmentOfferRow(int Id,string ApplicationReference,string ApplicantName,string Status,int? OnboardingRequestId);
public sealed record RecruitmentOfferWorkspace(IReadOnlyList<RecruitmentOfferCandidateRow> Candidates,IReadOnlyList<RecruitmentOfferRow> Offers);
