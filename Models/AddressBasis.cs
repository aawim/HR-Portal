using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class AddressBasis
{
    public int AddressBaseId { get; set; }

    public string? AddressLine1 { get; set; }

    public string? HomeNameDhivehi { get; set; }

    public string? AddressLine2 { get; set; }

    public string? StreetNameEnglish { get; set; }

    public string? StreetNameDhivehi { get; set; }

    public bool? IsCurrent { get; set; }

    public string? MunicipalityNumber { get; set; }

    public string? PostCode { get; set; }

    public int AddressBaseTypeId { get; set; }

    public int? WardId { get; set; }

    public virtual AddressBaseType AddressBaseType { get; set; } = null!;

    public virtual ICollection<AddressInstance> AddressInstances { get; set; } = new List<AddressInstance>();

    public virtual Ward? Ward { get; set; }
}
