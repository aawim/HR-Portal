using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class StaffAssignedDeduction
{
    public int StaffAssignedDeductionId { get; set; }

    public int JobId { get; set; }

    public DateOnly EffectiveDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsValid { get; set; }

    public int OperationLogId { get; set; }

    public int? RequestId { get; set; }

    public int DeductionTypeId { get; set; }

    public string? Description { get; set; }

    public int? ReferenceNo { get; set; }

    public int? CurrencyTypeId { get; set; }

    public virtual CurrencyType? CurrencyType { get; set; }

    public virtual DeductionType DeductionType { get; set; } = null!;

    public virtual Job Job { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Request? Request { get; set; }

    public virtual ICollection<StaffAssignedDeductionAmount> StaffAssignedDeductionAmounts { get; set; } = new List<StaffAssignedDeductionAmount>();

    public virtual ICollection<StaffSalaryDeduction> StaffSalaryDeductions { get; set; } = new List<StaffSalaryDeduction>();
}
