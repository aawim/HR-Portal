namespace HRM.DTOs.WorkTypes;
public sealed record WorkTypeStaffOption(int JobId, string Name, string Department);

public sealed class WorkTypeDto
{
    public int WorkTypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsValid { get; set; }

    public int AssignmentCount { get; set; }
}

public sealed class CreateWorkTypeDto
{
    public string Name { get; set; } = string.Empty;

    public bool IsValid { get; set; } = true;
}

public sealed class UpdateWorkTypeDto
{
    public int WorkTypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsValid { get; set; } = true;
}

public sealed class AssignedWorkTypeDto
{
    public int AssignedWorkTypeId { get; set; }

    public int WorkTypeId { get; set; }

    public string WorkTypeName { get; set; } = string.Empty;

    public int JobId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public int OrganisationId { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public string? Location { get; set; }

    public bool IsValid { get; set; }

    public int AssignedByUserId { get; set; }

    public DateOnly CreatedDate { get; set; }
}

public sealed class CreateAssignedWorkTypeDto
{
    public int WorkTypeId { get; set; }

    public int JobId { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public string? Location { get; set; }
}

public sealed class UpdateAssignedWorkTypeDto
{
    public int AssignedWorkTypeId { get; set; }

    public int WorkTypeId { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public string? Location { get; set; }

    public bool IsValid { get; set; } = true;
}
