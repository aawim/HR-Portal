using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class Group
{
    public int GroupId { get; set; }

    public int GroupTypeId { get; set; }

    public string Name { get; set; } = null!;

    public int BusinessEntityOrganisationId { get; set; }

    public int OrganisationStructureId { get; set; }

    public int OperationLogId { get; set; }

    public virtual Organisation BusinessEntityOrganisation { get; set; } = null!;

    public virtual ICollection<GroupConfigurableValue> GroupConfigurableValues { get; set; } = new List<GroupConfigurableValue>();

    public virtual ICollection<GroupLeaveSet> GroupLeaveSets { get; set; } = new List<GroupLeaveSet>();

    public virtual ICollection<GroupSchedule> GroupSchedules { get; set; } = new List<GroupSchedule>();

    public virtual ICollection<GroupStaff> GroupStaffs { get; set; } = new List<GroupStaff>();

    public virtual GroupType GroupType { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual OrganisationStructure OrganisationStructure { get; set; } = null!;
}
