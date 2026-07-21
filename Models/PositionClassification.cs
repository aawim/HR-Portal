using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class PositionClassification
{
    public int PositionClassificationId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? NameDhivehi { get; set; }

    public int? CscclassificationPimaryKey { get; set; }

    public bool? IsActive { get; set; }

    public int? OperationLogId { get; set; }

    public int? OrganisationBusinessEntityID { get; set; }

    public virtual OperationLog? OperationLog { get; set; }

    public virtual Organisation? OrganisationBusinessEntity { get; set; }

    public virtual ICollection<Position> Positions { get; set; } = new List<Position>();
}
