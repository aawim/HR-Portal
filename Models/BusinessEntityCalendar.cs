using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class BusinessEntityCalendar
{
    public int BusinessEntityCalenderId { get; set; }

    public int CalenderId { get; set; }

    public int BusinessEntityId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsLinked { get; set; }

    public int OperationLogId { get; set; }

    public virtual BusinessEntity BusinessEntity { get; set; } = null!;

    public virtual Calendar Calender { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;
}
