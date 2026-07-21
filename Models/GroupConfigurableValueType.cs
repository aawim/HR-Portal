using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class GroupConfigurableValueType
{
    public int GroupConfigurableValueTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Type { get; set; } = null!;

    public string? Comment { get; set; }

    public virtual ICollection<GroupConfigurableValue> GroupConfigurableValues { get; set; } = new List<GroupConfigurableValue>();
}
