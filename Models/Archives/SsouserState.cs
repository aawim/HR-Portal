using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class SsouserState
{
    public int SsouserStateId { get; set; }

    public string StateName { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
