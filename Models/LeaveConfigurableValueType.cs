using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class LeaveConfigurableValueType
{
    public int LeaveConfigurableValueTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Comment { get; set; }

    public bool IsJobTypeDependant { get; set; }

    public string? Unit { get; set; }

    public virtual ICollection<LeaveConfigurableValue> LeaveConfigurableValues { get; set; } = new List<LeaveConfigurableValue>();
}
