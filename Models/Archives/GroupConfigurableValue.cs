using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class GroupConfigurableValue
{
    public int GroupConfigurableValueId { get; set; }

    public int GroupConfigurableValueTypeId { get; set; }

    public string Value { get; set; } = null!;

    public bool IsValid { get; set; }

    public int GroupId { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual GroupConfigurableValueType GroupConfigurableValueType { get; set; } = null!;
}
