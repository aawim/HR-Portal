using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Sapsheet
{
    public int SapsheetId { get; set; }

    public string SheetName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsValid { get; set; }

    public bool? IsGenereatedForPayroll { get; set; }

    public virtual ICollection<PayrollItemType> PayrollItemTypes { get; set; } = new List<PayrollItemType>();

    public virtual ICollection<SapwageType> SapwageTypes { get; set; } = new List<SapwageType>();
}
