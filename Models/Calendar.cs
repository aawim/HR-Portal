using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Calendar
{
    public int CalenderId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsPublic { get; set; }

    public bool IsGlobal { get; set; }

    public int? CalendarTypeId { get; set; }

    public int? OperationLogId { get; set; }

    public virtual ICollection<BusinessEntityCalendar> BusinessEntityCalendars { get; set; } = new List<BusinessEntityCalendar>();

    public virtual ICollection<CalendarInstance> CalendarInstances { get; set; } = new List<CalendarInstance>();

    public virtual ICollection<CalendarSchedule> CalendarSchedules { get; set; } = new List<CalendarSchedule>();

    public virtual CalendarType? CalendarType { get; set; }

    public virtual OperationLog? OperationLog { get; set; }

    public virtual ICollection<OrganisationStructureCalendar> OrganisationStructureCalendars { get; set; } = new List<OrganisationStructureCalendar>();

    public virtual ICollection<Calendar> Calenders { get; set; } = new List<Calendar>();

    public virtual ICollection<Element> Elements { get; set; } = new List<Element>();

    public virtual ICollection<Calendar> ParentCalenders { get; set; } = new List<Calendar>();
}
