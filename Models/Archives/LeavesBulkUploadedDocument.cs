using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class LeavesBulkUploadedDocument
{
    public int DocumentId { get; set; }

    public int UploadedByUserId { get; set; }

    public DateTime UploadedDate { get; set; }

    public int DocumentStateId { get; set; }

    public int OperationLogId { get; set; }

    public virtual Document Document { get; set; } = null!;

    public virtual DocumentState DocumentState { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual User UploadedByUser { get; set; } = null!;
}
