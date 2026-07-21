using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class SapexceptionActionType
{
    public int SapexceptionActionTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public bool IsValid { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Sapexception> Sapexceptions { get; set; } = new List<Sapexception>();
}
