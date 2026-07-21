using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class ServiceDocument
{
    public int ServiceDocumentId { get; set; }

    public int ServiceId { get; set; }

    public int DocumentId { get; set; }

    public int LinkedByUserId { get; set; }

    public DateTime LinkedDate { get; set; }

    public int? OperationLogId { get; set; }

    public virtual Document Document { get; set; } = null!;

    public virtual OperationLog? OperationLog { get; set; }

    public virtual Service Service { get; set; } = null!;

    public virtual ICollection<ServiceDocumentVerification> ServiceDocumentVerifications { get; set; } = new List<ServiceDocumentVerification>();
}
