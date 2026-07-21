using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DocumentState
{
    public int DocumentStateId { get; set; }

    public string StateName { get; set; } = null!;

    public virtual ICollection<BulkUploadedDocument> BulkUploadedDocuments { get; set; } = new List<BulkUploadedDocument>();

    public virtual ICollection<LeavesBulkUploadedDocument> LeavesBulkUploadedDocuments { get; set; } = new List<LeavesBulkUploadedDocument>();
}
