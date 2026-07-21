using HRM.Resources;

public class LeaveResource : ResourceBase
{
   
    
    
    public int LeaveID { get; set; }    

    /// <summary>
    /// Leave Request ID.
    /// </summary>
    public int LeaveRequestID { get; set; }

    /// <summary>
    /// Active Job of the employee.
    /// </summary>
    public int? JobID { get; set; }

    /// <summary>
    /// Leave Type.
    /// </summary>
    public int LeaveTypeID { get; set; }

    /// <summary>
    /// Current Leave State.
    /// </summary>
    public int LeaveStateID { get; set; }

    /// <summary>
    /// Organisation Structure (Department).
    /// </summary>
    public int? OrganisationStructureID { get; set; }

    /// <summary>
    /// Direct supervisor of the employee.
    /// </summary>
    public int? SupervisorIndividualID { get; set; }

    /// <summary>
    /// Start Date.
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// End Date.
    /// </summary>
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Has this leave been approved?
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Has this leave been rejected?
    /// </summary>
    public bool IsRejected { get; set; }

    /// <summary>
    /// Has this leave been cancelled?
    /// </summary>
    public bool IsCancelled { get; set; }
}