using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class ContactInformationType
{
    public int ContactInformationTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Regex { get; set; }

    public string? Format { get; set; }

    public virtual ICollection<BussinessEntityContactInformation> BussinessEntityContactInformations { get; set; } = new List<BussinessEntityContactInformation>();
}
