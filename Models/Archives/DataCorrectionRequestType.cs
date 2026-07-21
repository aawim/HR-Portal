using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DataCorrectionRequestType
{
    public int DataCorrectionRequestTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsValueChange { get; set; }

    public virtual ICollection<DataCorrectionRequestTypeAttribute> DataCorrectionRequestTypeAttributes { get; set; } = new List<DataCorrectionRequestTypeAttribute>();

    public virtual ICollection<DataCorrectionRequest> DataCorrectionRequests { get; set; } = new List<DataCorrectionRequest>();
}
