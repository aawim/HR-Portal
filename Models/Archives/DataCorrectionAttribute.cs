using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DataCorrectionAttribute
{
    public int DataCorrectionAttributeId { get; set; }

    public string AttributeName { get; set; } = null!;

    public string AttributeCode { get; set; } = null!;

    public string ValueType { get; set; } = null!;

    public bool IsLookup { get; set; }

    public bool IsActive { get; set; }

    public int? DataCorrectionAttributeLookupTypeId { get; set; }

    public virtual DataCorrectionAttributeLookupType? DataCorrectionAttributeLookupType { get; set; }

    public virtual ICollection<DataCorrectionRequestAttributeValue> DataCorrectionRequestAttributeValues { get; set; } = new List<DataCorrectionRequestAttributeValue>();

    public virtual ICollection<DataCorrectionRequestTypeAttribute> DataCorrectionRequestTypeAttributes { get; set; } = new List<DataCorrectionRequestTypeAttribute>();
}
