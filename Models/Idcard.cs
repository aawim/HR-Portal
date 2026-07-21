using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class Idcard
{
    public int IdcardId { get; set; }

    public int BusinessEntityId { get; set; }

    public string IdcardNumber { get; set; } = null!;

    public DateTime? ExpiryDate { get; set; }

    public int IdcardStateId { get; set; }

    public int? OperationLogId { get; set; }

    public virtual Individual BusinessEntity { get; set; } = null!;

    public virtual IdcardState IdcardState { get; set; } = null!;

    public virtual OperationLog? OperationLog { get; set; }
}
