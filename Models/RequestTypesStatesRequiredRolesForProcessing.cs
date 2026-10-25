using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class RequestTypesStatesRequiredRolesForProcessing
{
    public int RequestTypeId { get; set; }

    public int RequestStateId { get; set; }

    public int RoleId { get; set; }

    public int? ToRequestStateId { get; set; }

    public virtual RequestState RequestState { get; set; } = null!;

    public virtual RequestType RequestType { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;

    public virtual RequestState? ToRequestState { get; set; }
}
