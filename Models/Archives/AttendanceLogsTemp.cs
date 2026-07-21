using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class AttendanceLogsTemp
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
}
