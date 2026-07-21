using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class JobTypesException
{
    public int SapexceptionId { get; set; }

    public int JobTypeId { get; set; }

    public virtual JobType JobType { get; set; } = null!;

    public virtual Sapexception Sapexception { get; set; } = null!;
}
