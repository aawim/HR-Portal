using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Otrequest
{
    public DateTime Date { get; set; }

    public int RequestId { get; set; }

    public bool IsPlanned { get; set; }

    public int? OtpreApprovalRequestId { get; set; }

    public int StaffJobId { get; set; }

    public int OttypeId { get; set; }

    public bool IsDocumentRequired { get; set; }

    public virtual OtpreApproval? OtpreApprovalRequest { get; set; }

    public virtual ICollection<Ot> Ots { get; set; } = new List<Ot>();

    public virtual Ottype Ottype { get; set; } = null!;

    public virtual Request Request { get; set; } = null!;

    public virtual Job StaffJob { get; set; } = null!;
}
