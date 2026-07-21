using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class RequestsView
{
    public int RequestId { get; set; }

    public int RequestTypeId { get; set; }

    public int RequestStateId { get; set; }

    public bool IsProcessingState { get; set; }

    public bool IsFinalState { get; set; }

    public DateTime? LeaveFromDate { get; set; }

    public DateTime? LeaveToDate { get; set; }

    public DateTime? LastUpdatedDate { get; set; }

    public DateTime? Otdate { get; set; }

    public DateTime? AttendanceLogDate { get; set; }

    public int? StaffId { get; set; }

    public int? AttendanceOrganisationId { get; set; }

    public DateTime? OutOfOfficeDate { get; set; }

    public int? JobId { get; set; }
}
