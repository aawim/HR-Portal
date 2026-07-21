using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OnlineAddressBasis
{
    public int OnlineAddressBaseId { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? StreetNameEnglish { get; set; }

    public string? StreetNameDhivehi { get; set; }

    public bool? IsCurrent { get; set; }

    public string? MunicipalityNumber { get; set; }

    public string? PostCode { get; set; }

    public int AddressBaseTypeId { get; set; }

    public int? WardId { get; set; }

    public virtual AddressBaseType AddressBaseType { get; set; } = null!;

    public virtual ICollection<OnlineAddressInstance> OnlineAddressInstances { get; set; } = new List<OnlineAddressInstance>();

    public virtual Ward? Ward { get; set; }
}
