using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OtherSalaryRateDefinition
{
    public int OtherSalaryRateDefinitionId { get; set; }

    public int SalaryRateDefinitionId { get; set; }

    public int SalaryRateDefinitionForTypeId { get; set; }

    public int NoOfDays { get; set; }

    public int NoOfHours { get; set; }

    public bool IsActive { get; set; }

    public int OperationLogId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual SalaryRateDefinition SalaryRateDefinition { get; set; } = null!;

    public virtual SalaryRateDefinitionForType SalaryRateDefinitionForType { get; set; } = null!;
}
