namespace HRM.DTOs;

public sealed record OnboardingOption(int Id, string Name);
public sealed record OnboardingDepartmentOption(int Id, string Name, int StructureTypeId);
public sealed class StaffOnboardingForm
{
    public int IndividualId { get; set; }
    public int OrganisationStructureId { get; set; }
    public int JobTypeId { get; set; }
    public int? PositionId { get; set; }
    public DateTime JoinedDate { get; set; } = DateTime.UtcNow.AddHours(5).Date;
    public string Remarks { get; set; } = "";
}
public sealed class StaffOnboardingData
{
    public string Organisation { get; set; } = "";
    public string Individual { get; set; } = "";
    public bool CanSubmit { get; set; }
    public List<OnboardingOption> StructureTypes { get; set; } = [];
    public List<OnboardingDepartmentOption> Departments { get; set; } = [];
    public List<OnboardingOption> JobTypes { get; set; } = [];
    public List<OnboardingOption> Positions { get; set; } = [];
}
public sealed class StaffOnboardingRow
{
    public int RequestId { get; set; }
    public int StateId { get; set; }
    public string Reference { get; set; } = "";
    public string Individual { get; set; } = "";
    public string Department { get; set; } = "";
    public string JobType { get; set; } = "";
    public string Position { get; set; } = "Assign later";
    public DateTime JoinedDate { get; set; }
    public string Status { get; set; } = "";
    public string Remarks { get; set; } = "";
    public string? Stage { get; set; }
    public bool IsOpen { get; set; }
    public bool CanApprove { get; set; }
    public bool CanCancel { get; set; }
    public string? ApprovalInformation { get; set; }
    public string AccountStatus { get; set; } = "Account will be created after approval";
    public List<string> History { get; set; } = [];
}
