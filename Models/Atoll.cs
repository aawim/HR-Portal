using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class Atoll
{
    public int AtollId { get; set; }

    public string NameEnglish { get; set; } = null!;

    public string? NameDhivehi { get; set; }

    public string? AbbreviationEnglish { get; set; }

    public string? AbbreviationDhivehi { get; set; }

    public string? AtollCode { get; set; }

    public int? CityId { get; set; }

    public virtual City? City { get; set; }

    public virtual ICollection<Island> Islands { get; set; } = new List<Island>();
}
