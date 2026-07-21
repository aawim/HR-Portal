using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class AddressBaseType
{
    public int AddressBaseTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<AddressBasis> AddressBases { get; set; } = new List<AddressBasis>();

    public virtual ICollection<OnlineAddressBasis> OnlineAddressBases { get; set; } = new List<OnlineAddressBasis>();
}
