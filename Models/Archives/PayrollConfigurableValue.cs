using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class PayrollConfigurableValue
{
    public int PayrollConfigurableValueId { get; set; }

    public int PayrollConfigurableValueTypeId { get; set; }

    public int OrganisationId { get; set; }

    public string Value { get; set; } = null!;

    public bool IsValid { get; set; }

    public int? JobTypeId { get; set; }

    public virtual JobType? JobType { get; set; }

    public virtual Organisation Organisation { get; set; } = null!;

    public virtual PayrollConfigurableValueType PayrollConfigurableValueType { get; set; } = null!;
}
