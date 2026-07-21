using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OutOfficeRequestType
{
    public int OutOfOfficeRequestTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<OutOfOfficeRequest> OutOfOfficeRequests { get; set; } = new List<OutOfOfficeRequest>();
}
