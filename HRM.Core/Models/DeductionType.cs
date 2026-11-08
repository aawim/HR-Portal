using System;
using System.Collections.Generic;
namespace HRM.Models.Archives;

public partial class DeductionType
{
    public int DeductionTypeId { get; set; }

    public string TypeNameEnglish { get; set; } = null!;

    public string TypeNameDhivehi { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsApplicableToAll { get; set; }

    public int? DerivedOnTypeId { get; set; }

    public int? DeductionAmountTypeId { get; set; }

    public bool HasSchedule { get; set; }

    public DateOnly EffectiveDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsValid { get; set; }

    public bool IsDerived { get; set; }

    public int OperationLogId { get; set; }

    public int? RequestId { get; set; }

    public int? CurrencyTypeId { get; set; }

    public double? MinAmount { get; set; }

    public double? MaxAmount { get; set; }

    public string? Code { get; set; }

    public bool HasMax { get; set; }

    public bool HasMin { get; set; }

    public bool IsAmountGivenWhenAssigning { get; set; }

    public int OrganisationId { get; set; }

    public virtual Organisation OwnerOrganisation { get; set; } = null!;

    public virtual Request? Request { get; set; }

    public virtual ICollection<StaffAssignedDeduction> StaffAssignedDeductions { get; set; } = new List<StaffAssignedDeduction>();
}
