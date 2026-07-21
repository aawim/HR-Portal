using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class LeaveChangeRequestType
{
    public int LeaveChangeRequestTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<LeaveChangeRequest> LeaveChangeRequests { get; set; } = new List<LeaveChangeRequest>();
}
