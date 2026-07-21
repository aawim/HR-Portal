using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Ottype
{
    public int OttypeId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<OtpreApproval> OtpreApprovals { get; set; } = new List<OtpreApproval>();

    public virtual ICollection<Otrequest> Otrequests { get; set; } = new List<Otrequest>();
}
