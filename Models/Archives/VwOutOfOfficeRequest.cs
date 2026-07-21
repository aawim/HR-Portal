using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class VwOutOfOfficeRequest
{
    public int RequestId { get; set; }

    public int IndividualId { get; set; }

    public int? TeamId { get; set; }

    public int? OrganisationStructureId { get; set; }
}
