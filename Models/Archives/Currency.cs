using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Currency
{
    public int CurrencyId { get; set; }

    public string CurrencyName { get; set; } = null!;

    public string CurrencyAbbreviation { get; set; } = null!;

    public string? Iso4217number { get; set; }

    public string? Iso4217exponent { get; set; }

    public string? Iso4271code { get; set; }
}
