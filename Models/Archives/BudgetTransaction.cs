using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class BudgetTransaction
{
    public int BudgetTransactionId { get; set; }

    public int BudgetItemId { get; set; }

    public DateOnly Date { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? LetterNumber { get; set; }

    public string? Details { get; set; }

    public decimal Amount { get; set; }

    public int TransactionTypeId { get; set; }

    public int OrganisationId { get; set; }

    public virtual BudgetItem BudgetItem { get; set; } = null!;

    public virtual Organisation Organisation { get; set; } = null!;

    public virtual BudgetTransactionType TransactionType { get; set; } = null!;
}
