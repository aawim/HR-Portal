using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class OrganisationStructureDraft
{
    public int OrganisationStructureDraftId { get; set; }

    public int OrganisationStructureId { get; set; }

    public int OrganisationChartRequestId { get; set; }

    public string Name { get; set; } = null!;

    public int OrganisationStructureTypeId { get; set; }

    public int? ParentOrganisationStructureId { get; set; }

    public int OrganisationStructureStateId { get; set; }

    public int? HeadInChargeIndividualId { get; set; }

    public int? OrganisationStructureRequestTypeId { get; set; }

    public virtual OrganisationStructureRequest OrganisationChartRequest { get; set; } = null!;

    public virtual OrganisationStructure OrganisationStructure { get; set; } = null!;

    public virtual OrganisationStructureRequestType? OrganisationStructureRequestType { get; set; }

    public virtual OrganisationStructureState OrganisationStructureState { get; set; } = null!;

    public virtual OrganisationStructureType OrganisationStructureType { get; set; } = null!;
}
