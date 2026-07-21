using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class AttendanceClientInstance
{
    public int AttendanceClientId { get; set; }

    public string Name { get; set; } = null!;

    public Guid AttendanceClientKey { get; set; }

    public int OrganisationId { get; set; }

    public int OrganisationStructureId { get; set; }

    public int AttendanceClientStateId { get; set; }

    public DateTime? LastAttendanceManuallyUpdatedDate { get; set; }

    public DateTime? LastAttendanceManualUpdateRecord { get; set; }

    public int? OperationLogId { get; set; }

    public virtual AttendanceClientState AttendanceClientState { get; set; } = null!;

    public virtual ICollection<AttendanceDevice> AttendanceDevices { get; set; } = new List<AttendanceDevice>();

    public virtual OperationLog? OperationLog { get; set; }

    public virtual Organisation Organisation { get; set; } = null!;

    public virtual OrganisationStructure OrganisationStructure { get; set; } = null!;
}
