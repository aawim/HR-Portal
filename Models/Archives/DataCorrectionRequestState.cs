using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DataCorrectionRequestState
{
    public int DataCorrectionRequestStateId { get; set; }

    public string StateName { get; set; } = null!;

    public bool? IsProcessingState { get; set; }

    public bool? IsFinalState { get; set; }

    public virtual ICollection<DataCorrectionRequest> DataCorrectionRequests { get; set; } = new List<DataCorrectionRequest>();
}
