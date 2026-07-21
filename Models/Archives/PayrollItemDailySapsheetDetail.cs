using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class PayrollItemDailySapsheetDetail
{
    public int PayrollItemDailySapsheetDetailId { get; set; }

    public int? Days { get; set; }

    public int? Hours { get; set; }

    public int? Minutes { get; set; }

    public double? Rate { get; set; }

    public bool? Weekend { get; set; }

    public bool? Ramazan { get; set; }

    public int OperationLogId { get; set; }

    public int PayrollItemTypeId { get; set; }

    public int StaffSalaryId { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual PayrollItemType PayrollItemType { get; set; } = null!;

    public virtual StaffSalary StaffSalary { get; set; } = null!;
}
