using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Sapexception
{
    public int SapexceptionId { get; set; }

    public int PayrollItemTypeId { get; set; }

    public DateTime EffectiveStartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? OperationLogId { get; set; }

    public int SapexceptionTypeId { get; set; }

    public int SapexceptionActionTypeId { get; set; }

    public int? SapwageTypeId { get; set; }

    public virtual JobSapexception? JobSapexception { get; set; }

    public virtual JobTypesException? JobTypesException { get; set; }

    public virtual OperationLog? OperationLog { get; set; }

    public virtual OrganisationSapexception? OrganisationSapexception { get; set; }

    public virtual OrganisationalStructureSapexception? OrganisationalStructureSapexception { get; set; }

    public virtual PayrollItemType PayrollItemType { get; set; } = null!;

    public virtual SapexceptionActionType SapexceptionActionType { get; set; } = null!;

    public virtual SapexceptionType SapexceptionType { get; set; } = null!;

    public virtual SapwageType? SapwageType { get; set; }
}
