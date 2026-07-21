using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class SalaryRateDefinitionForType
{
    public int SalaryRateDefinitionForTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<OtherSalaryRateDefinition> OtherSalaryRateDefinitions { get; set; } = new List<OtherSalaryRateDefinition>();
}
