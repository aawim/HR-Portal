using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class AttendanceLog
{
    public int AttendanceLogId { get; set; }

    public int? AttendanceDeviceId { get; set; }

    public int IndividualId { get; set; }

    public int OrganisationId { get; set; }

    public int OrganisationStructureId { get; set; }

    public int InOutModeId { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public int Day { get; set; }

    public int Hour { get; set; }

    public int Minute { get; set; }

    public int Second { get; set; }

    public DateTime Date { get; set; }

    public string Uidstamp { get; set; } = null!;

    public int AttendanceLogModeId { get; set; }

    public int AttendanceLogStateId { get; set; }

    public int? OperationLogId { get; set; }

    public int? RelatedAttendanceLogId { get; set; }

    public int? ActualInOutMode { get; set; }

    public virtual AttendanceDevice? AttendanceDevice { get; set; }

    public virtual ICollection<AttendanceLogChangeRequest> AttendanceLogChangeRequests { get; set; } = new List<AttendanceLogChangeRequest>();

    public virtual AttendanceLogMode AttendanceLogMode { get; set; } = null!;

    public virtual ICollection<AttendanceLogRequest> AttendanceLogRequests { get; set; } = new List<AttendanceLogRequest>();

    public virtual AttendanceLogState AttendanceLogState { get; set; } = null!;

    public virtual InOutMode InOutMode { get; set; } = null!;

    public virtual Staff Individual { get; set; } = null!;

    public virtual ICollection<AttendanceLog> InverseRelatedAttendanceLog { get; set; } = new List<AttendanceLog>();

    public virtual OperationLog? OperationLog { get; set; }

    public virtual Organisation Organisation { get; set; } = null!;

    public virtual OrganisationStructure OrganisationStructure { get; set; } = null!;

    public virtual ICollection<Ot> OtEndAttendanceLogs { get; set; } = new List<Ot>();

    public virtual ICollection<Ot> OtStartAttendanceLogs { get; set; } = new List<Ot>();

    public virtual AttendanceLog? RelatedAttendanceLog { get; set; }


    // newly added navigations by Sharieph 20226
    public virtual ICollection<AttendanceLogResolution>AttendanceLogResolutions{ get; set; } = new List<AttendanceLogResolution>();
}
