using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class BudgetItem
{
    public int BudgetItemId { get; set; }

    public int ItemTypeId { get; set; }

    public int? Code { get; set; }

    public int? ParentBudgetItemId { get; set; }

    public string DhivehiName { get; set; } = null!;

    public string? EnglishName { get; set; }

    public bool IsAmountGiven { get; set; }

    public int BudgetYear { get; set; }

    public virtual ICollection<BudgetTransaction> BudgetTransactions { get; set; } = new List<BudgetTransaction>();

    public virtual ICollection<OrganisationBudget> OrganisationBudgets { get; set; } = new List<OrganisationBudget>();
}
