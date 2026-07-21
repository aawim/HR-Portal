using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OrganisationBudgetSummeryAggregate
{
    public int Id { get; set; }

    public int Year { get; set; }

    public int OrganisationId { get; set; }

    public int ParentBudgetItemId { get; set; }

    public decimal Sum { get; set; }
}
