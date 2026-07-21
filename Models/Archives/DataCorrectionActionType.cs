using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DataCorrectionActionType
{
    public string ActionTypeName { get; set; } = null!;

    public int DataCorrectionActionTypeId { get; set; }

    public virtual ICollection<DataCorrectionRequestAttributeValue> DataCorrectionRequestAttributeValues { get; set; } = new List<DataCorrectionRequestAttributeValue>();

    public virtual ICollection<DataCorrectionRequestTypeAttribute> DataCorrectionRequestTypeAttributes { get; set; } = new List<DataCorrectionRequestTypeAttribute>();
}
