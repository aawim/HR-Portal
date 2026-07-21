using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class LeaveConfigurableValue
{
    public int LeaveConfigurableValueId { get; set; }

    public int LeaveConfigurableValueTypeId { get; set; }

    public int OrganisationId { get; set; }

    public string Value { get; set; } = null!;

    public bool IsValid { get; set; }

    public int? JobTypeId { get; set; }

    public virtual JobType? JobType { get; set; }

    public virtual LeaveConfigurableValueType LeaveConfigurableValueType { get; set; } = null!;

    public virtual Organisation Organisation { get; set; } = null!;
}
