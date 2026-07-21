using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class ServiceType
{
    public int ServiceTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public bool IsValid { get; set; }

    public int? SequenceNumberTypeId { get; set; }

    public virtual ICollection<DocumentType> DocumentTypes { get; set; } = new List<DocumentType>();

    public virtual ICollection<RequestType> RequestTypes { get; set; } = new List<RequestType>();

    public virtual SequenceNumberType? SequenceNumberType { get; set; }

    public virtual ICollection<ServiceTypesAllowedServiceStateTransition> ServiceTypesAllowedServiceStateTransitions { get; set; } = new List<ServiceTypesAllowedServiceStateTransition>();

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();

    public virtual ICollection<CheckListItem> CheckListItems { get; set; } = new List<CheckListItem>();
}
