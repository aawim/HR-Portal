using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class VwLeaveChangeRequest
{
    public int RequestId { get; set; }

    public int IndividualId { get; set; }

    public int? TeamId { get; set; }

    public int OrganisationStructureId { get; set; }

    public bool? IsValid { get; set; }
}
