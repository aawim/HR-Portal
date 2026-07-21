using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DataCorrectionRequestTypeAttribute
{
    public int DataCorrectionRequestTypeId { get; set; }

    public int DataCorrectionAttributeId { get; set; }

    public int DataCorrectionActionTypeId { get; set; }

    public virtual DataCorrectionActionType DataCorrectionActionType { get; set; } = null!;

    public virtual DataCorrectionAttribute DataCorrectionAttribute { get; set; } = null!;

    public virtual DataCorrectionRequestType DataCorrectionRequestType { get; set; } = null!;
}
