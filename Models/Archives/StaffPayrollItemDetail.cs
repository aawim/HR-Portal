using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class StaffPayrollItemDetail
{
    public int StaffPayrollItemDetailsId { get; set; }

    public int StaffPayrollItemTypeId { get; set; }

    public string? Cifno { get; set; }

    public string? BillNo { get; set; }

    public string? RefNo { get; set; }

    public string? FacilityType { get; set; }

    public string? DealNo { get; set; }

    public string? Installment { get; set; }

    public virtual StaffPayrollItemType StaffPayrollItemType { get; set; } = null!;
}
