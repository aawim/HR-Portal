using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class SalaryRateDefinition
{
    public int SalaryRateDefinitionId { get; set; }

    public int JobTypeId { get; set; }

    public int NoOfDays { get; set; }

    public int NoOfHours { get; set; }

    public bool IsSomeCalculatedDifferently { get; set; }

    public bool IsActive { get; set; }

    public int RequestId { get; set; }

    public int OperationLogId { get; set; }

    public int OrganizationId { get; set; }

    public DateOnly EffectiveDate { get; set; }

    public virtual JobType JobType { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual ICollection<OtherSalaryRateDefinition> OtherSalaryRateDefinitions { get; set; } = new List<OtherSalaryRateDefinition>();

    public virtual Request Request { get; set; } = null!;
}
