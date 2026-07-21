using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class IdcardState
{
    public int IdcardStateId { get; set; }

    public string StateName { get; set; } = null!;

    public virtual ICollection<Idcard> Idcards { get; set; } = new List<Idcard>();
}
