using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Schedule
{
    public int ScheduleId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsPublic { get; set; }

    public bool IsGlobal { get; set; }

    public int ScheduleTypeId { get; set; }

    public int? OperationLogId { get; set; }

    public bool IsValid { get; set; }

    public bool? IsRamazan { get; set; }

    public virtual ICollection<BusinessEntitySchedule> BusinessEntitySchedules { get; set; } = new List<BusinessEntitySchedule>();

    public virtual ICollection<CalendarSchedule> CalendarSchedules { get; set; } = new List<CalendarSchedule>();

    public virtual ICollection<GroupSchedule> GroupSchedules { get; set; } = new List<GroupSchedule>();

    public virtual OperationLog? OperationLog { get; set; }

    public virtual ICollection<OrganisationStructureSchedule> OrganisationStructureSchedules { get; set; } = new List<OrganisationStructureSchedule>();

    public virtual ICollection<ScheduleElement> ScheduleElements { get; set; } = new List<ScheduleElement>();

    public virtual ICollection<ScheduleOwner> ScheduleOwners { get; set; } = new List<ScheduleOwner>();

    public virtual ScheduleType ScheduleType { get; set; } = null!;

    public virtual ShiftSchedule? ShiftSchedule { get; set; }
}
