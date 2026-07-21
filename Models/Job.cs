using HRM.Models.Archives;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

public partial class Job
{
    public int JobId { get; set; }

    public int IndividualID { get; set; }

    public int OrganisationID { get; set; }

  
    public int OrganisationStructureId { get; set; }

    public int JobStateId { get; set; }

    public int JobTypeId { get; set; }

    public DateTime JoinedDate { get; set; }

    public DateTime? TerminatedDate { get; set; }

    public decimal? BasicSalary { get; set; }

    public int OperationLogId { get; set; }

    public string? Sapnumber { get; set; }

    public int? ServiceId { get; set; }

    public virtual ICollection<AssignedWorkType> AssignedWorkTypes { get; set; } = new List<AssignedWorkType>();

    public virtual Staff Individual { get; set; } = null!;

    public virtual ICollection<JobLeaveType> JobLeaveTypes { get; set; } = new List<JobLeaveType>();

    public virtual ICollection<JobPosition> JobPositions { get; set; } = new List<JobPosition>();

    public virtual ICollection<JobRequest> JobRequests { get; set; } = new List<JobRequest>();

    public virtual ICollection<JobSapexception> JobSapexceptions { get; set; } = new List<JobSapexception>();

    public virtual JobState JobState { get; set; } = null!;

    public virtual JobType JobType { get; set; } = null!;

    public virtual ICollection<LeaveWorkHandOver> LeaveWorkHandOvers { get; set; } = new List<LeaveWorkHandOver>();

    public virtual ICollection<Leaf> Leaves { get; set; } = new List<Leaf>();
 

    public virtual OrganisationStructure OrganisationStructure { get; set; } = null!;

    public virtual ICollection<OtpreApproval> OtpreApprovals { get; set; } = new List<OtpreApproval>();

    public virtual ICollection<Otrequest> Otrequests { get; set; } = new List<Otrequest>();

    public virtual ICollection<RequestJob> RequestJobs { get; set; } = new List<RequestJob>();

    public virtual Service? Service { get; set; }

    public virtual ICollection<StaffAssignedDeduction> StaffAssignedDeductions { get; set; } = new List<StaffAssignedDeduction>();

    public virtual ICollection<StaffPayrollItemType> StaffPayrollItemTypes { get; set; } = new List<StaffPayrollItemType>();

    public virtual ICollection<StaffSalary> StaffSalaries { get; set; } = new List<StaffSalary>();

    [ForeignKey("IndividualID")]
    [InverseProperty("Jobs")]
    public virtual User User { get; set; }

    [ForeignKey("OrganisationID")]
    public virtual Organisation Organisation { get; set; }


}
 