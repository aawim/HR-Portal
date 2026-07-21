using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class VwWorkTeamRequest
{
    public int RequestId { get; set; }

    public int IndividualId { get; set; }

    public int OrganisationStructureId { get; set; }

    public int TeamId { get; set; }
}
