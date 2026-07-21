using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class CurrencyType
{
    public int CurrencyTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string Abbreviation { get; set; } = null!;

    public string? Iso4217number { get; set; }

    public string? Iso4217exponent { get; set; }

    public string? Iso4271code { get; set; }

    public string? Symbol { get; set; }

    public virtual ICollection<CurrencyExchangeRate> CurrencyExchangeRates { get; set; } = new List<CurrencyExchangeRate>();

    public virtual ICollection<DeductionType> DeductionTypes { get; set; } = new List<DeductionType>();

    public virtual ICollection<PayrollItemType> PayrollItemTypes { get; set; } = new List<PayrollItemType>();

    public virtual ICollection<StaffAssignedDeduction> StaffAssignedDeductions { get; set; } = new List<StaffAssignedDeduction>();
}
