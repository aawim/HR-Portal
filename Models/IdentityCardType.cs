using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class IdentityCardType
{
    public int IdentityCardTypeId { get; set; }

    public string TypeName { get; set; } = null!;
}
