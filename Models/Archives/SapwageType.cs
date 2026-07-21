using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class SapwageType
{
    public int SapwageTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string WageTypeCode { get; set; } = null!;

    public int SheetId { get; set; }

    public string Glcode { get; set; } = null!;

    public virtual ICollection<PrimaryPayrollItemTypeSapwageType> PrimaryPayrollItemTypeSapwageTypes { get; set; } = new List<PrimaryPayrollItemTypeSapwageType>();

    public virtual ICollection<Sapexception> Sapexceptions { get; set; } = new List<Sapexception>();

    public virtual Sapsheet Sheet { get; set; } = null!;
}
