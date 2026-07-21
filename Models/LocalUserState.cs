using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class LocalUserState
{
    public int LocalUserStateId { get; set; }

    public string StateName { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
