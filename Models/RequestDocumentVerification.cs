using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class RequestDocumentVerification
{
    public int RequestDocumentVerificationId { get; set; }

    public int RequestDocumentId { get; set; }

    public bool IsVerifiedSuccessfully { get; set; }

    public int OperationLogId { get; set; }

    public string? Comments { get; set; }

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual RequestDocument RequestDocument { get; set; } = null!;
}
