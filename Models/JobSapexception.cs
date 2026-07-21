using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class JobSapexception
{
    public int SapexceptionId { get; set; }

    public int JobId { get; set; }

    public virtual Job Job { get; set; } = null!;

    public virtual Sapexception Sapexception { get; set; } = null!;
}
