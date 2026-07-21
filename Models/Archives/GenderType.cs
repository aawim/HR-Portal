using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class GenderType
{
    public int GenderTypeId { get; set; }

    public string Abbreviation { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<Individual> Individuals { get; set; } = new List<Individual>();
}
