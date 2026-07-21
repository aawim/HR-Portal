using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class JobTerminationType
{
    public int JobTerminationTypeId { get; set; }

    public string TypeName { get; set; } = null!;
}
