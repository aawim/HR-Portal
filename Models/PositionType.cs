using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class PositionType
{
    public int PositionTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Position> Positions { get; set; } = new List<Position>();
}
