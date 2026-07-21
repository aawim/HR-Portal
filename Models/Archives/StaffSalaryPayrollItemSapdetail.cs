using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class StaffSalaryPayrollItemSapdetail
{
    public int StaffSalaryPayrollItemSapdetailId { get; set; }

    public double? FixedAmount { get; set; }

    public double? Difference { get; set; }

    public int? Days { get; set; }

    public int? Hours { get; set; }

    public int? Minutes { get; set; }

    public double? Rate { get; set; }

    public int? StaffSalaryPayrollItemId { get; set; }

    public int OperationLogId { get; set; }

    public int PayrollItemTypeId { get; set; }

    public int? SapwageTypeId { get; set; }

    public int StaffSalaryId { get; set; }
}
