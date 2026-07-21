using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class PayrollItemTypeProcessingCode
{
    public int PayrollItemTypeProcessingCodeId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int? OperationLogId { get; set; }

    public int AdditionOrDeductionTypeId { get; set; }

    public int ProcessingOrder { get; set; }

    public string? Sapcode { get; set; }

    public bool IsSystemType { get; set; }

    public int? OwnerOrganisationBusinessEntityId { get; set; }

    public int? PayrollItemTypeId { get; set; }

    public virtual AdditionOrDeductionType AdditionOrDeductionType { get; set; } = null!;

    public virtual OperationLog? OperationLog { get; set; }

    public virtual Organisation? OwnerOrganisationBusinessEntity { get; set; }

    public virtual ICollection<PayrollItemProcessingFlow> PayrollItemProcessingFlowParentPayrollItemTypeProcessingCodes { get; set; } = new List<PayrollItemProcessingFlow>();

    public virtual ICollection<PayrollItemProcessingFlow> PayrollItemProcessingFlowPayrollItemTypeProcessingCodes { get; set; } = new List<PayrollItemProcessingFlow>();

    public virtual PayrollItemType? PayrollItemType { get; set; }
}
