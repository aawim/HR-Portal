using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class PayrollConfigurableValueType
{
    public int PayrollConfigurableValueTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Comment { get; set; }

    public bool IsJobTypeDependant { get; set; }

    public virtual ICollection<PayrollConfigurableValue> PayrollConfigurableValues { get; set; } = new List<PayrollConfigurableValue>();
}
