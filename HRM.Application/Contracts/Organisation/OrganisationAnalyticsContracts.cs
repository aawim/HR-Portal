namespace HRM.Application.Contracts.Organisation;

public sealed record AnalyticsFilterOption(int Id, string Name, int Count);

public sealed record TalentFlowPoint(string MonthLabel, int Hires, int Exits, int Net);

public sealed record DepartmentAnalyticsRow(
    int DepartmentId,
    string DepartmentName,
    int Headcount,
    decimal SharePercent,
    decimal MonthlyBasicPayroll,
    int TeamCount,
    int HiresLast12Months);

public sealed record JobTypeBreakdownRow(string Name, int Count, decimal SharePercent);

public sealed record RecentHireRow(
    int IndividualId,
    string FullName,
    string PositionTitle,
    string DepartmentName,
    string JobTypeName,
    DateTime JoinedDate,
    bool InProbation);

public sealed record WorkAnniversaryRow(
    int IndividualId,
    string FullName,
    string DepartmentName,
    int YearsCompleted,
    DateTime AnniversaryDate);

public sealed record OrganisationAnalyticsDto(
    int OrganisationId,
    string OrganisationName,
    DateTime GeneratedAtUtc,
    int TotalHeadcount,
    int PermanentCount,
    int OtherEmploymentCount,
    decimal TurnoverRatePercent,
    decimal RetentionRatePercent,
    int HiresLast12Months,
    int ExitsLast12Months,
    int OpenVacanciesCount,
    int OpenVacancySlots,
    decimal AverageTenureYears,
    decimal TotalMonthlyBasicPayroll,
    int ApprovedLeavesYtd,
    int FemaleCount,
    int MaleCount,
    int UnspecifiedGenderCount,
    int TotalApplications,
    int OffersIssued,
    int OffersAccepted,
    decimal OfferAcceptanceRatePercent,
    IReadOnlyList<AnalyticsFilterOption> DepartmentOptions,
    IReadOnlyList<AnalyticsFilterOption> JobTypeOptions,
    IReadOnlyList<TalentFlowPoint> TalentFlow,
    IReadOnlyList<DepartmentAnalyticsRow> Departments,
    IReadOnlyList<JobTypeBreakdownRow> JobTypes,
    IReadOnlyList<RecentHireRow> RecentHires,
    IReadOnlyList<WorkAnniversaryRow> UpcomingAnniversaries);
