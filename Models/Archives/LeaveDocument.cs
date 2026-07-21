using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class LeaveDocument
{
    public int LeaveDocumentId { get; set; }

    public int LeaveId { get; set; }

    public int DocumentId { get; set; }

    public int LinkedByUserId { get; set; }

    public DateTime LinkedDate { get; set; }

    public int? OperationLogId { get; set; }

    public virtual Document Document { get; set; } = null!;

    public virtual Leaf Leave { get; set; } = null!;

    public virtual User LinkedByUser { get; set; } = null!;

    public virtual OperationLog? OperationLog { get; set; }
}
