using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class RequestState
{
    public int RequestStateId { get; set; }

    public string StateName { get; set; } = null!;

    public bool IsProcessingState { get; set; }

    public bool IsFinalState { get; set; }

    public string? StateNameDhivehi { get; set; }

    public string? ButtonActionName { get; set; }

    public string? ButtonActionNameDhivehi { get; set; }

    public string? ButtonClass { get; set; }

    public bool CommentRequired { get; set; }

    public virtual ICollection<RequestTypesAllowedRequestStateTransition> RequestTypesAllowedRequestStateTransitionFromRequestStates { get; set; } = new List<RequestTypesAllowedRequestStateTransition>();

    public virtual ICollection<RequestTypesAllowedRequestStateTransition> RequestTypesAllowedRequestStateTransitionToRequestStates { get; set; } = new List<RequestTypesAllowedRequestStateTransition>();

    public virtual ICollection<RequestTypesStatesRequiredRole> RequestTypesStatesRequiredRoles { get; set; } = new List<RequestTypesStatesRequiredRole>();

    public virtual ICollection<RequestTypesStatesRequiredRolesForProcessing> RequestTypesStatesRequiredRolesForProcessingRequestStates { get; set; } = new List<RequestTypesStatesRequiredRolesForProcessing>();

    public virtual ICollection<RequestTypesStatesRequiredRolesForProcessing> RequestTypesStatesRequiredRolesForProcessingToRequestStates { get; set; } = new List<RequestTypesStatesRequiredRolesForProcessing>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
}
