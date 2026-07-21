using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class StaffSalary
{
    public int StaffSalaryId { get; set; }

    public int JobId { get; set; }

    public int PayrollCycleRequestId { get; set; }

    public int? DaysAttended { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public bool? IsProcessed { get; set; }

    public string? Comments { get; set; }

    public DateTime? LastUpdatedDate { get; set; }

    public virtual ICollection<AggregatedSalary> AggregatedSalaries { get; set; } = new List<AggregatedSalary>();

    public virtual Job Job { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual ICollection<PayrollCycleLinkedRequest> PayrollCycleLinkedRequests { get; set; } = new List<PayrollCycleLinkedRequest>();

    public virtual PayrollCycle PayrollCycleRequest { get; set; } = null!;

    public virtual ICollection<PayrollItemDailySapsheetDetail> PayrollItemDailySapsheetDetails { get; set; } = new List<PayrollItemDailySapsheetDetail>();

    public virtual ICollection<StaffDailyAttendanceSummery> StaffDailyAttendanceSummeries { get; set; } = new List<StaffDailyAttendanceSummery>();

    public virtual ICollection<StaffSalaryDeduction> StaffSalaryDeductions { get; set; } = new List<StaffSalaryDeduction>();

    public virtual ICollection<StaffSalaryPayrollItem> StaffSalaryPayrollItems { get; set; } = new List<StaffSalaryPayrollItem>();
}
