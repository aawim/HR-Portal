using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Kpidocument
{
    public int KpidocumentId { get; set; }

    public int Kpiid { get; set; }

    public int DocumentId { get; set; }

    public DateTime LinkedDate { get; set; }

    public int LinkedByUserId { get; set; }

    public int OperationLogId { get; set; }

    public virtual Document Document { get; set; } = null!;

    public virtual User LinkedByUser { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;
}
