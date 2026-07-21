using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Document
{
    public int DocumentId { get; set; }

    public string? DocumentName { get; set; }

    public DateOnly? DocumentDate { get; set; }

    public string? Comments { get; set; }

    public string? Mimetype { get; set; }

    public int? DocumentSize { get; set; }

    public bool IsDeleted { get; set; }

    public int DocumentTypeId { get; set; }

    public int? OperationLogId { get; set; }

    public virtual BulkUploadedDocument? BulkUploadedDocument { get; set; }

    public virtual ICollection<BusinessEntitiesDocument> BusinessEntitiesDocuments { get; set; } = new List<BusinessEntitiesDocument>();

    public virtual DocumentType DocumentType { get; set; } = null!;

    public virtual ICollection<Kpidocument> Kpidocuments { get; set; } = new List<Kpidocument>();

    public virtual ICollection<LeaveDocument> LeaveDocuments { get; set; } = new List<LeaveDocument>();

    public virtual LeavesBulkUploadedDocument? LeavesBulkUploadedDocument { get; set; }

    public virtual OperationLog? OperationLog { get; set; }

    public virtual ICollection<RequestDocument> RequestDocuments { get; set; } = new List<RequestDocument>();

    public virtual ICollection<ServiceDocument> ServiceDocuments { get; set; } = new List<ServiceDocument>();
}
