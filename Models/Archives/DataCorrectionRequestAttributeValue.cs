using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DataCorrectionRequestAttributeValue
{
    public int DataCorrectionRequestAttributeValueId { get; set; }

    public int DataCorrectionRequestId { get; set; }

    public int DataCorrectionAttributeId { get; set; }

    public int DataCorrectionActionTypeId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string OldValueDetails { get; set; } = null!;

    public string NewValueDetails { get; set; } = null!;

    public virtual DataCorrectionActionType DataCorrectionActionType { get; set; } = null!;

    public virtual DataCorrectionAttribute DataCorrectionAttribute { get; set; } = null!;

    public virtual DataCorrectionRequest DataCorrectionRequest { get; set; } = null!;
}
