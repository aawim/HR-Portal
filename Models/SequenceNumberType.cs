using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class SequenceNumberType
{
    public int SequenceNumberTypeId { get; set; }

    public string? TypeName { get; set; }

    public string? Prefix { get; set; }

    public string? FormatString { get; set; }

    public bool? IsResetAnnually { get; set; }

    public int? StartingNumber { get; set; }

    public int? NextNumber { get; set; }

    public virtual ICollection<RequestType> RequestTypes { get; set; } = new List<RequestType>();

    public virtual ICollection<ServiceType> ServiceTypes { get; set; } = new List<ServiceType>();
}
