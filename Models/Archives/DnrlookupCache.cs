using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class DnrlookupCache
{
    public string IdcardNo { get; set; } = null!;

    public string LookupData { get; set; } = null!;

    public string? CreatedByIpaddress { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime LastUpdatedDate { get; set; }

    public string LastUpdatedByIpaddress { get; set; } = null!;
}
