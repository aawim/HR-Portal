using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class StaffEnrollmentNumber
{
    public int StaffEnrollmentNumberId { get; set; }

    public int IndividualId { get; set; }

    public string EnrollmentNumber { get; set; } = null!;

    public int? OrganisationId { get; set; }

    public int? OperationLogId { get; set; }

    public virtual Staff Individual { get; set; } = null!;

    public virtual OperationLog? OperationLog { get; set; }

    public virtual Organisation? Organisation { get; set; }
}
