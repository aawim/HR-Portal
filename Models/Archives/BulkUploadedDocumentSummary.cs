using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class BulkUploadedDocumentSummary
{
    public int BulkUploadedDocumentSummaryId { get; set; }

    public int DocumentId { get; set; }

    public int RowId { get; set; }

    public string IndexName { get; set; } = null!;

    public string Remarks { get; set; } = null!;

    public bool IsValid { get; set; }

    public virtual BulkUploadedDocument Document { get; set; } = null!;
}
