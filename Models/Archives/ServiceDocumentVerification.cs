using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class ServiceDocumentVerification
{
    public int ServiceDocumentVerificationId { get; set; }

    public int ServiceDocumentId { get; set; }

    public bool IsVerifiedSuccessfully { get; set; }

    public int OperationLogId { get; set; }

    public string? Comments { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual ServiceDocument ServiceDocument { get; set; } = null!;
}
