using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class OperationLog
{
    public int OperationLogId { get; set; }

    public int OperationLogActionId { get; set; }

    public int? CreatedByIndividualId { get; set; }

    public int? CreatedByUserId { get; set; }

    public int CreatedByContextId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public int? UpdatedByIndividualId { get; set; }

    public int? UpdatedByUserId { get; set; }

    public string CreatedByIp { get; set; } = null!;

    public string UpdatedByIp { get; set; } = null!;

    public string? Remarks { get; set; }

    public int UpdatedContextId { get; set; }

    public int? CreatedByUserOrganisationId { get; set; }

    public int? UpdatedByUserOrganisationId { get; set; }

    public virtual ICollection<AttachedBreakTime> AttachedBreakTimes { get; set; } = new List<AttachedBreakTime>();

    public virtual ICollection<AttachedBreakTimesDay> AttachedBreakTimesDays { get; set; } = new List<AttachedBreakTimesDay>();

    public virtual ICollection<AttachedPayrollItemsProcessingWhiteList> AttachedPayrollItemsProcessingWhiteLists { get; set; } = new List<AttachedPayrollItemsProcessingWhiteList>();

    public virtual ICollection<AttendanceClientInstance> AttendanceClientInstances { get; set; } = new List<AttendanceClientInstance>();

    public virtual ICollection<AttendanceDevice> AttendanceDevices { get; set; } = new List<AttendanceDevice>();

    public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();

    public virtual ICollection<BasicSalary> BasicSalaries { get; set; } = new List<BasicSalary>();

    public virtual ICollection<BreakTime> BreakTimes { get; set; } = new List<BreakTime>();

    public virtual ICollection<BulkUploadedDocument> BulkUploadedDocuments { get; set; } = new List<BulkUploadedDocument>();

    public virtual ICollection<BusinessEntity> BusinessEntities { get; set; } = new List<BusinessEntity>();

    public virtual ICollection<BusinessEntitiesDocument> BusinessEntitiesDocuments { get; set; } = new List<BusinessEntitiesDocument>();

    public virtual ICollection<BusinessEntityCalendar> BusinessEntityCalendars { get; set; } = new List<BusinessEntityCalendar>();

    public virtual ICollection<BusinessEntityRelation> BusinessEntityRelations { get; set; } = new List<BusinessEntityRelation>();

    public virtual ICollection<BusinessEntitySchedule> BusinessEntitySchedules { get; set; } = new List<BusinessEntitySchedule>();

    public virtual ICollection<BussinessEntityContactInformation> BussinessEntityContactInformations { get; set; } = new List<BussinessEntityContactInformation>();

    public virtual ICollection<CalendarInstance> CalendarInstances { get; set; } = new List<CalendarInstance>();

    public virtual ICollection<CalendarMonth> CalendarMonths { get; set; } = new List<CalendarMonth>();

    public virtual ICollection<CalendarType> CalendarTypes { get; set; } = new List<CalendarType>();

    public virtual ICollection<Calendar> Calendars { get; set; } = new List<Calendar>();

    public virtual ICollection<CheckListItem> CheckListItems { get; set; } = new List<CheckListItem>();

    public virtual Individual? CreatedByIndividual { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual UserOrganisation? CreatedByUserOrganisation { get; set; }

    public virtual ICollection<CurrencyExchangeRate> CurrencyExchangeRates { get; set; } = new List<CurrencyExchangeRate>();

    public virtual ICollection<DataCorrectionRequest> DataCorrectionRequests { get; set; } = new List<DataCorrectionRequest>();

    public virtual ICollection<DeductionTypeAmount> DeductionTypeAmounts { get; set; } = new List<DeductionTypeAmount>();

    public virtual ICollection<DerivedOnPayrollItemType> DerivedOnPayrollItemTypes { get; set; } = new List<DerivedOnPayrollItemType>();

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<Element> Elements { get; set; } = new List<Element>();

    public virtual ICollection<GroupLeaveSet> GroupLeaveSets { get; set; } = new List<GroupLeaveSet>();

    public virtual ICollection<GroupSchedule> GroupSchedules { get; set; } = new List<GroupSchedule>();

    public virtual ICollection<GroupStaff> GroupStaffs { get; set; } = new List<GroupStaff>();

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    public virtual ICollection<Idcard> Idcards { get; set; } = new List<Idcard>();

    public virtual ICollection<JobLeaveType> JobLeaveTypes { get; set; } = new List<JobLeaveType>();

    public virtual ICollection<JobPositionBasicSalary> JobPositionBasicSalaries { get; set; } = new List<JobPositionBasicSalary>();

    public virtual ICollection<JobPosition> JobPositions { get; set; } = new List<JobPosition>();

    public virtual ICollection<JobPositionsRequest> JobPositionsRequests { get; set; } = new List<JobPositionsRequest>();

    public virtual ICollection<JobType> JobTypes { get; set; } = new List<JobType>();

    public virtual ICollection<KeyHolder> KeyHolders { get; set; } = new List<KeyHolder>();

    public virtual ICollection<Kpidocument> Kpidocuments { get; set; } = new List<Kpidocument>();

    public virtual ICollection<LeaveChangeRequest> LeaveChangeRequests { get; set; } = new List<LeaveChangeRequest>();

    public virtual ICollection<LeaveDocument> LeaveDocuments { get; set; } = new List<LeaveDocument>();

    public virtual ICollection<LeaveForm> LeaveForms { get; set; } = new List<LeaveForm>();

    public virtual ICollection<LeaveSetLeaveType> LeaveSetLeaveTypes { get; set; } = new List<LeaveSetLeaveType>();

    public virtual ICollection<LeaveSet> LeaveSets { get; set; } = new List<LeaveSet>();

    public virtual ICollection<LeaveSpendingLocation> LeaveSpendingLocations { get; set; } = new List<LeaveSpendingLocation>();

    public virtual ICollection<LeaveType> LeaveTypes { get; set; } = new List<LeaveType>();

    public virtual ICollection<Leaf> Leaves { get; set; } = new List<Leaf>();

    public virtual ICollection<LeavesBulkUploadedDocument> LeavesBulkUploadedDocuments { get; set; } = new List<LeavesBulkUploadedDocument>();

    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();

    public virtual ICollection<Month> Months { get; set; } = new List<Month>();

    public virtual ICollection<NoPayLeaf> NoPayLeaves { get; set; } = new List<NoPayLeaf>();

    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

    public virtual ICollection<OnlineLocation> OnlineLocations { get; set; } = new List<OnlineLocation>();

    public virtual ICollection<OnlineSignInOrganisation> OnlineSignInOrganisations { get; set; } = new List<OnlineSignInOrganisation>();

    public virtual OperationLogActionType OperationLogAction { get; set; } = null!;

    public virtual ICollection<OrganisationDeductionType> OrganisationDeductionTypes { get; set; } = new List<OrganisationDeductionType>();

    public virtual ICollection<OrganisationPayrollItemType> OrganisationPayrollItemTypes { get; set; } = new List<OrganisationPayrollItemType>();

    public virtual ICollection<OrganisationPayrollPeriodJobType> OrganisationPayrollPeriodJobTypes { get; set; } = new List<OrganisationPayrollPeriodJobType>();

    public virtual ICollection<OrganisationPayrollPeriod> OrganisationPayrollPeriods { get; set; } = new List<OrganisationPayrollPeriod>();

    public virtual ICollection<OrganisationStructureCalendar> OrganisationStructureCalendars { get; set; } = new List<OrganisationStructureCalendar>();

    public virtual ICollection<OrganisationStructureHeadIncharge> OrganisationStructureHeadIncharges { get; set; } = new List<OrganisationStructureHeadIncharge>();

    public virtual ICollection<OrganisationStructureSchedule> OrganisationStructureSchedules { get; set; } = new List<OrganisationStructureSchedule>();

    public virtual ICollection<OrganisationStructureSupervisor> OrganisationStructureSupervisors { get; set; } = new List<OrganisationStructureSupervisor>();

    public virtual ICollection<OrganisationStructure> OrganisationStructures { get; set; } = new List<OrganisationStructure>();

    public virtual ICollection<OtherSalaryRateDefinition> OtherSalaryRateDefinitions { get; set; } = new List<OtherSalaryRateDefinition>();

    public virtual ICollection<OtpreApprovalTime> OtpreApprovalTimes { get; set; } = new List<OtpreApprovalTime>();

    public virtual ICollection<Ot> Ots { get; set; } = new List<Ot>();

    public virtual ICollection<Passport> Passports { get; set; } = new List<Passport>();

    public virtual ICollection<PayrollCycleLinkedRequest> PayrollCycleLinkedRequests { get; set; } = new List<PayrollCycleLinkedRequest>();

    public virtual ICollection<PayrollItemDailySapsheetDetail> PayrollItemDailySapsheetDetails { get; set; } = new List<PayrollItemDailySapsheetDetail>();

    public virtual ICollection<PayrollItemProcessingFlow> PayrollItemProcessingFlows { get; set; } = new List<PayrollItemProcessingFlow>();

    public virtual ICollection<PayrollItemTypeAmount> PayrollItemTypeAmounts { get; set; } = new List<PayrollItemTypeAmount>();

    public virtual ICollection<PayrollItemTypeDependencyAmount> PayrollItemTypeDependencyAmounts { get; set; } = new List<PayrollItemTypeDependencyAmount>();

    public virtual ICollection<PayrollItemTypeDependencyTimePeriodAmount> PayrollItemTypeDependencyTimePeriodAmounts { get; set; } = new List<PayrollItemTypeDependencyTimePeriodAmount>();

    public virtual ICollection<PayrollItemTypeProcessingCode> PayrollItemTypeProcessingCodes { get; set; } = new List<PayrollItemTypeProcessingCode>();

    public virtual ICollection<PayrollItemType> PayrollItemTypes { get; set; } = new List<PayrollItemType>();

    public virtual ICollection<PayrollItemsProcessingWhiteList> PayrollItemsProcessingWhiteLists { get; set; } = new List<PayrollItemsProcessingWhiteList>();

    public virtual ICollection<PercentageVariableDeductionAmount> PercentageVariableDeductionAmounts { get; set; } = new List<PercentageVariableDeductionAmount>();

    public virtual ICollection<PositionBasicSalary> PositionBasicSalaries { get; set; } = new List<PositionBasicSalary>();

    public virtual ICollection<PositionClassification> PositionClassifications { get; set; } = new List<PositionClassification>();

    public virtual ICollection<PositionRank> PositionRanks { get; set; } = new List<PositionRank>();

    public virtual ICollection<Position> Positions { get; set; } = new List<Position>();

    public virtual ICollection<RequestCheckListVerification> RequestCheckListVerifications { get; set; } = new List<RequestCheckListVerification>();

    public virtual ICollection<RequestDocumentVerification> RequestDocumentVerifications { get; set; } = new List<RequestDocumentVerification>();

    public virtual ICollection<RequestDocument> RequestDocuments { get; set; } = new List<RequestDocument>();

    public virtual ICollection<RequestJob> RequestJobs { get; set; } = new List<RequestJob>();

    public virtual ICollection<RequestTeam> RequestTeams { get; set; } = new List<RequestTeam>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    public virtual ICollection<SalaryRateDefinition> SalaryRateDefinitions { get; set; } = new List<SalaryRateDefinition>();

    public virtual ICollection<Sapexception> Sapexceptions { get; set; } = new List<Sapexception>();

    public virtual ICollection<ScheduleElement> ScheduleElements { get; set; } = new List<ScheduleElement>();

    public virtual ICollection<ScheduleOwner> ScheduleOwners { get; set; } = new List<ScheduleOwner>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<ServiceCheckListVerification> ServiceCheckListVerifications { get; set; } = new List<ServiceCheckListVerification>();

    public virtual ICollection<ServiceDocumentVerification> ServiceDocumentVerifications { get; set; } = new List<ServiceDocumentVerification>();

    public virtual ICollection<ServiceDocument> ServiceDocuments { get; set; } = new List<ServiceDocument>();

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();

    public virtual ICollection<ShiftScheduleDay> ShiftScheduleDays { get; set; } = new List<ShiftScheduleDay>();

    public virtual ICollection<SickLeaveForm> SickLeaveForms { get; set; } = new List<SickLeaveForm>();

    public virtual ICollection<SpecialDayType> SpecialDayTypes { get; set; } = new List<SpecialDayType>();

    public virtual ICollection<SpecialDay> SpecialDays { get; set; } = new List<SpecialDay>();

    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();

    public virtual ICollection<StaffAssignedDeductionAmount> StaffAssignedDeductionAmounts { get; set; } = new List<StaffAssignedDeductionAmount>();

    public virtual ICollection<StaffAssignedDeduction> StaffAssignedDeductions { get; set; } = new List<StaffAssignedDeduction>();

    public virtual ICollection<StaffDailyAttendanceSummery> StaffDailyAttendanceSummeries { get; set; } = new List<StaffDailyAttendanceSummery>();

    public virtual ICollection<StaffEnrollmentNumber> StaffEnrollmentNumbers { get; set; } = new List<StaffEnrollmentNumber>();

    public virtual ICollection<StaffPayrollItemTypeAmount> StaffPayrollItemTypeAmounts { get; set; } = new List<StaffPayrollItemTypeAmount>();

    public virtual ICollection<StaffPayrollItemTypeSchedule> StaffPayrollItemTypeSchedules { get; set; } = new List<StaffPayrollItemTypeSchedule>();

    public virtual ICollection<StaffPayrollItemType> StaffPayrollItemTypes { get; set; } = new List<StaffPayrollItemType>();

    public virtual ICollection<StaffSalary> StaffSalaries { get; set; } = new List<StaffSalary>();

    public virtual ICollection<StaffSalaryDeduction> StaffSalaryDeductions { get; set; } = new List<StaffSalaryDeduction>();

    public virtual ICollection<StaffSalaryPayrollItem> StaffSalaryPayrollItems { get; set; } = new List<StaffSalaryPayrollItem>();

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();

    public virtual Context UpdatedContext { get; set; } = null!;

    public virtual ICollection<UserAssignedUserGroup> UserAssignedUserGroups { get; set; } = new List<UserAssignedUserGroup>();

    public virtual ICollection<UserGroupRole> UserGroupRoles { get; set; } = new List<UserGroupRole>();

    public virtual ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();

    public virtual ICollection<UserOrganisationRole> UserOrganisationRoles { get; set; } = new List<UserOrganisationRole>();

    public virtual ICollection<UserOrganisationUserGroup> UserOrganisationUserGroups { get; set; } = new List<UserOrganisationUserGroup>();

    public virtual ICollection<UserOrganisation> UserOrganisations { get; set; } = new List<UserOrganisation>();

    public virtual ICollection<UserPreference> UserPreferences { get; set; } = new List<UserPreference>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual ICollection<UserServiceRole> UserServiceRoles { get; set; } = new List<UserServiceRole>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<WorkShift> WorkShifts { get; set; } = new List<WorkShift>();

    public virtual ICollection<WorkShiftsBreakTime> WorkShiftsBreakTimes { get; set; } = new List<WorkShiftsBreakTime>();

    public virtual ICollection<WorkingDay> WorkingDays { get; set; } = new List<WorkingDay>();
}
