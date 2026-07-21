using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class BussinessEntityContactInformation
{
    public int BussinessEntityContactInformationId { get; set; }

    public int BussinessEntityId { get; set; }

    public int ContactInformationTypeId { get; set; }

    public string Value { get; set; } = null!;

    public bool IsValid { get; set; }

    public int? OperationLogId { get; set; }

    public virtual BusinessEntity BussinessEntity { get; set; } = null!;

    public virtual ContactInformationType ContactInformationType { get; set; } = null!;

    public virtual OperationLog? OperationLog { get; set; }
}
