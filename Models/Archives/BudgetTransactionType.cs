using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class BudgetTransactionType
{
    public int Id { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<BudgetTransaction> BudgetTransactions { get; set; } = new List<BudgetTransaction>();
}
