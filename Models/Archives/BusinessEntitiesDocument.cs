using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class BusinessEntitiesDocument
{
    public int BusinessEntityId { get; set; }

    public int DocumentId { get; set; }

    public DateTime LinkedDate { get; set; }

    public int LinkedByGlobalUserId { get; set; }

    public int? OperationLogId { get; set; }

    public virtual BusinessEntity BusinessEntity { get; set; } = null!;

    public virtual Document Document { get; set; } = null!;

    public virtual OperationLog? OperationLog { get; set; }
}
