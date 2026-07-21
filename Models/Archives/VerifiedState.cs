using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class VerifiedState
{
    public int VerifiedStateId { get; set; }

    public string StateName { get; set; } = null!;

    public virtual ICollection<BusinessEntity> BusinessEntities { get; set; } = new List<BusinessEntity>();
}
