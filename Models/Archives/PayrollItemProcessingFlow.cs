using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class PayrollItemProcessingFlow
{
    public int PayrollItemProcessingFlowId { get; set; }

    public int? PayrollItemTypeProcessingCodeId { get; set; }

    public int? Operator { get; set; }

    public int ElementOrder { get; set; }

    public bool IsEndOfFlow { get; set; }

    public int? OperationLogId { get; set; }

    public int? ParentPayrollItemTypeProcessingCodeId { get; set; }

    public virtual OperationLog? OperationLog { get; set; }

    public virtual PayrollItemTypeProcessingCode? ParentPayrollItemTypeProcessingCode { get; set; }

    public virtual PayrollItemTypeProcessingCode? PayrollItemTypeProcessingCode { get; set; }
}
