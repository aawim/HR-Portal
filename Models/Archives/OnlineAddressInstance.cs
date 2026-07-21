using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OnlineAddressInstance
{
    public int OnlineLocationId { get; set; }

    public string? Name { get; set; }

    public string? Floor { get; set; }

    public int? AddressInstanceTypeId { get; set; }

    public int OnlineAddressBaseId { get; set; }

    public virtual AddressInstanceType? AddressInstanceType { get; set; }

    public virtual OnlineAddressBasis OnlineAddressBase { get; set; } = null!;

    public virtual OnlineAddress OnlineLocation { get; set; } = null!;
}
