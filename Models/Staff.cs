using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class Staff
{
    public int IndividualId { get; set; }

    public string? EmployeeNumber { get; set; }

    public int OperationLogId { get; set; }

    public virtual ICollection<AttendanceDeviceStaff> AttendanceDeviceStaffs { get; set; } = new List<AttendanceDeviceStaff>();

    public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();

    public virtual ICollection<GroupStaff> GroupStaffs { get; set; } = new List<GroupStaff>();

    public virtual Individual Individual { get; set; } = null!;

    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();

    public virtual ICollection<KeyHolder> KeyHolders { get; set; } = new List<KeyHolder>();

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual ICollection<OrganisationStructureHeadIncharge> OrganisationStructureHeadIncharges { get; set; } = new List<OrganisationStructureHeadIncharge>();

    public virtual ICollection<OrganisationStructureSupervisor> OrganisationStructureSupervisors { get; set; } = new List<OrganisationStructureSupervisor>();

    public virtual ICollection<OtpreApproval> OtpreApprovals { get; set; } = new List<OtpreApproval>();

    public virtual ICollection<OutOfOfficeRequest> OutOfOfficeRequestStaffs { get; set; } = new List<OutOfOfficeRequest>();

    public virtual ICollection<OutOfOfficeRequest> OutOfOfficeRequestSupervisorStaffs { get; set; } = new List<OutOfOfficeRequest>();

    public virtual ICollection<StaffEnrollmentNumber> StaffEnrollmentNumbers { get; set; } = new List<StaffEnrollmentNumber>();

    public virtual ICollection<TeamStaff> TeamStaffs { get; set; } = new List<TeamStaff>();
}
