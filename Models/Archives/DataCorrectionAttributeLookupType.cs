using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DataCorrectionAttributeLookupType
{
    public int DataCorrectionAttributeLookupTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<DataCorrectionAttribute> DataCorrectionAttributes { get; set; } = new List<DataCorrectionAttribute>();
}
