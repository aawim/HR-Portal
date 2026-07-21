using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OrganisationBudgetMonthlySummary
{
    public int Id { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public int OrganisationId { get; set; }

    public int BudgetItemId { get; set; }

    public int BudgetItemTypeId { get; set; }

    public int Code { get; set; }

    public string DhivehiName { get; set; } = null!;

    public string? EnglishName { get; set; }

    public int ParentBudgetItemId { get; set; }

    public int ParentBudgetCode { get; set; }

    public string ParentDhivehiName { get; set; } = null!;

    public string? ParentEnglishName { get; set; }

    public decimal TransferTotal { get; set; }

    public decimal ExpenditureThisMonth { get; set; }

    public decimal ExpenditureTotal { get; set; }

    public decimal RemainingTotal { get; set; }
}
