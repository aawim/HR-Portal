using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class StaffPayrollItemType
{
    public int StaffPayrollItemTypeId { get; set; }

    public int JobId { get; set; }

    public DateOnly EffectiveStartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public int PayrollItemTypeId { get; set; }

    public int? RequestId { get; set; }

    public string? ReferenceNo { get; set; }

    public string? Remarks { get; set; }

    public virtual Job Job { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual PayrollItemType PayrollItemType { get; set; } = null!;

    public virtual Request? Request { get; set; }

    public virtual ICollection<StaffPayrollItemDetail> StaffPayrollItemDetails { get; set; } = new List<StaffPayrollItemDetail>();

    public virtual ICollection<StaffPayrollItemTypeAmount> StaffPayrollItemTypeAmounts { get; set; } = new List<StaffPayrollItemTypeAmount>();

    public virtual ICollection<StaffPayrollItemTypeSchedule> StaffPayrollItemTypeSchedules { get; set; } = new List<StaffPayrollItemTypeSchedule>();

    public virtual ICollection<StaffSalaryPayrollItem> StaffSalaryPayrollItems { get; set; } = new List<StaffSalaryPayrollItem>();
}
