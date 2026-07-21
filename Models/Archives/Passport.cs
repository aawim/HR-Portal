using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Passport
{
    public int PassportId { get; set; }

    public int BusinessEntityId { get; set; }

    public string PassportNumber { get; set; } = null!;

    public int CountryId { get; set; }

    public DateTime? IssuedDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public int PassportStateId { get; set; }

    public int? OperationLogId { get; set; }

    public virtual Individual BusinessEntity { get; set; } = null!;

    public virtual Country Country { get; set; } = null!;

    public virtual OperationLog? OperationLog { get; set; }

    public virtual PassportState PassportState { get; set; } = null!;
}
