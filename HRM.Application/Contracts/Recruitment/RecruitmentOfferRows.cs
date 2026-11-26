namespace HRM.Application.Contracts.Recruitment;
public sealed record RecruitmentOnboardingLookupOption(int Id,string Name);
public sealed record RecruitmentPayStepOption(int Id,string Label,decimal? BasicSalary);
public sealed record RecruitmentVacantPositionOption(int Id,string Name,int? PositionMatrixId,IReadOnlyList<RecruitmentPayStepOption> PaySteps);
public sealed record RecruitmentOfferContext(
    string PositionName,
    decimal? BasicSalary,
    string? EmploymentType,
    int? DefaultOrganisationStructureId,
    int? DefaultPositionId,
    IReadOnlyList<RecruitmentOnboardingLookupOption> Structures,
    IReadOnlyList<RecruitmentOnboardingLookupOption> JobTypes,
    IReadOnlyList<RecruitmentVacantPositionOption> VacantPositions,
    int? DefaultJobTypeId=null);
public sealed record RecruitmentOfferCandidateRow(int MeritResultId,string ApplicationReference,string ApplicantName,int Rank,decimal Score=0);
public sealed record RecruitmentOfferRow(int Id,string ApplicationReference,string ApplicantName,string Status,int? OnboardingRequestId,string? EfaasIdNumber=null,int? MatchedIndividualId=null,DateTime? OfferedAtUtc=null,DateTime? AcceptedAtUtc=null,string? TermsSnapshotJson=null,string? AppointeeSignatureDataUrl=null,string? SignatureVerificationCode=null);
public sealed record RecruitmentOfferWorkspace(IReadOnlyList<RecruitmentOfferCandidateRow> Candidates,IReadOnlyList<RecruitmentOfferRow> Offers,RecruitmentOfferContext? Context=null,int VacancySlots=0,bool IsResultFinal=false);
public sealed record JobOfferLetterDetailDto(
    int OfferId,
    string Status,
    string ApplicantName,
    string? EfaasIdNumber,
    string ApplicationCodeNumber,
    string PositionTitle,
    decimal? BasicSalary,
    string AnnouncementNumber,
    string AgencyName,
    string? AgencyNameDhivehi,
    DateTime? OfferedAtUtc,
    DateTime? AcceptedAtUtc,
    string TermsSnapshotJson,
    string? IssuedByName,
    string? AppointeeSignatureDataUrl,
    string? AppointeeVerificationCode,
    string? AgencySignatureDataUrl,
    string? AgencyVerificationCode,
    DateTime? AgencySignedAtUtc);
