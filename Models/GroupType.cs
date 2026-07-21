using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class GroupType
{
    public int GroupTypeId { get; set; }

    public string Name { get; set; } = null!;

    public int OrganisationBusinessEntityId { get; set; }

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    public virtual Organisation OrganisationBusinessEntity { get; set; } = null!;
}
