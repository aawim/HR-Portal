using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class PrimaryPayrollItemTypeSapwageType
{
    public int SapwageTypeId { get; set; }

    public int PayrollItemTypeId { get; set; }

    public bool IsValid { get; set; }

    public virtual PayrollItemType PayrollItemType { get; set; } = null!;

    public virtual SapwageType SapwageType { get; set; } = null!;
}
