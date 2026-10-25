using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class RequestTypeSpecificDocumentType
{
    public int RequestTypeSpecificDocumentTypeId { get; set; }

    public int RequestTypeId { get; set; }

    public int DocumentTypeId { get; set; }

    public bool IsValid { get; set; }

    public bool IsMandatory { get; set; }

    public int ContextId { get; set; }

    public virtual Context Context { get; set; } = null!;

    public virtual DocumentType DocumentType { get; set; } = null!;

    public virtual RequestType RequestType { get; set; } = null!;
}
