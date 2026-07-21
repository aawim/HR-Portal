using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class Position
{
    public int PositionId { get; set; }

    public int? PositionClassificationId { get; set; }

    public string Name { get; set; } = null!;

    public string? DhivehiName { get; set; }

    public string? Descriptions { get; set; }

    public bool? IsActive { get; set; }

    public int? CscdesignationPrimaryKey { get; set; }

    public int? OperationLogId { get; set; }

    public int? PositionRankId { get; set; }

    public int? PositionTypeId { get; set; }

    public int? OrganisationId { get; set; }

    public bool IsSalaryGivenBasedOnPostion { get; set; }

    public bool IsGlobal { get; set; }

    public virtual ICollection<JobPosition> JobPositions { get; set; } = new List<JobPosition>();

    public virtual OperationLog? OperationLog { get; set; }

    public virtual Organisation? Organisation { get; set; }

    public virtual ICollection<PositionBasicSalary> PositionBasicSalaries { get; set; } = new List<PositionBasicSalary>();

    public virtual PositionClassification? PositionClassification { get; set; }

    public virtual PositionRank? PositionRank { get; set; }

    public virtual PositionType? PositionType { get; set; }
}
