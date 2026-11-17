namespace HRM.Models.Recruitment;

public sealed class RecruitmentRequisition
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int OrganisationId { get; set; }
    public int? OrganisationStructureId { get; set; }
    public int PositionMatrixId { get; set; }
    public int RecruitmentPolicyVersionId { get; set; }
    public int RequestedSlots { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public string RecruitmentType { get; set; } = "CivilService";
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public string PositionSnapshotJson { get; set; } = "{}";
    public int RequestedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
}

public sealed class RecruitmentPositionReservation
{
    public int Id { get; set; }
    public int OrganisationId { get; set; }
    public int PositionMatrixId { get; set; }
    public int RecruitmentRequisitionId { get; set; }
    public int ReservedSlots { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime ReservedAtUtc { get; set; }
    public DateTime? ReleasedAtUtc { get; set; }
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
}

public sealed class RecruitmentVacancy
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int OrganisationId { get; set; }
    public int RecruitmentRequisitionId { get; set; }
    public int RecruitmentPolicyVersionId { get; set; }
    public int PositionMatrixId { get; set; }
    public int VacancySlots { get; set; }
    public string Status { get; set; } = "Draft";
    public string AdvertisementSnapshotJson { get; set; } = "{}";
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime? ClosingAtUtc { get; set; }
    public int Version { get; set; } = 1;
    public int OperationLogId { get; set; }
}
