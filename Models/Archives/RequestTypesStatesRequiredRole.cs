using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class RequestTypesStatesRequiredRole
{
    public int RequestTypeId { get; set; }

    public int RequestStateId { get; set; }

    public int RoleId { get; set; }

    public bool ShowAllRequests { get; set; }

    public virtual RequestState RequestState { get; set; } = null!;

    public virtual RequestType RequestType { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
