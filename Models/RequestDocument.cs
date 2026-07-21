using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class RequestDocument
{
    public int RequestDocumentId { get; set; }

    public int RequestId { get; set; }

    public int DocumentId { get; set; }

    public int LinkedByUserId { get; set; }

    public DateTime LinkedDate { get; set; }

    public int? OperationLogId { get; set; }

    public virtual Document Document { get; set; } = null!;

    public virtual OperationLog? OperationLog { get; set; }

    public virtual Request Request { get; set; } = null!;

    public virtual ICollection<RequestDocumentVerification> RequestDocumentVerifications { get; set; } = new List<RequestDocumentVerification>();
}
