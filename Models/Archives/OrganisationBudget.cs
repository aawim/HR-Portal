using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OrganisationBudget
{
    public int OrganisationBudgetId { get; set; }

    public int OrganisationId { get; set; }

    public int Year { get; set; }

    public int BudgetCodeId { get; set; }

    public decimal Amount { get; set; }

    public virtual BudgetItem BudgetCode { get; set; } = null!;

    public virtual Organisation Organisation { get; set; } = null!;
}
