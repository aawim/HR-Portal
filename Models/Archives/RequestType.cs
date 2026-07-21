using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class RequestType
{
    public int RequestTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public int? ServiceTypeId { get; set; }

    public string? ViewUrl { get; set; }

    public int? SequenceNumberTypeId { get; set; }

    public bool IsSystemType { get; set; }

    public virtual ICollection<LeaveType> LeaveTypes { get; set; } = new List<LeaveType>();

    public virtual ICollection<RequestTypeSpecificDocumentType> RequestTypeSpecificDocumentTypes { get; set; } = new List<RequestTypeSpecificDocumentType>();

    public virtual ICollection<RequestTypesAllowedRequestStateTransition> RequestTypesAllowedRequestStateTransitions { get; set; } = new List<RequestTypesAllowedRequestStateTransition>();

    public virtual ICollection<RequestTypesStatesRequiredRole> RequestTypesStatesRequiredRoles { get; set; } = new List<RequestTypesStatesRequiredRole>();

    public virtual ICollection<RequestTypesStatesRequiredRolesForProcessing> RequestTypesStatesRequiredRolesForProcessings { get; set; } = new List<RequestTypesStatesRequiredRolesForProcessing>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual SequenceNumberType? SequenceNumberType { get; set; }

    public virtual ServiceType? ServiceType { get; set; }

    public virtual ICollection<CheckListItem> CheckListItems { get; set; } = new List<CheckListItem>();
}
