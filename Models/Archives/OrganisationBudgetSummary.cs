using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class OrganisationBudgetSummary
{
    public int Id { get; set; }

    public int Year { get; set; }

    public int OrganisationId { get; set; }

    public int BudgetItemId { get; set; }

    public int BudgetItemTypeId { get; set; }

    public int Code { get; set; }

    public string DhivehiName { get; set; } = null!;

    public string? EnglishName { get; set; }

    public int ParentCode { get; set; }

    public int ParentbudgdetItemId { get; set; }

    public string ParentDhivehiName { get; set; } = null!;

    public string? ParentEnglishName { get; set; }

    public decimal Amount { get; set; }
}
