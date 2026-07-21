using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class PayrollItemType
{
    public int PayrollItemTypeId { get; set; }

    public string NameEnglish { get; set; } = null!;

    public string NameDhivehi { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? Code { get; set; }

    public int? RecurrenceTypeId { get; set; }

    public string? ReccurencePattern { get; set; }

    public bool? HasDependency { get; set; }

    public int? DependencyTypeId { get; set; }

    public int? AmountTypeId { get; set; }

    public bool? IsAttendanceDependent { get; set; }

    public bool? IsValid { get; set; }

    public int? OperationLogId { get; set; }

    public bool? IsRecurring { get; set; }

    public bool? HasMin { get; set; }

    public bool? HasMax { get; set; }

    public double? MinValue { get; set; }

    public double? MaxValue { get; set; }

    public int? DerivedOnTypeId { get; set; }

    public int? CurrencyTypeId { get; set; }

    public int? RequestId { get; set; }

    public bool? IsApplicableToAll { get; set; }

    public bool? IsGlobal { get; set; }

    public int? OwnerOrganisationId { get; set; }

    public bool? IsAmountGivenWhenAssigning { get; set; }

    public bool? HasSchedule { get; set; }

    public DateTime? EffectiveDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? AdditionOrDeductionTypeId { get; set; }

    public bool? IsSpecialType { get; set; }

    public string? Sapcode { get; set; }

    public int? AttendanceDependentTypeId { get; set; }

    public int? SapsheetId { get; set; }

    public bool? IsLateFineDeductable { get; set; }

    public virtual AdditionOrDeductionType? AdditionOrDeductionType { get; set; }

    public virtual AmountType? AmountType { get; set; }

    public virtual AttendanceDependentType? AttendanceDependentType { get; set; }

    public virtual CurrencyType? CurrencyType { get; set; }

    public virtual DependencyType? DependencyType { get; set; }

    public virtual ICollection<DerivedOnPayrollItemType> DerivedOnPayrollItemTypeOtherPayrollItemTypes { get; set; } = new List<DerivedOnPayrollItemType>();

    public virtual ICollection<DerivedOnPayrollItemType> DerivedOnPayrollItemTypePayrollItemTypes { get; set; } = new List<DerivedOnPayrollItemType>();

    public virtual DerivedOnType? DerivedOnType { get; set; }

    public virtual OperationLog? OperationLog { get; set; }

    public virtual ICollection<OrganisationPayrollItemType> OrganisationPayrollItemTypes { get; set; } = new List<OrganisationPayrollItemType>();

    public virtual Organisation? OwnerOrganisation { get; set; }

    public virtual ICollection<PayrollItemDailySapsheetDetail> PayrollItemDailySapsheetDetails { get; set; } = new List<PayrollItemDailySapsheetDetail>();

    public virtual ICollection<PayrollItemTypeAmount> PayrollItemTypeAmounts { get; set; } = new List<PayrollItemTypeAmount>();

    public virtual ICollection<PayrollItemTypeDependencyAmount> PayrollItemTypeDependencyAmounts { get; set; } = new List<PayrollItemTypeDependencyAmount>();

    public virtual ICollection<PayrollItemTypeDependencyTimePeriodAmount> PayrollItemTypeDependencyTimePeriodAmounts { get; set; } = new List<PayrollItemTypeDependencyTimePeriodAmount>();

    public virtual ICollection<PayrollItemTypeProcessingCode> PayrollItemTypeProcessingCodes { get; set; } = new List<PayrollItemTypeProcessingCode>();

    public virtual ICollection<PayrollItemsProcessingWhiteList> PayrollItemsProcessingWhiteLists { get; set; } = new List<PayrollItemsProcessingWhiteList>();

    public virtual PrimaryPayrollItemTypeSapwageType? PrimaryPayrollItemTypeSapwageType { get; set; }

    public virtual RecurrenceType? RecurrenceType { get; set; }

    public virtual Request? Request { get; set; }

    public virtual ICollection<Sapexception> Sapexceptions { get; set; } = new List<Sapexception>();

    public virtual Sapsheet? Sapsheet { get; set; }

    public virtual ICollection<StaffPayrollItemType> StaffPayrollItemTypes { get; set; } = new List<StaffPayrollItemType>();

    public virtual ICollection<StaffSalaryPayrollItem> StaffSalaryPayrollItems { get; set; } = new List<StaffSalaryPayrollItem>();
}
