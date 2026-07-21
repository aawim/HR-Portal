using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class OrganisationStructureCalendar
{
    public int OrganisationStructureCalendarId { get; set; }

    public int OrganisationStructureId { get; set; }

    public int CalenderId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsLinked { get; set; }

    public int OperationLogId { get; set; }

    public virtual Calendar Calender { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual OrganisationStructure OrganisationStructure { get; set; } = null!;
}
