using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Ward
{
    public int WardId { get; set; }

    public int IslandId { get; set; }

    public string NameEnglish { get; set; } = null!;

    public string? AbbreviationEnglish { get; set; }

    public string? PostCode { get; set; }

    public string? NameDhivehi { get; set; }

    public string? AbbreviationDhivehi { get; set; }

    public virtual ICollection<AddressBasis> AddressBases { get; set; } = new List<AddressBasis>();

    public virtual Island Island { get; set; } = null!;

    public virtual ICollection<OnlineAddressBasis> OnlineAddressBases { get; set; } = new List<OnlineAddressBasis>();

    public virtual ICollection<OnlineArea> OnlineAreas { get; set; } = new List<OnlineArea>();
}
