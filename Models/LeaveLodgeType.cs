using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class LeaveLodgeType
{
    public int LeaveLodgeTypeId { get; set; }

    public string TypeName { get; set; } = null!;
}
