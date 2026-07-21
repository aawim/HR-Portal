using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DocumentType
{
    public int DocumentTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsSystemType { get; set; }

    public bool IsImage { get; set; }

    public bool ShouldBeColor { get; set; }

    public int? ImageMinimumResolution { get; set; }

    public bool IsActive { get; set; }

    public int? OrganisationId { get; set; }

    public int? ServiceTypeId { get; set; }

    public string? TypeNameDhivehi { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual Organisation? Organisation { get; set; }

    public virtual ICollection<RequestTypeSpecificDocumentType> RequestTypeSpecificDocumentTypes { get; set; } = new List<RequestTypeSpecificDocumentType>();

    public virtual ServiceType? ServiceType { get; set; }
}
