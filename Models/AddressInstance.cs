using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class AddressInstance
{
    public int LocationId { get; set; }

    public string? Name { get; set; }

    public string? Floor { get; set; }

    public int? AddressInstanceTypeId { get; set; }

    public int AddressBaseId { get; set; }

    public virtual AddressBasis AddressBase { get; set; } = null!;

    public virtual AddressInstanceType? AddressInstanceType { get; set; }

    public virtual Address Location { get; set; } = null!;
}
