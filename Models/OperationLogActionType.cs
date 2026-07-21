using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class OperationLogActionType
{
    public int OperationLogActionTypes { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<OperationLog> OperationLogs { get; set; } = new List<OperationLog>();
}
