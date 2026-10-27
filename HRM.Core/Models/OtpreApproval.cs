using System;
using System.Collections.Generic;
namespace HRM.Models.Archives;

public partial class OtpreApproval
{
    public int OttypeId { get; set; }

    public int StaffJobId { get; set; }

    public int SupervisorId { get; set; }

    public bool IsBulkDate { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Works { get; set; } = null!;

    public int RequestId { get; set; }

    public virtual Request Request { get; set; } = null!;

    public virtual Job StaffJob { get; set; } = null!;

    public virtual Staff Supervisor { get; set; } = null!;
}
