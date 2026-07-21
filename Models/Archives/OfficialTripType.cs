using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OfficialTripType
{
    public int OfficialTripTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public bool IsBasicSalaryGiven { get; set; }

    public bool IsAllowancesGiven { get; set; }

    public int OperationLogId { get; set; }

    public virtual ICollection<OfficialTripDetail> OfficialTripDetails { get; set; } = new List<OfficialTripDetail>();
}
