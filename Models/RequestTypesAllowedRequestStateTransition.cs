using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class RequestTypesAllowedRequestStateTransition
{
    public int RequestTypeId { get; set; }

    public int FromRequestStateId { get; set; }

    public int ToRequestStateId { get; set; }

    public virtual RequestState FromRequestState { get; set; } = null!;

    public virtual RequestType RequestType { get; set; } = null!;

    public virtual RequestState ToRequestState { get; set; } = null!;
}
