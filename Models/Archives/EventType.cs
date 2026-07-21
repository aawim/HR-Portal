using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class EventType
{
    public int EventTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }
}
