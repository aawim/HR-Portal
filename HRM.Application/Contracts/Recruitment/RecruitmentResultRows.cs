namespace HRM.Application.Contracts.Recruitment;
public sealed record RecruitmentResultRow(int ApplicationId,decimal Score,int Rank,string? ApplicationReference=null,string? ApplicantName=null);
public sealed record RecruitmentObjectionRow(int Id,int ApplicationId,string Grounds,string Status,string? Resolution);
public sealed record RecruitmentResultWorkspace(int? PublicationId,bool IsFinal,DateTime? ObjectionClosesAtUtc,IReadOnlyList<RecruitmentResultRow> Results,IReadOnlyList<RecruitmentObjectionRow> Objections);
public sealed record A2EvaluationCandidateRow(
    int ApplicationId,
    string ApplicationReference,
    string FullName,
    bool MeetsBasicRequirements,
    decimal BasicReqPoints,
    decimal CoreDisciplinePoints,
    decimal HigherQualPoints,
    string AdditionalExpDuration,
    decimal AdditionalExpPoints,
    decimal? Panelist1Score,
    decimal? Panelist2Score,
    decimal? Panelist3Score,
    decimal? RawExamScore,
    decimal? DirectExamScore,
    decimal? DirectInterviewScore);
public sealed record A2EvaluationSheetDto(
    int VacancyId,
    string AnnouncementNumber,
    string PositionTitle,
    decimal? BasicSalary,
    DateTime? ExpiryDateUtc,
    string? BasicRequirements,
    string AgencyName,
    string? AgencyNameDhivehi,
    IReadOnlyList<A2EvaluationCandidateRow> Candidates);
