using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class Organisation
{
    //public int BusinessEntityId { get; set; }

    public int BusinessEntityID { get; set; }
    public int OrganisationTypeId { get; set; }

    public string? RegistrationNumber { get; set; }

    public string OrganisationName { get; set; } = null!;

    public string? OrganisationNameDhivehi { get; set; }

    public DateTime? RegistrationDate { get; set; }

    public int CountryId { get; set; }

    public int? ParentOrganisationBusinessEntityId { get; set; }

    public int? CscofficePimaryKey { get; set; }


    public virtual BusinessEntity BusinessEntity { get; set; } = null!;

    public virtual ICollection<AssignedWorkType> AssignedWorkTypes { get; set; } = new List<AssignedWorkType>();

    public virtual ICollection<AttachedPayrollItemsProcessingWhiteList> AttachedPayrollItemsProcessingWhiteLists { get; set; } = new List<AttachedPayrollItemsProcessingWhiteList>();

    public virtual ICollection<AttendanceClientInstance> AttendanceClientInstances { get; set; } = new List<AttendanceClientInstance>();

    public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();

    public virtual ICollection<BreakTime> BreakTimes { get; set; } = new List<BreakTime>();

    public virtual ICollection<BudgetTransaction> BudgetTransactions { get; set; } = new List<BudgetTransaction>();

    

    public virtual Country Country { get; set; } = null!;

    public virtual ICollection<DeductionType> DeductionTypes { get; set; } = new List<DeductionType>();

    public virtual ICollection<DocumentType> DocumentTypes { get; set; } = new List<DocumentType>();

    public virtual ICollection<GroupType> GroupTypes { get; set; } = new List<GroupType>();

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    public virtual ICollection<Organisation> InverseParentOrganisationBusinessEntity { get; set; } = new List<Organisation>();

    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();

    public virtual ICollection<LeaveConfigurableValue> LeaveConfigurableValues { get; set; } = new List<LeaveConfigurableValue>();

    public virtual ICollection<LeaveSet> LeaveSets { get; set; } = new List<LeaveSet>();

    public virtual ICollection<LeaveType> LeaveTypes { get; set; } = new List<LeaveType>();

    public virtual OnlineSignInOrganisation? OnlineSignInOrganisation { get; set; }

    public virtual ICollection<OrganisationBudget> OrganisationBudgets { get; set; } = new List<OrganisationBudget>();

    public virtual ICollection<OrganisationDeductionType> OrganisationDeductionTypes { get; set; } = new List<OrganisationDeductionType>();

    public virtual ICollection<OrganisationPayrollItemType> OrganisationPayrollItemTypes { get; set; } = new List<OrganisationPayrollItemType>();

    public virtual ICollection<OrganisationPayrollPeriod> OrganisationPayrollPeriods { get; set; } = new List<OrganisationPayrollPeriod>();

    public virtual ICollection<OrganisationSapexception> OrganisationSapexceptions { get; set; } = new List<OrganisationSapexception>();

    public virtual ICollection<OrganisationStructureHistory> OrganisationStructureHistories { get; set; } = new List<OrganisationStructureHistory>();

    public virtual ICollection<OrganisationStructureRequest> OrganisationStructureRequests { get; set; } = new List<OrganisationStructureRequest>();

    public virtual ICollection<OrganisationStructure> OrganisationStructures { get; set; } = new List<OrganisationStructure>();

    public virtual OrganisationType OrganisationType { get; set; } = null!;

    public virtual ICollection<OutOfOfficeRequest> OutOfOfficeRequests { get; set; } = new List<OutOfOfficeRequest>();

    public virtual Organisation? ParentOrganisationBusinessEntity { get; set; }

    public virtual ICollection<PayrollConfigurableValue> PayrollConfigurableValues { get; set; } = new List<PayrollConfigurableValue>();

    public virtual ICollection<PayrollItemTypeProcessingCode> PayrollItemTypeProcessingCodes { get; set; } = new List<PayrollItemTypeProcessingCode>();

    public virtual ICollection<PayrollItemType> PayrollItemTypes { get; set; } = new List<PayrollItemType>();

    public virtual ICollection<PayrollItemsProcessingWhiteList> PayrollItemsProcessingWhiteLists { get; set; } = new List<PayrollItemsProcessingWhiteList>();

    public virtual ICollection<PositionClassification> PositionClassifications { get; set; } = new List<PositionClassification>();

    public virtual ICollection<PositionRank> PositionRanks { get; set; } = new List<PositionRank>();

    public virtual ICollection<Position> Positions { get; set; } = new List<Position>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual ICollection<SpecialDayType> SpecialDayTypes { get; set; } = new List<SpecialDayType>();

    public virtual ICollection<StaffEnrollmentNumber> StaffEnrollmentNumbers { get; set; } = new List<StaffEnrollmentNumber>();

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();

    public virtual ICollection<UserOrganisation> UserOrganisations { get; set; } = new List<UserOrganisation>();

    public virtual ICollection<WorkShift> WorkShifts { get; set; } = new List<WorkShift>();


    public virtual ICollection<UserOrganisationRole> UserOrganisationRoles { get; set; } = new List<UserOrganisationRole>();

}
