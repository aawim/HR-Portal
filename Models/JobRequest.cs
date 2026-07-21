using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class JobRequest
{
    public int JobId { get; set; }

    public int RequestId { get; set; }

    public virtual Job Job { get; set; } = null!;

    public virtual Request Request { get; set; } = null!;
}
