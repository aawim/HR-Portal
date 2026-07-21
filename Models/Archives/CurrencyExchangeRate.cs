using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class CurrencyExchangeRate
{
    public int CurrencyExchangeRateId { get; set; }

    public int CurrencyTpeId { get; set; }

    public DateOnly EffectiveDate { get; set; }

    public double Rate { get; set; }

    public bool IsActive { get; set; }

    public int OrganisationId { get; set; }

    public int OperationLogId { get; set; }

    public virtual CurrencyType CurrencyTpe { get; set; } = null!;

    public virtual OperationLog OperationLog { get; set; } = null!;
}
