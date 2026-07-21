using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class VwMergedRequestDatum
{
    public int RequestId { get; set; }

    public int RequestTypeId { get; set; }

    public int RequestStateId { get; set; }

    public DateTime? Otdate { get; set; }

    public DateTime? AttendanceLogDate { get; set; }

    public DateTime? Date { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public bool IsFinalState { get; set; }

    public bool IsProcessingState { get; set; }
}
