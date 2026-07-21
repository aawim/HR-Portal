using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class AssignedWorkType
{
    public int AssignedWorkTypeId { get; set; }

    public int WorkTypeId { get; set; }

    public int JobId { get; set; }

    public int OrganisationId { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public string? Location { get; set; }

    public bool IsValid { get; set; }

    public int AssignedByUserId { get; set; }

    public DateOnly CreatedDate { get; set; }

    public virtual Job Job { get; set; } = null!;

    public virtual Organisation Organisation { get; set; } = null!;

    public virtual WorkType WorkType { get; set; } = null!;
}
