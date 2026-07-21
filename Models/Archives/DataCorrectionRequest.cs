using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DataCorrectionRequest
{
    public int DataCorrectionRequestId { get; set; }

    public int DataCorrectionRequestStateId { get; set; }

    public int DataCorrectionRequestTypeId { get; set; }

    public int? BusinessEntityId { get; set; }

    public int? ServiceId { get; set; }

    public int? RequestId { get; set; }

    public int LastUpdatedByUserId { get; set; }

    public DateTime LastUpdatedDate { get; set; }

    public int LastStateChangedByUserId { get; set; }

    public DateTime LastStateChangedDate { get; set; }

    public string? StateChangeRemarks { get; set; }

    public int OperationLogId { get; set; }

    public virtual BusinessEntity? BusinessEntity { get; set; }

    public virtual ICollection<DataCorrectionRequestAttributeValue> DataCorrectionRequestAttributeValues { get; set; } = new List<DataCorrectionRequestAttributeValue>();

    public virtual DataCorrectionRequestState DataCorrectionRequestState { get; set; } = null!;

    public virtual DataCorrectionRequestType DataCorrectionRequestType { get; set; } = null!;

    public virtual User LastStateChangedByUser { get; set; } = null!;

    public virtual User LastUpdatedByUser { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Request? Request { get; set; }

    public virtual Service? Service { get; set; }
}
