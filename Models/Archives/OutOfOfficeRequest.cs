using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OutOfOfficeRequest
{
    public int RequestId { get; set; }

    public int OrganisationBusinessEntityID { get; set; }

    public int? OrganisationStructureId { get; set; }

    public int StaffId { get; set; }

    public int SupervisorStaffId { get; set; }

    public DateTime Date { get; set; }

    public TimeOnly ExpectedClockOutTime { get; set; }

    public TimeOnly ExpectedClockInTime { get; set; }

    public string Remarks { get; set; } = null!;

    public int OutOfOfficeRequestTypeId { get; set; }

    public int? ClockInAttendanceLogId { get; set; }

    public int? ClockOutAttendanceLogId { get; set; }

    public virtual Organisation OrganisationBusinessEntity { get; set; } = null!;

    public virtual OrganisationStructure? OrganisationStructure { get; set; }

    public virtual OutOfficeRequestType OutOfOfficeRequestType { get; set; } = null!;

    public virtual Request Request { get; set; } = null!;

    public virtual Staff Staff { get; set; } = null!;

    public virtual Staff SupervisorStaff { get; set; } = null!;
}
