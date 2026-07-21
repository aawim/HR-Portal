using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class AddressInstanceType
{
    public int AddressInstanceTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<AddressInstance> AddressInstances { get; set; } = new List<AddressInstance>();

    public virtual ICollection<OnlineAddressInstance> OnlineAddressInstances { get; set; } = new List<OnlineAddressInstance>();
}
