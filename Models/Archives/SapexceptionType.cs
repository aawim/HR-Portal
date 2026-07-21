using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class SapexceptionType
{
    public int SapexceptionTypeId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Sapexception> Sapexceptions { get; set; } = new List<Sapexception>();
}
