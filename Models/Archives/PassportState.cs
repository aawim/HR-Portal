using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class PassportState
{
    public int PassportStateId { get; set; }

    public string StateName { get; set; } = null!;

    public virtual ICollection<Passport> Passports { get; set; } = new List<Passport>();
}
