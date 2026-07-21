using System;
using System.Collections.Generic;
using HRM.Models.Archives;
using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

public partial class HrmTeContext : DbContext
{
    public HrmTeContext()
    {
    }

    public HrmTeContext(DbContextOptions<HrmTeContext> options)
        : base(options)
    {
    }
    public virtual DbSet<PlanningProvider> PlanningProviders { get; set; }

    public virtual DbSet<OrganisationWorkPlanningSetting>
        OrganisationWorkPlanningSettings
    { get; set; }

    public virtual DbSet<AttendanceLogResolution> AttendanceLogResolutions { get; set; }

    public virtual DbSet<AttendanceResolutionStatus> AttendanceResolutionStatuses { get; set; }

    public virtual DbSet<WorkTemplateType> WorkTemplateTypes { get; set; }

    public virtual DbSet<WorkSegmentType> WorkSegmentTypes { get; set; }

    public virtual DbSet<WorkTemplate> WorkTemplates { get; set; }

    public virtual DbSet<WorkTemplateSegment>WorkTemplateSegments{ get; set; }

    public virtual DbSet<WorkAssignmentState>WorkAssignmentStates{ get; set; }

    public virtual DbSet<WorkAssignmentTransferState>WorkAssignmentTransferStates{ get; set; }

    public virtual DbSet<WorkPlan> WorkPlans { get; set; }

    public virtual DbSet<WorkAssignment> WorkAssignments { get; set; }

    public virtual DbSet<WorkAssignmentSegment>WorkAssignmentSegments{ get; set; }

    public virtual DbSet<WorkAssignmentOwner>WorkAssignmentOwners{ get; set; }

    public virtual DbSet<WorkAssignmentTransfer>WorkAssignmentTransfers{ get; set; }

   

















    public virtual DbSet<AdditionDeductionTypeDependencyTimePeriodAmount> AdditionDeductionTypeDependencyTimePeriodAmounts { get; set; }

    public virtual DbSet<AdditionOrDeductionType> AdditionOrDeductionTypes { get; set; }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<AddressBaseType> AddressBaseTypes { get; set; }

    public virtual DbSet<AddressBasis> AddressBases { get; set; }

    public virtual DbSet<AddressInstance> AddressInstances { get; set; }

    public virtual DbSet<AddressInstanceType> AddressInstanceTypes { get; set; }

    public virtual DbSet<AggregatedSalary> AggregatedSalaries { get; set; }

    public virtual DbSet<AmountType> AmountTypes { get; set; }

    public virtual DbSet<AssignedWorkType> AssignedWorkTypes { get; set; }

    public virtual DbSet<Atoll> Atolls { get; set; }

    public virtual DbSet<AttachedBreakTime> AttachedBreakTimes { get; set; }

    public virtual DbSet<AttachedBreakTimesDay> AttachedBreakTimesDays { get; set; }

    public virtual DbSet<AttachedPayrollItemsProcessingWhiteList> AttachedPayrollItemsProcessingWhiteLists { get; set; }

    public virtual DbSet<AttendanceClientInstance> AttendanceClientInstances { get; set; }

    public virtual DbSet<AttendanceClientState> AttendanceClientStates { get; set; }

    public virtual DbSet<AttendanceDependentType> AttendanceDependentTypes { get; set; }

    public virtual DbSet<AttendanceDevice> AttendanceDevices { get; set; }

    public virtual DbSet<AttendanceDeviceInOutType> AttendanceDeviceInOutTypes { get; set; }

    public virtual DbSet<AttendanceDeviceStaff> AttendanceDeviceStaffs { get; set; }

    public virtual DbSet<AttendanceDeviceState> AttendanceDeviceStates { get; set; }

    public virtual DbSet<AttendanceLog> AttendanceLogs { get; set; }

    public virtual DbSet<AttendanceLogChangeRequest> AttendanceLogChangeRequests { get; set; }

    public virtual DbSet<AttendanceLogChangeRequestType> AttendanceLogChangeRequestTypes { get; set; }

    public virtual DbSet<AttendanceLogMode> AttendanceLogModes { get; set; }

    public virtual DbSet<AttendanceLogRequest> AttendanceLogRequests { get; set; }

    public virtual DbSet<AttendanceLogState> AttendanceLogStates { get; set; }

    public virtual DbSet<AttendanceLogsTemp> AttendanceLogsTemps { get; set; }

    public virtual DbSet<BasicSalary> BasicSalaries { get; set; }

    public virtual DbSet<BreakTime> BreakTimes { get; set; }

    public virtual DbSet<BudgetItem> BudgetItems { get; set; }

    public virtual DbSet<BudgetTransaction> BudgetTransactions { get; set; }

    public virtual DbSet<BudgetTransactionType> BudgetTransactionTypes { get; set; }

    public virtual DbSet<BulkUploadedDocument> BulkUploadedDocuments { get; set; }

    public virtual DbSet<BulkUploadedDocumentSummary> BulkUploadedDocumentSummaries { get; set; }

    public virtual DbSet<BusinessEntitiesDocument> BusinessEntitiesDocuments { get; set; }

    public virtual DbSet<BusinessEntity> BusinessEntities { get; set; }

    public virtual DbSet<BusinessEntityCalendar> BusinessEntityCalendars { get; set; }

    public virtual DbSet<BusinessEntityLocation> BusinessEntityLocations { get; set; }

    public virtual DbSet<BusinessEntityLocationType> BusinessEntityLocationTypes { get; set; }

    public virtual DbSet<BusinessEntityRelatedLocationType> BusinessEntityRelatedLocationTypes { get; set; }

    public virtual DbSet<BusinessEntityRelation> BusinessEntityRelations { get; set; }

    public virtual DbSet<BusinessEntityRelationAssignedRole> BusinessEntityRelationAssignedRoles { get; set; }

    public virtual DbSet<BusinessEntityRelationState> BusinessEntityRelationStates { get; set; }

    public virtual DbSet<BusinessEntityRelationType> BusinessEntityRelationTypes { get; set; }

    public virtual DbSet<BusinessEntitySchedule> BusinessEntitySchedules { get; set; }

    public virtual DbSet<BusinessEntityState> BusinessEntityStates { get; set; }

    public virtual DbSet<BusinessEntityType> BusinessEntityTypes { get; set; }

    public virtual DbSet<BussinessEntityContactInformation> BussinessEntityContactInformations { get; set; }

    public virtual DbSet<Calendar> Calendars { get; set; }

    public virtual DbSet<CalendarInstance> CalendarInstances { get; set; }

    public virtual DbSet<CalendarMonth> CalendarMonths { get; set; }

    public virtual DbSet<CalendarSchedule> CalendarSchedules { get; set; }

    public virtual DbSet<CalendarType> CalendarTypes { get; set; }

    public virtual DbSet<CheckListItem> CheckListItems { get; set; }

    public virtual DbSet<City> Cities { get; set; }

    public virtual DbSet<CommercialIslandType> CommercialIslandTypes { get; set; }

    public virtual DbSet<ConfigurableValue> ConfigurableValues { get; set; }

    public virtual DbSet<ContactInformationType> ContactInformationTypes { get; set; }

    public virtual DbSet<Context> Contexts { get; set; }

    public virtual DbSet<Country> Countries { get; set; }

    public virtual DbSet<Currency> Currencies { get; set; }

    public virtual DbSet<CurrencyExchangeRate> CurrencyExchangeRates { get; set; }

    public virtual DbSet<CurrencyType> CurrencyTypes { get; set; }

    public virtual DbSet<DataCorrectionActionType> DataCorrectionActionTypes { get; set; }

    public virtual DbSet<DataCorrectionAttribute> DataCorrectionAttributes { get; set; }

    public virtual DbSet<DataCorrectionAttributeLookupType> DataCorrectionAttributeLookupTypes { get; set; }

    public virtual DbSet<DataCorrectionRequest> DataCorrectionRequests { get; set; }

    public virtual DbSet<DataCorrectionRequestAttributeValue> DataCorrectionRequestAttributeValues { get; set; }

    public virtual DbSet<DataCorrectionRequestState> DataCorrectionRequestStates { get; set; }

    public virtual DbSet<DataCorrectionRequestType> DataCorrectionRequestTypes { get; set; }

    public virtual DbSet<DataCorrectionRequestTypeAttribute> DataCorrectionRequestTypeAttributes { get; set; }

    //public virtual DbSet<DayOfWeek> DayOfWeeks { get; set; }

    public virtual DbSet<DayType> DayTypes { get; set; }

    public virtual DbSet<DeductionAmountType> DeductionAmountTypes { get; set; }

    public virtual DbSet<DeductionType> DeductionTypes { get; set; }

    public virtual DbSet<DeductionTypeAmount> DeductionTypeAmounts { get; set; }

    public virtual DbSet<DependencyType> DependencyTypes { get; set; }

    public virtual DbSet<DerivedAdditionDeductionType> DerivedAdditionDeductionTypes { get; set; }

    public virtual DbSet<DerivedAdditionDeductionTypeItem> DerivedAdditionDeductionTypeItems { get; set; }

    public virtual DbSet<DerivedOnPayrollItemType> DerivedOnPayrollItemTypes { get; set; }

    public virtual DbSet<DerivedOnType> DerivedOnTypes { get; set; }

    public virtual DbSet<Dhafthar> Dhafthars { get; set; }

    public virtual DbSet<DnrlookupCache> DnrlookupCaches { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<DocumentState> DocumentStates { get; set; }

    public virtual DbSet<DocumentType> DocumentTypes { get; set; }

    public virtual DbSet<Element> Elements { get; set; }

    public virtual DbSet<EventType> EventTypes { get; set; }

    public virtual DbSet<FacilityRegistrationType> FacilityRegistrationTypes { get; set; }

    public virtual DbSet<GenderType> GenderTypes { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<GroupConfigurableValue> GroupConfigurableValues { get; set; }

    public virtual DbSet<GroupConfigurableValueType> GroupConfigurableValueTypes { get; set; }

    public virtual DbSet<GroupLeaveSet> GroupLeaveSets { get; set; }

    public virtual DbSet<GroupSchedule> GroupSchedules { get; set; }

    public virtual DbSet<GroupStaff> GroupStaffs { get; set; }

    public virtual DbSet<GroupType> GroupTypes { get; set; }

    public virtual DbSet<Idcard> Idcards { get; set; }

    public virtual DbSet<IdcardState> IdcardStates { get; set; }

    public virtual DbSet<IdentityCardType> IdentityCardTypes { get; set; }

    public virtual DbSet<InOutMode> InOutModes { get; set; }

    public virtual DbSet<Individual> Individuals { get; set; }

    public virtual DbSet<Island> Islands { get; set; }

    public virtual DbSet<Job> Jobs { get; set; }

    public virtual DbSet<JobLeaveType> JobLeaveTypes { get; set; }

    public virtual DbSet<JobPosition> JobPositions { get; set; }

    public virtual DbSet<JobPositionBasicSalary> JobPositionBasicSalaries { get; set; }

    public virtual DbSet<JobPositionState> JobPositionStates { get; set; }

    public virtual DbSet<JobPositionsRequest> JobPositionsRequests { get; set; }

    public virtual DbSet<JobRequest> JobRequests { get; set; }

    public virtual DbSet<JobSapexception> JobSapexceptions { get; set; }

    public virtual DbSet<JobState> JobStates { get; set; }

    public virtual DbSet<JobTerminationType> JobTerminationTypes { get; set; }

    public virtual DbSet<JobType> JobTypes { get; set; }

    public virtual DbSet<JobTypesException> JobTypesExceptions { get; set; }

    public virtual DbSet<KeyHolder> KeyHolders { get; set; }

    public virtual DbSet<Kpidocument> Kpidocuments { get; set; }

    public virtual DbSet<Language> Languages { get; set; }

    public virtual DbSet<Leaf> Leaves { get; set; }

    public virtual DbSet<LeaveChangeRequest> LeaveChangeRequests { get; set; }

    public virtual DbSet<LeaveChangeRequestType> LeaveChangeRequestTypes { get; set; }

    public virtual DbSet<LeaveConfigurableValue> LeaveConfigurableValues { get; set; }

    public virtual DbSet<LeaveConfigurableValueType> LeaveConfigurableValueTypes { get; set; }

    public virtual DbSet<LeaveDocument> LeaveDocuments { get; set; }

    public virtual DbSet<LeaveForm> LeaveForms { get; set; }

    public virtual DbSet<LeaveLodgeType> LeaveLodgeTypes { get; set; }

    public virtual DbSet<LeaveReason> LeaveReasons { get; set; }

    public virtual DbSet<LeaveRequest> LeaveRequests { get; set; }

    public virtual DbSet<LeaveSet> LeaveSets { get; set; }

    public virtual DbSet<LeaveSetLeaveType> LeaveSetLeaveTypes { get; set; }

    public virtual DbSet<LeaveSpendingLocation> LeaveSpendingLocations { get; set; }

    public virtual DbSet<LeaveState> LeaveStates { get; set; }

    public virtual DbSet<LeaveType> LeaveTypes { get; set; }

    public virtual DbSet<LeaveTypeReasonType> LeaveTypeReasonTypes { get; set; }

    public virtual DbSet<LeaveWorkHandOver> LeaveWorkHandOvers { get; set; }

    public virtual DbSet<LeaveWorkHandOverTask> LeaveWorkHandOverTasks { get; set; }

    public virtual DbSet<LeavesBulkUploadedDocument> LeavesBulkUploadedDocuments { get; set; }

    public virtual DbSet<LinkType> LinkTypes { get; set; }

    public virtual DbSet<LocalUserState> LocalUserStates { get; set; }

    public virtual DbSet<Location> Locations { get; set; }

    public virtual DbSet<LocationType> LocationTypes { get; set; }

    public virtual DbSet<Mimetype> Mimetypes { get; set; }

    public virtual DbSet<Module> Modules { get; set; }

    public virtual DbSet<Month> Months { get; set; }

    public virtual DbSet<NavigationLink> NavigationLinks { get; set; }

    public virtual DbSet<NavigationLinkFacilityType> NavigationLinkFacilityTypes { get; set; }

    public virtual DbSet<NoPayLeaf> NoPayLeaves { get; set; }

    public virtual DbSet<NoPayLeaveType> NoPayLeaveTypes { get; set; }

    public virtual DbSet<Note> Notes { get; set; }

    public virtual DbSet<NoteReason> NoteReasons { get; set; }

    public virtual DbSet<OfficialTripDetail> OfficialTripDetails { get; set; }

    public virtual DbSet<OfficialTripLocation> OfficialTripLocations { get; set; }

    public virtual DbSet<OfficialTripType> OfficialTripTypes { get; set; }

    public virtual DbSet<OnlineAddress> OnlineAddresses { get; set; }

    public virtual DbSet<OnlineAddressBasis> OnlineAddressBases { get; set; }

    public virtual DbSet<OnlineAddressInstance> OnlineAddressInstances { get; set; }

    public virtual DbSet<OnlineArea> OnlineAreas { get; set; }

    public virtual DbSet<OnlineBusinessEntityLocation> OnlineBusinessEntityLocations { get; set; }

    public virtual DbSet<OnlineDhafthar> OnlineDhafthars { get; set; }

    public virtual DbSet<OnlineLocation> OnlineLocations { get; set; }

    public virtual DbSet<OnlineSignInOrganisation> OnlineSignInOrganisations { get; set; }

    public virtual DbSet<OperationLog> OperationLogs { get; set; }

    public virtual DbSet<OperationLogActionType> OperationLogActionTypes { get; set; }

    public virtual DbSet<Organisation> Organisations { get; set; }

    public virtual DbSet<OrganisationBudget> OrganisationBudgets { get; set; }

    public virtual DbSet<OrganisationBudgetMonthlySummary> OrganisationBudgetMonthlySummaries { get; set; }

    public virtual DbSet<OrganisationBudgetSummary> OrganisationBudgetSummaries { get; set; }

    public virtual DbSet<OrganisationBudgetSummeryAggregate> OrganisationBudgetSummeryAggregates { get; set; }

    public virtual DbSet<OrganisationDeductionType> OrganisationDeductionTypes { get; set; }

    public virtual DbSet<OrganisationPayrollItemType> OrganisationPayrollItemTypes { get; set; }

    public virtual DbSet<OrganisationPayrollPeriod> OrganisationPayrollPeriods { get; set; }

    public virtual DbSet<OrganisationPayrollPeriodJobType> OrganisationPayrollPeriodJobTypes { get; set; }

    public virtual DbSet<OrganisationSapexception> OrganisationSapexceptions { get; set; }

    public virtual DbSet<OrganisationStructure> OrganisationStructures { get; set; }

    public virtual DbSet<OrganisationStructureCalendar> OrganisationStructureCalendars { get; set; }

    public virtual DbSet<OrganisationStructureDraft> OrganisationStructureDrafts { get; set; }

    public virtual DbSet<OrganisationStructureHeadIncharge> OrganisationStructureHeadIncharges { get; set; }

    public virtual DbSet<OrganisationStructureHistory> OrganisationStructureHistories { get; set; }

    public virtual DbSet<OrganisationStructureLocation> OrganisationStructureLocations { get; set; }

    public virtual DbSet<OrganisationStructureLocationType> OrganisationStructureLocationTypes { get; set; }

    public virtual DbSet<OrganisationStructureRelation> OrganisationStructureRelations { get; set; }

    public virtual DbSet<OrganisationStructureRequest> OrganisationStructureRequests { get; set; }

    public virtual DbSet<OrganisationStructureRequestType> OrganisationStructureRequestTypes { get; set; }

    public virtual DbSet<OrganisationStructureSchedule> OrganisationStructureSchedules { get; set; }

    public virtual DbSet<OrganisationStructureState> OrganisationStructureStates { get; set; }

    public virtual DbSet<OrganisationStructureSupervisor> OrganisationStructureSupervisors { get; set; }

    public virtual DbSet<OrganisationStructureType> OrganisationStructureTypes { get; set; }

    public virtual DbSet<OrganisationType> OrganisationTypes { get; set; }

    public virtual DbSet<OrganisationalStructureSapexception> OrganisationalStructureSapexceptions { get; set; }

    public virtual DbSet<Ot> Ots { get; set; }

    public virtual DbSet<OtherSalaryRateDefinition> OtherSalaryRateDefinitions { get; set; }

    public virtual DbSet<OtpreApproval> OtpreApprovals { get; set; }

    public virtual DbSet<OtpreApprovalTime> OtpreApprovalTimes { get; set; }

    public virtual DbSet<Otrequest> Otrequests { get; set; }

    public virtual DbSet<Ottype> Ottypes { get; set; }

    public virtual DbSet<OutOfOfficeRequest> OutOfOfficeRequests { get; set; }

    public virtual DbSet<OutOfficeRequestType> OutOfficeRequestTypes { get; set; }

    public virtual DbSet<OwnerType> OwnerTypes { get; set; }

    public virtual DbSet<Passport> Passports { get; set; }

    public virtual DbSet<PassportState> PassportStates { get; set; }

    public virtual DbSet<PayrollConfigurableValue> PayrollConfigurableValues { get; set; }

    public virtual DbSet<PayrollConfigurableValueType> PayrollConfigurableValueTypes { get; set; }

    public virtual DbSet<PayrollCycle> PayrollCycles { get; set; }

    public virtual DbSet<PayrollCycleLinkedRequest> PayrollCycleLinkedRequests { get; set; }

    public virtual DbSet<PayrollCycleProcessingState> PayrollCycleProcessingStates { get; set; }

    public virtual DbSet<PayrollItemDailySapsheetDetail> PayrollItemDailySapsheetDetails { get; set; }

    public virtual DbSet<PayrollItemProcessingFlow> PayrollItemProcessingFlows { get; set; }

    public virtual DbSet<PayrollItemType> PayrollItemTypes { get; set; }

    public virtual DbSet<PayrollItemTypeAmount> PayrollItemTypeAmounts { get; set; }

    public virtual DbSet<PayrollItemTypeDependencyAmount> PayrollItemTypeDependencyAmounts { get; set; }

    public virtual DbSet<PayrollItemTypeDependencyTimePeriodAmount> PayrollItemTypeDependencyTimePeriodAmounts { get; set; }

    public virtual DbSet<PayrollItemTypeProcessingCode> PayrollItemTypeProcessingCodes { get; set; }

    public virtual DbSet<PayrollItemsProcessingWhiteList> PayrollItemsProcessingWhiteLists { get; set; }

    public virtual DbSet<PercentageVariableDeductionAmount> PercentageVariableDeductionAmounts { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<PositionBasicSalary> PositionBasicSalaries { get; set; }

    public virtual DbSet<PositionClassification> PositionClassifications { get; set; }

    public virtual DbSet<PositionRank> PositionRanks { get; set; }

    public virtual DbSet<PositionType> PositionTypes { get; set; }

    public virtual DbSet<PrimaryPayrollItemTypeSapwageType> PrimaryPayrollItemTypeSapwageTypes { get; set; }

    public virtual DbSet<RecurrenceType> RecurrenceTypes { get; set; }

    public virtual DbSet<Region> Regions { get; set; }

    public virtual DbSet<Request> Requests { get; set; }

    public virtual DbSet<RequestCheckListVerification> RequestCheckListVerifications { get; set; }

    public virtual DbSet<RequestDocument> RequestDocuments { get; set; }

    public virtual DbSet<RequestDocumentVerification> RequestDocumentVerifications { get; set; }

    public virtual DbSet<RequestJob> RequestJobs { get; set; }

    public virtual DbSet<RequestState> RequestStates { get; set; }

    public virtual DbSet<RequestTeam> RequestTeams { get; set; }

    public virtual DbSet<RequestType> RequestTypes { get; set; }

    public virtual DbSet<RequestTypeSpecificDocumentType> RequestTypeSpecificDocumentTypes { get; set; }

    public virtual DbSet<RequestTypesAllowedRequestStateTransition> RequestTypesAllowedRequestStateTransitions { get; set; }

    public virtual DbSet<RequestTypesStatesRequiredRole> RequestTypesStatesRequiredRoles { get; set; }

    public virtual DbSet<RequestTypesStatesRequiredRolesForProcessing> RequestTypesStatesRequiredRolesForProcessings { get; set; }

    public virtual DbSet<RequestsView> RequestsViews { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SalaryRateDefinition> SalaryRateDefinitions { get; set; }

    public virtual DbSet<SalaryRateDefinitionForType> SalaryRateDefinitionForTypes { get; set; }

    public virtual DbSet<Sapexception> Sapexceptions { get; set; }

    public virtual DbSet<SapexceptionActionType> SapexceptionActionTypes { get; set; }

    public virtual DbSet<SapexceptionType> SapexceptionTypes { get; set; }

    public virtual DbSet<Sapsheet> Sapsheets { get; set; }

    public virtual DbSet<SapwageType> SapwageTypes { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<ScheduleElement> ScheduleElements { get; set; }

    public virtual DbSet<ScheduleOwner> ScheduleOwners { get; set; }

    public virtual DbSet<ScheduleType> ScheduleTypes { get; set; }

    public virtual DbSet<SequenceNumberType> SequenceNumberTypes { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ServiceCheckListVerification> ServiceCheckListVerifications { get; set; }

    public virtual DbSet<ServiceDocument> ServiceDocuments { get; set; }

    public virtual DbSet<ServiceDocumentVerification> ServiceDocumentVerifications { get; set; }

    public virtual DbSet<ServiceState> ServiceStates { get; set; }

    public virtual DbSet<ServiceType> ServiceTypes { get; set; }

    public virtual DbSet<ServiceTypesAllowedServiceStateTransition> ServiceTypesAllowedServiceStateTransitions { get; set; }

    public virtual DbSet<ShiftSchedule> ShiftSchedules { get; set; }

    public virtual DbSet<ShiftScheduleDay> ShiftScheduleDays { get; set; }

    public virtual DbSet<SickLeaveForm> SickLeaveForms { get; set; }

    public virtual DbSet<SickLeaveLodgeType> SickLeaveLodgeTypes { get; set; }

    public virtual DbSet<SpecialDay> SpecialDays { get; set; }

    public virtual DbSet<SpecialDayShift> SpecialDayShifts { get; set; }

    public virtual DbSet<SpecialDayType> SpecialDayTypes { get; set; }

    public virtual DbSet<SsouserState> SsouserStates { get; set; }

    public virtual DbSet<Staff> Staffs { get; set; }

    public virtual DbSet<StaffAssignedDeduction> StaffAssignedDeductions { get; set; }

    public virtual DbSet<StaffAssignedDeductionAmount> StaffAssignedDeductionAmounts { get; set; }

    public virtual DbSet<StaffDailyAttandanceSummaryIssue> StaffDailyAttandanceSummaryIssues { get; set; }

    public virtual DbSet<StaffDailyAttandanceSummaryIssueType> StaffDailyAttandanceSummaryIssueTypes { get; set; }

    public virtual DbSet<StaffDailyAttendanceSummery> StaffDailyAttendanceSummeries { get; set; }

    public virtual DbSet<StaffEnrollmentNumber> StaffEnrollmentNumbers { get; set; }

    public virtual DbSet<StaffPayrollItemDetail> StaffPayrollItemDetails { get; set; }

    public virtual DbSet<StaffPayrollItemType> StaffPayrollItemTypes { get; set; }

    public virtual DbSet<StaffPayrollItemTypeAmount> StaffPayrollItemTypeAmounts { get; set; }

    public virtual DbSet<StaffPayrollItemTypeSchedule> StaffPayrollItemTypeSchedules { get; set; }

    public virtual DbSet<StaffSalary> StaffSalaries { get; set; }

    public virtual DbSet<StaffSalaryDeduction> StaffSalaryDeductions { get; set; }

    public virtual DbSet<StaffSalaryPayrollItem> StaffSalaryPayrollItems { get; set; }

    public virtual DbSet<StaffSalaryPayrollItemSapdetail> StaffSalaryPayrollItemSapdetails { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<TeamStaff> TeamStaffs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAssignedUserGroup> UserAssignedUserGroups { get; set; }

    public virtual DbSet<UserGroup> UserGroups { get; set; }

    public virtual DbSet<UserGroupRole> UserGroupRoles { get; set; }

    public virtual DbSet<UserOrganisation> UserOrganisations { get; set; }

    public virtual DbSet<UserOrganisationRole> UserOrganisationRoles { get; set; }

    public virtual DbSet<UserOrganisationUserGroup> UserOrganisationUserGroups { get; set; }

    public virtual DbSet<UserPreference> UserPreferences { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<UserServiceRole> UserServiceRoles { get; set; }

    public virtual DbSet<VerifiedState> VerifiedStates { get; set; }

    public virtual DbSet<VwLeaveChangeRequest> VwLeaveChangeRequests { get; set; }

    public virtual DbSet<VwLeaveRequest> VwLeaveRequests { get; set; }

    public virtual DbSet<VwMergedRequestDatum> VwMergedRequestData { get; set; }

    public virtual DbSet<VwOtRequest> VwOtRequests { get; set; }

    public virtual DbSet<VwOutOfOfficeRequest> VwOutOfOfficeRequests { get; set; }

    public virtual DbSet<VwTeamRequest> VwTeamRequests { get; set; }

    public virtual DbSet<VwWorkTeamRequest> VwWorkTeamRequests { get; set; }

    public virtual DbSet<Ward> Wards { get; set; }

    public virtual DbSet<WebServiceRequestLog> WebServiceRequestLogs { get; set; }

    public virtual DbSet<WorkShift> WorkShifts { get; set; }

    public virtual DbSet<WorkShiftsBreakTime> WorkShiftsBreakTimes { get; set; }

    public virtual DbSet<WorkType> WorkTypes { get; set; }

    public virtual DbSet<WorkingDay> WorkingDays { get; set; }

    public virtual DbSet<WorkingDayShift> WorkingDayShifts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-IGBGLUQ\\SQLEXPRESS;Database=NewHRM_DB;Integrated Security=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HrmTeContext).Assembly);


        modelBuilder.Entity<AdditionDeductionTypeDependencyTimePeriodAmount>(entity =>
        {
            entity.HasKey(e => e.PayrollItemTypeDependencyTimePeriodAmountId);

            entity.Property(e => e.PayrollItemTypeDependencyTimePeriodAmountId).HasColumnName("PayrollItemTypeDependencyTimePeriodAmountID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.PayrollItemTypeId).HasColumnName("PayrollItemTypeID");
        });

        modelBuilder.Entity<AdditionOrDeductionType>(entity =>
        {
            entity.Property(e => e.AdditionOrDeductionTypeId).HasColumnName("AdditionOrDeductionTypeID");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.TypeName)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.LocationId).HasName("PK_Address");

            entity.Property(e => e.LocationId)
                .ValueGeneratedNever()
                .HasColumnName("LocationID");
            entity.Property(e => e.IslandId).HasColumnName("IslandID");

            entity.HasOne(d => d.Island).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.IslandId)
                .HasConstraintName("FK_Address_Islands");

            entity.HasOne(d => d.Location).WithOne(p => p.Address)
                .HasForeignKey<Address>(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Addresses_Locations");
        });

        modelBuilder.Entity<AddressBaseType>(entity =>
        {
            entity.Property(e => e.AddressBaseTypeId).HasColumnName("AddressBaseTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<AddressBasis>(entity =>
        {
            entity.HasKey(e => e.AddressBaseId).HasName("PK_Addresses");

            entity.Property(e => e.AddressBaseId).HasColumnName("AddressBaseID");
            entity.Property(e => e.AddressBaseTypeId).HasColumnName("AddressBaseTypeID");
            entity.Property(e => e.AddressLine1).HasMaxLength(250);
            entity.Property(e => e.AddressLine2).HasMaxLength(250);
            entity.Property(e => e.HomeNameDhivehi).HasMaxLength(250);
            entity.Property(e => e.MunicipalityNumber).HasMaxLength(50);
            entity.Property(e => e.PostCode).HasMaxLength(50);
            entity.Property(e => e.StreetNameDhivehi).HasMaxLength(250);
            entity.Property(e => e.StreetNameEnglish).HasMaxLength(250);
            entity.Property(e => e.WardId).HasColumnName("WardID");

            entity.HasOne(d => d.AddressBaseType).WithMany(p => p.AddressBases)
                .HasForeignKey(d => d.AddressBaseTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AddresseBases_AddressBaseTypes");

            entity.HasOne(d => d.Ward).WithMany(p => p.AddressBases)
                .HasForeignKey(d => d.WardId)
                .HasConstraintName("FK_Addresses_Wards");
        });

        modelBuilder.Entity<AddressInstance>(entity =>
        {
            entity.HasKey(e => e.LocationId);

            entity.Property(e => e.LocationId)
                .ValueGeneratedNever()
                .HasColumnName("LocationID");
            entity.Property(e => e.AddressBaseId).HasColumnName("AddressBaseID");
            entity.Property(e => e.AddressInstanceTypeId).HasColumnName("AddressInstanceTypeID");
            entity.Property(e => e.Floor).HasMaxLength(250);
            entity.Property(e => e.Name).HasMaxLength(250);

            entity.HasOne(d => d.AddressBase).WithMany(p => p.AddressInstances)
                .HasForeignKey(d => d.AddressBaseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AddressInstances_AddressBases");

            entity.HasOne(d => d.AddressInstanceType).WithMany(p => p.AddressInstances)
                .HasForeignKey(d => d.AddressInstanceTypeId)
                .HasConstraintName("FK_AddressInstances_AddressInstanceTypes");

            entity.HasOne(d => d.Location).WithOne(p => p.AddressInstance)
                .HasForeignKey<AddressInstance>(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AddressInstances_Addresses");
        });

        modelBuilder.Entity<AddressInstanceType>(entity =>
        {
            entity.Property(e => e.AddressInstanceTypeId).HasColumnName("AddressInstanceTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<AggregatedSalary>(entity =>
        {
            entity.Property(e => e.AggregatedSalaryId).HasColumnName("AggregatedSalaryID");
            entity.Property(e => e.AbsentFine).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.Additions).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.CalculatedSalaryAmount).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.Deductions).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.FinalSalary).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.LateFine).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.Ottotal)
                .HasColumnType("decimal(38, 8)")
                .HasColumnName("OTTotal");
            entity.Property(e => e.StaffSalaryId).HasColumnName("StaffSalaryID");

            entity.HasOne(d => d.StaffSalary).WithMany(p => p.AggregatedSalaries)
                .HasForeignKey(d => d.StaffSalaryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AggregatedSalaries_StaffSalaries");
        });

        modelBuilder.Entity<AmountType>(entity =>
        {
            entity.Property(e => e.AmountTypeId).HasColumnName("AmountTypeID");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AssignedWorkType>(entity =>
        {
            entity.Property(e => e.AssignedWorkTypeId).HasColumnName("AssignedWorkTypeID");
            entity.Property(e => e.AssignedByUserId).HasColumnName("AssignedByUserID");
            entity.Property(e => e.IsValid).HasDefaultValue(true);
            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.Location).HasMaxLength(250);
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.WorkTypeId).HasColumnName("WorkTypeID");

            entity.HasOne(d => d.Job).WithMany(p => p.AssignedWorkTypes)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssignedWorkTypes_Jobs");

            entity.HasOne(d => d.Organisation).WithMany(p => p.AssignedWorkTypes)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssignedWorkTypes_Organisations");

            entity.HasOne(d => d.WorkType).WithMany(p => p.AssignedWorkTypes)
                .HasForeignKey(d => d.WorkTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssignedWorkTypes_WorkTypes");
        });

        modelBuilder.Entity<Atoll>(entity =>
        {
            entity.Property(e => e.AtollId).HasColumnName("AtollID");
            entity.Property(e => e.AbbreviationDhivehi).HasMaxLength(50);
            entity.Property(e => e.AbbreviationEnglish).HasMaxLength(50);
            entity.Property(e => e.AtollCode).HasMaxLength(50);
            entity.Property(e => e.CityId).HasColumnName("CityID");
            entity.Property(e => e.NameDhivehi).HasMaxLength(250);
            entity.Property(e => e.NameEnglish).HasMaxLength(250);

            entity.HasOne(d => d.City).WithMany(p => p.Atolls)
                .HasForeignKey(d => d.CityId)
                .HasConstraintName("FK_Atolls_Cities");
        });

        modelBuilder.Entity<AttachedBreakTime>(entity =>
        {
            entity.Property(e => e.AttachedBreakTimeId).HasColumnName("AttachedBreakTimeID");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.IsValid).HasDefaultValue(true);
            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OwnerTypeId).HasColumnName("OwnerTypeID");
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.AttachedBreakTimes)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttachedBreakTimes_OperationLogs");

            entity.HasOne(d => d.OwnerType).WithMany(p => p.AttachedBreakTimes)
                .HasForeignKey(d => d.OwnerTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttachedBreakTimes_OwnerTypes");
        });

        // This forces EF Core to understand there is only ONE relationship
        modelBuilder.Entity<Job>()
            .HasOne(j => j.User)         // 1. A Job has one User
            .WithMany(u => u.Jobs)       // 2. A User has many Jobs (Make sure 'Jobs' matches the exact name of the list in your User.cs file!)
            .HasForeignKey(j => j.IndividualID); // 3. They are linked specifically by IndividualID

        modelBuilder.Entity<AttachedBreakTimesDay>(entity =>
        {
            entity.Property(e => e.AttachedBreakTimesDayId).HasColumnName("AttachedBreakTimesDayID");
            entity.Property(e => e.AttachedBreakTimeId).HasColumnName("AttachedBreakTimeID");
            entity.Property(e => e.BreakTimeId).HasColumnName("BreakTimeID");
            entity.Property(e => e.DayOfWeekId).HasColumnName("DayOfWeekID");
            entity.Property(e => e.IsValid).HasDefaultValue(true);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.AttachedBreakTime).WithMany(p => p.AttachedBreakTimesDays)
                .HasForeignKey(d => d.AttachedBreakTimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttachedBreakTimesDays_AttachedBreakTimes");

            entity.HasOne(d => d.BreakTime).WithMany(p => p.AttachedBreakTimesDays)
                .HasForeignKey(d => d.BreakTimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttachedBreakTimesDays_BreakTimes");

            entity.HasOne(d => d.DayOfWeek).WithMany(p => p.AttachedBreakTimesDays)
                .HasForeignKey(d => d.DayOfWeekId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttachedBreakTimesDays_DayOfWeeks");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.AttachedBreakTimesDays)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttachedBreakTimesDays_OperationLogs");
        });

        modelBuilder.Entity<AttachedPayrollItemsProcessingWhiteList>(entity =>
        {
            entity.Property(e => e.AttachedPayrollItemsProcessingWhiteListId).HasColumnName("AttachedPayrollItemsProcessingWhiteListID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.PayrollItemsProcessingWhiteListId).HasColumnName("PayrollItemsProcessingWhiteListID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.AttachedPayrollItemsProcessingWhiteLists)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttachedPayrollItemsProcessingWhiteLists_OperationLogs");

            entity.HasOne(d => d.Organisation).WithMany(p => p.AttachedPayrollItemsProcessingWhiteLists)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttachedPayrollItemsProcessingWhiteLists_Organisations");

            entity.HasOne(d => d.PayrollItemsProcessingWhiteList).WithMany(p => p.AttachedPayrollItemsProcessingWhiteLists)
                .HasForeignKey(d => d.PayrollItemsProcessingWhiteListId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttachedPayrollItemsProcessingWhiteLists_PayrollItemsProcessingWhiteLists");
        });

        modelBuilder.Entity<AttendanceClientInstance>(entity =>
        {
            entity.HasKey(e => e.AttendanceClientId).HasName("PK_AttendanceClients_1");

            entity.Property(e => e.AttendanceClientId).HasColumnName("AttendanceClientID");
            entity.Property(e => e.AttendanceClientStateId).HasColumnName("AttendanceClientStateID");
            entity.Property(e => e.LastAttendanceManualUpdateRecord).HasColumnType("datetime");
            entity.Property(e => e.LastAttendanceManuallyUpdatedDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");

            entity.HasOne(d => d.AttendanceClientState).WithMany(p => p.AttendanceClientInstances)
                .HasForeignKey(d => d.AttendanceClientStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceClients_AttendanceClientStates1");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.AttendanceClientInstances)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_AttendanceClientInstances_OperationLogs");

            entity.HasOne(d => d.Organisation).WithMany(p => p.AttendanceClientInstances)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceClients_Organisations1");

            entity.HasOne(d => d.OrganisationStructure).WithMany(p => p.AttendanceClientInstances)
                .HasForeignKey(d => d.OrganisationStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceClients_OrganisationStructure1");
        });

        modelBuilder.Entity<AttendanceClientState>(entity =>
        {
            entity.Property(e => e.AttendanceClientStateId).HasColumnName("AttendanceClientStateID");
            entity.Property(e => e.StateName).HasMaxLength(250);
        });

        modelBuilder.Entity<AttendanceDependentType>(entity =>
        {
            entity.Property(e => e.AttendanceDependentTypeId).HasColumnName("AttendanceDependentTypeID");
            entity.Property(e => e.TypeName)
                .HasMaxLength(500)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AttendanceDevice>(entity =>
        {
            entity.Property(e => e.AttendanceDeviceId).HasColumnName("AttendanceDeviceID");
            entity.Property(e => e.AttendanceClientId).HasColumnName("AttendanceClientID");
            entity.Property(e => e.AttendanceDeviceInOutTypeId)
                .HasDefaultValue(1)
                .HasColumnName("AttendanceDeviceInOutTypeID");
            entity.Property(e => e.AttendanceDeviceStateId).HasColumnName("AttendanceDeviceStateID");
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(250)
                .HasColumnName("IPAddress");
            entity.Property(e => e.Location).HasMaxLength(250);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.AttendanceClient).WithMany(p => p.AttendanceDevices)
                .HasForeignKey(d => d.AttendanceClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceDevices_AttendanceClients");

            entity.HasOne(d => d.AttendanceDeviceInOutType).WithMany(p => p.AttendanceDevices)
                .HasForeignKey(d => d.AttendanceDeviceInOutTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceDevices_AttendanceDeviceInOutTypeID");

            entity.HasOne(d => d.AttendanceDeviceState).WithMany(p => p.AttendanceDevices)
                .HasForeignKey(d => d.AttendanceDeviceStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceDevices_AttendanceDeviceStates");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.AttendanceDevices)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_AttendanceDevices_OperationLogs");
        });

        modelBuilder.Entity<AttendanceDeviceInOutType>(entity =>
        {
            entity.HasKey(e => e.AttendanceDeviceInOutTypeId).HasName("PK__Attendan__696D3B51D59EE0C4");

            entity.ToTable("AttendanceDeviceInOutType");

            entity.Property(e => e.AttendanceDeviceInOutTypeId).HasColumnName("AttendanceDeviceInOutTypeID");
            entity.Property(e => e.AttendanceDeviceInOutTypeName)
                .HasMaxLength(20)
                .IsFixedLength();
        });

        modelBuilder.Entity<AttendanceDeviceStaff>(entity =>
        {
            entity.Property(e => e.AttendanceDeviceStaffId).HasColumnName("AttendanceDeviceStaffID");
            entity.Property(e => e.AttendanceDeviceId).HasColumnName("AttendanceDeviceID");
            entity.Property(e => e.EnrollmentNumber).HasMaxLength(250);
            entity.Property(e => e.IndividualId).HasColumnName("IndividualID");

            entity.HasOne(d => d.AttendanceDevice).WithMany(p => p.AttendanceDeviceStaffs)
                .HasForeignKey(d => d.AttendanceDeviceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceDeviceStaffs_AttendanceDevices");

            entity.HasOne(d => d.Individual).WithMany(p => p.AttendanceDeviceStaffs)
                .HasForeignKey(d => d.IndividualId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceDeviceStaffs_Staffs");
        });

        modelBuilder.Entity<AttendanceDeviceState>(entity =>
        {
            entity.Property(e => e.AttendanceDeviceStateId).HasColumnName("AttendanceDeviceStateID");
            entity.Property(e => e.StateName).HasMaxLength(250);
        });

        modelBuilder.Entity<AttendanceLog>(entity =>
        {
            entity.HasKey(e => e.AttendanceLogId).HasName("PK_AttendanceLogss");

            entity.HasIndex(e => e.Date, "IDX_AttendanceLogs_Date").IsDescending();

            entity.HasIndex(e => new { e.IndividualId, e.OrganisationId, e.Date, e.InOutModeId }, "IX_AttendanceLogs_LookUpByIndividualAndDate").IsDescending(false, false, true, false);

            entity.HasIndex(e => e.Uidstamp, "UQ_AttendanceLogs").IsUnique();

            entity.HasIndex(e => e.IndividualId, "ix_IndexName_10_MARCH");

            entity.Property(e => e.AttendanceLogId).HasColumnName("AttendanceLogID");
            entity.Property(e => e.ActualInOutMode).HasDefaultValueSql("(NULL)");
            entity.Property(e => e.AttendanceDeviceId).HasColumnName("AttendanceDeviceID");
            entity.Property(e => e.AttendanceLogModeId)
                .HasDefaultValue(1)
                .HasColumnName("AttendanceLogModeID");
            entity.Property(e => e.AttendanceLogStateId)
                .HasDefaultValue(1)
                .HasColumnName("AttendanceLogStateID");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.InOutModeId).HasColumnName("InOutModeID");
            entity.Property(e => e.IndividualId).HasColumnName("IndividualID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.RelatedAttendanceLogId).HasColumnName("RelatedAttendanceLogID");
            entity.Property(e => e.Uidstamp)
                .HasMaxLength(100)
                .HasColumnName("UIDStamp");

            entity.HasOne(d => d.AttendanceDevice).WithMany(p => p.AttendanceLogs)
                .HasForeignKey(d => d.AttendanceDeviceId)
                .HasConstraintName("FK_AttendanceLogs_AttendanceDevices");

            entity.HasOne(d => d.AttendanceLogMode).WithMany(p => p.AttendanceLogs)
                .HasForeignKey(d => d.AttendanceLogModeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceLogs_AttendanceLogModes");

            entity.HasOne(d => d.AttendanceLogState).WithMany(p => p.AttendanceLogs)
                .HasForeignKey(d => d.AttendanceLogStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceLogs_AttendanceLogStates");

            entity.HasOne(d => d.InOutMode).WithMany(p => p.AttendanceLogs)
                .HasForeignKey(d => d.InOutModeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceLogs_InOutModes");

            entity.HasOne(d => d.Individual).WithMany(p => p.AttendanceLogs)
                .HasForeignKey(d => d.IndividualId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceLogs_Staffs");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.AttendanceLogs)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_AttendanceLogs_OperationLogs");

            entity.HasOne(d => d.Organisation).WithMany(p => p.AttendanceLogs)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceLogs_Organisations");

            entity.HasOne(d => d.OrganisationStructure).WithMany(p => p.AttendanceLogs)
                .HasForeignKey(d => d.OrganisationStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceLogs_OrganisationStructure");

            entity.HasOne(d => d.RelatedAttendanceLog).WithMany(p => p.InverseRelatedAttendanceLog)
                .HasForeignKey(d => d.RelatedAttendanceLogId)
                .HasConstraintName("FK_AttendanceLogs_AttendanceLogs");
        });

        modelBuilder.Entity<AttendanceLogChangeRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.HasIndex(e => e.AttendanceLogId, "IX_AttendanceLogChangeRequests");

            entity.Property(e => e.RequestId)
                .ValueGeneratedNever()
                .HasColumnName("RequestID");
            entity.Property(e => e.AttendanceLogId).HasColumnName("AttendanceLogID");
            entity.Property(e => e.Comments).HasMaxLength(250);

            entity.HasOne(d => d.AttendanceLog).WithMany(p => p.AttendanceLogChangeRequests)
                .HasForeignKey(d => d.AttendanceLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceLogRequests_AttendanceLogs");

            entity.HasOne(d => d.Request).WithOne(p => p.AttendanceLogChangeRequest)
                .HasForeignKey<AttendanceLogChangeRequest>(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttendanceLogRequests_Requests");
        });

        modelBuilder.Entity<AttendanceLogChangeRequestType>(entity =>
        {
            entity.HasKey(e => e.AttendanceLogChangeRequestTypeId).HasName("PK_AttendanceLogRequestTypes");

            entity.Property(e => e.AttendanceLogChangeRequestTypeId).HasColumnName("AttendanceLogChangeRequestTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<AttendanceLogMode>(entity =>
        {
            entity.Property(e => e.AttendanceLogModeId).HasColumnName("AttendanceLogModeID");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<AttendanceLogRequest>(entity =>
        {
            entity.HasKey(e => e.AttendanceLogRequestId).HasName("PK_ManualAttendanceLogRequests");

            entity.Property(e => e.AttendanceLogRequestId).HasColumnName("AttendanceLogRequestID");
            entity.Property(e => e.AttendanceLogId).HasColumnName("AttendanceLogID");
            entity.Property(e => e.Comments).HasMaxLength(250);
            entity.Property(e => e.RequestId).HasColumnName("RequestID");

            entity.HasOne(d => d.AttendanceLog).WithMany(p => p.AttendanceLogRequests)
                .HasForeignKey(d => d.AttendanceLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ManualAttendanceLogRequests_AttendanceLogs");

            entity.HasOne(d => d.Request).WithMany(p => p.AttendanceLogRequests)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ManualAttendanceLogRequests_Requests");
        });

        modelBuilder.Entity<AttendanceLogState>(entity =>
        {
            entity.Property(e => e.AttendanceLogStateId).HasColumnName("AttendanceLogStateID");
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<AttendanceLogsTemp>(entity =>
        {
            entity.HasKey(e => e.AttendanceLogId);

            entity.ToTable("AttendanceLogsTemp");

            entity.HasIndex(e => new { e.IndividualId, e.OrganisationId, e.Date }, "IX_AttendanceLogsTemp_LookUpByIndividualAndDate");

            entity.Property(e => e.AttendanceLogId).HasColumnName("AttendanceLogID");
            entity.Property(e => e.AttendanceDeviceId).HasColumnName("AttendanceDeviceID");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.InOutModeId).HasColumnName("InOutModeID");
            entity.Property(e => e.IndividualId).HasColumnName("IndividualID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.Uidstamp)
                .HasMaxLength(100)
                .HasColumnName("UIDStamp");
        });

        modelBuilder.Entity<BasicSalary>(entity =>
        {
            entity.Property(e => e.BasicSalaryId).HasColumnName("BasicSalaryID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.BasicSalaries)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BasicSalaries_OperationLogs");
        });

        modelBuilder.Entity<BreakTime>(entity =>
        {
            entity.Property(e => e.BreakTimeId).HasColumnName("BreakTimeID");
            entity.Property(e => e.EndTime).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.StartTime).HasColumnType("datetime");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.BreakTimes)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BreakTimes_OperationLogs");

            entity.HasOne(d => d.Organisation).WithMany(p => p.BreakTimes)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BreakTimes_Organisations");

            entity.HasOne(d => d.OrganisationStructure).WithMany(p => p.BreakTimes)
                .HasForeignKey(d => d.OrganisationStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BreakTimes_OrganisationStructure");
        });

        modelBuilder.Entity<BudgetItem>(entity =>
        {
            entity.HasKey(e => e.BudgetItemId).HasName("PK_BudgetCodeSet");

            entity.Property(e => e.BudgetItemId).HasColumnName("BudgetItemID");
            entity.Property(e => e.DhivehiName).HasMaxLength(500);
            entity.Property(e => e.EnglishName).HasMaxLength(500);
            entity.Property(e => e.ItemTypeId).HasColumnName("ItemTypeID");
            entity.Property(e => e.ParentBudgetItemId).HasColumnName("ParentBudgetItemID");
        });

        modelBuilder.Entity<BudgetTransaction>(entity =>
        {
            entity.HasKey(e => e.BudgetTransactionId).HasName("PK_BudgetAdditionDeductions");

            entity.Property(e => e.BudgetTransactionId).HasColumnName("BudgetTransactionID");
            entity.Property(e => e.Amount).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.BudgetItemId).HasColumnName("BudgetItemID");
            entity.Property(e => e.LetterNumber).HasMaxLength(100);
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
            entity.Property(e => e.TransactionTypeId).HasColumnName("TransactionTypeID");

            entity.HasOne(d => d.BudgetItem).WithMany(p => p.BudgetTransactions)
                .HasForeignKey(d => d.BudgetItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BudgetTransactions_BudgetItems");

            entity.HasOne(d => d.Organisation).WithMany(p => p.BudgetTransactions)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BudgetTransactions_Organisations");

            entity.HasOne(d => d.TransactionType).WithMany(p => p.BudgetTransactions)
                .HasForeignKey(d => d.TransactionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BudgetTransactions_BudgetTransactionTypes");
        });

        modelBuilder.Entity<BudgetTransactionType>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.TypeName).HasMaxLength(50);
        });

        modelBuilder.Entity<BulkUploadedDocument>(entity =>
        {
            entity.HasKey(e => e.DocumentId);

            entity.Property(e => e.DocumentId)
                .ValueGeneratedNever()
                .HasColumnName("DocumentID");
            entity.Property(e => e.DocumentStateId).HasColumnName("DocumentStateID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.UploadedByUserId).HasColumnName("UploadedByUserID");
            entity.Property(e => e.UploadedDate).HasColumnType("datetime");

            entity.HasOne(d => d.Document).WithOne(p => p.BulkUploadedDocument)
                .HasForeignKey<BulkUploadedDocument>(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BulkUploadedDocuments_Documents");

            entity.HasOne(d => d.DocumentState).WithMany(p => p.BulkUploadedDocuments)
                .HasForeignKey(d => d.DocumentStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BulkUploadedDocuments_DocumentStates");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.BulkUploadedDocuments)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BulkUploadedDocuments_OperationLogs");

            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.BulkUploadedDocuments)
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BulkUploadedDocuments_Users");
        });

        modelBuilder.Entity<BulkUploadedDocumentSummary>(entity =>
        {
            entity.Property(e => e.BulkUploadedDocumentSummaryId).HasColumnName("BulkUploadedDocumentSummaryID");
            entity.Property(e => e.DocumentId).HasColumnName("DocumentID");
            entity.Property(e => e.IndexName).HasMaxLength(250);
            entity.Property(e => e.Remarks).HasMaxLength(500);
            entity.Property(e => e.RowId).HasColumnName("RowID");

            entity.HasOne(d => d.Document).WithMany(p => p.BulkUploadedDocumentSummaries)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BulkUploadedDocumentSummaries_BulkUploadedDocuments");
        });

        modelBuilder.Entity<BusinessEntitiesDocument>(entity =>
        {
            entity.HasKey(e => new { e.BusinessEntityId, e.DocumentId });

            entity.Property(e => e.BusinessEntityId).HasColumnName("BusinessEntityID");
            entity.Property(e => e.DocumentId).HasColumnName("DocumentID");
            entity.Property(e => e.LinkedByGlobalUserId).HasColumnName("LinkedByGlobalUserID");
            entity.Property(e => e.LinkedDate).HasColumnType("datetime");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.BusinessEntity).WithMany(p => p.BusinessEntitiesDocuments)
                .HasForeignKey(d => d.BusinessEntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntitiesDocuments_BusinessEntities");

            entity.HasOne(d => d.Document).WithMany(p => p.BusinessEntitiesDocuments)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntitiesDocuments_Documents");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.BusinessEntitiesDocuments)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_BusinessEntitiesDocuments_OperationLogs");
        });

        modelBuilder.Entity<BusinessEntity>(entity =>
        {
            entity.Property(e => e.BusinessEntityID).HasColumnName("BusinessEntityID");
            entity.Property(e => e.BusinessEntityStateId).HasColumnName("BusinessEntityStateID");
            entity.Property(e => e.BusinessEntityTypeId).HasColumnName("BusinessEntityTypeID");
            entity.Property(e => e.LastStateChangeDate).HasColumnType("datetime");
            entity.Property(e => e.LastStateChangedByUserId).HasColumnName("LastStateChangedByUserID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.VerifiedDate).HasColumnType("datetime");
            entity.Property(e => e.VerifiedStateId).HasColumnName("VerifiedStateID");

            entity.HasOne(d => d.BusinessEntityState).WithMany(p => p.BusinessEntities)
                .HasForeignKey(d => d.BusinessEntityStateId)
                .HasConstraintName("FK_BusinessEntities_BusinessEntityStates");

            entity.HasOne(d => d.BusinessEntityType).WithMany(p => p.BusinessEntities)
                .HasForeignKey(d => d.BusinessEntityTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntities_BusinessEntityTypes");

            entity.HasOne(d => d.LastStateChangedByUser).WithMany(p => p.BusinessEntityLastStateChangedByUsers)
                .HasForeignKey(d => d.LastStateChangedByUserId)
                .HasConstraintName("FK_BusinessEntities_LastStateChangedByUser");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.BusinessEntities)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_BusinessEntities_OperationLogs");

            entity.HasOne(d => d.VerifiedByNavigation).WithMany(p => p.BusinessEntityVerifiedByNavigations)
                .HasForeignKey(d => d.VerifiedBy)
                .HasConstraintName("FK_BusinessEntities_VerifiedByUser");

            entity.HasOne(d => d.VerifiedState).WithMany(p => p.BusinessEntities)
                .HasForeignKey(d => d.VerifiedStateId)
                .HasConstraintName("FK_BusinessEntities_VerifiedStates");
        });

        modelBuilder.Entity<BusinessEntityCalendar>(entity =>
        {
            entity.HasKey(e => e.BusinessEntityCalenderId).HasName("PK_BusinessEntityCalenders");

            entity.Property(e => e.BusinessEntityCalenderId).HasColumnName("BusinessEntityCalenderID");
            entity.Property(e => e.BusinessEntityId).HasColumnName("BusinessEntityID");
            entity.Property(e => e.CalenderId).HasColumnName("CalenderID");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.IsLinked).HasDefaultValue(true);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.BusinessEntity).WithMany(p => p.BusinessEntityCalendars)
                .HasForeignKey(d => d.BusinessEntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntityCalenders_BusinessEntities");

            entity.HasOne(d => d.Calender).WithMany(p => p.BusinessEntityCalendars)
                .HasForeignKey(d => d.CalenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntityCalenders_Calenders");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.BusinessEntityCalendars)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntityCalendars_OperationLogs");
        });

        modelBuilder.Entity<BusinessEntityLocation>(entity =>
        {
            entity.HasKey(e => e.BusinessEntityLocationId).HasName("PK_BussinessEntityAddresses");

            entity.Property(e => e.BusinessEntityLocationId).HasColumnName("BusinessEntityLocationID");
            entity.Property(e => e.BusinessEntityId).HasColumnName("BusinessEntityID");
            entity.Property(e => e.BusinessEntityLocationTypeId).HasColumnName("BusinessEntityLocationTypeID");
            entity.Property(e => e.IsValid).HasDefaultValue(true);
            entity.Property(e => e.LocationId).HasColumnName("LocationID");

            entity.HasOne(d => d.BusinessEntity).WithMany(p => p.BusinessEntityLocations)
                .HasForeignKey(d => d.BusinessEntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntityLocations_BusinessEntities");

            entity.HasOne(d => d.BusinessEntityLocationType).WithMany(p => p.BusinessEntityLocations)
                .HasForeignKey(d => d.BusinessEntityLocationTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntityLocations_BusinessEntityLocationTypes");

            entity.HasOne(d => d.Location).WithMany(p => p.BusinessEntityLocations)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntityLocations_Locations");
        });

        modelBuilder.Entity<BusinessEntityLocationType>(entity =>
        {
            entity.HasKey(e => e.BusinessEntityLocationTypeId).HasName("PK_AddressTypes");

            entity.Property(e => e.BusinessEntityLocationTypeId).HasColumnName("BusinessEntityLocationTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<BusinessEntityRelatedLocationType>(entity =>
        {
            entity.Property(e => e.BusinessEntityRelatedLocationTypeId).HasColumnName("BusinessEntityRelatedLocationTypeID");
            entity.Property(e => e.BusinessEntityTypeId).HasColumnName("BusinessEntityTypeID");
            entity.Property(e => e.LocationTypeId).HasColumnName("LocationTypeID");

            entity.HasOne(d => d.BusinessEntityType).WithMany(p => p.BusinessEntityRelatedLocationTypes)
                .HasForeignKey(d => d.BusinessEntityTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntityRelatedLocationTypes_BusinessEntityTypes1");

            entity.HasOne(d => d.LocationType).WithMany(p => p.BusinessEntityRelatedLocationTypes)
                .HasForeignKey(d => d.LocationTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntityRelatedLocationTypes_LocationTypes1");
        });

        modelBuilder.Entity<BusinessEntityRelation>(entity =>
        {
            entity.Property(e => e.BusinessEntityRelationId).HasColumnName("BusinessEntityRelationID");
            entity.Property(e => e.BusinessEntityRelationStateId).HasColumnName("BusinessEntityRelationStateID");
            entity.Property(e => e.BusinessEntityRelationTypeId).HasColumnName("BusinessEntityRelationTypeID");
            entity.Property(e => e.DelegateeBusinessEntityId).HasColumnName("DelegateeBusinessEntityID");
            entity.Property(e => e.DelegatorBusinessEntityId).HasColumnName("DelegatorBusinessEntityID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.PreviousBusinessEntityRelationSateId).HasColumnName("PreviousBusinessEntityRelationSateID");

            entity.HasOne(d => d.BusinessEntityRelationState).WithMany(p => p.BusinessEntityRelations)
                .HasForeignKey(d => d.BusinessEntityRelationStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntityRelations_BusinessEntityRelationStates");

            entity.HasOne(d => d.BusinessEntityRelationType).WithMany(p => p.BusinessEntityRelations)
                .HasForeignKey(d => d.BusinessEntityRelationTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntityRelations_BusinessEntityRelationTypes");

            entity.HasOne(d => d.DelegateeBusinessEntity).WithMany(p => p.BusinessEntityRelationDelegateeBusinessEntities)
                .HasForeignKey(d => d.DelegateeBusinessEntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntityRelations_Delegatee");

            entity.HasOne(d => d.DelegatorBusinessEntity).WithMany(p => p.BusinessEntityRelationDelegatorBusinessEntities)
                .HasForeignKey(d => d.DelegatorBusinessEntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntityRelations_Delegator");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.BusinessEntityRelations)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_BusinessEntityRelations_OperationLogs");

            entity.HasMany(d => d.Requests).WithMany(p => p.BusinessEntityRelations)
                .UsingEntity<Dictionary<string, object>>(
                    "BusinessEntityRequest",
                    r => r.HasOne<Request>().WithMany()
                        .HasForeignKey("RequestId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_BusinessEntityRequests_Requests"),
                    l => l.HasOne<BusinessEntityRelation>().WithMany()
                        .HasForeignKey("BusinessEntityRelationId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_BusinessEntityRequests_BusinessEntityRelations"),
                    j =>
                    {
                        j.HasKey("BusinessEntityRelationId", "RequestId");
                        j.ToTable("BusinessEntityRequests");
                        j.IndexerProperty<int>("BusinessEntityRelationId").HasColumnName("BusinessEntityRelationID");
                        j.IndexerProperty<int>("RequestId").HasColumnName("RequestID");
                    });
        });

        modelBuilder.Entity<BusinessEntityRelationAssignedRole>(entity =>
        {
            entity.HasKey(e => e.BsinessEntityRelationAssignedRoleId);

            entity.Property(e => e.BsinessEntityRelationAssignedRoleId).HasColumnName("BsinessEntityRelationAssignedRoleID");
            entity.Property(e => e.BusinessEntityRelationId).HasColumnName("BusinessEntityRelationID");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");

            entity.HasOne(d => d.BusinessEntityRelation).WithMany(p => p.BusinessEntityRelationAssignedRoles)
                .HasForeignKey(d => d.BusinessEntityRelationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntityRelationAssignedRoles_BusinessEntityRelations");

            entity.HasOne(d => d.Role).WithMany(p => p.BusinessEntityRelationAssignedRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntityRelationAssignedRoles_Roles");
        });

        modelBuilder.Entity<BusinessEntityRelationState>(entity =>
        {
            entity.Property(e => e.BusinessEntityRelationStateId).HasColumnName("BusinessEntityRelationStateID");
            entity.Property(e => e.StateName)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<BusinessEntityRelationType>(entity =>
        {
            entity.Property(e => e.BusinessEntityRelationTypeId).HasColumnName("BusinessEntityRelationTypeID");
            entity.Property(e => e.BusinessEntityId).HasColumnName("BusinessEntityID");
            entity.Property(e => e.TypeName)
                .HasMaxLength(250)
                .IsUnicode(false);

            entity.HasOne(d => d.BusinessEntity).WithMany(p => p.BusinessEntityRelationTypes)
                .HasForeignKey(d => d.BusinessEntityId)
                .HasConstraintName("FK_BusinessEntityRelationTypes_BusinessEntities1");
        });

        modelBuilder.Entity<BusinessEntitySchedule>(entity =>
        {
            entity.Property(e => e.BusinessEntityScheduleId).HasColumnName("BusinessEntityScheduleID");
            entity.Property(e => e.BusinessEntityId).HasColumnName("BusinessEntityID");
            entity.Property(e => e.IsLinked).HasDefaultValue(true);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");

            entity.HasOne(d => d.BusinessEntity).WithMany(p => p.BusinessEntitySchedules)
                .HasForeignKey(d => d.BusinessEntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntitySchedules_BusinessEntities");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.BusinessEntitySchedules)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntitySchedules_OperationLogs");

            entity.HasOne(d => d.Schedule).WithMany(p => p.BusinessEntitySchedules)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BusinessEntitySchedules_Schedules");
        });

        modelBuilder.Entity<BusinessEntityState>(entity =>
        {
            entity.Property(e => e.BusinessEntityStateId).HasColumnName("BusinessEntityStateID");
            entity.Property(e => e.StateName)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<BusinessEntityType>(entity =>
        {
            entity.Property(e => e.BusinessEntityTypeId).HasColumnName("BusinessEntityTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<BussinessEntityContactInformation>(entity =>
        {
            entity.Property(e => e.BussinessEntityContactInformationId).HasColumnName("BussinessEntityContactInformationID");
            entity.Property(e => e.BussinessEntityId).HasColumnName("BussinessEntityID");
            entity.Property(e => e.ContactInformationTypeId).HasColumnName("ContactInformationTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.Value)
                .HasMaxLength(250)
                .IsUnicode(false);

            entity.HasOne(d => d.BussinessEntity).WithMany(p => p.BussinessEntityContactInformations)
                .HasForeignKey(d => d.BussinessEntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BussinessEntityContactInformations_BusinessEntities");

            entity.HasOne(d => d.ContactInformationType).WithMany(p => p.BussinessEntityContactInformations)
                .HasForeignKey(d => d.ContactInformationTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BussinessEntityContactInformations_ContactInformationTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.BussinessEntityContactInformations)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_BussinessEntityContactInformations_OperationLogs");
        });

        modelBuilder.Entity<Calendar>(entity =>
        {
            entity.HasKey(e => e.CalenderId).HasName("PK_Calenders");

            entity.Property(e => e.CalenderId).HasColumnName("CalenderID");
            entity.Property(e => e.CalendarTypeId).HasColumnName("CalendarTypeID");
            entity.Property(e => e.Description).IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.CalendarType).WithMany(p => p.Calendars)
                .HasForeignKey(d => d.CalendarTypeId)
                .HasConstraintName("FK_Calendars_CalendarTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Calendars)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_Calendars_OperationLogs");

            entity.HasMany(d => d.Calenders).WithMany(p => p.ParentCalenders)
                .UsingEntity<Dictionary<string, object>>(
                    "ParentCalender",
                    r => r.HasOne<Calendar>().WithMany()
                        .HasForeignKey("CalenderId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ParentCalenders_Child"),
                    l => l.HasOne<Calendar>().WithMany()
                        .HasForeignKey("ParentCalenderId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ParentCalenders_Parent"),
                    j =>
                    {
                        j.HasKey("CalenderId", "ParentCalenderId");
                        j.ToTable("ParentCalenders");
                        j.IndexerProperty<int>("CalenderId").HasColumnName("CalenderID");
                        j.IndexerProperty<int>("ParentCalenderId").HasColumnName("ParentCalenderID");
                    });

            entity.HasMany(d => d.Elements).WithMany(p => p.Calendars)
                .UsingEntity<Dictionary<string, object>>(
                    "CalendarElement",
                    r => r.HasOne<Element>().WithMany()
                        .HasForeignKey("ElementId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_CalendarElements_Elements"),
                    l => l.HasOne<Calendar>().WithMany()
                        .HasForeignKey("CalendarId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_CalendarElements_Calendars"),
                    j =>
                    {
                        j.HasKey("CalendarId", "ElementId");
                        j.ToTable("CalendarElements");
                        j.IndexerProperty<int>("CalendarId").HasColumnName("CalendarID");
                        j.IndexerProperty<int>("ElementId").HasColumnName("ElementID");
                    });

            entity.HasMany(d => d.ParentCalenders).WithMany(p => p.Calenders)
                .UsingEntity<Dictionary<string, object>>(
                    "ParentCalender",
                    r => r.HasOne<Calendar>().WithMany()
                        .HasForeignKey("ParentCalenderId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ParentCalenders_Parent"),
                    l => l.HasOne<Calendar>().WithMany()
                        .HasForeignKey("CalenderId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ParentCalenders_Child"),
                    j =>
                    {
                        j.HasKey("CalenderId", "ParentCalenderId");
                        j.ToTable("ParentCalenders");
                        j.IndexerProperty<int>("CalenderId").HasColumnName("CalenderID");
                        j.IndexerProperty<int>("ParentCalenderId").HasColumnName("ParentCalenderID");
                    });
        });

        modelBuilder.Entity<CalendarInstance>(entity =>
        {
            entity.Property(e => e.CalendarInstanceId).HasColumnName("CalendarInstanceID");
            entity.Property(e => e.CalendarId).HasColumnName("CalendarID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.Calendar).WithMany(p => p.CalendarInstances)
                .HasForeignKey(d => d.CalendarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CalendarInstances_Calendars");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.CalendarInstances)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CalendarInstances_OperationLogs");
        });

        modelBuilder.Entity<CalendarMonth>(entity =>
        {
            entity.Property(e => e.CalendarMonthId).HasColumnName("CalendarMonthID");
            entity.Property(e => e.CalendarInstanceId).HasColumnName("CalendarInstanceID");
            entity.Property(e => e.MonthId).HasColumnName("MonthID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.CalendarInstance).WithMany(p => p.CalendarMonths)
                .HasForeignKey(d => d.CalendarInstanceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CalendarMonths_CalendarInstances");

            entity.HasOne(d => d.Month).WithMany(p => p.CalendarMonths)
                .HasForeignKey(d => d.MonthId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CalendarMonths_Months");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.CalendarMonths)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CalendarMonths_OperationLogs");
        });

        modelBuilder.Entity<CalendarSchedule>(entity =>
        {
            entity.Property(e => e.CalendarScheduleId).HasColumnName("CalendarScheduleID");
            entity.Property(e => e.CalendarId).HasColumnName("CalendarID");
            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");

            entity.HasOne(d => d.Calendar).WithMany(p => p.CalendarSchedules)
                .HasForeignKey(d => d.CalendarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CalendarSchedules_Calendars");

            entity.HasOne(d => d.Schedule).WithMany(p => p.CalendarSchedules)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CalendarSchedules_Schedules");
        });

        modelBuilder.Entity<CalendarType>(entity =>
        {
            entity.HasKey(e => e.CalendarTypeId).HasName("PK_CalendarVariants");

            entity.Property(e => e.CalendarTypeId).HasColumnName("CalendarTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.TypeName)
                .HasMaxLength(250)
                .IsUnicode(false);

            entity.HasOne(d => d.OperationLog).WithMany(p => p.CalendarTypes)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CalendarVariants_OperationLogs");
        });

        modelBuilder.Entity<CheckListItem>(entity =>
        {
            entity.Property(e => e.CheckListItemId).HasColumnName("CheckListItemID");
            entity.Property(e => e.CheckListItemName)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.CheckListItems)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CheckListItems_OperationLogs");

            entity.HasMany(d => d.RequestTypes).WithMany(p => p.CheckListItems)
                .UsingEntity<Dictionary<string, object>>(
                    "RequestTypeSpecificCheckListItem",
                    r => r.HasOne<RequestType>().WithMany()
                        .HasForeignKey("RequestTypeId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_RequestTypeSpecificCheckListItems_RequestTypes"),
                    l => l.HasOne<CheckListItem>().WithMany()
                        .HasForeignKey("CheckListItemId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_RequestTypeSpecificCheckListItems_CheckListItems"),
                    j =>
                    {
                        j.HasKey("CheckListItemId", "RequestTypeId");
                        j.ToTable("RequestTypeSpecificCheckListItems");
                        j.IndexerProperty<int>("CheckListItemId").HasColumnName("CheckListItemID");
                        j.IndexerProperty<int>("RequestTypeId").HasColumnName("RequestTypeID");
                    });

            entity.HasMany(d => d.ServiceTypes).WithMany(p => p.CheckListItems)
                .UsingEntity<Dictionary<string, object>>(
                    "ServiceTypeSpecificCheckListItem",
                    r => r.HasOne<ServiceType>().WithMany()
                        .HasForeignKey("ServiceTypeId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ServiceTypeSpecificCheckListItems_ServiceTypes"),
                    l => l.HasOne<CheckListItem>().WithMany()
                        .HasForeignKey("CheckListItemId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ServiceTypeSpecificCheckListItems_CheckListItems"),
                    j =>
                    {
                        j.HasKey("CheckListItemId", "ServiceTypeId");
                        j.ToTable("ServiceTypeSpecificCheckListItems");
                        j.IndexerProperty<int>("CheckListItemId").HasColumnName("CheckListItemID");
                        j.IndexerProperty<int>("ServiceTypeId").HasColumnName("ServiceTypeID");
                    });
        });

        modelBuilder.Entity<City>(entity =>
        {
            entity.Property(e => e.CityId).HasColumnName("CityID");
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<CommercialIslandType>(entity =>
        {
            entity.Property(e => e.CommercialIslandTypeId).HasColumnName("CommercialIslandTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<ConfigurableValue>(entity =>
        {
            entity.Property(e => e.ConfigurableValueId).HasColumnName("ConfigurableValueID");
            entity.Property(e => e.Comment).HasColumnType("text");
            entity.Property(e => e.Name).HasMaxLength(60);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.Value).HasColumnType("text");
        });

        modelBuilder.Entity<ContactInformationType>(entity =>
        {
            entity.Property(e => e.ContactInformationTypeId).HasColumnName("ContactInformationTypeID");
            entity.Property(e => e.Format).HasMaxLength(100);
            entity.Property(e => e.Regex).HasMaxLength(250);
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<Context>(entity =>
        {
            entity.ToTable("Context");

            entity.Property(e => e.ContextId).HasColumnName("ContextID");
            entity.Property(e => e.ContextKey).HasMaxLength(250);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Name).HasMaxLength(250);
        });

        modelBuilder.Entity<Country>(entity =>
        {
            entity.Property(e => e.CountryId).HasColumnName("CountryID");
            entity.Property(e => e.Isoalpha2Code)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("ISOAlpha2Code");
            entity.Property(e => e.Isoalpha3Code)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasColumnName("ISOAlpha3Code");
            entity.Property(e => e.IsonumericCode).HasColumnName("ISONumericCode");
            entity.Property(e => e.NameDhivehi).HasMaxLength(250);
            entity.Property(e => e.NameEnglish).HasMaxLength(250);
            entity.Property(e => e.NationalityDhivehi).HasMaxLength(250);
            entity.Property(e => e.NationalityEnglish).HasMaxLength(250);
            entity.Property(e => e.RegionId).HasColumnName("RegionID");

            entity.HasOne(d => d.Region).WithMany(p => p.Countries)
                .HasForeignKey(d => d.RegionId)
                .HasConstraintName("FK_Countries_Regions");
        });

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.Property(e => e.CurrencyId)
                .ValueGeneratedNever()
                .HasColumnName("currencyId");
            entity.Property(e => e.CurrencyAbbreviation)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("currencyAbbreviation");
            entity.Property(e => e.CurrencyName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("currencyName");
            entity.Property(e => e.Iso4217exponent)
                .HasMaxLength(3)
                .HasColumnName("ISO4217Exponent");
            entity.Property(e => e.Iso4217number)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasColumnName("ISO4217Number");
            entity.Property(e => e.Iso4271code)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasColumnName("ISO4271Code");
        });

        modelBuilder.Entity<CurrencyExchangeRate>(entity =>
        {
            entity.Property(e => e.CurrencyExchangeRateId).HasColumnName("CurrencyExchangeRateID");
            entity.Property(e => e.CurrencyTpeId).HasColumnName("CurrencyTpeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");

            entity.HasOne(d => d.CurrencyTpe).WithMany(p => p.CurrencyExchangeRates)
                .HasForeignKey(d => d.CurrencyTpeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CurrencyExchangeRates_CurrencyTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.CurrencyExchangeRates)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CurrencyExchangeRates_OperationLogs");
        });

        modelBuilder.Entity<CurrencyType>(entity =>
        {
            entity.Property(e => e.CurrencyTypeId).HasColumnName("CurrencyTypeID");
            entity.Property(e => e.Abbreviation).HasMaxLength(250);
            entity.Property(e => e.Iso4217exponent)
                .HasMaxLength(3)
                .HasColumnName("ISO4217Exponent");
            entity.Property(e => e.Iso4217number)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasColumnName("ISO4217Number");
            entity.Property(e => e.Iso4271code)
                .HasMaxLength(3)
                .IsFixedLength()
                .HasColumnName("ISO4271Code");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Symbol).HasMaxLength(250);
        });

        modelBuilder.Entity<DataCorrectionActionType>(entity =>
        {
            entity.Property(e => e.DataCorrectionActionTypeId).HasColumnName("DataCorrectionActionTypeID");
            entity.Property(e => e.ActionTypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<DataCorrectionAttribute>(entity =>
        {
            entity.Property(e => e.DataCorrectionAttributeId).HasColumnName("DataCorrectionAttributeID");
            entity.Property(e => e.AttributeCode).HasMaxLength(50);
            entity.Property(e => e.AttributeName).HasMaxLength(250);
            entity.Property(e => e.DataCorrectionAttributeLookupTypeId).HasColumnName("DataCorrectionAttributeLookupTypeID");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ValueType).HasMaxLength(50);

            entity.HasOne(d => d.DataCorrectionAttributeLookupType).WithMany(p => p.DataCorrectionAttributes)
                .HasForeignKey(d => d.DataCorrectionAttributeLookupTypeId)
                .HasConstraintName("FK_DataCorrectionAttributes_DataCorrectionAttributeLookupTypes");
        });

        modelBuilder.Entity<DataCorrectionAttributeLookupType>(entity =>
        {
            entity.Property(e => e.DataCorrectionAttributeLookupTypeId).HasColumnName("DataCorrectionAttributeLookupTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<DataCorrectionRequest>(entity =>
        {
            entity.Property(e => e.DataCorrectionRequestId).HasColumnName("DataCorrectionRequestID");
            entity.Property(e => e.BusinessEntityId).HasColumnName("BusinessEntityID");
            entity.Property(e => e.DataCorrectionRequestStateId).HasColumnName("DataCorrectionRequestStateID");
            entity.Property(e => e.DataCorrectionRequestTypeId).HasColumnName("DataCorrectionRequestTypeID");
            entity.Property(e => e.LastStateChangedByUserId).HasColumnName("LastStateChangedByUserID");
            entity.Property(e => e.LastStateChangedDate).HasColumnType("datetime");
            entity.Property(e => e.LastUpdatedByUserId).HasColumnName("LastUpdatedByUserID");
            entity.Property(e => e.LastUpdatedDate).HasColumnType("datetime");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.StateChangeRemarks).HasMaxLength(500);

            entity.HasOne(d => d.BusinessEntity).WithMany(p => p.DataCorrectionRequests)
                .HasForeignKey(d => d.BusinessEntityId)
                .HasConstraintName("FK_DataCorrectionRequests_BusinessEntities");

            entity.HasOne(d => d.DataCorrectionRequestState).WithMany(p => p.DataCorrectionRequests)
                .HasForeignKey(d => d.DataCorrectionRequestStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DataCorrectionRequests_DataCorrectionRequestStates");

            entity.HasOne(d => d.DataCorrectionRequestType).WithMany(p => p.DataCorrectionRequests)
                .HasForeignKey(d => d.DataCorrectionRequestTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DataCorrectionRequests_DataCorrectionRequestTypes");

            entity.HasOne(d => d.LastStateChangedByUser).WithMany(p => p.DataCorrectionRequestLastStateChangedByUsers)
                .HasForeignKey(d => d.LastStateChangedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DataCorrectionRequests_Users2");

            entity.HasOne(d => d.LastUpdatedByUser).WithMany(p => p.DataCorrectionRequestLastUpdatedByUsers)
                .HasForeignKey(d => d.LastUpdatedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DataCorrectionRequests_Users1");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.DataCorrectionRequests)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DataCorrectionRequests_OperationLogs");

            entity.HasOne(d => d.Request).WithMany(p => p.DataCorrectionRequests)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("FK_DataCorrectionRequests_Requests");

            entity.HasOne(d => d.Service).WithMany(p => p.DataCorrectionRequests)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK_DataCorrectionRequests_Services");
        });

        modelBuilder.Entity<DataCorrectionRequestAttributeValue>(entity =>
        {
            entity.Property(e => e.DataCorrectionRequestAttributeValueId).HasColumnName("DataCorrectionRequestAttributeValueID");
            entity.Property(e => e.DataCorrectionActionTypeId).HasColumnName("DataCorrectionActionTypeID");
            entity.Property(e => e.DataCorrectionAttributeId).HasColumnName("DataCorrectionAttributeID");
            entity.Property(e => e.DataCorrectionRequestId).HasColumnName("DataCorrectionRequestID");

            entity.HasOne(d => d.DataCorrectionActionType).WithMany(p => p.DataCorrectionRequestAttributeValues)
                .HasForeignKey(d => d.DataCorrectionActionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DataCorrectionRequestAttributeValues_DataCorrectionActionTypes");

            entity.HasOne(d => d.DataCorrectionAttribute).WithMany(p => p.DataCorrectionRequestAttributeValues)
                .HasForeignKey(d => d.DataCorrectionAttributeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DataCorrectionRequestAttributeValues_DataCorrectionAttributes");

            entity.HasOne(d => d.DataCorrectionRequest).WithMany(p => p.DataCorrectionRequestAttributeValues)
                .HasForeignKey(d => d.DataCorrectionRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DataCorrectionRequestAttributeValues_DataCorrectionRequests");
        });

        modelBuilder.Entity<DataCorrectionRequestState>(entity =>
        {
            entity.Property(e => e.DataCorrectionRequestStateId).HasColumnName("DataCorrectionRequestStateID");
            entity.Property(e => e.IsFinalState).HasDefaultValue(false);
            entity.Property(e => e.IsProcessingState).HasDefaultValue(false);
            entity.Property(e => e.StateName).HasMaxLength(250);
        });

        modelBuilder.Entity<DataCorrectionRequestType>(entity =>
        {
            entity.Property(e => e.DataCorrectionRequestTypeId).HasColumnName("DataCorrectionRequestTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<DataCorrectionRequestTypeAttribute>(entity =>
        {
            entity.HasKey(e => new { e.DataCorrectionRequestTypeId, e.DataCorrectionAttributeId, e.DataCorrectionActionTypeId });

            entity.Property(e => e.DataCorrectionRequestTypeId).HasColumnName("DataCorrectionRequestTypeID");
            entity.Property(e => e.DataCorrectionAttributeId).HasColumnName("DataCorrectionAttributeID");
            entity.Property(e => e.DataCorrectionActionTypeId).HasColumnName("DataCorrectionActionTypeID");

            entity.HasOne(d => d.DataCorrectionActionType).WithMany(p => p.DataCorrectionRequestTypeAttributes)
                .HasForeignKey(d => d.DataCorrectionActionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DataCorrectionRequestTypeAttributes_DataCorrectionActionTypes");

            entity.HasOne(d => d.DataCorrectionAttribute).WithMany(p => p.DataCorrectionRequestTypeAttributes)
                .HasForeignKey(d => d.DataCorrectionAttributeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DataCorrectionRequestTypeAttributes_DataCorrectionAttributes");

            entity.HasOne(d => d.DataCorrectionRequestType).WithMany(p => p.DataCorrectionRequestTypeAttributes)
                .HasForeignKey(d => d.DataCorrectionRequestTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DataCorrectionRequestTypeAttributes_DataCorrectionRequestTypes");
        });

        //modelBuilder.Entity<DayOfWeek>(entity =>
        //{
        //    entity.Property(e => e.DayOfWeekId).HasColumnName("DayOfWeekID");
        //    entity.Property(e => e.Name).HasMaxLength(50);
        //});

        modelBuilder.Entity<DayType>(entity =>
        {
            entity.Property(e => e.DayTypeId).HasColumnName("DayTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(50);
        });

        modelBuilder.Entity<DeductionAmountType>(entity =>
        {
            entity.Property(e => e.DeductionAmountTypeId).HasColumnName("DeductionAmountTypeID");
            entity.Property(e => e.TypeName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<DeductionType>(entity =>
        {
            entity.Property(e => e.DeductionTypeId).HasColumnName("DeductionTypeID");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyTypeId).HasColumnName("CurrencyTypeID");
            entity.Property(e => e.DeductionAmountTypeId).HasColumnName("DeductionAmountTypeID");
            entity.Property(e => e.DerivedOnTypeId).HasColumnName("DerivedOnTypeID");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OwnerOrganisationId).HasColumnName("OwnerOrganisationID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.TypeNameDhivehi).HasMaxLength(50);
            entity.Property(e => e.TypeNameEnglish).HasMaxLength(50);

            entity.HasOne(d => d.CurrencyType).WithMany(p => p.DeductionTypes)
                .HasForeignKey(d => d.CurrencyTypeId)
                .HasConstraintName("FK_DeductionTypes_CurrencyTypes");

            entity.HasOne(d => d.DeductionAmountType).WithMany(p => p.DeductionTypes)
                .HasForeignKey(d => d.DeductionAmountTypeId)
                .HasConstraintName("FK_DeductionTypes_DeductionAmountTypes");

            entity.HasOne(d => d.DerivedOnType).WithMany(p => p.DeductionTypes)
                .HasForeignKey(d => d.DerivedOnTypeId)
                .HasConstraintName("FK_DeductionTypes_DerivedOnTypes");

            entity.HasOne(d => d.OwnerOrganisation).WithMany(p => p.DeductionTypes)
                .HasForeignKey(d => d.OwnerOrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeductionTypes_Organisations");

            entity.HasOne(d => d.Request).WithMany(p => p.DeductionTypes)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("FK_DeductionTypes_Requests");
        });

        modelBuilder.Entity<DeductionTypeAmount>(entity =>
        {
            entity.Property(e => e.DeductionTypeAmountId).HasColumnName("DeductionTypeAmountID");
            entity.Property(e => e.DeductionTypeId).HasColumnName("DeductionTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.DeductionType).WithMany(p => p.DeductionTypeAmounts)
                .HasForeignKey(d => d.DeductionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeductionTypeAmounts_DeductionTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.DeductionTypeAmounts)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeductionTypeAmounts_OperationLogs");
        });

        modelBuilder.Entity<DependencyType>(entity =>
        {
            entity.HasKey(e => e.DependencyTypeId).HasName("PK_DependencyType");

            entity.Property(e => e.DependencyTypeId).HasColumnName("DependencyTypeID");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.IsValid)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<DerivedAdditionDeductionType>(entity =>
        {
            entity.Property(e => e.DerivedAdditionDeductionTypeId).HasColumnName("DerivedAdditionDeductionTypeID");
            entity.Property(e => e.DeductionTypeId).HasColumnName("DeductionTypeID");
            entity.Property(e => e.DerivedAdditionDeductionTypeItemId).HasColumnName("DerivedAdditionDeductionTypeItemID");

            entity.HasOne(d => d.DeductionType).WithMany(p => p.DerivedAdditionDeductionTypes)
                .HasForeignKey(d => d.DeductionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DerivedAdditionDeductionTypes_DeductionTypes");

            entity.HasOne(d => d.DerivedAdditionDeductionTypeItem).WithMany(p => p.DerivedAdditionDeductionTypes)
                .HasForeignKey(d => d.DerivedAdditionDeductionTypeItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DerivedAdditionDeductionTypes_DerivedAdditionDeductionTypeItems");
        });

        modelBuilder.Entity<DerivedAdditionDeductionTypeItem>(entity =>
        {
            entity.Property(e => e.DerivedAdditionDeductionTypeItemId).HasColumnName("DerivedAdditionDeductionTypeItemID");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<DerivedOnPayrollItemType>(entity =>
        {
            entity.Property(e => e.DerivedOnPayrollItemTypeId).HasColumnName("DerivedOnPayrollItemTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OtherPayrollItemTypeId).HasColumnName("OtherPayrollItemTypeID");
            entity.Property(e => e.PayrollItemTypeId).HasColumnName("PayrollItemTypeID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.DerivedOnPayrollItemTypes)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DerivedOnPayrollItemTypes_OperationLogs");

            entity.HasOne(d => d.OtherPayrollItemType).WithMany(p => p.DerivedOnPayrollItemTypeOtherPayrollItemTypes)
                .HasForeignKey(d => d.OtherPayrollItemTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DerivedOnPayrollItemTypes_PayrollItemTypes1");

            entity.HasOne(d => d.PayrollItemType).WithMany(p => p.DerivedOnPayrollItemTypePayrollItemTypes)
                .HasForeignKey(d => d.PayrollItemTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DerivedOnPayrollItemTypes_PayrollItemTypes");
        });

        modelBuilder.Entity<DerivedOnType>(entity =>
        {
            entity.Property(e => e.DerivedOnTypeId).HasColumnName("DerivedOnTypeID");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Dhafthar>(entity =>
        {
            entity.HasKey(e => e.LocationId);

            entity.ToTable("Dhafthar");

            entity.Property(e => e.LocationId)
                .ValueGeneratedNever()
                .HasColumnName("LocationID");
            entity.Property(e => e.DhaftharNumber).HasMaxLength(50);

            entity.HasOne(d => d.Location).WithOne(p => p.Dhafthar)
                .HasForeignKey<Dhafthar>(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dhafthar_Address");
        });

        modelBuilder.Entity<DnrlookupCache>(entity =>
        {
            entity.HasKey(e => e.IdcardNo);

            entity.ToTable("DNRLookupCache");

            entity.Property(e => e.IdcardNo)
                .HasMaxLength(10)
                .HasColumnName("IDCardNo");
            entity.Property(e => e.CreatedByIpaddress)
                .HasMaxLength(50)
                .HasColumnName("CreatedByIPAddress");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.LastUpdatedByIpaddress)
                .HasMaxLength(50)
                .HasColumnName("LastUpdatedByIPAddress");
            entity.Property(e => e.LastUpdatedDate).HasColumnType("datetime");
            entity.Property(e => e.LookupData).HasColumnType("xml");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasIndex(e => e.DocumentTypeId, "ix_IndexName_10_MARCH");

            entity.Property(e => e.DocumentId).HasColumnName("DocumentID");
            entity.Property(e => e.DocumentName).HasMaxLength(250);
            entity.Property(e => e.DocumentTypeId).HasColumnName("DocumentTypeID");
            entity.Property(e => e.Mimetype)
                .HasMaxLength(250)
                .HasColumnName("MIMEType");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.DocumentType).WithMany(p => p.Documents)
                .HasForeignKey(d => d.DocumentTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Documents_DocumentTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Documents)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_Documents_OperationLogs");
        });

        modelBuilder.Entity<DocumentState>(entity =>
        {
            entity.Property(e => e.DocumentStateId).HasColumnName("DocumentStateID");
            entity.Property(e => e.StateName).HasMaxLength(250);
        });

        modelBuilder.Entity<DocumentType>(entity =>
        {
            entity.Property(e => e.DocumentTypeId).HasColumnName("DocumentTypeID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.ServiceTypeId).HasColumnName("ServiceTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
            entity.Property(e => e.TypeNameDhivehi).HasMaxLength(250);

            entity.HasOne(d => d.Organisation).WithMany(p => p.DocumentTypes)
                .HasForeignKey(d => d.OrganisationId)
                .HasConstraintName("FK_DocumentTypes_Organisations");

            entity.HasOne(d => d.ServiceType).WithMany(p => p.DocumentTypes)
                .HasForeignKey(d => d.ServiceTypeId)
                .HasConstraintName("FK_DocumentTypes_ServiceTypes");
        });

        modelBuilder.Entity<Element>(entity =>
        {
            entity.Property(e => e.ElementId).HasColumnName("ElementID");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.DayOfWeekId).HasColumnName("DayOfWeekID");
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.SpecialDayTypeId).HasColumnName("SpecialDayTypeID");
            entity.Property(e => e.WorkShiftId).HasColumnName("WorkShiftID");

            entity.HasOne(d => d.DayOfWeek).WithMany(p => p.Elements)
                .HasForeignKey(d => d.DayOfWeekId)
                .HasConstraintName("FK_ScheduleElements_DayOfWeeks");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Elements)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Elements_OperationLogs");

            entity.HasOne(d => d.SpecialDayType).WithMany(p => p.Elements)
                .HasForeignKey(d => d.SpecialDayTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScheduleElements_SpecialDayTypes");

            entity.HasOne(d => d.WorkShift).WithMany(p => p.Elements)
                .HasForeignKey(d => d.WorkShiftId)
                .HasConstraintName("FK_ScheduleElements_WorkShifts");
        });

        modelBuilder.Entity<EventType>(entity =>
        {
            entity.Property(e => e.EventTypeId).HasColumnName("EventTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<FacilityRegistrationType>(entity =>
        {
            entity.Property(e => e.FacilityRegistrationTypeId).HasColumnName("FacilityRegistrationTypeID");
            entity.Property(e => e.RegistrationNumberFormat)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RegistrationNumberRegex)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TypeName)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<GenderType>(entity =>
        {
            entity.Property(e => e.GenderTypeId).HasColumnName("GenderTypeID");
            entity.Property(e => e.Abbreviation).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.Property(e => e.GroupId).HasColumnName("GroupID");
            entity.Property(e => e.BusinessEntityOrganisationId).HasColumnName("BusinessEntityOrganisationID");
            entity.Property(e => e.GroupTypeId).HasColumnName("GroupTypeID");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.OperationLogId)
                .HasDefaultValue(2)
                .HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");

            entity.HasOne(d => d.BusinessEntityOrganisation).WithMany(p => p.Groups)
                .HasForeignKey(d => d.BusinessEntityOrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Groups_Organisations");

            entity.HasOne(d => d.GroupType).WithMany(p => p.Groups)
                .HasForeignKey(d => d.GroupTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Groups_GroupTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Groups)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Groups_OperationLogs");

            entity.HasOne(d => d.OrganisationStructure).WithMany(p => p.Groups)
                .HasForeignKey(d => d.OrganisationStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Groups_OrganisationStructure");
        });

        modelBuilder.Entity<GroupConfigurableValue>(entity =>
        {
            entity.Property(e => e.GroupConfigurableValueId).HasColumnName("GroupConfigurableValueID");
            entity.Property(e => e.GroupConfigurableValueTypeId).HasColumnName("GroupConfigurableValueTypeID");
            entity.Property(e => e.GroupId).HasColumnName("GroupID");

            entity.HasOne(d => d.GroupConfigurableValueType).WithMany(p => p.GroupConfigurableValues)
                .HasForeignKey(d => d.GroupConfigurableValueTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupConfigurableValues_GroupConfigurableValueTypes");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupConfigurableValues)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupConfigurableValues_Groups");
        });

        modelBuilder.Entity<GroupConfigurableValueType>(entity =>
        {
            entity.Property(e => e.GroupConfigurableValueTypeId).HasColumnName("GroupConfigurableValueTypeID");
            entity.Property(e => e.Comment).HasColumnType("text");
            entity.Property(e => e.Type).HasMaxLength(50);
        });

        modelBuilder.Entity<GroupLeaveSet>(entity =>
        {
            entity.Property(e => e.GroupLeaveSetId).HasColumnName("GroupLeaveSetID");
            entity.Property(e => e.GroupId).HasColumnName("GroupID");
            entity.Property(e => e.LeaveSetId).HasColumnName("LeaveSetID");
            entity.Property(e => e.OperationLogId)
                .HasDefaultValue(2)
                .HasColumnName("OperationLogID");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupLeaveSets)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupLeaveSets_Groups");

            entity.HasOne(d => d.LeaveSet).WithMany(p => p.GroupLeaveSets)
                .HasForeignKey(d => d.LeaveSetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupLeaveSets_LeaveSets");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.GroupLeaveSets)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupLeaveSets_OperationLogs");
        });

        modelBuilder.Entity<GroupSchedule>(entity =>
        {
            entity.Property(e => e.GroupScheduleId).HasColumnName("GroupScheduleID");
            entity.Property(e => e.GroupId).HasColumnName("GroupID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupSchedules)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupSchedules_Groups");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.GroupSchedules)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupSchedules_OperationLogs");

            entity.HasOne(d => d.Schedule).WithMany(p => p.GroupSchedules)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupSchedules_Schedules");
        });

        modelBuilder.Entity<GroupStaff>(entity =>
        {
            entity.Property(e => e.GroupStaffId).HasColumnName("GroupStaffID");
            entity.Property(e => e.EffectiveEndDate).HasColumnType("datetime");
            entity.Property(e => e.EffectiveStartDate).HasColumnType("datetime");
            entity.Property(e => e.GroupId).HasColumnName("GroupID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.StaffIndividualId).HasColumnName("StaffIndividualID");

            entity.HasOne(d => d.Group).WithMany(p => p.GroupStaffs)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupStaffs_Groups");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.GroupStaffs)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupStaffs_OperationLogs");

            entity.HasOne(d => d.StaffIndividual).WithMany(p => p.GroupStaffs)
                .HasForeignKey(d => d.StaffIndividualId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupStaffs_Staffs");
        });

        modelBuilder.Entity<GroupType>(entity =>
        {
            entity.Property(e => e.GroupTypeId).HasColumnName("GroupTypeID");
            entity.Property(e => e.Name).HasMaxLength(150);

            entity.HasOne(d => d.OrganisationBusinessEntity).WithMany(p => p.GroupTypes)
                .HasForeignKey(d => d.OrganisationBusinessEntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GroupTypes_Organisations");
        });

        modelBuilder.Entity<Idcard>(entity =>
        {
            entity.ToTable("IDCards");

            entity.HasIndex(e => e.IdcardNumber, "IX_IDCards").IsUnique();

            entity.Property(e => e.IdcardId).HasColumnName("IDCardID");
            entity.Property(e => e.BusinessEntityId).HasColumnName("BusinessEntityID");
            entity.Property(e => e.ExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.IdcardNumber)
                .HasMaxLength(50)
                .HasColumnName("IDCardNumber");
            entity.Property(e => e.IdcardStateId).HasColumnName("IDCardStateID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.BusinessEntity).WithMany(p => p.Idcards)
                .HasForeignKey(d => d.BusinessEntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IDCards_Individuals");

            entity.HasOne(d => d.IdcardState).WithMany(p => p.Idcards)
                .HasForeignKey(d => d.IdcardStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IDCards_IDCardStates");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Idcards)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_IDCards_OperationLogs");
        });

        modelBuilder.Entity<IdcardState>(entity =>
        {
            entity.ToTable("IDCardStates");

            entity.Property(e => e.IdcardStateId).HasColumnName("IDCardStateID");
            entity.Property(e => e.StateName).HasMaxLength(250);
        });

        modelBuilder.Entity<IdentityCardType>(entity =>
        {
            entity.Property(e => e.IdentityCardTypeId).HasColumnName("IdentityCardTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<InOutMode>(entity =>
        {
            entity.Property(e => e.InOutModeId).HasColumnName("InOutModeID");
            entity.Property(e => e.Icon).HasMaxLength(100);
            entity.Property(e => e.NameLong).HasMaxLength(100);
            entity.Property(e => e.NameShort).HasMaxLength(50);
        });

        modelBuilder.Entity<Individual>(entity =>
        {
            entity.HasKey(e => e.BusinessEntityId);

            entity.Property(e => e.BusinessEntityId)
                .ValueGeneratedNever()
                .HasColumnName("BusinessEntityID");
            entity.Property(e => e.CountryId)
                .HasDefaultValue(1)
                .HasColumnName("CountryID");
            entity.Property(e => e.DateOfBirth).HasColumnType("datetime");
            entity.Property(e => e.FirstNameDhivehi).HasMaxLength(250);
            entity.Property(e => e.FirstNameEnglish).HasMaxLength(250);
            entity.Property(e => e.GenderTypeId).HasColumnName("GenderTypeID");
            entity.Property(e => e.LastNameDhivehi).HasMaxLength(250);
            entity.Property(e => e.LastNameEnglish).HasMaxLength(250);
            entity.Property(e => e.MiddleNameDhivehi).HasMaxLength(250);
            entity.Property(e => e.MiddleNameEnglish).HasMaxLength(250);

            entity.HasOne(d => d.BusinessEntity).WithOne(p => p.Individual)
                .HasForeignKey<Individual>(d => d.BusinessEntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Individuals_BusinessEntities");

            entity.HasOne(d => d.Country).WithMany(p => p.Individuals)
                .HasForeignKey(d => d.CountryId)
                .HasConstraintName("FK_Individuals_Countries");

            entity.HasOne(d => d.GenderType).WithMany(p => p.Individuals)
                .HasForeignKey(d => d.GenderTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Individuals_GenderTypes");
        });

        modelBuilder.Entity<Island>(entity =>
        {
            entity.Property(e => e.IslandId).HasColumnName("IslandID");
            entity.Property(e => e.AtollId).HasColumnName("AtollID");
            entity.Property(e => e.CityId).HasColumnName("CityID");
            entity.Property(e => e.NameDhivehi).HasMaxLength(250);
            entity.Property(e => e.NameEnglish).HasMaxLength(250);
            entity.Property(e => e.PostCode).HasMaxLength(50);

            entity.HasOne(d => d.Atoll).WithMany(p => p.Islands)
                .HasForeignKey(d => d.AtollId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Islands_Atolls");

            entity.HasOne(d => d.City).WithMany(p => p.Islands)
                .HasForeignKey(d => d.CityId)
                .HasConstraintName("FK_Islands_Cities");
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.JobId).HasName("PK_Staffs");

            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.BasicSalary).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.IndividualID).HasColumnName("IndividualID");
            entity.Property(e => e.JobStateId).HasColumnName("JobStateID");
            entity.Property(e => e.JobTypeId).HasColumnName("JobTypeID");
            entity.Property(e => e.JoinedDate).HasColumnType("datetime");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationID).HasColumnName("OrganisationID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.Sapnumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SAPNumber");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.TerminatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.Individual).WithMany(p => p.Jobs)
                .HasForeignKey(d => d.IndividualID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Jobs_Staffs");

            entity.HasOne(d => d.JobState).WithMany(p => p.Jobs)
                .HasForeignKey(d => d.JobStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Jobs_JobStates1");

            entity.HasOne(d => d.JobType).WithMany(p => p.Jobs)
                .HasForeignKey(d => d.JobTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Jobs_JobTypes");

            entity.HasOne(d => d.Organisation).WithMany(p => p.Jobs)
                .HasForeignKey(d => d.OrganisationID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Jobs_Organisations");

            entity.HasOne(d => d.OrganisationStructure).WithMany(p => p.Jobs)
                .HasForeignKey(d => d.OrganisationStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Jobs_OrganisationStructure");

            entity.HasOne(d => d.Service).WithMany(p => p.Jobs)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK_Jobs_Services");
        });

        modelBuilder.Entity<JobLeaveType>(entity =>
        {
            entity.HasKey(e => e.JobLeaveTypeId).HasName("PK_JobLeaves");

            entity.HasIndex(e => new { e.JobId, e.LeaveTypeId }, "IX_JobLeaveTypes");

            entity.Property(e => e.JobLeaveTypeId).HasColumnName("JobLeaveTypeID");
            entity.Property(e => e.AutoCorrectDate).HasColumnType("datetime");
            entity.Property(e => e.AutoCorrectRemark).HasMaxLength(250);
            entity.Property(e => e.EffectiveFromDate).HasColumnType("datetime");
            entity.Property(e => e.EffectiveToDate).HasColumnType("datetime");
            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.LastLeaveTakenDate).HasColumnType("datetime");
            entity.Property(e => e.LeaveTypeId).HasColumnName("LeaveTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.RenewedDate).HasColumnType("datetime");

            entity.HasOne(d => d.Job).WithMany(p => p.JobLeaveTypes)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobLeaves_Jobs");

            entity.HasOne(d => d.LeaveType).WithMany(p => p.JobLeaveTypes)
                .HasForeignKey(d => d.LeaveTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobLeaves_LeaveTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.JobLeaveTypes)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobLeaves_OperationLogs");
        });

        modelBuilder.Entity<JobPosition>(entity =>
        {
            entity.HasIndex(e => new { e.FromDate, e.ToDate }, "IDX_JobPositions_FRDT_TODT").IsDescending();

            entity.HasIndex(e => e.PositionId, "IDX_JobPositions_PositionID");

            entity.HasIndex(e => e.JobId, "NonClusteredIndex-20160804-145620");

            entity.Property(e => e.JobPositionId).HasColumnName("JobPositionID");
            entity.Property(e => e.FromDate).HasColumnType("datetime");
            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.JobPositionStateId).HasColumnName("JobPositionStateID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.PositionId).HasColumnName("PositionID");
            entity.Property(e => e.ToDate).HasColumnType("datetime");

            entity.HasOne(d => d.Job).WithMany(p => p.JobPositions)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobPositions_Jobs");

            entity.HasOne(d => d.JobPositionState).WithMany(p => p.JobPositions)
                .HasForeignKey(d => d.JobPositionStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobPositions_JobPositionStates");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.JobPositions)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobPositions_OperationLogs");

            entity.HasOne(d => d.Position).WithMany(p => p.JobPositions)
                .HasForeignKey(d => d.PositionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobPositions_Positions");
        });

        modelBuilder.Entity<JobPositionBasicSalary>(entity =>
        {
            entity.HasKey(e => e.JobPositionBasicSalaryId).HasName("PK_JobPositionBasicSalaries_1");

            entity.Property(e => e.JobPositionBasicSalaryId).HasColumnName("JobPositionBasicSalaryID");
            entity.Property(e => e.JobPoistionId).HasColumnName("JobPoistionID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.JobPoistion).WithMany(p => p.JobPositionBasicSalaries)
                .HasForeignKey(d => d.JobPoistionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobPositionBasicSalaries_JobPositions");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.JobPositionBasicSalaries)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobPositionBasicSalaries_OperationLogs");
        });

        modelBuilder.Entity<JobPositionState>(entity =>
        {
            entity.Property(e => e.JobPositionStateId).HasColumnName("JobPositionStateID");
            entity.Property(e => e.StateName)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<JobPositionsRequest>(entity =>
        {
            entity.Property(e => e.JobPositionsRequestId).HasColumnName("JobPositionsRequestID");
            entity.Property(e => e.JobPositionId).HasColumnName("JobPositionID");
            entity.Property(e => e.JobPositionRemovalDate).HasColumnType("datetime");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");

            entity.HasOne(d => d.JobPosition).WithMany(p => p.JobPositionsRequests)
                .HasForeignKey(d => d.JobPositionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobPositionsRequests_JobPositions");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.JobPositionsRequests)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobPositionsRequests_OperationLogs");
        });

        modelBuilder.Entity<JobRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK_StaffPositionRequests");

            entity.Property(e => e.RequestId)
                .ValueGeneratedNever()
                .HasColumnName("RequestID");
            entity.Property(e => e.JobId).HasColumnName("JobID");

            entity.HasOne(d => d.Job).WithMany(p => p.JobRequests)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobRequests_Jobs");

            entity.HasOne(d => d.Request).WithOne(p => p.JobRequest)
                .HasForeignKey<JobRequest>(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobPositionRequests_Requests");
        });

        modelBuilder.Entity<JobSapexception>(entity =>
        {
            entity.HasKey(e => e.SapexceptionId);

            entity.ToTable("JobSAPExceptions");

            entity.Property(e => e.SapexceptionId)
                .ValueGeneratedNever()
                .HasColumnName("SAPExceptionID");
            entity.Property(e => e.JobId).HasColumnName("JobID");

            entity.HasOne(d => d.Job).WithMany(p => p.JobSapexceptions)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobSAPExceptions_Jobs");

            entity.HasOne(d => d.Sapexception).WithOne(p => p.JobSapexception)
                .HasForeignKey<JobSapexception>(d => d.SapexceptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobSAPExceptions_SAPExceptions");
        });

        modelBuilder.Entity<JobState>(entity =>
        {
            entity.HasKey(e => e.JobStateId).HasName("PK_StaffPositionStates");

            entity.Property(e => e.JobStateId).HasColumnName("JobStateID");
            entity.Property(e => e.StateName)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<JobTerminationType>(entity =>
        {
            entity.Property(e => e.JobTerminationTypeId).HasColumnName("JobTerminationTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<JobType>(entity =>
        {
            entity.HasKey(e => e.JobTypeId).HasName("PK_EmployeeTypes");

            entity.Property(e => e.JobTypeId).HasColumnName("JobTypeID");
            entity.Property(e => e.CscjobTypePrimaryKey).HasColumnName("CSCJobTypePrimaryKey");
            entity.Property(e => e.TypeName).HasMaxLength(250);
            entity.Property(e => e.TypeNameDhivehi).HasMaxLength(50);

            entity.HasOne(d => d.OperationLog).WithMany(p => p.JobTypes)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_JobTypes_OperationLogs");
        });

        modelBuilder.Entity<JobTypesException>(entity =>
        {
            entity.HasKey(e => e.SapexceptionId);

            entity.Property(e => e.SapexceptionId)
                .ValueGeneratedNever()
                .HasColumnName("SAPExceptionID");
            entity.Property(e => e.JobTypeId).HasColumnName("JobTypeID");

            entity.HasOne(d => d.JobType).WithMany(p => p.JobTypesExceptions)
                .HasForeignKey(d => d.JobTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobTypesExceptions_JobTypes");

            entity.HasOne(d => d.Sapexception).WithOne(p => p.JobTypesException)
                .HasForeignKey<JobTypesException>(d => d.SapexceptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_JobTypesExceptions_SAPExceptions");
        });

        modelBuilder.Entity<KeyHolder>(entity =>
        {
            entity.HasKey(e => e.KeyHolderId).HasName("PK_KeyHolder");

            entity.Property(e => e.KeyHolderId).HasColumnName("KeyHolderID");
            entity.Property(e => e.IndividualId).HasColumnName("IndividualID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.Individual).WithMany(p => p.KeyHolders)
                .HasForeignKey(d => d.IndividualId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KeyHolders_Staffs");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.KeyHolders)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KeyHolders_OperationLogs");
        });

        modelBuilder.Entity<Kpidocument>(entity =>
        {
            entity.ToTable("KPIDocuments");

            entity.Property(e => e.KpidocumentId).HasColumnName("KPIDocumentID");
            entity.Property(e => e.DocumentId).HasColumnName("DocumentID");
            entity.Property(e => e.Kpiid).HasColumnName("KPIID");
            entity.Property(e => e.LinkedByUserId).HasColumnName("LinkedByUserID");
            entity.Property(e => e.LinkedDate).HasColumnType("datetime");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.Document).WithMany(p => p.Kpidocuments)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KPIDocuments_Documents");

            entity.HasOne(d => d.LinkedByUser).WithMany(p => p.Kpidocuments)
                .HasForeignKey(d => d.LinkedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KPIDocuments_Users");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Kpidocuments)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KPIDocuments_OperationLogs");
        });

        modelBuilder.Entity<Language>(entity =>
        {
            entity.Property(e => e.LanguageId).HasColumnName("LanguageID");
            entity.Property(e => e.CultureName).HasMaxLength(250);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.NameDhivehi).HasMaxLength(250);
        });

        modelBuilder.Entity<Leaf>(entity =>
        {
            entity.HasKey(e => e.LeaveId);

            entity.HasIndex(e => new { e.FromDate, e.ToDate }, "IDX_LEAVES_FRDT_TODT").IsDescending();

            entity.HasIndex(e => new { e.JobId, e.LeaveTypeId, e.LeaveStateId }, "IX_Leaves_LeaveType_LeaveState");

            entity.HasIndex(e => new { e.LeaveTypeId, e.FromDate, e.ToDate }, "ix_IndexName_10_MARCH");

            entity.Property(e => e.LeaveId).HasColumnName("LeaveID");
            entity.Property(e => e.ChitNo).HasMaxLength(250);
            entity.Property(e => e.FromDate).HasColumnType("datetime");
            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.LeaveStateId).HasColumnName("LeaveStateID");
            entity.Property(e => e.LeaveTypeId).HasColumnName("LeaveTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.ToDate).HasColumnType("datetime");

            entity.HasOne(d => d.Job).WithMany(p => p.Leaves)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Leaves_Jobs");

            entity.HasOne(d => d.LeaveState).WithMany(p => p.Leaves)
                .HasForeignKey(d => d.LeaveStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Leaves_LeaveStates");

            entity.HasOne(d => d.LeaveType).WithMany(p => p.Leaves)
                .HasForeignKey(d => d.LeaveTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Leaves_LeaveTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Leaves)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Leaves_OperationLogs");
        });

        modelBuilder.Entity<LeaveChangeRequest>(entity =>
        {
            entity.Property(e => e.LeaveChangeRequestId).HasColumnName("LeaveChangeRequestID");
            entity.Property(e => e.FromDate).HasColumnType("datetime");
            entity.Property(e => e.LeaveChangeRequestTypeId).HasColumnName("LeaveChangeRequestTypeID");
            entity.Property(e => e.LeaveId).HasColumnName("LeaveID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.Reason).HasMaxLength(250);
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.ToDate).HasColumnType("datetime");

            entity.HasOne(d => d.LeaveChangeRequestType).WithMany(p => p.LeaveChangeRequests)
                .HasForeignKey(d => d.LeaveChangeRequestTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveChangeRequests_LeaveChangeRequestTypes");

            entity.HasOne(d => d.Leave).WithMany(p => p.LeaveChangeRequests)
                .HasForeignKey(d => d.LeaveId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveChangeRequests_Leaves");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.LeaveChangeRequests)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveChangeRequests_OperationLogs");

            entity.HasOne(d => d.Request).WithMany(p => p.LeaveChangeRequests)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveChangeRequests_Requests");
        });

        modelBuilder.Entity<LeaveChangeRequestType>(entity =>
        {
            entity.Property(e => e.LeaveChangeRequestTypeId).HasColumnName("LeaveChangeRequestTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<LeaveConfigurableValue>(entity =>
        {
            entity.HasIndex(e => new { e.OrganisationId, e.JobTypeId, e.LeaveConfigurableValueTypeId }, "UQ_OrganisationID_JobTypeID_LeaveConfigurableValueTypeID").IsUnique();

            entity.Property(e => e.LeaveConfigurableValueId).HasColumnName("LeaveConfigurableValueID");
            entity.Property(e => e.JobTypeId).HasColumnName("JobTypeID");
            entity.Property(e => e.LeaveConfigurableValueTypeId).HasColumnName("LeaveConfigurableValueTypeID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.Value).HasColumnType("text");

            entity.HasOne(d => d.JobType).WithMany(p => p.LeaveConfigurableValues)
                .HasForeignKey(d => d.JobTypeId)
                .HasConstraintName("FK_LeaveConfigurableValues_JobTypes");

            entity.HasOne(d => d.LeaveConfigurableValueType).WithMany(p => p.LeaveConfigurableValues)
                .HasForeignKey(d => d.LeaveConfigurableValueTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveConfigurableValues_LeaveConfigurableValueTypes");

            entity.HasOne(d => d.Organisation).WithMany(p => p.LeaveConfigurableValues)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveConfigurableValues_Organisations");
        });

        modelBuilder.Entity<LeaveConfigurableValueType>(entity =>
        {
            entity.Property(e => e.LeaveConfigurableValueTypeId).HasColumnName("LeaveConfigurableValueTypeID");
            entity.Property(e => e.Comment).HasColumnType("text");
            entity.Property(e => e.IsJobTypeDependant).HasColumnName("isJobTypeDependant");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.Unit).HasMaxLength(250);
        });

        modelBuilder.Entity<LeaveDocument>(entity =>
        {
            entity.Property(e => e.LeaveDocumentId).HasColumnName("LeaveDocumentID");
            entity.Property(e => e.DocumentId).HasColumnName("DocumentID");
            entity.Property(e => e.LeaveId).HasColumnName("LeaveID");
            entity.Property(e => e.LinkedByUserId).HasColumnName("LinkedByUserID");
            entity.Property(e => e.LinkedDate).HasColumnType("datetime");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.Document).WithMany(p => p.LeaveDocuments)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveDocuments_Documents");

            entity.HasOne(d => d.Leave).WithMany(p => p.LeaveDocuments)
                .HasForeignKey(d => d.LeaveId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveDocuments_Leaves");

            entity.HasOne(d => d.LinkedByUser).WithMany(p => p.LeaveDocuments)
                .HasForeignKey(d => d.LinkedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveDocuments_Users");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.LeaveDocuments)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_LeaveDocuments_OperationLogs");
        });

        modelBuilder.Entity<LeaveForm>(entity =>
        {
            entity.HasKey(e => e.LeaveId).HasName("PK_LeaveForms_1");

            entity.Property(e => e.LeaveId)
                .ValueGeneratedNever()
                .HasColumnName("LeaveID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.ReportedDateTime).HasColumnType("datetime");

            entity.HasOne(d => d.Leave).WithOne(p => p.LeaveForm)
                .HasForeignKey<LeaveForm>(d => d.LeaveId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveForms_Leaves");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.LeaveForms)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveForms_OperationLogs");
        });

        modelBuilder.Entity<LeaveLodgeType>(entity =>
        {
            entity.Property(e => e.LeaveLodgeTypeId).HasColumnName("LeaveLodgeTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<LeaveReason>(entity =>
        {
            entity.HasKey(e => e.LeaveId);

            entity.Property(e => e.LeaveId)
                .ValueGeneratedNever()
                .HasColumnName("LeaveID");
            entity.Property(e => e.LeaveTypeReasonTypeId).HasColumnName("LeaveTypeReasonTypeID");
            entity.Property(e => e.Reason).HasMaxLength(250);

            entity.HasOne(d => d.Leave).WithOne(p => p.LeaveReason)
                .HasForeignKey<LeaveReason>(d => d.LeaveId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveReasons_Leaves");

            entity.HasOne(d => d.LeaveTypeReasonType).WithMany(p => p.LeaveReasons)
                .HasForeignKey(d => d.LeaveTypeReasonTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveReasons_LeaveTypeReasonTypes");
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(e => e.LeaveId);

            entity.Property(e => e.LeaveId)
                .ValueGeneratedNever()
                .HasColumnName("LeaveID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");

            entity.HasOne(d => d.Leave).WithOne(p => p.LeaveRequest)
                .HasForeignKey<LeaveRequest>(d => d.LeaveId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveRequests_Leaves");

            entity.HasOne(d => d.Request).WithMany(p => p.LeaveRequests)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveRequests_Requests");
        });

        modelBuilder.Entity<LeaveSet>(entity =>
        {
            entity.Property(e => e.LeaveSetId).HasColumnName("LeaveSetID");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.LeaveSets)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveSets_OperationLogs");

            entity.HasOne(d => d.Organisation).WithMany(p => p.LeaveSets)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveSets_Organisations");
        });

        modelBuilder.Entity<LeaveSetLeaveType>(entity =>
        {
            entity.Property(e => e.LeaveSetLeaveTypeId).HasColumnName("LeaveSetLeaveTypeID");
            entity.Property(e => e.LeaveSetId).HasColumnName("LeaveSetID");
            entity.Property(e => e.LeaveTypeId).HasColumnName("LeaveTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.LeaveSet).WithMany(p => p.LeaveSetLeaveTypes)
                .HasForeignKey(d => d.LeaveSetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveSetLeaveTypes_LeaveSets");

            entity.HasOne(d => d.LeaveType).WithMany(p => p.LeaveSetLeaveTypes)
                .HasForeignKey(d => d.LeaveTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveSetLeaveTypes_LeaveTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.LeaveSetLeaveTypes)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveSetLeaveTypes_OperationLogs");
        });

        modelBuilder.Entity<LeaveSpendingLocation>(entity =>
        {
            entity.HasKey(e => e.LeaveId).HasName("PK_LeaveSpendingLocations_1");

            entity.Property(e => e.LeaveId)
                .ValueGeneratedNever()
                .HasColumnName("LeaveID");
            entity.Property(e => e.Address).HasMaxLength(250);
            entity.Property(e => e.ContactNumber).HasMaxLength(250);
            entity.Property(e => e.CountryId).HasColumnName("CountryID");
            entity.Property(e => e.IslandId).HasColumnName("IslandID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.Country).WithMany(p => p.LeaveSpendingLocations)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveSpendingLocations_Countries");

            entity.HasOne(d => d.Island).WithMany(p => p.LeaveSpendingLocations)
                .HasForeignKey(d => d.IslandId)
                .HasConstraintName("FK_LeaveSpendingLocations_Islands");

            entity.HasOne(d => d.Leave).WithOne(p => p.LeaveSpendingLocation)
                .HasForeignKey<LeaveSpendingLocation>(d => d.LeaveId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveSpendingLocations_Leaves");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.LeaveSpendingLocations)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveSpendingLocations_OperationLogs");
        });

        modelBuilder.Entity<LeaveState>(entity =>
        {
            entity.Property(e => e.LeaveStateId).HasColumnName("LeaveStateID");
            entity.Property(e => e.StateName).HasMaxLength(250);
        });

        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.Property(e => e.LeaveTypeId).HasColumnName("LeaveTypeID");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.NameDhivehi).HasMaxLength(250);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.RequestTypeId).HasColumnName("RequestTypeID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.LeaveTypes)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveTypes_OperationLogs");

            entity.HasOne(d => d.Organisation).WithMany(p => p.LeaveTypes)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveTypes_Organisations");

            entity.HasOne(d => d.RequestType).WithMany(p => p.LeaveTypes)
                .HasForeignKey(d => d.RequestTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveTypes_RequestTypes");
        });

        modelBuilder.Entity<LeaveTypeReasonType>(entity =>
        {
            entity.Property(e => e.LeaveTypeReasonTypeId).HasColumnName("LeaveTypeReasonTypeID");
            entity.Property(e => e.LeaveTypeId).HasColumnName("LeaveTypeID");
            entity.Property(e => e.ReasonTypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<LeaveWorkHandOver>(entity =>
        {
            entity.HasKey(e => e.LeaveWorkHandOverId).HasName("PK_LeaveWorkHandOvers_1");

            entity.Property(e => e.LeaveWorkHandOverId).HasColumnName("LeaveWorkHandOverID");
            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.LeaveId).HasColumnName("LeaveID");

            entity.HasOne(d => d.Job).WithMany(p => p.LeaveWorkHandOvers)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveWorkHandOvers_Jobs1");

            entity.HasOne(d => d.Leave).WithMany(p => p.LeaveWorkHandOvers)
                .HasForeignKey(d => d.LeaveId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveWorkHandOvers_Leaves1");
        });

        modelBuilder.Entity<LeaveWorkHandOverTask>(entity =>
        {
            entity.HasKey(e => e.LeaveId);

            entity.Property(e => e.LeaveId)
                .ValueGeneratedNever()
                .HasColumnName("LeaveID");
            entity.Property(e => e.TaskDetails).HasMaxLength(250);

            entity.HasOne(d => d.Leave).WithOne(p => p.LeaveWorkHandOverTask)
                .HasForeignKey<LeaveWorkHandOverTask>(d => d.LeaveId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveWorkHandOverTasks_Leaves");
        });

        modelBuilder.Entity<LeavesBulkUploadedDocument>(entity =>
        {
            entity.HasKey(e => e.DocumentId);

            entity.Property(e => e.DocumentId)
                .ValueGeneratedNever()
                .HasColumnName("DocumentID");
            entity.Property(e => e.DocumentStateId).HasColumnName("DocumentStateID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.UploadedByUserId).HasColumnName("UploadedByUserID");
            entity.Property(e => e.UploadedDate).HasColumnType("datetime");

            entity.HasOne(d => d.Document).WithOne(p => p.LeavesBulkUploadedDocument)
                .HasForeignKey<LeavesBulkUploadedDocument>(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeavesBulkUploadedDocuments_Documents");

            entity.HasOne(d => d.DocumentState).WithMany(p => p.LeavesBulkUploadedDocuments)
                .HasForeignKey(d => d.DocumentStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeavesBulkUploadedDocuments_DocumentStates");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.LeavesBulkUploadedDocuments)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeavesBulkUploadedDocuments_OperationLogs");

            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.LeavesBulkUploadedDocuments)
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeavesBulkUploadedDocuments_Users");
        });

        modelBuilder.Entity<LinkType>(entity =>
        {
            entity.Property(e => e.LinkTypeId).HasColumnName("LinkTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<LocalUserState>(entity =>
        {
            entity.Property(e => e.LocalUserStateId).HasColumnName("LocalUserStateID");
            entity.Property(e => e.StateName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.Property(e => e.LocationId).HasColumnName("LocationID");
            entity.Property(e => e.CountryId).HasColumnName("CountryID");
            entity.Property(e => e.LocationTypeId).HasColumnName("LocationTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.Country).WithMany(p => p.Locations)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Locations_Countries");

            entity.HasOne(d => d.LocationType).WithMany(p => p.Locations)
                .HasForeignKey(d => d.LocationTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Locations_LocationTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Locations)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_Locations_OperationLogs");
        });

        modelBuilder.Entity<LocationType>(entity =>
        {
            entity.Property(e => e.LocationTypeId).HasColumnName("LocationTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<Mimetype>(entity =>
        {
            entity.HasKey(e => e.Extension);

            entity.ToTable("MIMETypes");

            entity.Property(e => e.Extension).HasMaxLength(20);
            entity.Property(e => e.Mimetype1)
                .HasMaxLength(250)
                .HasColumnName("MIMEType");
        });

        modelBuilder.Entity<Module>(entity =>
        {
            entity.HasKey(e => e.ModuleId).HasName("PK_Module");

            entity.Property(e => e.ModuleId).HasColumnName("ModuleID");
            entity.Property(e => e.Name).HasMaxLength(250);
        });

        modelBuilder.Entity<Month>(entity =>
        {
            entity.Property(e => e.MonthId).HasColumnName("MonthID");
            entity.Property(e => e.CalendarTypeId).HasColumnName("CalendarTypeID");
            entity.Property(e => e.DhivehiName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.EnglishName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.CalendarType).WithMany(p => p.Months)
                .HasForeignKey(d => d.CalendarTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Months_CalendarVariants");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Months)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_Months_OperationLogs");
        });

        modelBuilder.Entity<NavigationLink>(entity =>
        {
            entity.Property(e => e.NavigationLinkId).HasColumnName("NavigationLinkID");
            entity.Property(e => e.ContextId).HasColumnName("ContextID");
            entity.Property(e => e.LinkName).HasMaxLength(250);
            entity.Property(e => e.LinkNameDhivehiName).HasMaxLength(250);
            entity.Property(e => e.LinkTypeId).HasColumnName("LinkTypeID");
            entity.Property(e => e.ModuleId)
                .HasDefaultValue(1)
                .HasColumnName("ModuleID");
            entity.Property(e => e.ParentNavigationLinkId).HasColumnName("ParentNavigationLinkID");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.Url)
                .HasMaxLength(250)
                .HasColumnName("URL");

            entity.HasOne(d => d.Context).WithMany(p => p.NavigationLinks)
                .HasForeignKey(d => d.ContextId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NavigationLinks_Context");

            entity.HasOne(d => d.LinkType).WithMany(p => p.NavigationLinks)
                .HasForeignKey(d => d.LinkTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NavigationLinks_LinkTypes");

            entity.HasOne(d => d.Module).WithMany(p => p.NavigationLinks)
                .HasForeignKey(d => d.ModuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NavigationLinks_Modules");

            entity.HasOne(d => d.ParentNavigationLink).WithMany(p => p.InverseParentNavigationLink)
                .HasForeignKey(d => d.ParentNavigationLinkId)
                .HasConstraintName("FK_NavigationLinks_NavigationLinks");

            entity.HasOne(d => d.Role).WithMany(p => p.NavigationLinks)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_NavigationLinks_Roles");
        });

        modelBuilder.Entity<NavigationLinkFacilityType>(entity =>
        {
            entity.Property(e => e.NavigationLinkFacilityTypeId).HasColumnName("NavigationLinkFacilityTypeID");
            entity.Property(e => e.FacilityTypeId).HasColumnName("FacilityTypeID");
            entity.Property(e => e.NavigationLinkId).HasColumnName("NavigationLinkID");

            entity.HasOne(d => d.FacilityType).WithMany(p => p.NavigationLinkFacilityTypes)
                .HasForeignKey(d => d.FacilityTypeId)
                .HasConstraintName("FK_NavigationLinkFacilityTypes_FacilityRegistrationTypes");

            entity.HasOne(d => d.NavigationLink).WithMany(p => p.NavigationLinkFacilityTypes)
                .HasForeignKey(d => d.NavigationLinkId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NavigationLinkFacilityTypes_NavigationLinks");
        });

        modelBuilder.Entity<NoPayLeaf>(entity =>
        {
            entity.HasKey(e => e.LeaveId).HasName("PK_NoPayLeaves_1");

            entity.Property(e => e.LeaveId)
                .ValueGeneratedNever()
                .HasColumnName("LeaveID");
            entity.Property(e => e.NoPayLeaveTypeId).HasColumnName("NoPayLeaveTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.Leave).WithOne(p => p.NoPayLeaf)
                .HasForeignKey<NoPayLeaf>(d => d.LeaveId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NoPayLeaves_Leaves");

            entity.HasOne(d => d.NoPayLeaveType).WithMany(p => p.NoPayLeaves)
                .HasForeignKey(d => d.NoPayLeaveTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NoPayLeaves_NoPayLeaveTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.NoPayLeaves)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NoPayLeaves_OperationLogs");
        });

        modelBuilder.Entity<NoPayLeaveType>(entity =>
        {
            entity.Property(e => e.NoPayLeaveTypeId).HasColumnName("NoPayLeaveTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.Property(e => e.NoteId).HasColumnName("NoteID");
            entity.Property(e => e.NoteReasonId).HasColumnName("NoteReasonID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.Text).HasColumnType("text");

            entity.HasOne(d => d.NoteReason).WithMany(p => p.Notes)
                .HasForeignKey(d => d.NoteReasonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notes_NoteReasons1");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Notes)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notes_OperationLogs1");
        });

        modelBuilder.Entity<NoteReason>(entity =>
        {
            entity.Property(e => e.NoteReasonId).HasColumnName("NoteReasonID");
            entity.Property(e => e.Name).HasMaxLength(250);
        });

        modelBuilder.Entity<OfficialTripDetail>(entity =>
        {
            entity.HasKey(e => e.LeaveId);

            entity.Property(e => e.LeaveId)
                .ValueGeneratedNever()
                .HasColumnName("LeaveID");
            entity.Property(e => e.FocalPointAddress).HasMaxLength(250);
            entity.Property(e => e.FocalPointContactNumber).HasMaxLength(250);
            entity.Property(e => e.FocalPointEmail).HasMaxLength(250);
            entity.Property(e => e.Purpose).HasMaxLength(250);
            entity.Property(e => e.TypeId).HasColumnName("TypeID");

            entity.HasOne(d => d.Leave).WithOne(p => p.OfficialTripDetail)
                .HasForeignKey<OfficialTripDetail>(d => d.LeaveId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OfficialTripDetails_Leaves");

            entity.HasOne(d => d.Type).WithMany(p => p.OfficialTripDetails)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OfficialTripDetails_OfficialTripTypes");
        });

        modelBuilder.Entity<OfficialTripLocation>(entity =>
        {
            entity.HasKey(e => e.OfficialTripLocationsId).HasName("PK_OfficialTripLeaveLocations");

            entity.Property(e => e.OfficialTripLocationsId).HasColumnName("OfficialTripLocationsID");
            entity.Property(e => e.ContactNumber).HasMaxLength(250);
            entity.Property(e => e.FromDate).HasColumnType("datetime");
            entity.Property(e => e.LeaveId).HasColumnName("LeaveID");
            entity.Property(e => e.LocationId).HasColumnName("LocationID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.ToDate).HasColumnType("datetime");

            entity.HasOne(d => d.Leave).WithMany(p => p.OfficialTripLocations)
                .HasForeignKey(d => d.LeaveId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OfficialTripLocations_OfficialTripDetails");

            entity.HasOne(d => d.Location).WithMany(p => p.OfficialTripLocations)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OfficialTripLocations_Locations");
        });

        modelBuilder.Entity<OfficialTripType>(entity =>
        {
            entity.Property(e => e.OfficialTripTypeId).HasColumnName("OfficialTripTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<OnlineAddress>(entity =>
        {
            entity.HasKey(e => e.OnlineLocationId);

            entity.Property(e => e.OnlineLocationId)
                .ValueGeneratedNever()
                .HasColumnName("OnlineLocationID");
            entity.Property(e => e.CountryId).HasColumnName("CountryID");
            entity.Property(e => e.IslandId).HasColumnName("IslandID");

            entity.HasOne(d => d.Country).WithMany(p => p.OnlineAddresses)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OnlineAddresses_Countries");

            entity.HasOne(d => d.Island).WithMany(p => p.OnlineAddresses)
                .HasForeignKey(d => d.IslandId)
                .HasConstraintName("FK_OnlineAddresses_Islands");

            entity.HasOne(d => d.OnlineLocation).WithOne(p => p.OnlineAddress)
                .HasForeignKey<OnlineAddress>(d => d.OnlineLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OnlineAddresses_OnlineLocations");
        });

        modelBuilder.Entity<OnlineAddressBasis>(entity =>
        {
            entity.HasKey(e => e.OnlineAddressBaseId);

            entity.Property(e => e.OnlineAddressBaseId).HasColumnName("OnlineAddressBaseID");
            entity.Property(e => e.AddressBaseTypeId).HasColumnName("AddressBaseTypeID");
            entity.Property(e => e.AddressLine1).HasMaxLength(250);
            entity.Property(e => e.AddressLine2).HasMaxLength(250);
            entity.Property(e => e.MunicipalityNumber).HasMaxLength(50);
            entity.Property(e => e.PostCode).HasMaxLength(50);
            entity.Property(e => e.StreetNameDhivehi).HasMaxLength(250);
            entity.Property(e => e.StreetNameEnglish).HasMaxLength(250);
            entity.Property(e => e.WardId).HasColumnName("WardID");

            entity.HasOne(d => d.AddressBaseType).WithMany(p => p.OnlineAddressBases)
                .HasForeignKey(d => d.AddressBaseTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OnlineAddressBases_AddressBaseTypes");

            entity.HasOne(d => d.Ward).WithMany(p => p.OnlineAddressBases)
                .HasForeignKey(d => d.WardId)
                .HasConstraintName("FK_OnlineAddressBases_Wards");
        });

        modelBuilder.Entity<OnlineAddressInstance>(entity =>
        {
            entity.HasKey(e => e.OnlineLocationId);

            entity.Property(e => e.OnlineLocationId)
                .ValueGeneratedNever()
                .HasColumnName("OnlineLocationID");
            entity.Property(e => e.AddressInstanceTypeId).HasColumnName("AddressInstanceTypeID");
            entity.Property(e => e.Floor).HasMaxLength(250);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.OnlineAddressBaseId).HasColumnName("OnlineAddressBaseID");

            entity.HasOne(d => d.AddressInstanceType).WithMany(p => p.OnlineAddressInstances)
                .HasForeignKey(d => d.AddressInstanceTypeId)
                .HasConstraintName("FK_OnlineAddressInstances_AddressInstanceTypes");

            entity.HasOne(d => d.OnlineAddressBase).WithMany(p => p.OnlineAddressInstances)
                .HasForeignKey(d => d.OnlineAddressBaseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OnlineAddressInstances_OnlineAddressBases");

            entity.HasOne(d => d.OnlineLocation).WithOne(p => p.OnlineAddressInstance)
                .HasForeignKey<OnlineAddressInstance>(d => d.OnlineLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OnlineAddressInstances_OnlineAddresses");
        });

        modelBuilder.Entity<OnlineArea>(entity =>
        {
            entity.HasKey(e => e.OnlineLocationId);

            entity.Property(e => e.OnlineLocationId)
                .ValueGeneratedNever()
                .HasColumnName("OnlineLocationID");
            entity.Property(e => e.AreaName).HasMaxLength(250);
            entity.Property(e => e.WardId).HasColumnName("WardID");

            entity.HasOne(d => d.OnlineLocation).WithOne(p => p.OnlineArea)
                .HasForeignKey<OnlineArea>(d => d.OnlineLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OnlineAreas_OnlineAddresses");

            entity.HasOne(d => d.Ward).WithMany(p => p.OnlineAreas)
                .HasForeignKey(d => d.WardId)
                .HasConstraintName("FK_OnlineAreas_Wards");
        });

        modelBuilder.Entity<OnlineBusinessEntityLocation>(entity =>
        {
            entity.Property(e => e.OnlineBusinessEntityLocationId).HasColumnName("OnlineBusinessEntityLocationID");
            entity.Property(e => e.BusinessEntityId).HasColumnName("BusinessEntityID");
            entity.Property(e => e.BusinessEntityLocationTypeId).HasColumnName("BusinessEntityLocationTypeID");
            entity.Property(e => e.OnlineLocationId).HasColumnName("OnlineLocationID");

            entity.HasOne(d => d.BusinessEntity).WithMany(p => p.OnlineBusinessEntityLocations)
                .HasForeignKey(d => d.BusinessEntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OnlineBusinessEntityLocations_BusinessEntities");

            entity.HasOne(d => d.BusinessEntityLocationType).WithMany(p => p.OnlineBusinessEntityLocations)
                .HasForeignKey(d => d.BusinessEntityLocationTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OnlineBusinessEntityLocations_BusinessEntityLocationTypes");

            entity.HasOne(d => d.OnlineLocation).WithMany(p => p.OnlineBusinessEntityLocations)
                .HasForeignKey(d => d.OnlineLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OnlineBusinessEntityLocations_OnlineLocations1");
        });

        modelBuilder.Entity<OnlineDhafthar>(entity =>
        {
            entity.HasKey(e => e.OnlineLocationId);

            entity.ToTable("OnlineDhafthar");

            entity.Property(e => e.OnlineLocationId)
                .ValueGeneratedNever()
                .HasColumnName("OnlineLocationID");
            entity.Property(e => e.DhaftharNumber).HasMaxLength(50);

            entity.HasOne(d => d.OnlineLocation).WithOne(p => p.OnlineDhafthar)
                .HasForeignKey<OnlineDhafthar>(d => d.OnlineLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OnlineDhafthar_OnlineAddresses");
        });

        modelBuilder.Entity<OnlineLocation>(entity =>
        {
            entity.Property(e => e.OnlineLocationId).HasColumnName("OnlineLocationID");
            entity.Property(e => e.LocationTypeId).HasColumnName("LocationTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.LocationType).WithMany(p => p.OnlineLocations)
                .HasForeignKey(d => d.LocationTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OnlineLocations_LocationTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.OnlineLocations)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_OnlineLocations_OperationLogs");
        });

        modelBuilder.Entity<OnlineSignInOrganisation>(entity =>
        {
            entity.HasKey(e => e.OrganisationId);

            entity.Property(e => e.OrganisationId)
                .ValueGeneratedNever()
                .HasColumnName("OrganisationID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.OnlineSignInOrganisations)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OnlineSignInOrganisations_OperationLogs");

            entity.HasOne(d => d.Organisation).WithOne(p => p.OnlineSignInOrganisation)
                .HasForeignKey<OnlineSignInOrganisation>(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OnlineSignInOrganisations_Organisations");
        });

        modelBuilder.Entity<OperationLog>(entity =>
        {
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.CreatedByContextId).HasColumnName("CreatedByContextID");
            entity.Property(e => e.CreatedByIndividualId).HasColumnName("CreatedByIndividualID");
            entity.Property(e => e.CreatedByIp)
                .HasMaxLength(50)
                .HasColumnName("CreatedByIP");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserID");
            entity.Property(e => e.CreatedByUserOrganisationId).HasColumnName("CreatedByUserOrganisationID");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.OperationLogActionId).HasColumnName("OperationLogActionID");
            entity.Property(e => e.Remarks).HasColumnType("text");
            entity.Property(e => e.UpdatedByIndividualId).HasColumnName("UpdatedByIndividualID");
            entity.Property(e => e.UpdatedByIp)
                .HasMaxLength(50)
                .HasColumnName("UpdatedByIP");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("UpdatedByUserID");
            entity.Property(e => e.UpdatedByUserOrganisationId).HasColumnName("UpdatedByUserOrganisationID");
            entity.Property(e => e.UpdatedContextId).HasColumnName("UpdatedContextID");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.CreatedByIndividual).WithMany(p => p.OperationLogs)
                .HasForeignKey(d => d.CreatedByIndividualId)
                .HasConstraintName("FK_OperationLogs_Individuals");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.OperationLogs)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_OperationLogs_Users");

            entity.HasOne(d => d.CreatedByUserOrganisation).WithMany(p => p.OperationLogs)
                .HasForeignKey(d => d.CreatedByUserOrganisationId)
                .HasConstraintName("FK_OperationLogs_UserOrganisations");

            entity.HasOne(d => d.OperationLogAction).WithMany(p => p.OperationLogs)
                .HasForeignKey(d => d.OperationLogActionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OperationLogs_OperationLogActionTypes");

            entity.HasOne(d => d.UpdatedContext).WithMany(p => p.OperationLogs)
                .HasForeignKey(d => d.UpdatedContextId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OperationLogs_Context");
        });

        modelBuilder.Entity<OperationLogActionType>(entity =>
        {
            entity.HasKey(e => e.OperationLogActionTypes);

            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Organisation>(entity =>
        {
            entity.HasKey(e => e.BusinessEntityID);

            entity.Property(e => e.BusinessEntityID)
                .ValueGeneratedNever()
                .HasColumnName("BusinessEntityID");
            entity.Property(e => e.CountryId).HasColumnName("CountryID");
            entity.Property(e => e.CscofficePimaryKey).HasColumnName("CSCOfficePimaryKey");
            entity.Property(e => e.OrganisationName).HasMaxLength(250);
            entity.Property(e => e.OrganisationNameDhivehi).HasMaxLength(250);
            entity.Property(e => e.OrganisationTypeId).HasColumnName("OrganisationTypeID");
            entity.Property(e => e.ParentOrganisationBusinessEntityId).HasColumnName("ParentOrganisationBusinessEntityID");
            entity.Property(e => e.RegistrationDate).HasColumnType("datetime");
            entity.Property(e => e.RegistrationNumber).HasMaxLength(50);


 

 

            entity.HasOne(d => d.Country).WithMany(p => p.Organisations)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Organisations_Countries");

            entity.HasOne(d => d.OrganisationType).WithMany(p => p.Organisations)
                .HasForeignKey(d => d.OrganisationTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Organisations_OrganisationTypes");

            entity.HasOne(d => d.ParentOrganisationBusinessEntity).WithMany(p => p.InverseParentOrganisationBusinessEntity)
                .HasForeignKey(d => d.ParentOrganisationBusinessEntityId)
                .HasConstraintName("FK_Organisations_Organisations");
        });

        modelBuilder.Entity<OrganisationBudget>(entity =>
        {
            entity.HasKey(e => e.OrganisationBudgetId).HasName("PK_Budgets");

            entity.Property(e => e.Amount).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.BudgetCodeId).HasColumnName("BudgetCodeID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");

            entity.HasOne(d => d.BudgetCode).WithMany(p => p.OrganisationBudgets)
                .HasForeignKey(d => d.BudgetCodeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Budgets_BudgetCodes");

            entity.HasOne(d => d.Organisation).WithMany(p => p.OrganisationBudgets)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Budgets_Organisations");
        });

        modelBuilder.Entity<OrganisationBudgetMonthlySummary>(entity =>
        {
            entity.HasIndex(e => e.BudgetItemId, "IX_OrganisationBudgetMonthlySummaries");

            entity.HasIndex(e => e.ParentBudgetItemId, "IX_OrganisationBudgetMonthlySummaries_1");

            entity.Property(e => e.DhivehiName).HasMaxLength(500);
            entity.Property(e => e.EnglishName).HasMaxLength(500);
            entity.Property(e => e.ExpenditureThisMonth).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.ExpenditureTotal).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.ParentDhivehiName).HasMaxLength(500);
            entity.Property(e => e.ParentEnglishName).HasMaxLength(500);
            entity.Property(e => e.RemainingTotal).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.TransferTotal).HasColumnType("decimal(38, 8)");
        });

        modelBuilder.Entity<OrganisationBudgetSummary>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(38, 8)")
                .HasColumnName("amount");
            entity.Property(e => e.BudgetItemId).HasColumnName("budgetItemId");
            entity.Property(e => e.BudgetItemTypeId).HasColumnName("budgetItemTypeId");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.DhivehiName)
                .HasMaxLength(500)
                .HasColumnName("dhivehiName");
            entity.Property(e => e.EnglishName)
                .HasMaxLength(500)
                .HasColumnName("englishName");
            entity.Property(e => e.OrganisationId).HasColumnName("organisationID");
            entity.Property(e => e.ParentCode).HasColumnName("parentCode");
            entity.Property(e => e.ParentDhivehiName)
                .HasMaxLength(500)
                .HasColumnName("parentDhivehiName");
            entity.Property(e => e.ParentEnglishName)
                .HasMaxLength(500)
                .HasColumnName("parentEnglishName");
            entity.Property(e => e.ParentbudgdetItemId).HasColumnName("parentbudgdetItemId");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<OrganisationBudgetSummeryAggregate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_OrganisationBudgetSummeryAggregate");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganisationId).HasColumnName("organisationID");
            entity.Property(e => e.ParentBudgetItemId).HasColumnName("parentBudgetItemID");
            entity.Property(e => e.Sum)
                .HasColumnType("decimal(18, 0)")
                .HasColumnName("sum");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<OrganisationDeductionType>(entity =>
        {
            entity.Property(e => e.OrganisationDeductionTypeId).HasColumnName("OrganisationDeductionTypeID");
            entity.Property(e => e.DeductionTypeId).HasColumnName("DeductionTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");

            entity.HasOne(d => d.DeductionType).WithMany(p => p.OrganisationDeductionTypes)
                .HasForeignKey(d => d.DeductionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationDeductionTypes_DeductionTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.OrganisationDeductionTypes)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationDeductionTypes_OperationLogs");

            entity.HasOne(d => d.Organisation).WithMany(p => p.OrganisationDeductionTypes)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationDeductionTypes_Organisations");
        });

        modelBuilder.Entity<OrganisationPayrollItemType>(entity =>
        {
            entity.HasKey(e => e.OrganisationPayrollItemTypeId).HasName("PK_OrganisationAllowanceTypes");

            entity.Property(e => e.OrganisationPayrollItemTypeId).HasColumnName("OrganisationPayrollItemTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.PayrollItemTypeId).HasColumnName("PayrollItemTypeID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.OrganisationPayrollItemTypes)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationAllowanceTypes_OperationLogs");

            entity.HasOne(d => d.Organisation).WithMany(p => p.OrganisationPayrollItemTypes)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationAllowanceTypes_Organisations");

            entity.HasOne(d => d.PayrollItemType).WithMany(p => p.OrganisationPayrollItemTypes)
                .HasForeignKey(d => d.PayrollItemTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationAllowanceTypes_Allowances");
        });

        modelBuilder.Entity<OrganisationPayrollPeriod>(entity =>
        {
            entity.Property(e => e.OrganisationPayrollPeriodId).HasColumnName("OrganisationPayrollPeriodID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.OrganisationPayrollPeriods)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_OrganisationPayrollPeriods_OperationLogs");

            entity.HasOne(d => d.Organisation).WithMany(p => p.OrganisationPayrollPeriods)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationPayrollPeriods_Organisations");
        });

        modelBuilder.Entity<OrganisationPayrollPeriodJobType>(entity =>
        {
            entity.Property(e => e.OrganisationPayrollPeriodJobTypeId).HasColumnName("OrganisationPayrollPeriodJobTypeID");
            entity.Property(e => e.JobTypeId).HasColumnName("JobTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationPayrollPeriodId).HasColumnName("OrganisationPayrollPeriodID");

            entity.HasOne(d => d.JobType).WithMany(p => p.OrganisationPayrollPeriodJobTypes)
                .HasForeignKey(d => d.JobTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationPayrollPeriodJobTypes_JobTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.OrganisationPayrollPeriodJobTypes)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_OrganisationPayrollPeriodJobTypes_OperationLogs");

            entity.HasOne(d => d.OrganisationPayrollPeriod).WithMany(p => p.OrganisationPayrollPeriodJobTypes)
                .HasForeignKey(d => d.OrganisationPayrollPeriodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationPayrollPeriodJobTypes_OrganisationPayrollPeriods");
        });

        modelBuilder.Entity<OrganisationSapexception>(entity =>
        {
            entity.HasKey(e => e.SapexceptionId);

            entity.ToTable("OrganisationSAPExceptions");

            entity.Property(e => e.SapexceptionId)
                .ValueGeneratedNever()
                .HasColumnName("SAPExceptionID");
            entity.Property(e => e.OrganisationBusinessEntityID).HasColumnName("OrganisationBusinessEntityID");

            entity.HasOne(d => d.OrganisationBusinessEntity).WithMany(p => p.OrganisationSapexceptions)
                .HasForeignKey(d => d.OrganisationBusinessEntityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationSAPExceptions_Organisations");

            entity.HasOne(d => d.Sapexception).WithOne(p => p.OrganisationSapexception)
                .HasForeignKey<OrganisationSapexception>(d => d.SapexceptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationSAPExceptions_SAPExceptions");
        });

        modelBuilder.Entity<OrganisationStructure>(entity =>
        {
            entity.ToTable("OrganisationStructure");

            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationBusinessEntityID).HasColumnName("OrganisationBusinessEntityID");
            entity.Property(e => e.OrganisationStructureStateId).HasColumnName("OrganisationStructureStateID");
            entity.Property(e => e.OrganisationStructureTypeId).HasColumnName("OrganisationStructureTypeID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.OrganisationStructures)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructure_OperationLogs");

            entity.HasOne(d => d.OrganisationBusinessEntity).WithMany(p => p.OrganisationStructures)
                .HasForeignKey(d => d.OrganisationBusinessEntityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructure_Organisations");

            entity.HasOne(d => d.OrganisationStructureState).WithMany(p => p.OrganisationStructures)
                .HasForeignKey(d => d.OrganisationStructureStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructure_OrganisationStructureStates");

            entity.HasOne(d => d.OrganisationStructureType).WithMany(p => p.OrganisationStructures)
                .HasForeignKey(d => d.OrganisationStructureTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructure_OrganisationStructureTypes");
        });

        modelBuilder.Entity<OrganisationStructureCalendar>(entity =>
        {
            entity.Property(e => e.OrganisationStructureCalendarId).HasColumnName("OrganisationStructureCalendarID");
            entity.Property(e => e.CalenderId).HasColumnName("CalenderID");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.Calender).WithMany(p => p.OrganisationStructureCalendars)
                .HasForeignKey(d => d.CalenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureCalenders_Calenders");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.OrganisationStructureCalendars)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureCalendars_OperationLogs");

            entity.HasOne(d => d.OrganisationStructure).WithMany(p => p.OrganisationStructureCalendars)
                .HasForeignKey(d => d.OrganisationStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureCalenders_OrganisationStructure");
        });

        modelBuilder.Entity<OrganisationStructureDraft>(entity =>
        {
            entity.HasKey(e => e.OrganisationStructureDraftId).HasName("PK_OrganisationStructureChangeRequests");

            entity.Property(e => e.OrganisationStructureDraftId).HasColumnName("OrganisationStructureDraftID");
            entity.Property(e => e.HeadInChargeIndividualId).HasColumnName("HeadInChargeIndividualID");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.OrganisationChartRequestId).HasColumnName("OrganisationChartRequestID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.OrganisationStructureRequestTypeId).HasColumnName("OrganisationStructureRequestTypeID");
            entity.Property(e => e.OrganisationStructureStateId).HasColumnName("OrganisationStructureStateID");
            entity.Property(e => e.OrganisationStructureTypeId).HasColumnName("OrganisationStructureTypeID");
            entity.Property(e => e.ParentOrganisationStructureId).HasColumnName("ParentOrganisationStructureID");

            entity.HasOne(d => d.OrganisationChartRequest).WithMany(p => p.OrganisationStructureDrafts)
                .HasForeignKey(d => d.OrganisationChartRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureRequests_OrganisationChartRequests1");

            entity.HasOne(d => d.OrganisationStructure).WithMany(p => p.OrganisationStructureDrafts)
                .HasForeignKey(d => d.OrganisationStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureRequests_OrganisationStructure");

            entity.HasOne(d => d.OrganisationStructureRequestType).WithMany(p => p.OrganisationStructureDrafts)
                .HasForeignKey(d => d.OrganisationStructureRequestTypeId)
                .HasConstraintName("FK_OrganisationStructureRequests_OrganisationStructureRequestTypes");

            entity.HasOne(d => d.OrganisationStructureState).WithMany(p => p.OrganisationStructureDrafts)
                .HasForeignKey(d => d.OrganisationStructureStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureRequests_OrganisationStructureStates");

            entity.HasOne(d => d.OrganisationStructureType).WithMany(p => p.OrganisationStructureDrafts)
                .HasForeignKey(d => d.OrganisationStructureTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureDrafts_OrganisationStructureTypes");
        });

        modelBuilder.Entity<OrganisationStructureHeadIncharge>(entity =>
        {
            entity.Property(e => e.OrganisationStructureHeadInchargeId).HasColumnName("OrganisationStructureHeadInchargeID");
            entity.Property(e => e.IndividualId).HasColumnName("IndividualID");
            entity.Property(e => e.IsValid).HasDefaultValue(true);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");

            entity.HasOne(d => d.Individual).WithMany(p => p.OrganisationStructureHeadIncharges)
                .HasForeignKey(d => d.IndividualId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureHeadIncharges_Staffs");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.OrganisationStructureHeadIncharges)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_OrganisationStructureHeadIncharges_OperationLogs");

            entity.HasOne(d => d.OrganisationStructure).WithMany(p => p.OrganisationStructureHeadIncharges)
                .HasForeignKey(d => d.OrganisationStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureHeadIncharges_OrganisationStructure");
        });

        modelBuilder.Entity<OrganisationStructureHistory>(entity =>
        {
            entity.HasKey(e => e.OrganisationStructureHistoryId).HasName("PK_OrganisationCharts");

            entity.ToTable("OrganisationStructureHistory");

            entity.Property(e => e.OrganisationStructureHistoryId).HasColumnName("OrganisationStructureHistoryID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.OrganisationStructureRelationId).HasColumnName("OrganisationStructureRelationID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");

            entity.HasOne(d => d.Organisation).WithMany(p => p.OrganisationStructureHistories)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureHistory_Organisations");

            entity.HasOne(d => d.OrganisationStructureRelation).WithMany(p => p.OrganisationStructureHistories)
                .HasForeignKey(d => d.OrganisationStructureRelationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureHistory_OrganisationStructureRelations");

            entity.HasOne(d => d.Request).WithMany(p => p.OrganisationStructureHistories)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureHistory_Requests");
        });

        modelBuilder.Entity<OrganisationStructureLocation>(entity =>
        {
            entity.Property(e => e.OrganisationStructureLocationId).HasColumnName("OrganisationStructureLocationID");
            entity.Property(e => e.LocationId).HasColumnName("LocationID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.OrganisationStructureLocationTypeId).HasColumnName("OrganisationStructureLocationTypeID");

            entity.HasOne(d => d.Location).WithMany(p => p.OrganisationStructureLocations)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureLocations_Locations");

            entity.HasOne(d => d.OrganisationStructure).WithMany(p => p.OrganisationStructureLocations)
                .HasForeignKey(d => d.OrganisationStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureLocations_OrganisationStructure");

            entity.HasOne(d => d.OrganisationStructureLocationType).WithMany(p => p.OrganisationStructureLocations)
                .HasForeignKey(d => d.OrganisationStructureLocationTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureLocations_OrganisationStructureLocationTypes");
        });

        modelBuilder.Entity<OrganisationStructureLocationType>(entity =>
        {
            entity.Property(e => e.OrganisationStructureLocationTypeId).HasColumnName("OrganisationStructureLocationTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<OrganisationStructureRelation>(entity =>
        {
            entity.Property(e => e.OrganisationStructureRelationId).HasColumnName("OrganisationStructureRelationID");
            entity.Property(e => e.ActiveFromDate).HasColumnType("datetime");
            entity.Property(e => e.ActiveToDate).HasColumnType("datetime");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.ParentOrganisationStructureId).HasColumnName("ParentOrganisationStructureID");

            entity.HasOne(d => d.OrganisationStructure).WithMany(p => p.OrganisationStructureRelationOrganisationStructures)
                .HasForeignKey(d => d.OrganisationStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureRelations_OrganisationStructure");

            entity.HasOne(d => d.ParentOrganisationStructure).WithMany(p => p.OrganisationStructureRelationParentOrganisationStructures)
                .HasForeignKey(d => d.ParentOrganisationStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureRelations_OrganisationStructure1");
        });

        modelBuilder.Entity<OrganisationStructureRequest>(entity =>
        {
            entity.HasKey(e => e.OrganisationStructureRequestId).HasName("PK_OrganisationChartRequests");

            entity.Property(e => e.OrganisationStructureRequestId).HasColumnName("OrganisationStructureRequestID");
            entity.Property(e => e.OrganisationBusinessEntityID).HasColumnName("OrganisationBusinessEntityID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");

            entity.HasOne(d => d.OrganisationBusinessEntity).WithMany(p => p.OrganisationStructureRequests)
                .HasForeignKey(d => d.OrganisationBusinessEntityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationChartRequests_Organisations");

            entity.HasOne(d => d.Request).WithMany(p => p.OrganisationStructureRequests)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureRequests_Requests");
        });

        modelBuilder.Entity<OrganisationStructureRequestType>(entity =>
        {
            entity.Property(e => e.OrganisationStructureRequestTypeId).HasColumnName("OrganisationStructureRequestTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<OrganisationStructureSchedule>(entity =>
        {
            entity.Property(e => e.OrganisationStructureScheduleId).HasColumnName("OrganisationStructureScheduleID");
            entity.Property(e => e.IsLinked).HasColumnName("isLinked");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.OrganisationStructureSchedules)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureSchedules_OperationLogs");

            entity.HasOne(d => d.OrganisationStructure).WithMany(p => p.OrganisationStructureSchedules)
                .HasForeignKey(d => d.OrganisationStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureSchedules_OrganisationStructure");

            entity.HasOne(d => d.Schedule).WithMany(p => p.OrganisationStructureSchedules)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureSchedules_Schedules");
        });

        modelBuilder.Entity<OrganisationStructureState>(entity =>
        {
            entity.Property(e => e.OrganisationStructureStateId).HasColumnName("OrganisationStructureStateID");
            entity.Property(e => e.StateName).HasMaxLength(250);
        });

        modelBuilder.Entity<OrganisationStructureSupervisor>(entity =>
        {
            entity.Property(e => e.OrganisationStructureSupervisorId).HasColumnName("OrganisationStructureSupervisorID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.SupervisorStaffId).HasColumnName("SupervisorStaffID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.OrganisationStructureSupervisors)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureSupervisors_OperationLogs");

            entity.HasOne(d => d.OrganisationStructure).WithMany(p => p.OrganisationStructureSupervisors)
                .HasForeignKey(d => d.OrganisationStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureSupervisors_OrganisationStructure");

            entity.HasOne(d => d.SupervisorStaff).WithMany(p => p.OrganisationStructureSupervisors)
                .HasForeignKey(d => d.SupervisorStaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationStructureSupervisors_Staffs");
        });

        modelBuilder.Entity<OrganisationStructureType>(entity =>
        {
            entity.Property(e => e.OrganisationStructureTypeId).HasColumnName("OrganisationStructureTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<OrganisationType>(entity =>
        {
            entity.Property(e => e.OrganisationTypeId).HasColumnName("OrganisationTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<OrganisationalStructureSapexception>(entity =>
        {
            entity.HasKey(e => e.SapexceptionId);

            entity.ToTable("OrganisationalStructureSAPExceptions");

            entity.Property(e => e.SapexceptionId)
                .ValueGeneratedNever()
                .HasColumnName("SAPExceptionID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");

            entity.HasOne(d => d.OrganisationStructure).WithMany(p => p.OrganisationalStructureSapexceptions)
                .HasForeignKey(d => d.OrganisationStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationalStructureSAPExceptions_OrganisationStructure");

            entity.HasOne(d => d.Sapexception).WithOne(p => p.OrganisationalStructureSapexception)
                .HasForeignKey<OrganisationalStructureSapexception>(d => d.SapexceptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrganisationalStructureSAPExceptions_SAPExceptions");
        });

        modelBuilder.Entity<Ot>(entity =>
        {
            entity.ToTable("OT");

            entity.Property(e => e.Otid).HasColumnName("OTID");
            entity.Property(e => e.EndAttendanceLogId).HasColumnName("EndAttendanceLogID");
            entity.Property(e => e.IsValid).HasDefaultValue(true);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OtrequestId).HasColumnName("OTRequestID");
            entity.Property(e => e.StartAttendanceLogId).HasColumnName("StartAttendanceLogID");
            entity.Property(e => e.WorkDone).HasMaxLength(250);

            entity.HasOne(d => d.EndAttendanceLog).WithMany(p => p.OtEndAttendanceLogs)
                .HasForeignKey(d => d.EndAttendanceLogId)
                .HasConstraintName("FK_OT_AttendanceLogs1");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Ots)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OT_OperationLogs");

            entity.HasOne(d => d.Otrequest).WithMany(p => p.Ots)
                .HasForeignKey(d => d.OtrequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OT_OTRequests");

            entity.HasOne(d => d.StartAttendanceLog).WithMany(p => p.OtStartAttendanceLogs)
                .HasForeignKey(d => d.StartAttendanceLogId)
                .HasConstraintName("FK_OT_AttendanceLogs");
        });

        modelBuilder.Entity<OtherSalaryRateDefinition>(entity =>
        {
            entity.Property(e => e.OtherSalaryRateDefinitionId).HasColumnName("OtherSalaryRateDefinitionID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.SalaryRateDefinitionForTypeId).HasColumnName("SalaryRateDefinitionForTypeID");
            entity.Property(e => e.SalaryRateDefinitionId).HasColumnName("SalaryRateDefinitionID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.OtherSalaryRateDefinitions)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OtherSalaryRateDefinitions_OperationLogs");

            entity.HasOne(d => d.SalaryRateDefinitionForType).WithMany(p => p.OtherSalaryRateDefinitions)
                .HasForeignKey(d => d.SalaryRateDefinitionForTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OtherSalaryRateDefinitions_SalaryRateDefinitionForTypes");

            entity.HasOne(d => d.SalaryRateDefinition).WithMany(p => p.OtherSalaryRateDefinitions)
                .HasForeignKey(d => d.SalaryRateDefinitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OtherSalaryRateDefinitions_SalaryRateDefinitions");
        });

        modelBuilder.Entity<OtpreApproval>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK_OTPreApprovals_1");

            entity.ToTable("OTPreApprovals");

            entity.Property(e => e.RequestId)
                .ValueGeneratedNever()
                .HasColumnName("RequestID");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.OttypeId).HasColumnName("OTTypeID");
            entity.Property(e => e.StaffJobId).HasColumnName("StaffJobID");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.SupervisorId).HasColumnName("SupervisorID");
            entity.Property(e => e.Works).HasMaxLength(250);

            entity.HasOne(d => d.Ottype).WithMany(p => p.OtpreApprovals)
                .HasForeignKey(d => d.OttypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OTPreApprovals_OTTypes");

            entity.HasOne(d => d.Request).WithOne(p => p.OtpreApproval)
                .HasForeignKey<OtpreApproval>(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OTPreApprovals_Requests");

            entity.HasOne(d => d.StaffJob).WithMany(p => p.OtpreApprovals)
                .HasForeignKey(d => d.StaffJobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OTPreApprovals_Jobs");

            entity.HasOne(d => d.Supervisor).WithMany(p => p.OtpreApprovals)
                .HasForeignKey(d => d.SupervisorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OTPreApprovals_Staffs2");
        });

        modelBuilder.Entity<OtpreApprovalTime>(entity =>
        {
            entity.ToTable("OTPreApprovalTimes");

            entity.Property(e => e.OtpreApprovalTimeId).HasColumnName("OTPreApprovalTimeID");
            entity.Property(e => e.DayTypeId).HasColumnName("DayTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");

            entity.HasOne(d => d.DayType).WithMany(p => p.OtpreApprovalTimes)
                .HasForeignKey(d => d.DayTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OTPreApprovalTimes_DayTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.OtpreApprovalTimes)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OTPreApprovalTimes_OperationLogs");

            entity.HasOne(d => d.Request).WithMany(p => p.OtpreApprovalTimes)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OTPreApprovalTimes_OTPreApprovals");
        });

        modelBuilder.Entity<Otrequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.ToTable("OTRequests");

            entity.HasIndex(e => e.StaffJobId, "IDX_OTRequests_JOBID");

            entity.HasIndex(e => e.Date, "NonClusteredIndex-20160804-141927").IsDescending();

            entity.Property(e => e.RequestId)
                .ValueGeneratedNever()
                .HasColumnName("RequestID");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.OtpreApprovalRequestId).HasColumnName("OTPreApprovalRequestID");
            entity.Property(e => e.OttypeId).HasColumnName("OTTypeID");
            entity.Property(e => e.StaffJobId).HasColumnName("StaffJobID");

            entity.HasOne(d => d.OtpreApprovalRequest).WithMany(p => p.Otrequests)
                .HasForeignKey(d => d.OtpreApprovalRequestId)
                .HasConstraintName("FK_OTRequests_OTPreApprovals");

            entity.HasOne(d => d.Ottype).WithMany(p => p.Otrequests)
                .HasForeignKey(d => d.OttypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OTRequests_OTTypes");

            entity.HasOne(d => d.Request).WithOne(p => p.Otrequest)
                .HasForeignKey<Otrequest>(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OTRequests_Requests");

            entity.HasOne(d => d.StaffJob).WithMany(p => p.Otrequests)
                .HasForeignKey(d => d.StaffJobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OTRequests_Jobs");
        });

        modelBuilder.Entity<Ottype>(entity =>
        {
            entity.ToTable("OTTypes");

            entity.Property(e => e.OttypeId).HasColumnName("OTTypeID");
            entity.Property(e => e.Name).HasMaxLength(250);
        });

        modelBuilder.Entity<OutOfOfficeRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.HasIndex(e => e.StaffId, "missing_index_16_15_OutOfOfficeRequests").HasFillFactor(75);

            entity.Property(e => e.RequestId)
                .ValueGeneratedNever()
                .HasColumnName("RequestID");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.OrganisationBusinessEntityID).HasColumnName("OrganisationBusinessEntityID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.OutOfOfficeRequestTypeId).HasColumnName("OutOfOfficeRequestTypeID");
            entity.Property(e => e.StaffId).HasColumnName("StaffID");

            entity.HasOne(d => d.OrganisationBusinessEntity).WithMany(p => p.OutOfOfficeRequests)
                .HasForeignKey(d => d.OrganisationBusinessEntityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OutOfOfficeRequests_Organisations");

            entity.HasOne(d => d.OrganisationStructure).WithMany(p => p.OutOfOfficeRequests)
                .HasForeignKey(d => d.OrganisationStructureId)
                .HasConstraintName("FK_OutOfOfficeRequests_OrganisationStructure");

            entity.HasOne(d => d.OutOfOfficeRequestType).WithMany(p => p.OutOfOfficeRequests)
                .HasForeignKey(d => d.OutOfOfficeRequestTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OutOfOfficeRequests_OutOfficeRequestTypes");

            entity.HasOne(d => d.Request).WithOne(p => p.OutOfOfficeRequest)
                .HasForeignKey<OutOfOfficeRequest>(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OutOfOfficeRequests_Requests");

            entity.HasOne(d => d.Staff).WithMany(p => p.OutOfOfficeRequestStaffs)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OutOfOfficeRequests_Staffs");

            entity.HasOne(d => d.SupervisorStaff).WithMany(p => p.OutOfOfficeRequestSupervisorStaffs)
                .HasForeignKey(d => d.SupervisorStaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OutOfOfficeRequests_Staffs1");
        });

        modelBuilder.Entity<OutOfficeRequestType>(entity =>
        {
            entity.HasKey(e => e.OutOfOfficeRequestTypeId);

            entity.Property(e => e.OutOfOfficeRequestTypeId).HasColumnName("OutOfOfficeRequestTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(150);
        });

        modelBuilder.Entity<OwnerType>(entity =>
        {
            entity.Property(e => e.OwnerTypeId).HasColumnName("OwnerTypeID");
            entity.Property(e => e.Name).HasMaxLength(250);
        });

        modelBuilder.Entity<Passport>(entity =>
        {
            entity.Property(e => e.PassportId).HasColumnName("PassportID");
            entity.Property(e => e.BusinessEntityId).HasColumnName("BusinessEntityID");
            entity.Property(e => e.CountryId).HasColumnName("CountryID");
            entity.Property(e => e.ExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.IssuedDate).HasColumnType("datetime");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.PassportNumber).HasMaxLength(50);
            entity.Property(e => e.PassportStateId).HasColumnName("PassportStateID");

            entity.HasOne(d => d.BusinessEntity).WithMany(p => p.Passports)
                .HasForeignKey(d => d.BusinessEntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Passports_Individuals");

            entity.HasOne(d => d.Country).WithMany(p => p.Passports)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Passports_Countries");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Passports)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_Passports_OperationLogs");

            entity.HasOne(d => d.PassportState).WithMany(p => p.Passports)
                .HasForeignKey(d => d.PassportStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Passports_PassportStates");
        });

        modelBuilder.Entity<PassportState>(entity =>
        {
            entity.Property(e => e.PassportStateId).HasColumnName("PassportStateID");
            entity.Property(e => e.StateName).HasMaxLength(250);
        });

        modelBuilder.Entity<PayrollConfigurableValue>(entity =>
        {
            entity.Property(e => e.PayrollConfigurableValueId).HasColumnName("PayrollConfigurableValueID");
            entity.Property(e => e.JobTypeId).HasColumnName("JobTypeID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.PayrollConfigurableValueTypeId).HasColumnName("PayrollConfigurableValueTypeID");
            entity.Property(e => e.Value).HasColumnType("text");

            entity.HasOne(d => d.JobType).WithMany(p => p.PayrollConfigurableValues)
                .HasForeignKey(d => d.JobTypeId)
                .HasConstraintName("FK_PayrollConfigurableValues_JobTypes");

            entity.HasOne(d => d.Organisation).WithMany(p => p.PayrollConfigurableValues)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollConfigurableValues_Organisations");

            entity.HasOne(d => d.PayrollConfigurableValueType).WithMany(p => p.PayrollConfigurableValues)
                .HasForeignKey(d => d.PayrollConfigurableValueTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollConfigurableValues_PayrollConfigurableValueTypes");
        });

        modelBuilder.Entity<PayrollConfigurableValueType>(entity =>
        {
            entity.Property(e => e.PayrollConfigurableValueTypeId).HasColumnName("PayrollConfigurableValueTypeID");
            entity.Property(e => e.Comment).HasColumnType("text");
            entity.Property(e => e.IsJobTypeDependant).HasColumnName("isJobTypeDependant");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.Type).HasMaxLength(50);
        });

        modelBuilder.Entity<PayrollCycle>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.Property(e => e.RequestId)
                .ValueGeneratedNever()
                .HasColumnName("RequestID");
            entity.Property(e => e.OrganisationPayrollPeriodId).HasColumnName("OrganisationPayrollPeriodID");

            entity.HasOne(d => d.OrganisationPayrollPeriod).WithMany(p => p.PayrollCycles)
                .HasForeignKey(d => d.OrganisationPayrollPeriodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollCycles_OrganisationPayrollPeriods");

            entity.HasOne(d => d.Request).WithOne(p => p.PayrollCycle)
                .HasForeignKey<PayrollCycle>(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollCycles_Requests");
        });

        modelBuilder.Entity<PayrollCycleLinkedRequest>(entity =>
        {
            entity.HasIndex(e => new { e.PayrollCycleRequestId, e.RequestId, e.StaffSalaryId }, "IX_PayrollCycleLinkedRequests");

            entity.Property(e => e.PayrollCycleLinkedRequestId).HasColumnName("PayrollCycleLinkedRequestID");
            entity.Property(e => e.LastUpdatedDate).HasColumnType("datetime");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.PayrollCycleRequestId).HasColumnName("PayrollCycleRequestID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.StaffSalaryId).HasColumnName("StaffSalaryID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.PayrollCycleLinkedRequests)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollCycleLinkedRequests_OperationLogs");

            entity.HasOne(d => d.PayrollCycleRequest).WithMany(p => p.PayrollCycleLinkedRequests)
                .HasForeignKey(d => d.PayrollCycleRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollCycleLinkedRequests_PayrollCycles");

            entity.HasOne(d => d.Request).WithMany(p => p.PayrollCycleLinkedRequests)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollCycleLinkedRequests_Requests");

            entity.HasOne(d => d.StaffSalary).WithMany(p => p.PayrollCycleLinkedRequests)
                .HasForeignKey(d => d.StaffSalaryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollCycleLinkedRequests_StaffSalaries1");
        });

        modelBuilder.Entity<PayrollCycleProcessingState>(entity =>
        {
            entity.Property(e => e.PayrollCycleProcessingStateId).HasColumnName("PayrollCycleProcessingStateID");
            entity.Property(e => e.Name).HasMaxLength(500);
        });

        modelBuilder.Entity<PayrollItemDailySapsheetDetail>(entity =>
        {
            entity.ToTable("PayrollItemDailySAPSheetDetails");

            entity.Property(e => e.PayrollItemDailySapsheetDetailId).HasColumnName("PayrollItemDailySAPSheetDetailID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.PayrollItemTypeId).HasColumnName("PayrollItemTypeID");
            entity.Property(e => e.StaffSalaryId).HasColumnName("StaffSalaryID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.PayrollItemDailySapsheetDetails)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollItemDailySAPSheetDetails_OperationLogs");

            entity.HasOne(d => d.PayrollItemType).WithMany(p => p.PayrollItemDailySapsheetDetails)
                .HasForeignKey(d => d.PayrollItemTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollItemDailySAPSheetDetails_PayrollItemTypes");

            entity.HasOne(d => d.StaffSalary).WithMany(p => p.PayrollItemDailySapsheetDetails)
                .HasForeignKey(d => d.StaffSalaryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollItemDailySAPSheetDetails_StaffSalaries");
        });

        modelBuilder.Entity<PayrollItemProcessingFlow>(entity =>
        {
            entity.Property(e => e.PayrollItemProcessingFlowId).HasColumnName("PayrollItemProcessingFlowID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.ParentPayrollItemTypeProcessingCodeId).HasColumnName("ParentPayrollItemTypeProcessingCodeID");
            entity.Property(e => e.PayrollItemTypeProcessingCodeId).HasColumnName("PayrollItemTypeProcessingCodeID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.PayrollItemProcessingFlows)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_PayrollItemProcessingFlows_OperationLogs");

            entity.HasOne(d => d.ParentPayrollItemTypeProcessingCode).WithMany(p => p.PayrollItemProcessingFlowParentPayrollItemTypeProcessingCodes)
                .HasForeignKey(d => d.ParentPayrollItemTypeProcessingCodeId)
                .HasConstraintName("FK_PayrollItemProcessingFlows_PayrollItemTypeProcessingCodes1");

            entity.HasOne(d => d.PayrollItemTypeProcessingCode).WithMany(p => p.PayrollItemProcessingFlowPayrollItemTypeProcessingCodes)
                .HasForeignKey(d => d.PayrollItemTypeProcessingCodeId)
                .HasConstraintName("FK_PayrollItemProcessingFlows_PayrollItemTypeProcessingCodes");
        });

        modelBuilder.Entity<PayrollItemType>(entity =>
        {
            entity.HasKey(e => e.PayrollItemTypeId).HasName("PK_Allowances");

            entity.Property(e => e.PayrollItemTypeId).HasColumnName("PayrollItemTypeID");
            entity.Property(e => e.AdditionOrDeductionTypeId).HasColumnName("AdditionOrDeductionTypeID");
            entity.Property(e => e.AmountTypeId).HasColumnName("AmountTypeID");
            entity.Property(e => e.AttendanceDependentTypeId).HasColumnName("AttendanceDependentTypeID");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CurrencyTypeId).HasColumnName("CurrencyTypeID");
            entity.Property(e => e.DependencyTypeId).HasColumnName("DependencyTypeID");
            entity.Property(e => e.DerivedOnTypeId).HasColumnName("DerivedOnTypeID");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.EffectiveDate).HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.NameDhivehi).HasMaxLength(50);
            entity.Property(e => e.NameEnglish).HasMaxLength(50);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OwnerOrganisationId).HasColumnName("ownerOrganisationID");
            entity.Property(e => e.ReccurencePattern)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RecurrenceTypeId).HasColumnName("RecurrenceTypeID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.Sapcode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SAPCode");
            entity.Property(e => e.SapsheetId).HasColumnName("SAPSheetID");

            entity.HasOne(d => d.AdditionOrDeductionType).WithMany(p => p.PayrollItemTypes)
                .HasForeignKey(d => d.AdditionOrDeductionTypeId)
                .HasConstraintName("FK_PayrollItemTypes_AdditionOrDeductionTypes");

            entity.HasOne(d => d.AmountType).WithMany(p => p.PayrollItemTypes)
                .HasForeignKey(d => d.AmountTypeId)
                .HasConstraintName("FK_Allowances_AmountTypes");

            entity.HasOne(d => d.AttendanceDependentType).WithMany(p => p.PayrollItemTypes)
                .HasForeignKey(d => d.AttendanceDependentTypeId)
                .HasConstraintName("FK_PayrollItemTypes_AttendanceDependentTypes");

            entity.HasOne(d => d.CurrencyType).WithMany(p => p.PayrollItemTypes)
                .HasForeignKey(d => d.CurrencyTypeId)
                .HasConstraintName("FK_Allowances_CurrencyTypes");

            entity.HasOne(d => d.DependencyType).WithMany(p => p.PayrollItemTypes)
                .HasForeignKey(d => d.DependencyTypeId)
                .HasConstraintName("FK_Allowances_DependencyTypes");

            entity.HasOne(d => d.DerivedOnType).WithMany(p => p.PayrollItemTypes)
                .HasForeignKey(d => d.DerivedOnTypeId)
                .HasConstraintName("FK_Allowances_DerivedOnTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.PayrollItemTypes)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_Allowances_OperationLogs");

            entity.HasOne(d => d.OwnerOrganisation).WithMany(p => p.PayrollItemTypes)
                .HasForeignKey(d => d.OwnerOrganisationId)
                .HasConstraintName("FK_Allowances_Organisations");

            entity.HasOne(d => d.RecurrenceType).WithMany(p => p.PayrollItemTypes)
                .HasForeignKey(d => d.RecurrenceTypeId)
                .HasConstraintName("FK_Allowances_RecurrenceTypes");

            entity.HasOne(d => d.Request).WithMany(p => p.PayrollItemTypes)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("FK_Allowances_Requests");

            entity.HasOne(d => d.Sapsheet).WithMany(p => p.PayrollItemTypes)
                .HasForeignKey(d => d.SapsheetId)
                .HasConstraintName("FK_PayrollItemTypes_SAPSheets");
        });

        modelBuilder.Entity<PayrollItemTypeAmount>(entity =>
        {
            entity.HasKey(e => e.PayrollItemTypeAmountId).HasName("PK_AmountType");

            entity.Property(e => e.PayrollItemTypeAmountId).HasColumnName("PayrollItemTypeAmountID");
            entity.Property(e => e.Amount).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.PayrollItemTypeId).HasColumnName("PayrollItemTypeID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.PayrollItemTypeAmounts)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AllowanceAmounts_OperationLogs");

            entity.HasOne(d => d.PayrollItemType).WithMany(p => p.PayrollItemTypeAmounts)
                .HasForeignKey(d => d.PayrollItemTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AllowanceAmounts_Allowances");
        });

        modelBuilder.Entity<PayrollItemTypeDependencyAmount>(entity =>
        {
            entity.HasKey(e => e.PayrollItemTypeDependencyAmountId).HasName("PK_AllowanceDependencyAmount");

            entity.Property(e => e.PayrollItemTypeDependencyAmountId).HasColumnName("PayrollItemTypeDependencyAmountID");
            entity.Property(e => e.Amount).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.DependencyId).HasColumnName("DependencyID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.PayrollItemTypeId).HasColumnName("PayrollItemTypeID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.PayrollItemTypeDependencyAmounts)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AllowanceDependencyAmounts_OperationLogs");

            entity.HasOne(d => d.PayrollItemType).WithMany(p => p.PayrollItemTypeDependencyAmounts)
                .HasForeignKey(d => d.PayrollItemTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AllowanceDependencyAmounts_Allowances");
        });

        modelBuilder.Entity<PayrollItemTypeDependencyTimePeriodAmount>(entity =>
        {
            entity.HasKey(e => e.PayrollItemTypeDependencyTimePeriodAmounts).HasName("PK_AllowanceDependencyTimePeriodAmounts_1");

            entity.Property(e => e.Amount).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.PayrollItemTypeId).HasColumnName("PayrollItemTypeID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.PayrollItemTypeDependencyTimePeriodAmounts)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AllowanceDependencyTimePeriodAmounts_OperationLogs");

            entity.HasOne(d => d.PayrollItemType).WithMany(p => p.PayrollItemTypeDependencyTimePeriodAmounts)
                .HasForeignKey(d => d.PayrollItemTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollItemTypeDependencyTimePeriodAmounts_PayrollItemTypes");
        });

        modelBuilder.Entity<PayrollItemTypeProcessingCode>(entity =>
        {
            entity.Property(e => e.PayrollItemTypeProcessingCodeId).HasColumnName("PayrollItemTypeProcessingCodeID");
            entity.Property(e => e.AdditionOrDeductionTypeId).HasColumnName("AdditionOrDeductionTypeID");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OwnerOrganisationBusinessEntityId).HasColumnName("OwnerOrganisationBusinessEntityID");
            entity.Property(e => e.PayrollItemTypeId).HasColumnName("PayrollItemTypeID");
            entity.Property(e => e.Sapcode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SAPCode");

            entity.HasOne(d => d.AdditionOrDeductionType).WithMany(p => p.PayrollItemTypeProcessingCodes)
                .HasForeignKey(d => d.AdditionOrDeductionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollItemTypeProcessingCodes_AdditionOrDeductionTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.PayrollItemTypeProcessingCodes)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_PayrollItemTypeProcessingCodes_OperationLogs");

            entity.HasOne(d => d.OwnerOrganisationBusinessEntity).WithMany(p => p.PayrollItemTypeProcessingCodes)
                .HasForeignKey(d => d.OwnerOrganisationBusinessEntityId)
                .HasConstraintName("FK_PayrollItemTypeProcessingCodes_Organisations");

            entity.HasOne(d => d.PayrollItemType).WithMany(p => p.PayrollItemTypeProcessingCodes)
                .HasForeignKey(d => d.PayrollItemTypeId)
                .HasConstraintName("FK_PayrollItemTypeProcessingCodes_PayrollItemTypes");
        });

        modelBuilder.Entity<PayrollItemsProcessingWhiteList>(entity =>
        {
            entity.Property(e => e.PayrollItemsProcessingWhiteListId).HasColumnName("PayrollItemsProcessingWhiteListID");
            entity.Property(e => e.JobTypeId).HasColumnName("JobTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OwnerOrganisationId).HasColumnName("OwnerOrganisationID");
            entity.Property(e => e.PayrollItemTypeId).HasColumnName("PayrollItemTypeID");

            entity.HasOne(d => d.JobType).WithMany(p => p.PayrollItemsProcessingWhiteLists)
                .HasForeignKey(d => d.JobTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollItemsProcessingWhiteLists_JobTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.PayrollItemsProcessingWhiteLists)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_PayrollItemsProcessingWhiteLists_OperationLogs");

            entity.HasOne(d => d.OwnerOrganisation).WithMany(p => p.PayrollItemsProcessingWhiteLists)
                .HasForeignKey(d => d.OwnerOrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollItemsProcessingWhiteLists_Organisations");

            entity.HasOne(d => d.PayrollItemType).WithMany(p => p.PayrollItemsProcessingWhiteLists)
                .HasForeignKey(d => d.PayrollItemTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollItemsProcessingWhiteLists_PayrollItemTypes");
        });

        modelBuilder.Entity<PercentageVariableDeductionAmount>(entity =>
        {
            entity.Property(e => e.PercentageVariableDeductionAmountId).HasColumnName("PercentageVariableDeductionAmountID");
            entity.Property(e => e.DeductionTypeId).HasColumnName("DeductionTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.Percentage).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.DeductionType).WithMany(p => p.PercentageVariableDeductionAmounts)
                .HasForeignKey(d => d.DeductionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PercentageVariableDeductionAmounts_DeductionTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.PercentageVariableDeductionAmounts)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PercentageVariableDeductionAmounts_OperationLogs");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.Property(e => e.PositionId).HasColumnName("PositionID");
            entity.Property(e => e.CscdesignationPrimaryKey).HasColumnName("CSCDesignationPrimaryKey");
            entity.Property(e => e.Descriptions).HasColumnType("text");
            entity.Property(e => e.DhivehiName).HasMaxLength(250);
            entity.Property(e => e.IsActive).HasDefaultValue(false);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.PositionClassificationId).HasColumnName("PositionClassificationID");
            entity.Property(e => e.PositionRankId).HasColumnName("PositionRankID");
            entity.Property(e => e.PositionTypeId).HasColumnName("PositionTypeID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Positions)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_Positions_OperationLogs");

            entity.HasOne(d => d.Organisation).WithMany(p => p.Positions)
                .HasForeignKey(d => d.OrganisationId)
                .HasConstraintName("FK_Positions_Organisations");

            entity.HasOne(d => d.PositionClassification).WithMany(p => p.Positions)
                .HasForeignKey(d => d.PositionClassificationId)
                .HasConstraintName("FK_Positions_PositionClassifications");

            entity.HasOne(d => d.PositionRank).WithMany(p => p.Positions)
                .HasForeignKey(d => d.PositionRankId)
                .HasConstraintName("FK_Positions_PositionRanks1");

            entity.HasOne(d => d.PositionType).WithMany(p => p.Positions)
                .HasForeignKey(d => d.PositionTypeId)
                .HasConstraintName("FK_Positions_PositionTypes");
        });

        modelBuilder.Entity<PositionBasicSalary>(entity =>
        {
            entity.HasKey(e => e.PostionBasicSalaryId);

            entity.Property(e => e.PostionBasicSalaryId).HasColumnName("PostionBasicSalaryID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.PositionId).HasColumnName("PositionID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.PositionBasicSalaries)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PositionBasicSalaries_OperationLogs");

            entity.HasOne(d => d.Position).WithMany(p => p.PositionBasicSalaries)
                .HasForeignKey(d => d.PositionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PositionBasicSalaries_Positions");
        });

        modelBuilder.Entity<PositionClassification>(entity =>
        {
            entity.Property(e => e.PositionClassificationId).HasColumnName("PositionClassificationID");
            entity.Property(e => e.CscclassificationPimaryKey).HasColumnName("CSCClassificationPimaryKey");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.NameDhivehi).HasMaxLength(250);
            entity.Property(e => e.OrganisationBusinessEntityID).HasColumnName("OrganisationBusinessEntityID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.PositionClassifications)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_PositionClassifications_OperationLogs");

            entity.HasOne(d => d.OrganisationBusinessEntity).WithMany(p => p.PositionClassifications)
                .HasForeignKey(d => d.OrganisationBusinessEntityID)
                .HasConstraintName("FK_PositionClassifications_Organisations");
        });

        modelBuilder.Entity<PositionRank>(entity =>
        {
            entity.Property(e => e.PositionRankId).HasColumnName("PositionRankID");
            entity.Property(e => e.CscrankPimaryKey).HasColumnName("CSCRankPimaryKey");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NameDhivehi).HasMaxLength(550);
            entity.Property(e => e.OrganisationBusinessEntityID).HasColumnName("OrganisationBusinessEntityID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.PositionRanks)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_PositionRanks_OperationLogs");

            entity.HasOne(d => d.OrganisationBusinessEntity).WithMany(p => p.PositionRanks)
                .HasForeignKey(d => d.OrganisationBusinessEntityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PositionRanks_Organisations");
        });

        modelBuilder.Entity<PositionType>(entity =>
        {
            entity.Property(e => e.PositionTypeId).HasColumnName("PositionTypeID");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PrimaryPayrollItemTypeSapwageType>(entity =>
        {
            entity.HasKey(e => new { e.SapwageTypeId, e.PayrollItemTypeId });

            entity.ToTable("PrimaryPayrollItemTypeSAPWageTypes");

            entity.HasIndex(e => e.PayrollItemTypeId, "IX_PrimaryPayrollItemTypeSAPWageTypes_Unique_PayrollItemTypeID").IsUnique();

            entity.Property(e => e.SapwageTypeId).HasColumnName("SAPWageTypeID");
            entity.Property(e => e.PayrollItemTypeId).HasColumnName("PayrollItemTypeID");

            entity.HasOne(d => d.PayrollItemType).WithOne(p => p.PrimaryPayrollItemTypeSapwageType)
                .HasForeignKey<PrimaryPayrollItemTypeSapwageType>(d => d.PayrollItemTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrimaryPayrollItemTypeSAPWageTypes_PayrollItemTypes");

            entity.HasOne(d => d.SapwageType).WithMany(p => p.PrimaryPayrollItemTypeSapwageTypes)
                .HasForeignKey(d => d.SapwageTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrimaryPayrollItemTypeSAPWageTypes_SAPWageTypes");
        });

        modelBuilder.Entity<RecurrenceType>(entity =>
        {
            entity.HasKey(e => e.RecurrenceTypeId).HasName("PK_RecurrenceType");

            entity.Property(e => e.RecurrenceTypeId).HasColumnName("RecurrenceTypeID");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Region>(entity =>
        {
            entity.Property(e => e.RegionId).HasColumnName("RegionID");
            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Request>(entity =>
        {
            entity.HasIndex(e => new { e.OrganisationBusinessEntityID, e.RequestTypeId, e.RequestStateId, e.ApplicationDate }, "IX_Requests_RequestType_RequestState").IsDescending(false, false, false, true);

            entity.HasIndex(e => e.RequestStateId, "ix_IndexName_10_MARCH");

            entity.HasIndex(e => e.RequestTypeId, "missing_index_4_3_Requests").HasFillFactor(75);

            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.ApplicantBusinessEntityId).HasColumnName("ApplicantBusinessEntityID");
            entity.Property(e => e.ApplicationDate).HasColumnType("datetime");
            entity.Property(e => e.LastStateChangedByUserId).HasColumnName("LastStateChangedByUserID");
            entity.Property(e => e.LastStateChangedDate).HasColumnType("datetime");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationBusinessEntityID).HasColumnName("OrganisationBusinessEntityID");
            entity.Property(e => e.ReferenceNumber).HasMaxLength(50);
            entity.Property(e => e.RequestEffectiveDate).HasColumnType("datetime");
            entity.Property(e => e.RequestStateId).HasColumnName("RequestStateID");
            entity.Property(e => e.RequestTypeId).HasColumnName("RequestTypeID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.StateChangeRemarks)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.LastStateChangedByUser).WithMany(p => p.Requests)
                .HasForeignKey(d => d.LastStateChangedByUserId)
                .HasConstraintName("FK_Requests_LastStateChangedByUsers");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Requests)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Requests_OperationLogs");

            entity.HasOne(d => d.OrganisationBusinessEntity).WithMany(p => p.Requests)
                .HasForeignKey(d => d.OrganisationBusinessEntityID)
                .HasConstraintName("FK_Requests_Organisations");

            entity.HasOne(d => d.RequestState).WithMany(p => p.Requests)
                .HasForeignKey(d => d.RequestStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Requests_RequestStates");

            entity.HasOne(d => d.RequestType).WithMany(p => p.Requests)
                .HasForeignKey(d => d.RequestTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Requests_RequestTypes1");

            entity.HasOne(d => d.Service).WithMany(p => p.Requests)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK_Requests_Services");
        });

        modelBuilder.Entity<RequestCheckListVerification>(entity =>
        {
            entity.Property(e => e.RequestCheckListVerificationId).HasColumnName("RequestCheckListVerificationID");
            entity.Property(e => e.CheckListItemId).HasColumnName("CheckListItemID");
            entity.Property(e => e.Comment).HasColumnType("text");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");

            entity.HasOne(d => d.CheckListItem).WithMany(p => p.RequestCheckListVerifications)
                .HasForeignKey(d => d.CheckListItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestCheckListVerifications_CheckListItems");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.RequestCheckListVerifications)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestCheckListVerifications_OperationLogs");

            entity.HasOne(d => d.Request).WithMany(p => p.RequestCheckListVerifications)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestCheckListVerifications_Requests");
        });

        modelBuilder.Entity<RequestDocument>(entity =>
        {
            entity.Property(e => e.RequestDocumentId).HasColumnName("RequestDocumentID");
            entity.Property(e => e.DocumentId).HasColumnName("DocumentID");
            entity.Property(e => e.LinkedByUserId).HasColumnName("LinkedByUserID");
            entity.Property(e => e.LinkedDate).HasColumnType("datetime");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");

            entity.HasOne(d => d.Document).WithMany(p => p.RequestDocuments)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestDocuments_Documents");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.RequestDocuments)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_RequestDocuments_OperationLogs");

            entity.HasOne(d => d.Request).WithMany(p => p.RequestDocuments)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestDocuments_Requests");
        });

        modelBuilder.Entity<RequestDocumentVerification>(entity =>
        {
            entity.Property(e => e.RequestDocumentVerificationId).HasColumnName("RequestDocumentVerificationID");
            entity.Property(e => e.Comments).HasColumnType("text");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.RequestDocumentId).HasColumnName("RequestDocumentID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.RequestDocumentVerifications)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestDocumentVerifications_OperationLogs");

            entity.HasOne(d => d.RequestDocument).WithMany(p => p.RequestDocumentVerifications)
                .HasForeignKey(d => d.RequestDocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestDocumentVerifications_RequestDocuments");
        });

        modelBuilder.Entity<RequestJob>(entity =>
        {
            entity.HasKey(e => new { e.RequestId, e.JobId });

            entity.HasIndex(e => new { e.RequestId, e.JobId }, "IX_RequestJobs");

            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.Job).WithMany(p => p.RequestJobs)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestJobs_Jobs");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.RequestJobs)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestJobs_OperationLogs");

            entity.HasOne(d => d.Request).WithMany(p => p.RequestJobs)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestJobs_Requests");
        });

        modelBuilder.Entity<RequestState>(entity =>
        {
            entity.Property(e => e.RequestStateId).HasColumnName("RequestStateID");
            entity.Property(e => e.ButtonActionName).HasMaxLength(50);
            entity.Property(e => e.ButtonActionNameDhivehi).HasMaxLength(50);
            entity.Property(e => e.ButtonClass)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CommentRequired).HasDefaultValue(true);
            entity.Property(e => e.StateName)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.StateNameDhivehi).HasMaxLength(250);
        });

        modelBuilder.Entity<RequestTeam>(entity =>
        {
            entity.HasKey(e => new { e.RequestId, e.TeamId });

            entity.HasIndex(e => new { e.RequestId, e.TeamId }, "IX_RequestTeams");

            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.TeamId).HasColumnName("TeamID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.RequestTeams)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestTeams_OperationLogs");

            entity.HasOne(d => d.Request).WithMany(p => p.RequestTeams)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestTeams_Requests");

            entity.HasOne(d => d.Team).WithMany(p => p.RequestTeams)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestTeams_Teams");
        });

        modelBuilder.Entity<RequestType>(entity =>
        {
            entity.Property(e => e.RequestTypeId).HasColumnName("RequestTypeID");
            entity.Property(e => e.SequenceNumberTypeId).HasColumnName("SequenceNumberTypeID");
            entity.Property(e => e.ServiceTypeId).HasColumnName("ServiceTypeID");
            entity.Property(e => e.TypeName)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.ViewUrl)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("ViewURL");

            entity.HasOne(d => d.SequenceNumberType).WithMany(p => p.RequestTypes)
                .HasForeignKey(d => d.SequenceNumberTypeId)
                .HasConstraintName("FK_RequestTypes_SequenceNumberTypes");

            entity.HasOne(d => d.ServiceType).WithMany(p => p.RequestTypes)
                .HasForeignKey(d => d.ServiceTypeId)
                .HasConstraintName("FK_RequestTypes_ServiceTypes");
        });

        modelBuilder.Entity<RequestTypeSpecificDocumentType>(entity =>
        {
            entity.Property(e => e.ContextId)
                .HasDefaultValue(1)
                .HasColumnName("ContextID");
            entity.Property(e => e.DocumentTypeId).HasColumnName("DocumentTypeID");
            entity.Property(e => e.RequestTypeId).HasColumnName("RequestTypeID");

            entity.HasOne(d => d.Context).WithMany(p => p.RequestTypeSpecificDocumentTypes)
                .HasForeignKey(d => d.ContextId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestTypeSpecificDocumentTypes_Context");

            entity.HasOne(d => d.DocumentType).WithMany(p => p.RequestTypeSpecificDocumentTypes)
                .HasForeignKey(d => d.DocumentTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestTypeSpecificDocumentTypes_DocumentTypes");

            entity.HasOne(d => d.RequestType).WithMany(p => p.RequestTypeSpecificDocumentTypes)
                .HasForeignKey(d => d.RequestTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestTypeSpecificDocumentTypes_RequestTypes");
        });

        modelBuilder.Entity<RequestTypesAllowedRequestStateTransition>(entity =>
        {
            entity.HasKey(e => new { e.RequestTypeId, e.FromRequestStateId, e.ToRequestStateId });

            entity.Property(e => e.RequestTypeId).HasColumnName("RequestTypeID");
            entity.Property(e => e.FromRequestStateId).HasColumnName("FromRequestStateID");
            entity.Property(e => e.ToRequestStateId).HasColumnName("ToRequestStateID");

            entity.HasOne(d => d.FromRequestState).WithMany(p => p.RequestTypesAllowedRequestStateTransitionFromRequestStates)
                .HasForeignKey(d => d.FromRequestStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestTypesAllowedRequestStateTransitions_FromRequestStates");

            entity.HasOne(d => d.RequestType).WithMany(p => p.RequestTypesAllowedRequestStateTransitions)
                .HasForeignKey(d => d.RequestTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestTypesAllowedRequestStateTransitions_RequestTypes");

            entity.HasOne(d => d.ToRequestState).WithMany(p => p.RequestTypesAllowedRequestStateTransitionToRequestStates)
                .HasForeignKey(d => d.ToRequestStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestTypesAllowedRequestStateTransitions_ToRequestStates");
        });

        modelBuilder.Entity<RequestTypesStatesRequiredRole>(entity =>
        {
            entity.HasKey(e => new { e.RequestTypeId, e.RequestStateId, e.RoleId }).HasName("PK_RequestTypesStatesRequiredRoles_1");

            entity.HasIndex(e => new { e.RequestStateId, e.RequestTypeId, e.RoleId }, "IX_RequestTypesStatesRequiredRoles");

            entity.Property(e => e.RequestTypeId).HasColumnName("RequestTypeID");
            entity.Property(e => e.RequestStateId).HasColumnName("RequestStateID");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");

            entity.HasOne(d => d.RequestState).WithMany(p => p.RequestTypesStatesRequiredRoles)
                .HasForeignKey(d => d.RequestStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestTypesStatesRequiredRoles_RequestStates");

            entity.HasOne(d => d.RequestType).WithMany(p => p.RequestTypesStatesRequiredRoles)
                .HasForeignKey(d => d.RequestTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestTypesStatesRequiredRoles_RequestTypes");

            entity.HasOne(d => d.Role).WithMany(p => p.RequestTypesStatesRequiredRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestTypesStatesRequiredRoles_Roles");
        });

        modelBuilder.Entity<RequestTypesStatesRequiredRolesForProcessing>(entity =>
        {
            entity.HasKey(e => new { e.RequestTypeId, e.RequestStateId, e.RoleId }).HasName("PK_RequestTypesStatesRequiredRolesForProcessing_1");

            entity.ToTable("RequestTypesStatesRequiredRolesForProcessing");

            entity.Property(e => e.RequestTypeId).HasColumnName("RequestTypeID");
            entity.Property(e => e.RequestStateId).HasColumnName("RequestStateID");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.ToRequestStateId).HasColumnName("ToRequestStateID");

            entity.HasOne(d => d.RequestState).WithMany(p => p.RequestTypesStatesRequiredRolesForProcessingRequestStates)
                .HasForeignKey(d => d.RequestStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestTypesStatesRequiredRolesForProcessing_RequestStates");

            entity.HasOne(d => d.RequestType).WithMany(p => p.RequestTypesStatesRequiredRolesForProcessings)
                .HasForeignKey(d => d.RequestTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestTypesStatesRequiredRolesForProcessing_RequestTypes");

            entity.HasOne(d => d.Role).WithMany(p => p.RequestTypesStatesRequiredRolesForProcessings)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestTypesStatesRequiredRolesForProcessing_Roles");

            entity.HasOne(d => d.ToRequestState).WithMany(p => p.RequestTypesStatesRequiredRolesForProcessingToRequestStates)
                .HasForeignKey(d => d.ToRequestStateId)
                .HasConstraintName("FK_RequestTypesStatesRequiredRolesForProcessing_RequestStates1");
        });

        modelBuilder.Entity<RequestsView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("RequestsView");

            entity.Property(e => e.AttendanceLogDate).HasColumnType("datetime");
            entity.Property(e => e.AttendanceOrganisationId).HasColumnName("AttendanceOrganisationID");
            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.LastUpdatedDate).HasColumnType("datetime");
            entity.Property(e => e.LeaveFromDate).HasColumnType("datetime");
            entity.Property(e => e.LeaveToDate).HasColumnType("datetime");
            entity.Property(e => e.Otdate)
                .HasColumnType("datetime")
                .HasColumnName("OTDate");
            entity.Property(e => e.OutOfOfficeDate).HasColumnType("datetime");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.RequestStateId).HasColumnName("RequestStateID");
            entity.Property(e => e.RequestTypeId).HasColumnName("RequestTypeID");
            entity.Property(e => e.StaffId).HasColumnName("StaffID");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(e => e.RoleID).HasColumnName("RoleID");
            entity.Property(e => e.ContextId).HasColumnName("ContextID");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ModuleId).HasColumnName("ModuleID");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.ParentRoleId).HasColumnName("ParentRoleID");
            entity.Property(e => e.RoleKey).HasMaxLength(100);

            entity.HasOne(d => d.Context).WithMany(p => p.Roles)
                .HasForeignKey(d => d.ContextId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Roles_Context");

            entity.HasOne(d => d.Module).WithMany(p => p.Roles)
                .HasForeignKey(d => d.ModuleId)
                .HasConstraintName("FK_Roles_Module");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Roles)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_Roles_OperationLogs");
        });

        modelBuilder.Entity<SalaryRateDefinition>(entity =>
        {
            entity.Property(e => e.SalaryRateDefinitionId).HasColumnName("SalaryRateDefinitionID");
            entity.Property(e => e.JobTypeId).HasColumnName("JobTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");

            entity.HasOne(d => d.JobType).WithMany(p => p.SalaryRateDefinitions)
                .HasForeignKey(d => d.JobTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalaryRateDefinitions_JobTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.SalaryRateDefinitions)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalaryRateDefinitions_OperationLogs");

            entity.HasOne(d => d.Request).WithMany(p => p.SalaryRateDefinitions)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SalaryRateDefinitions_Requests");
        });

        modelBuilder.Entity<SalaryRateDefinitionForType>(entity =>
        {
            entity.Property(e => e.SalaryRateDefinitionForTypeId).HasColumnName("SalaryRateDefinitionForTypeID");
            entity.Property(e => e.TypeName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Sapexception>(entity =>
        {
            entity.ToTable("SAPExceptions");

            entity.Property(e => e.SapexceptionId).HasColumnName("SAPExceptionID");
            entity.Property(e => e.EffectiveStartDate).HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.PayrollItemTypeId).HasColumnName("PayrollItemTypeID");
            entity.Property(e => e.SapexceptionActionTypeId).HasColumnName("SAPExceptionActionTypeID");
            entity.Property(e => e.SapexceptionTypeId).HasColumnName("SAPExceptionTypeID");
            entity.Property(e => e.SapwageTypeId).HasColumnName("SAPWageTypeID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Sapexceptions)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_SAPExceptions_OperationLogs");

            entity.HasOne(d => d.PayrollItemType).WithMany(p => p.Sapexceptions)
                .HasForeignKey(d => d.PayrollItemTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SAPExceptions_NewPayrollItemTypes");

            entity.HasOne(d => d.SapexceptionActionType).WithMany(p => p.Sapexceptions)
                .HasForeignKey(d => d.SapexceptionActionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SAPExceptions_SAPExceptionActionTypes");

            entity.HasOne(d => d.SapexceptionType).WithMany(p => p.Sapexceptions)
                .HasForeignKey(d => d.SapexceptionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SAPExceptions_SAPExceptionTypes");

            entity.HasOne(d => d.SapwageType).WithMany(p => p.Sapexceptions)
                .HasForeignKey(d => d.SapwageTypeId)
                .HasConstraintName("FK_SAPExceptions_SAPWageTypes");
        });

        modelBuilder.Entity<SapexceptionActionType>(entity =>
        {
            entity.ToTable("SAPExceptionActionTypes");

            entity.Property(e => e.SapexceptionActionTypeId).HasColumnName("SAPExceptionActionTypeID");
            entity.Property(e => e.Description).IsUnicode(false);
            entity.Property(e => e.TypeName)
                .HasMaxLength(500)
                .IsUnicode(false);
        });

        modelBuilder.Entity<SapexceptionType>(entity =>
        {
            entity.ToTable("SAPExceptionTypes");

            entity.Property(e => e.SapexceptionTypeId).HasColumnName("SAPExceptionTypeID");
            entity.Property(e => e.Name).HasMaxLength(250);
        });

        modelBuilder.Entity<Sapsheet>(entity =>
        {
            entity.ToTable("SAPSheets");

            entity.Property(e => e.SapsheetId).HasColumnName("SAPSheetID");
            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.SheetName)
                .HasMaxLength(500)
                .IsUnicode(false);
        });

        modelBuilder.Entity<SapwageType>(entity =>
        {
            entity.ToTable("SAPWageTypes");

            entity.HasIndex(e => e.WageTypeCode, "IX_SAPWageTypes").IsUnique();

            entity.Property(e => e.SapwageTypeId).HasColumnName("SAPWageTypeID");
            entity.Property(e => e.Description).IsUnicode(false);
            entity.Property(e => e.Glcode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("GLCode");
            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.SheetId).HasColumnName("SheetID");
            entity.Property(e => e.WageTypeCode)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Sheet).WithMany(p => p.SapwageTypes)
                .HasForeignKey(d => d.SheetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SAPWageTypes_SAPSheets");
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");
            entity.Property(e => e.Description).IsUnicode(false);
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.IsRamazan).HasDefaultValue(false);
            entity.Property(e => e.IsValid).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.ScheduleTypeId)
                .HasDefaultValue(1)
                .HasColumnName("ScheduleTypeID");
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_Schedules_OperationLogs");

            entity.HasOne(d => d.ScheduleType).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.ScheduleTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Schedules_ScheduleTypes");
        });

        modelBuilder.Entity<ScheduleElement>(entity =>
        {
            entity.HasKey(e => e.ScheduleElementsId);

            entity.Property(e => e.ScheduleElementsId).HasColumnName("ScheduleElementsID");
            entity.Property(e => e.ElementId).HasColumnName("ElementID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");

            entity.HasOne(d => d.Element).WithMany(p => p.ScheduleElements)
                .HasForeignKey(d => d.ElementId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScheduleElements_Elements");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.ScheduleElements)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScheduleElements_OperationLogs");

            entity.HasOne(d => d.Schedule).WithMany(p => p.ScheduleElements)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScheduleElements_Schedules1");
        });

        modelBuilder.Entity<ScheduleOwner>(entity =>
        {
            entity.Property(e => e.ScheduleOwnerId).HasColumnName("ScheduleOwnerID");
            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OwnerTypeId).HasColumnName("OwnerTypeID");
            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.ScheduleOwners)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScheduleOwners_OperationLogs");

            entity.HasOne(d => d.OwnerType).WithMany(p => p.ScheduleOwners)
                .HasForeignKey(d => d.OwnerTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScheduleOwners_OwnerTypes");

            entity.HasOne(d => d.Schedule).WithMany(p => p.ScheduleOwners)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScheduleOwners_Schedules");
        });

        modelBuilder.Entity<ScheduleType>(entity =>
        {
            entity.Property(e => e.ScheduleTypeId).HasColumnName("ScheduleTypeID");
            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .IsUnicode(false);
        });

        modelBuilder.Entity<SequenceNumberType>(entity =>
        {
            entity.Property(e => e.SequenceNumberTypeId).HasColumnName("SequenceNumberTypeID");
            entity.Property(e => e.FormatString)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Prefix)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TypeName)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK_Products");

            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.RegistrationNumber).HasMaxLength(50);
            entity.Property(e => e.ServiceStateId).HasColumnName("ServiceStateID");
            entity.Property(e => e.ServiceTypeId).HasColumnName("ServiceTypeID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Services)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Services_OperationLogs");

            entity.HasOne(d => d.ServiceState).WithMany(p => p.Services)
                .HasForeignKey(d => d.ServiceStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Services_ServiceStates");

            entity.HasOne(d => d.ServiceType).WithMany(p => p.Services)
                .HasForeignKey(d => d.ServiceTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Services_ServiceTypes");
        });

        modelBuilder.Entity<ServiceCheckListVerification>(entity =>
        {
            entity.Property(e => e.ServiceCheckListVerificationId).HasColumnName("ServiceCheckListVerificationID");
            entity.Property(e => e.CheckListItemId).HasColumnName("CheckListItemID");
            entity.Property(e => e.Comment).HasColumnType("text");
            entity.Property(e => e.OprationLogId).HasColumnName("OprationLogID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

            entity.HasOne(d => d.CheckListItem).WithMany(p => p.ServiceCheckListVerifications)
                .HasForeignKey(d => d.CheckListItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceCheckListVerifications_CheckListItems");

            entity.HasOne(d => d.OprationLog).WithMany(p => p.ServiceCheckListVerifications)
                .HasForeignKey(d => d.OprationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceCheckListVerifications_OperationLogs");

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceCheckListVerifications)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceCheckListVerifications_Services");
        });

        modelBuilder.Entity<ServiceDocument>(entity =>
        {
            entity.Property(e => e.ServiceDocumentId).HasColumnName("ServiceDocumentID");
            entity.Property(e => e.DocumentId).HasColumnName("DocumentID");
            entity.Property(e => e.LinkedByUserId).HasColumnName("LinkedByUserID");
            entity.Property(e => e.LinkedDate).HasColumnType("datetime");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");

            entity.HasOne(d => d.Document).WithMany(p => p.ServiceDocuments)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceDocuments_Documents");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.ServiceDocuments)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_ServiceDocuments_OperationLogs");

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceDocuments)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceDocuments_Services");
        });

        modelBuilder.Entity<ServiceDocumentVerification>(entity =>
        {
            entity.Property(e => e.ServiceDocumentVerificationId).HasColumnName("ServiceDocumentVerificationID");
            entity.Property(e => e.Comments).HasColumnType("text");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.ServiceDocumentId).HasColumnName("ServiceDocumentID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.ServiceDocumentVerifications)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceDocumentVerifications_OperationLogs");

            entity.HasOne(d => d.ServiceDocument).WithMany(p => p.ServiceDocumentVerifications)
                .HasForeignKey(d => d.ServiceDocumentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceDocumentVerifications_ServiceDocuments");
        });

        modelBuilder.Entity<ServiceState>(entity =>
        {
            entity.Property(e => e.ServiceStateId).HasColumnName("ServiceStateID");
            entity.Property(e => e.StateName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ServiceType>(entity =>
        {
            entity.HasKey(e => e.ServiceTypeId).HasName("PK_ProductTypes");

            entity.Property(e => e.ServiceTypeId).HasColumnName("ServiceTypeID");
            entity.Property(e => e.SequenceNumberTypeId).HasColumnName("SequenceNumberTypeID");
            entity.Property(e => e.TypeName)
                .HasMaxLength(250)
                .IsUnicode(false);

            entity.HasOne(d => d.SequenceNumberType).WithMany(p => p.ServiceTypes)
                .HasForeignKey(d => d.SequenceNumberTypeId)
                .HasConstraintName("FK_ServiceTypes_SequenceNumberTypes");
        });

        modelBuilder.Entity<ServiceTypesAllowedServiceStateTransition>(entity =>
        {
            entity.HasKey(e => new { e.ServiceTypeId, e.FromServiceStateId, e.ToServiceStateId });

            entity.Property(e => e.ServiceTypeId).HasColumnName("ServiceTypeID");
            entity.Property(e => e.FromServiceStateId).HasColumnName("FromServiceStateID");
            entity.Property(e => e.ToServiceStateId).HasColumnName("ToServiceStateID");

            entity.HasOne(d => d.ServiceType).WithMany(p => p.ServiceTypesAllowedServiceStateTransitions)
                .HasForeignKey(d => d.ServiceTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceTypesAllowedServiceStateTransitions_ServiceTypes");

            entity.HasOne(d => d.ToServiceState).WithMany(p => p.ServiceTypesAllowedServiceStateTransitions)
                .HasForeignKey(d => d.ToServiceStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceTypesAllowedServiceStateTransitions_ServiceStates");
        });

        modelBuilder.Entity<ShiftSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId);

            entity.Property(e => e.ScheduleId)
                .ValueGeneratedNever()
                .HasColumnName("ScheduleID");

            entity.HasOne(d => d.Schedule).WithOne(p => p.ShiftSchedule)
                .HasForeignKey<ShiftSchedule>(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShiftSchedules_Schedules");
        });

        modelBuilder.Entity<ShiftScheduleDay>(entity =>
        {
            entity.Property(e => e.ShiftScheduleDayId).HasColumnName("ShiftScheduleDayID");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.IsValid).HasDefaultValue(true);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");
            entity.Property(e => e.SpecialDayTypeId).HasColumnName("SpecialDayTypeID");
            entity.Property(e => e.WorkShiftId).HasColumnName("WorkShiftID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.ShiftScheduleDays)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShiftScheduleDays_OperationLogs");

            entity.HasOne(d => d.Schedule).WithMany(p => p.ShiftScheduleDays)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShiftScheduleDays_ShiftSchedules");

            entity.HasOne(d => d.SpecialDayType).WithMany(p => p.ShiftScheduleDays)
                .HasForeignKey(d => d.SpecialDayTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShiftScheduleDays_SpecialDayTypes");

            entity.HasOne(d => d.WorkShift).WithMany(p => p.ShiftScheduleDays)
                .HasForeignKey(d => d.WorkShiftId)
                .HasConstraintName("FK_ShiftScheduleDays_WorkShifts");
        });

        modelBuilder.Entity<SickLeaveForm>(entity =>
        {
            entity.HasKey(e => e.LeaveId).HasName("PK_SickLeaveForms_1");

            entity.Property(e => e.LeaveId)
                .ValueGeneratedNever()
                .HasColumnName("LeaveID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.ReportedDateTime).HasColumnType("datetime");

            entity.HasOne(d => d.Leave).WithOne(p => p.SickLeaveForm)
                .HasForeignKey<SickLeaveForm>(d => d.LeaveId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SickLeaveForms_Leaves");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.SickLeaveForms)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SickLeaveForms_OperationLogs");
        });

        modelBuilder.Entity<SickLeaveLodgeType>(entity =>
        {
            entity.Property(e => e.SickLeaveLodgeTypeId).HasColumnName("SickLeaveLodgeTypeID");
            entity.Property(e => e.TypeName).HasMaxLength(250);
        });

        modelBuilder.Entity<SpecialDay>(entity =>
        {
            entity.Property(e => e.SpecialDayId).HasColumnName("SpecialDayID");
            entity.Property(e => e.CalenderDayTypeId).HasColumnName("CalenderDayTypeID");
            entity.Property(e => e.CalenderId).HasColumnName("CalenderID");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.SpecialDays)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SpecialDays_OperationLogs");
        });

        modelBuilder.Entity<SpecialDayShift>(entity =>
        {
            entity.Property(e => e.SpecialDayShiftId).HasColumnName("SpecialDayShiftID");
            entity.Property(e => e.SpecialDayId).HasColumnName("SpecialDayID");
            entity.Property(e => e.WorkShiftId).HasColumnName("WorkShiftID");

            entity.HasOne(d => d.SpecialDay).WithMany(p => p.SpecialDayShifts)
                .HasForeignKey(d => d.SpecialDayId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SpecialDayShifts_SpecialDays");
        });

        modelBuilder.Entity<SpecialDayType>(entity =>
        {
            entity.Property(e => e.SpecialDayTypeId).HasColumnName("SpecialDayTypeID");
            entity.Property(e => e.DayTypeId).HasColumnName("DayTypeID");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");

            entity.HasOne(d => d.DayType).WithMany(p => p.SpecialDayTypes)
                .HasForeignKey(d => d.DayTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SpecialDayTypes_DayTypes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.SpecialDayTypes)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SpecialDayTypes_OperationLogs");

            entity.HasOne(d => d.Organisation).WithMany(p => p.SpecialDayTypes)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SpecialDayTypes_Organisations");

            entity.HasOne(d => d.OrganisationStructure).WithMany(p => p.SpecialDayTypes)
                .HasForeignKey(d => d.OrganisationStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SpecialDayTypes_OrganisationStructure");
        });

        modelBuilder.Entity<SsouserState>(entity =>
        {
            entity.ToTable("SSOUserStates");

            entity.Property(e => e.SsouserStateId).HasColumnName("SSOUserStateID");
            entity.Property(e => e.StateName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.IndividualId).HasName("PK_Staffs_2");

            entity.Property(e => e.IndividualId)
                .ValueGeneratedNever()
                .HasColumnName("IndividualID");
            entity.Property(e => e.EmployeeNumber).HasMaxLength(50);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.Individual).WithOne(p => p.Staff)
                .HasForeignKey<Staff>(d => d.IndividualId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Staffs_Individuals1");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Staff)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Staffs_OperationLogs");
        });

        modelBuilder.Entity<StaffAssignedDeduction>(entity =>
        {
            entity.Property(e => e.StaffAssignedDeductionId).HasColumnName("StaffAssignedDeductionID");
            entity.Property(e => e.CurrencyTypeId).HasColumnName("CurrencyTypeID");
            entity.Property(e => e.DeductionTypeId).HasColumnName("DeductionTypeID");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");

            entity.HasOne(d => d.CurrencyType).WithMany(p => p.StaffAssignedDeductions)
                .HasForeignKey(d => d.CurrencyTypeId)
                .HasConstraintName("FK_StaffAssignedDeductions_CurrencyTypes");

            entity.HasOne(d => d.DeductionType).WithMany(p => p.StaffAssignedDeductions)
                .HasForeignKey(d => d.DeductionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffAssignedDeductions_DeductionTypes");

            entity.HasOne(d => d.Job).WithMany(p => p.StaffAssignedDeductions)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffAssignedDeductions_Jobs");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.StaffAssignedDeductions)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffAssignedDeductions_OperationLogs");

            entity.HasOne(d => d.Request).WithMany(p => p.StaffAssignedDeductions)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("FK_StaffAssignedDeductions_Requests");
        });

        modelBuilder.Entity<StaffAssignedDeductionAmount>(entity =>
        {
            entity.Property(e => e.StaffAssignedDeductionAmountId).HasColumnName("StaffAssignedDeductionAmountID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.StaffAssignedDeductionId).HasColumnName("StaffAssignedDeductionID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.StaffAssignedDeductionAmounts)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffAssignedDeductionAmounts_OperationLogs");

            entity.HasOne(d => d.StaffAssignedDeduction).WithMany(p => p.StaffAssignedDeductionAmounts)
                .HasForeignKey(d => d.StaffAssignedDeductionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffAssignedDeductionAmounts_StaffAssignedDeductions");
        });

        modelBuilder.Entity<StaffDailyAttandanceSummaryIssue>(entity =>
        {
            entity.Property(e => e.StaffDailyAttandanceSummaryIssueId).HasColumnName("StaffDailyAttandanceSummaryIssueID");
            entity.Property(e => e.CheckedDate).HasColumnType("datetime");
            entity.Property(e => e.Comments)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.PayrollCycleProcessingStateId).HasColumnName("PayrollCycleProcessingStateID");
            entity.Property(e => e.StaffDailyAttendanceSummeryId).HasColumnName("StaffDailyAttendanceSummeryID");

            entity.HasOne(d => d.PayrollCycleProcessingState).WithMany(p => p.StaffDailyAttandanceSummaryIssues)
                .HasForeignKey(d => d.PayrollCycleProcessingStateId)
                .HasConstraintName("FK_StaffDailyAttandanceSummaryIssues_PayrollCycleProcessingStates");

            entity.HasOne(d => d.StaffDailyAttandanceSummaryIssueType).WithMany(p => p.StaffDailyAttandanceSummaryIssues)
                .HasForeignKey(d => d.StaffDailyAttandanceSummaryIssueTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffDailyAttandanceSummaryIssues_StaffDailyAttandanceSummaryIssueTypes");

            entity.HasOne(d => d.StaffDailyAttendanceSummery).WithMany(p => p.StaffDailyAttandanceSummaryIssues)
                .HasForeignKey(d => d.StaffDailyAttendanceSummeryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffDailyAttandanceSummaryIssues_StaffDailyAttendanceSummeries");
        });

        modelBuilder.Entity<StaffDailyAttandanceSummaryIssueType>(entity =>
        {
            entity.Property(e => e.TypeName)
                .HasMaxLength(500)
                .IsUnicode(false);
        });

        modelBuilder.Entity<StaffDailyAttendanceSummery>(entity =>
        {
            entity.Property(e => e.StaffDailyAttendanceSummeryId).HasColumnName("StaffDailyAttendanceSummeryID");
            entity.Property(e => e.AbsentFine).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.Comments)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.JobPositionId).HasColumnName("JobPositionID");
            entity.Property(e => e.LateFine).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.Otamount)
                .HasColumnType("decimal(38, 8)")
                .HasColumnName("OTAmount");
            entity.Property(e => e.Otduration).HasColumnName("OTDuration");
            entity.Property(e => e.SalaryAmount).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.SalaryToPayReason).HasMaxLength(250);
            entity.Property(e => e.StaffSalaryId).HasColumnName("StaffSalaryID");

            entity.HasOne(d => d.JobPosition).WithMany(p => p.StaffDailyAttendanceSummeries)
                .HasForeignKey(d => d.JobPositionId)
                .HasConstraintName("FK_StaffDailyAttendanceSummeries_JobPositions");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.StaffDailyAttendanceSummeries)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_StaffDailyAttendanceSummeries_OperationLogs");

            entity.HasOne(d => d.StaffSalary).WithMany(p => p.StaffDailyAttendanceSummeries)
                .HasForeignKey(d => d.StaffSalaryId)
                .HasConstraintName("FK_StaffDailyAttendanceSummeries_StaffSalaries");
        });

        modelBuilder.Entity<StaffEnrollmentNumber>(entity =>
        {
            entity.Property(e => e.StaffEnrollmentNumberId).HasColumnName("StaffEnrollmentNumberID");
            entity.Property(e => e.EnrollmentNumber)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.IndividualId).HasColumnName("IndividualID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");

            entity.HasOne(d => d.Individual).WithMany(p => p.StaffEnrollmentNumbers)
                .HasForeignKey(d => d.IndividualId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffEnrollmentNumbers_Staffs");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.StaffEnrollmentNumbers)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_StaffEnrollmentNumbers_OperationLogs");

            entity.HasOne(d => d.Organisation).WithMany(p => p.StaffEnrollmentNumbers)
                .HasForeignKey(d => d.OrganisationId)
                .HasConstraintName("FK_StaffEnrollmentNumbers_Organisations");
        });

        modelBuilder.Entity<StaffPayrollItemDetail>(entity =>
        {
            entity.HasKey(e => e.StaffPayrollItemDetailsId);

            entity.Property(e => e.StaffPayrollItemDetailsId)
                .ValueGeneratedNever()
                .HasColumnName("StaffPayrollItemDetailsID");
            entity.Property(e => e.BillNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Cifno)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("CIFNo");
            entity.Property(e => e.DealNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FacilityType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Installment)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RefNo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.StaffPayrollItemTypeId).HasColumnName("StaffPayrollItemTypeID");

            entity.HasOne(d => d.StaffPayrollItemType).WithMany(p => p.StaffPayrollItemDetails)
                .HasForeignKey(d => d.StaffPayrollItemTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffPayrollItemDetails_StaffPayrollItemTypes");
        });

        modelBuilder.Entity<StaffPayrollItemType>(entity =>
        {
            entity.HasKey(e => e.StaffPayrollItemTypeId).HasName("PK_StaffAllowances");

            entity.HasIndex(e => new { e.PayrollItemTypeId, e.EffectiveStartDate, e.EndDate, e.IsValid, e.JobId }, "IX_StaffPayrollItemTypes");

            entity.Property(e => e.StaffPayrollItemTypeId).HasColumnName("StaffPayrollItemTypeID");
            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.PayrollItemTypeId).HasColumnName("PayrollItemTypeID");
            entity.Property(e => e.ReferenceNo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Remarks)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.RequestId).HasColumnName("RequestID");

            entity.HasOne(d => d.Job).WithMany(p => p.StaffPayrollItemTypes)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffAllowances_Jobs");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.StaffPayrollItemTypes)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffAllowances_OperationLogs");

            entity.HasOne(d => d.PayrollItemType).WithMany(p => p.StaffPayrollItemTypes)
                .HasForeignKey(d => d.PayrollItemTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffAllowances_Allowances");

            entity.HasOne(d => d.Request).WithMany(p => p.StaffPayrollItemTypes)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("FK_StaffPayrollItemTypes_Requests");
        });

        modelBuilder.Entity<StaffPayrollItemTypeAmount>(entity =>
        {
            entity.Property(e => e.StaffPayrollItemTypeAmountId).HasColumnName("StaffPayrollItemTypeAmountID");
            entity.Property(e => e.Amount).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.CurrencyTypeId).HasColumnName("CurrencyTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.StaffPayrollItemTypeId).HasColumnName("StaffPayrollItemTypeID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.StaffPayrollItemTypeAmounts)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_StaffPayrollItemTypeAmounts_OperationLogs");

            entity.HasOne(d => d.StaffPayrollItemType).WithMany(p => p.StaffPayrollItemTypeAmounts)
                .HasForeignKey(d => d.StaffPayrollItemTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffPayrollItemTypeAmounts_StaffPayrollItemTypes");
        });

        modelBuilder.Entity<StaffPayrollItemTypeSchedule>(entity =>
        {
            entity.HasKey(e => e.StaffPayrollItemTypeScheduleId).HasName("PK_DeductionSchedules");

            entity.Property(e => e.StaffPayrollItemTypeScheduleId).HasColumnName("StaffPayrollItemTypeScheduleID");
            entity.Property(e => e.CurrencyTypeId).HasColumnName("CurrencyTypeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.PayrollCycleRequestId).HasColumnName("PayrollCycleRequestID");
            entity.Property(e => e.StaffPayrollItemTypeId).HasColumnName("StaffPayrollItemTypeID");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.OperationLog).WithMany(p => p.StaffPayrollItemTypeSchedules)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_DeductionSchedules_OperationLogs");

            entity.HasOne(d => d.PayrollCycleRequest).WithMany(p => p.StaffPayrollItemTypeSchedules)
                .HasForeignKey(d => d.PayrollCycleRequestId)
                .HasConstraintName("FK_StaffPayrollItemTypeSchedules_PayrollCycles");

            entity.HasOne(d => d.StaffPayrollItemType).WithMany(p => p.StaffPayrollItemTypeSchedules)
                .HasForeignKey(d => d.StaffPayrollItemTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffPayrollItemTypeSchedules_StaffPayrollItemTypes");
        });

        modelBuilder.Entity<StaffSalary>(entity =>
        {
            entity.Property(e => e.StaffSalaryId).HasColumnName("StaffSalaryID");
            entity.Property(e => e.Comments)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.JobId).HasColumnName("JobID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.PayrollCycleRequestId).HasColumnName("PayrollCycleRequestID");

            entity.HasOne(d => d.Job).WithMany(p => p.StaffSalaries)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffSalaries_Jobs");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.StaffSalaries)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffSalaries_OperationLogs");

            entity.HasOne(d => d.PayrollCycleRequest).WithMany(p => p.StaffSalaries)
                .HasForeignKey(d => d.PayrollCycleRequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffSalaries_PayrollCycles1");
        });

        modelBuilder.Entity<StaffSalaryDeduction>(entity =>
        {
            entity.Property(e => e.StaffSalaryDeductionId).HasColumnName("StaffSalaryDeductionID");
            entity.Property(e => e.Details)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.StaffAssignedDeductionId).HasColumnName("StaffAssignedDeductionID");
            entity.Property(e => e.StaffSalaryId).HasColumnName("StaffSalaryID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.StaffSalaryDeductions)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffSalaryDeductions_OperationLogs");

            entity.HasOne(d => d.StaffAssignedDeduction).WithMany(p => p.StaffSalaryDeductions)
                .HasForeignKey(d => d.StaffAssignedDeductionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffSalaryDeductions_StaffAssignedDeductions");

            entity.HasOne(d => d.StaffSalary).WithMany(p => p.StaffSalaryDeductions)
                .HasForeignKey(d => d.StaffSalaryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffSalaryDeductions_StaffSalaries");
        });

        modelBuilder.Entity<StaffSalaryPayrollItem>(entity =>
        {
            entity.HasKey(e => e.StaffSalaryPayrollItemId).HasName("PK_StaffSalaryAdditions");

            entity.Property(e => e.StaffSalaryPayrollItemId).HasColumnName("StaffSalaryPayrollItemID");
            entity.Property(e => e.Amount).HasColumnType("decimal(38, 8)");
            entity.Property(e => e.Details)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.PayrollItemTypeId).HasColumnName("PayrollItemTypeID");
            entity.Property(e => e.StaffPayrollItemTypeId).HasColumnName("StaffPayrollItemTypeID");
            entity.Property(e => e.StaffSalaryId).HasColumnName("StaffSalaryID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.StaffSalaryPayrollItems)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffSalaryAdditions_OperationLogs");

            entity.HasOne(d => d.PayrollItemType).WithMany(p => p.StaffSalaryPayrollItems)
                .HasForeignKey(d => d.PayrollItemTypeId)
                .HasConstraintName("FK_StaffSalaryPayrollItems_PayrollItemTypes");

            entity.HasOne(d => d.StaffPayrollItemType).WithMany(p => p.StaffSalaryPayrollItems)
                .HasForeignKey(d => d.StaffPayrollItemTypeId)
                .HasConstraintName("FK_StaffSalaryPayrollItems_StaffPayrollItemTypes");

            entity.HasOne(d => d.StaffSalary).WithMany(p => p.StaffSalaryPayrollItems)
                .HasForeignKey(d => d.StaffSalaryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StaffSalaryAdditions_StaffSalaries");
        });

        modelBuilder.Entity<StaffSalaryPayrollItemSapdetail>(entity =>
        {
            entity.ToTable("StaffSalaryPayrollItemSAPDetails");

            entity.Property(e => e.StaffSalaryPayrollItemSapdetailId).HasColumnName("StaffSalaryPayrollItemSAPDetailID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.PayrollItemTypeId).HasColumnName("PayrollItemTypeID");
            entity.Property(e => e.SapwageTypeId).HasColumnName("SAPWageTypeID");
            entity.Property(e => e.StaffSalaryId).HasColumnName("StaffSalaryID");
            entity.Property(e => e.StaffSalaryPayrollItemId).HasColumnName("StaffSalaryPayrollItemID");
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.Property(e => e.TeamId).HasColumnName("TeamID");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.NameDhivehi).HasMaxLength(250);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Teams)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Teams_OperationLogs");

            entity.HasOne(d => d.Organisation).WithMany(p => p.Teams)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Teams_Organisations");
        });

        modelBuilder.Entity<TeamStaff>(entity =>
        {
            entity.HasKey(e => e.TeamStaffsId);

            entity.Property(e => e.TeamStaffsId).HasColumnName("TeamStaffsID");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.StaffId).HasColumnName("StaffID");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.TeamId).HasColumnName("TeamID");
            entity.Property(e => e.IsSuperVisor).HasColumnName("IsSuperVisor");
            entity.Property(e => e.IsValid).HasColumnName("IsValid");

            entity.HasOne(d => d.Staff).WithMany(p => p.TeamStaffs)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeamStaffs_Staffs");

            entity.HasOne(d => d.Team).WithMany(p => p.TeamStaffs)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TeamStaffs_Teams");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username, "IX_UserName").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.ApplicationName).HasMaxLength(255);
            entity.Property(e => e.BusinessEntityID).HasColumnName("BusinessEntityID");
            entity.Property(e => e.Comment).HasMaxLength(255);
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserID");
            entity.Property(e => e.CreatedContextId).HasColumnName("CreatedContextID");
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(128);
            entity.Property(e => e.FullName).HasMaxLength(255);
            entity.Property(e => e.LastStateChangedByUserId).HasColumnName("LastStateChangedByUserID");
            entity.Property(e => e.LastStateChangedContextId).HasColumnName("LastStateChangedContextID");
            entity.Property(e => e.LastStateChangedDate).HasColumnType("datetime");
            entity.Property(e => e.LastSynchronisedByUserId).HasColumnName("LastSynchronisedByUserID");
            entity.Property(e => e.LastSynchronisedContextId).HasColumnName("LastSynchronisedContextID");
            entity.Property(e => e.LastSynchronisedDate).HasColumnType("datetime");
            entity.Property(e => e.LastUpdatedByUserId).HasColumnName("LastUpdatedByUserID");
            entity.Property(e => e.LastUpdatedContextId).HasColumnName("LastUpdatedContextID");
            entity.Property(e => e.LastUpdatedDate).HasColumnType("datetime");
            entity.Property(e => e.LocalUserStateId)
                .HasDefaultValue(1)
                .HasColumnName("LocalUserStateID");
            entity.Property(e => e.Mobile).HasMaxLength(20);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.SsouserKey)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("SSOUserKey");
            entity.Property(e => e.SsouserLastUpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("SSOUserLastUpdatedDate");
            entity.Property(e => e.SsouserStateId)
                .HasDefaultValue(3)
                .HasColumnName("SSOUserStateID");
            entity.Property(e => e.UserKey).HasDefaultValueSql("(newid())");
            entity.Property(e => e.UserOrganisationId).HasColumnName("UserOrganisationID");
            entity.Property(e => e.UserPreferenceId).HasColumnName("UserPreferenceID");
            entity.Property(e => e.Username).HasMaxLength(255);

            entity.HasOne(d => d.BusinessEntity).WithMany(p => p.Users)
                .HasForeignKey(d => d.BusinessEntityID)
                .HasConstraintName("FK_Users_Individuals");

            entity.HasOne(d => d.LocalUserState).WithMany(p => p.Users)
                .HasForeignKey(d => d.LocalUserStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_LocalUserStates");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Users)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_Users_OperationLogs");

            entity.HasOne(d => d.Request).WithMany(p => p.Users)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("FK_Users_Requests");

            entity.HasOne(d => d.SsouserState).WithMany(p => p.Users)
                .HasForeignKey(d => d.SsouserStateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_SSOUserStates");

            entity.HasOne(d => d.UserOrganisation).WithMany(p => p.Users)
                .HasForeignKey(d => d.UserOrganisationId)
                .HasConstraintName("FK_Users_UserOrganisations");

            entity.HasOne(d => d.UserPreference).WithMany(p => p.Users)
                .HasForeignKey(d => d.UserPreferenceId)
                .HasConstraintName("FK_Users_UserPreferences");
        });

        modelBuilder.Entity<UserAssignedUserGroup>(entity =>
        {
            entity.HasKey(e => new { e.UserGroupId, e.UserId });

            entity.Property(e => e.UserGroupId).HasColumnName("UserGroupID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.UserAssignedUserGroups)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_UserAssignedUserGroups_OperationLogs");

            entity.HasOne(d => d.UserGroup).WithMany(p => p.UserAssignedUserGroups)
                .HasForeignKey(d => d.UserGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserAssignedUserGroups_UserGroups");

            entity.HasOne(d => d.User).WithMany(p => p.UserAssignedUserGroups)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserAssignedUserGroups_Users");
        });

        modelBuilder.Entity<UserGroup>(entity =>
        {
            entity.Property(e => e.UserGroupId).HasColumnName("UserGroupID");
            entity.Property(e => e.GroupName).HasMaxLength(250);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.ParentUserGroupId).HasColumnName("ParentUserGroupID");
            entity.Property(e => e.UserOrganisationId).HasColumnName("UserOrganisationID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.UserGroups)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserGroups_OperationLogs");

            entity.HasOne(d => d.UserOrganisation).WithMany(p => p.UserGroups)
                .HasForeignKey(d => d.UserOrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserGroups_UserOrganisations");
        });

        modelBuilder.Entity<UserGroupRole>(entity =>
        {
            entity.HasKey(e => new { e.UserGroupId, e.RoleId });

            entity.Property(e => e.UserGroupId).HasColumnName("UserGroupID");
            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.UserGroupRoles)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserGroupRoles_OperationLogs");

            entity.HasOne(d => d.Role).WithMany(p => p.UserGroupRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserGroupRoles_Roles");

            entity.HasOne(d => d.UserGroup).WithMany(p => p.UserGroupRoles)
                .HasForeignKey(d => d.UserGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserGroupRoles_UserGroups");
        });

        modelBuilder.Entity<UserOrganisation>(entity =>
        {
            entity.Property(e => e.UserOrganisationID).HasColumnName("UserOrganisationID");
            entity.Property(e => e.BusinessEntityID).HasColumnName("BusinessEntityID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            //entity.HasOne(d => d.BusinessEntity).WithMany(p => p.UserOrganisations)
            //    .HasForeignKey(d => d.BusinessEntityID)
            //    .OnDelete(DeleteBehavior.ClientSetNull)
            //    .HasConstraintName("FK_UserOrganisations_Organisations");

         


            entity.HasKey(e => e.UserOrganisationID);

            entity.HasOne(uo => uo.Organisation)
                .WithMany(o => o.UserOrganisations)
                .HasForeignKey(uo => uo.BusinessEntityID)
                .HasPrincipalKey(o => o.BusinessEntityID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserOrganisations_Organisations");



            entity.HasOne(d => d.OperationLog).WithMany(p => p.UserOrganisations)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_UserOrganisations_OperationLogs");
        });

        modelBuilder.Entity<UserOrganisationRole>(entity =>
        {
            entity.HasKey(e => new { e.UserOrganisationID, e.RoleID });

            entity.Property(e => e.UserOrganisationID).HasColumnName("UserOrganisationID");
            entity.Property(e => e.RoleID).HasColumnName("RoleID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");


            //entity.HasKey(e => e.UserOrganisationRoleID);

            //entity.HasOne(d => d.UserOrganisation)
            //    .WithMany(p => p.UserOrganisationRoles)
            //    .HasForeignKey(d => d.UserOrganisationID);

            //entity.HasOne(d => d.Role)
            //    .WithMany(p => p.UserOrganisationRoles)
            //    .HasForeignKey(d => d.RoleID);




            entity.HasOne(d => d.OperationLog).WithMany(p => p.UserOrganisationRoles)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserOrganisationRoles_OperationLogs");

            entity.HasOne(d => d.Role).WithMany(p => p.UserOrganisationRoles)
                .HasForeignKey(d => d.RoleID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserOrganisationRoles_Roles");

            entity.HasOne(d => d.UserOrganisation).WithMany(p => p.UserOrganisationRoles)
                .HasForeignKey(d => d.UserOrganisationID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserOrganisationRoles_UserOrganisations");
        });

        modelBuilder.Entity<UserOrganisationUserGroup>(entity =>
        {
            entity.HasKey(e => new { e.UserOrganisationId, e.UserGroupId });

            entity.Property(e => e.UserOrganisationId).HasColumnName("UserOrganisationID");
            entity.Property(e => e.UserGroupId).HasColumnName("UserGroupID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.UserOrganisationUserGroups)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserOrganisationUserGroups_OperationLogs");

            entity.HasOne(d => d.UserGroup).WithMany(p => p.UserOrganisationUserGroups)
                .HasForeignKey(d => d.UserGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserOrganisationUserGroups_UserGroups");

            entity.HasOne(d => d.UserOrganisation).WithMany(p => p.UserOrganisationUserGroups)
                .HasForeignKey(d => d.UserOrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserOrganisationUserGroups_UserOrganisations");
        });

        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.Property(e => e.UserPreferenceId).HasColumnName("UserPreferenceID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.SelectedContextId).HasColumnName("SelectedContextID");
            entity.Property(e => e.SelectedLanguageId).HasColumnName("SelectedLanguageID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.UserPreferences)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserPreferences_OperationLogs");

            entity.HasOne(d => d.SelectedContext).WithMany(p => p.UserPreferences)
                .HasForeignKey(d => d.SelectedContextId)
                .HasConstraintName("FK_UserPreferences_Context");

            entity.HasOne(d => d.SelectedLanguage).WithMany(p => p.UserPreferences)
                .HasForeignKey(d => d.SelectedLanguageId)
                .HasConstraintName("FK_UserPreferences_Languages");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.UserId, e.UserOrganisationId });

            entity.HasIndex(e => new { e.UserId, e.UserOrganisationId }, "IX_UserRoles_LookUpByUserAndUserOrganisation");

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.UserOrganisationId).HasColumnName("UserOrganisationID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_OperationLogs");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_Roles");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_Users");

            entity.HasOne(d => d.UserOrganisation).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserOrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_UserOrganisations");
        });

        modelBuilder.Entity<UserServiceRole>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.ServiceId, e.UserId });

            entity.Property(e => e.RoleId).HasColumnName("RoleID");
            entity.Property(e => e.ServiceId).HasColumnName("ServiceID");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.UserServiceRoles)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserServiceRoles_OperationLogs");

            entity.HasOne(d => d.Role).WithMany(p => p.UserServiceRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserServiceRoles_Roles");

            entity.HasOne(d => d.Service).WithMany(p => p.UserServiceRoles)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserServiceRoles_Services");

            entity.HasOne(d => d.User).WithMany(p => p.UserServiceRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserServiceRoles_Users");
        });

        modelBuilder.Entity<VerifiedState>(entity =>
        {
            entity.Property(e => e.VerifiedStateId).HasColumnName("VerifiedStateID");
            entity.Property(e => e.StateName)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwLeaveChangeRequest>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_LeaveChangeRequests");

            entity.Property(e => e.IndividualId).HasColumnName("IndividualID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.TeamId).HasColumnName("TeamID");
        });

        modelBuilder.Entity<VwLeaveRequest>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_LeaveRequests");

            entity.Property(e => e.IndividualId).HasColumnName("IndividualID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.TeamId).HasColumnName("TeamID");
        });

        modelBuilder.Entity<VwMergedRequestDatum>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_MergedRequestData");

            entity.Property(e => e.AttendanceLogDate).HasColumnType("datetime");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.Otdate)
                .HasColumnType("datetime")
                .HasColumnName("OTDate");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.RequestStateId).HasColumnName("RequestStateID");
            entity.Property(e => e.RequestTypeId).HasColumnName("RequestTypeID");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<VwOtRequest>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_OtRequests");

            entity.Property(e => e.IndividualId).HasColumnName("IndividualID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.TeamId).HasColumnName("TeamID");
        });

        modelBuilder.Entity<VwOutOfOfficeRequest>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_OutOfOfficeRequests");

            entity.Property(e => e.IndividualId).HasColumnName("IndividualID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.TeamId).HasColumnName("TeamID");
        });

        modelBuilder.Entity<VwTeamRequest>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_TeamRequests");

            entity.Property(e => e.IndividualId).HasColumnName("IndividualID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.TeamId).HasColumnName("TeamID");
        });

        modelBuilder.Entity<VwWorkTeamRequest>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_WorkTeamRequests");

            entity.Property(e => e.IndividualId).HasColumnName("IndividualID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.TeamId).HasColumnName("TeamID");
        });

        modelBuilder.Entity<Ward>(entity =>
        {
            entity.Property(e => e.WardId).HasColumnName("WardID");
            entity.Property(e => e.AbbreviationDhivehi).HasMaxLength(50);
            entity.Property(e => e.AbbreviationEnglish).HasMaxLength(50);
            entity.Property(e => e.IslandId).HasColumnName("IslandID");
            entity.Property(e => e.NameDhivehi).HasMaxLength(250);
            entity.Property(e => e.NameEnglish).HasMaxLength(250);
            entity.Property(e => e.PostCode).HasMaxLength(50);

            entity.HasOne(d => d.Island).WithMany(p => p.Wards)
                .HasForeignKey(d => d.IslandId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Wards_Islands");
        });

        modelBuilder.Entity<WebServiceRequestLog>(entity =>
        {
            entity.HasKey(e => e.RequestLogId);

            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.Controller).HasMaxLength(50);
            entity.Property(e => e.DateTime).HasColumnType("datetime");
            entity.Property(e => e.Ip)
                .HasMaxLength(15)
                .IsFixedLength()
                .HasColumnName("IP");
        });

        modelBuilder.Entity<WorkShift>(entity =>
        {
            entity.Property(e => e.WorkShiftId).HasColumnName("WorkShiftID");
            entity.Property(e => e.Name).HasMaxLength(500);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.WorkShifts)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkShifts_OperationLogs");

            entity.HasOne(d => d.Organisation).WithMany(p => p.WorkShifts)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkShifts_Organisations");

            entity.HasOne(d => d.OrganisationStructure).WithMany(p => p.WorkShifts)
                .HasForeignKey(d => d.OrganisationStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkShifts_OrganisationStructure");
        });

        modelBuilder.Entity<WorkShiftsBreakTime>(entity =>
        {
            entity.HasKey(e => e.WorkShiftsBreakTimesId);

            entity.Property(e => e.WorkShiftsBreakTimesId).HasColumnName("WorkShiftsBreakTimesID");
            entity.Property(e => e.BreakTimeId).HasColumnName("BreakTimeID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.WorkShiftId).HasColumnName("WorkShiftID");

            entity.HasOne(d => d.BreakTime).WithMany(p => p.WorkShiftsBreakTimes)
                .HasForeignKey(d => d.BreakTimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkShiftsBreakTimes_BreakTimes");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.WorkShiftsBreakTimes)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkShiftsBreakTimes_OperationLogs");

            entity.HasOne(d => d.WorkShift).WithMany(p => p.WorkShiftsBreakTimes)
                .HasForeignKey(d => d.WorkShiftId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkShiftsBreakTimes_WorkShifts");
        });

        modelBuilder.Entity<WorkType>(entity =>
        {
            entity.HasKey(e => e.WorkTypeId).HasName("PK__WorkType__CCC06CC09CD52C96");

            entity.Property(e => e.WorkTypeId)
                .ValueGeneratedNever()
                .HasColumnName("WorkTypeID");
            entity.Property(e => e.IsValid).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<WorkingDay>(entity =>
        {
            entity.Property(e => e.WorkingDayId).HasColumnName("WorkingDayID");
            entity.Property(e => e.CalenderId).HasColumnName("CalenderID");
            entity.Property(e => e.DayOfWeekId).HasColumnName("DayOfWeekID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.WorkingDays)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkingDays_OperationLogs");
        });

        modelBuilder.Entity<WorkingDayShift>(entity =>
        {
            entity.Property(e => e.WorkingDayShiftId).HasColumnName("WorkingDayShiftID");
            entity.Property(e => e.WorkShiftId).HasColumnName("WorkShiftID");
            entity.Property(e => e.WorkingDayId).HasColumnName("WorkingDayID");

            entity.HasOne(d => d.WorkingDay).WithMany(p => p.WorkingDayShifts)
                .HasForeignKey(d => d.WorkingDayId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkingDayShifts_WorkingDays");
        });


        ConfigurePlanningProvider(modelBuilder);
        ConfigureOrganisationWorkPlanningSetting(modelBuilder);
        ConfigureWorkTemplateType(modelBuilder);
        ConfigureWorkSegmentType(modelBuilder);
        ConfigureWorkTemplate(modelBuilder);
        ConfigureWorkTemplateSegment(modelBuilder);
        //ConfigureWorkPlan(modelBuilder);
        ConfigureWorkAssignment(modelBuilder);


        OnModelCreatingPartial(modelBuilder);
        //ConfigureWorkPlanningConfiguration(modelBuilder);
        ConfigureWorkPlanningRuntime(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    private static void ConfigureWorkPlan(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkPlan>(entity =>
        {
            entity.ToTable("WorkPlans");

            entity.HasKey(x => x.WorkPlanId)
                .HasName("PK_WorkPlans");

            entity.Property(x => x.WorkPlanId)
                .HasColumnName("WorkPlanID");

            entity.Property(x => x.IndividualId)
                .HasColumnName("IndividualID");

            entity.Property(x => x.JobId)
                .HasColumnName("JobID");

            entity.Property(e => e.WorkTemplateId)
    .HasColumnName("WorkTemplateID");

            entity.Property(e => e.PlanGuid)
                .HasDefaultValueSql("(newid())");

            entity.Property(e => e.Version)
    .HasDefaultValue(1);

            entity.Property(e => e.IsGenerated)
                .HasDefaultValue(false);

            entity.Property(e => e.IsManual)
                .HasDefaultValue(false);


            entity.Property(x => x.OrganisationBusinessEntityId)
                .HasColumnName("OrganisationBusinessEntityID");

            entity.Property(x => x.PlanningProviderId)
                .HasColumnName("PlanningProviderID");

            entity.Property(x => x.WorkDate)
                .HasColumnType("date");

            entity.Property(x => x.GenerationSource)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.GeneratedDate)
                .HasColumnType("datetime2(0)");

            entity.Property(x => x.GeneratedByUserId)
                .HasColumnName("GeneratedByUserID");

            entity.Property(x => x.IsFinalized)
                .HasDefaultValue(false);

            entity.Property(x => x.FinalizedDate)
                .HasColumnType("datetime2(0)");

            entity.Property(x => x.FinalizedByUserId)
                .HasColumnName("FinalizedByUserID");

            entity.Property(x => x.Remarks)
                .HasMaxLength(1000);

            entity.Property(x => x.IsValid)
                .HasDefaultValue(true);

            entity.Property(x => x.OperationLogId)
                .HasColumnName("OperationLogID");

            entity.Property(x => x.CreatedDate)
                .HasColumnType("datetime2(0)");

            entity.Property(e => e.WorkTemplateId)
                    .HasColumnName("WorkTemplateID");

            entity.HasOne(x => x.Individual)
                .WithMany()
                .HasForeignKey(x => x.IndividualId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WorkPlans_Individuals");

            entity.HasOne(e => e.WorkTemplate)
            .WithMany()
            .HasForeignKey(e => e.WorkTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Job)
                .WithMany()
                .HasForeignKey(x => x.JobId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WorkPlans_Jobs");

            entity.HasOne(x => x.Organisation)
                .WithMany()
                .HasForeignKey(x => x.OrganisationBusinessEntityId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WorkPlans_Organisations");

            entity.HasOne(x => x.PlanningProvider)
                .WithMany(x => x.WorkPlans)
                .HasForeignKey(x => x.PlanningProviderId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WorkPlans_PlanningProviders");

            entity.HasOne(x => x.GeneratedByUser)
                .WithMany()
                .HasForeignKey(x => x.GeneratedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WorkPlans_GeneratedByUser");

            entity.HasOne(x => x.FinalizedByUser)
                .WithMany()
                .HasForeignKey(x => x.FinalizedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WorkPlans_FinalizedByUser");

            entity.HasOne(x => x.OperationLog)
                .WithMany()
                .HasForeignKey(x => x.OperationLogId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WorkPlans_OperationLogs");

            entity.HasOne(e => e.WorkTemplate)
                .WithMany()
                .HasForeignKey(e => e.WorkTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new
            {
                x.JobId,
                x.WorkDate
            })
            .IsUnique()
            .HasFilter("[IsValid] = 1")
            .HasDatabaseName("UX_WorkPlans_Job_WorkDate");
        });
    }
    
    private static void ConfigurePlanningProvider(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlanningProvider>(entity =>
        {
            entity.ToTable("PlanningProviders");

            entity.HasKey(x => x.PlanningProviderId)
                .HasName("PK_PlanningProviders");

            entity.Property(x => x.PlanningProviderId)
                .HasColumnName("PlanningProviderID");

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.Property(x => x.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(x => x.Code)
                .IsUnique()
                .HasDatabaseName("UQ_PlanningProviders_Code");
        });
    }

    private static void ConfigureOrganisationWorkPlanningSetting(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrganisationWorkPlanningSetting>(entity =>
        {
            entity.ToTable("OrganisationWorkPlanningSettings");

            entity.HasKey(x => x.OrganisationWorkPlanningSettingId)
                .HasName("PK_OrganisationWorkPlanningSettings");

            entity.Property(x => x.OrganisationWorkPlanningSettingId)
                .HasColumnName("OrganisationWorkPlanningSettingID");

            entity.Property(x => x.OrganisationBusinessEntityId)
                .HasColumnName("OrganisationBusinessEntityID");

            entity.Property(x => x.PlanningProviderId)
                .HasColumnName("PlanningProviderID");

            entity.Property(x => x.EffectiveFromDate)
                .HasColumnType("date");

            entity.Property(x => x.EffectiveToDate)
                .HasColumnType("date");

            entity.Property(x => x.IsActive)
                .HasDefaultValue(true);

            entity.Property(x => x.OperationLogId)
                .HasColumnName("OperationLogID");

            entity.HasOne(x => x.Organisation)
                .WithMany()
                .HasForeignKey(x => x.OrganisationBusinessEntityId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(
                    "FK_OrganisationWorkPlanningSettings_Organisation");

            entity.HasOne(x => x.PlanningProvider)
                .WithMany(x => x.OrganisationWorkPlanningSettings)
                .HasForeignKey(x => x.PlanningProviderId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(
                    "FK_OrganisationWorkPlanningSettings_Provider");

            entity.HasOne(x => x.OperationLog)
                .WithMany()
                .HasForeignKey(x => x.OperationLogId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(
                    "FK_OrganisationWorkPlanningSettings_OperationLogs");
        });
    }

    private static void ConfigureWorkTemplateType(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkTemplateType>(entity =>
        {
            entity.ToTable("WorkTemplateTypes");

            entity.HasKey(x => x.WorkTemplateTypeId)
                .HasName("PK_WorkTemplateTypes");

            entity.Property(x => x.WorkTemplateTypeId)
                .HasColumnName("WorkTemplateTypeID");

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.Property(x => x.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(x => x.Code)
                .IsUnique()
                .HasDatabaseName("UQ_WorkTemplateTypes_Code");
        });
    }

    private static void ConfigureWorkSegmentType(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkSegmentType>(entity =>
        {
            entity.ToTable("WorkSegmentTypes");

            entity.HasKey(x => x.WorkSegmentTypeId)
                .HasName("PK_WorkSegmentTypes");

            entity.Property(x => x.WorkSegmentTypeId)
                .HasColumnName("WorkSegmentTypeID");

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(x => x.Code)
                .IsUnique()
                .HasDatabaseName("UQ_WorkSegmentTypes_Code");
        });
    }

    private static void ConfigureWorkTemplate(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkTemplate>(entity =>
        {
            entity.ToTable("WorkTemplates");

            entity.HasKey(x => x.WorkTemplateId);

            entity.Property(x => x.WorkTemplateId)
                .HasColumnName("WorkTemplateID")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.WorkTemplateTypeId)
                .HasColumnName("WorkTemplateTypeID");

            entity.Property(x => x.OrganisationBusinessEntityId)
                .HasColumnName("OrganisationBusinessEntityID");

            entity.Property(x => x.Name)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(x => x.Code)
                .HasMaxLength(100);

            entity.Property(x => x.Description);

            entity.Property(x => x.DefaultStartTime)
                .HasColumnType("time");

            entity.Property(x => x.DefaultEndTime)
                .HasColumnType("time");

            entity.Property(x => x.OperationLogId)
                .HasColumnName("OperationLogID");

            entity.HasOne(x => x.WorkTemplateType)
                .WithMany(x => x.WorkTemplates)
                .HasForeignKey(x => x.WorkTemplateTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Organisation)
                .WithMany()
                .HasForeignKey(x => x.OrganisationBusinessEntityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.OperationLog)
                .WithMany()
                .HasForeignKey(x => x.OperationLogId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureWorkTemplateSegment(
      ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkTemplateSegment>(entity =>
        {
            entity.ToTable("WorkTemplateSegments");

            entity.HasKey(x => x.WorkTemplateSegmentId);

            entity.Property(x => x.WorkTemplateSegmentId)
                .HasColumnName("WorkTemplateSegmentID")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.WorkTemplateId)
                .HasColumnName("WorkTemplateID");

            entity.Property(x => x.WorkSegmentTypeId)
                .HasColumnName("WorkSegmentTypeID");

            entity.HasOne(x => x.WorkTemplate)
                .WithMany(x => x.WorkTemplateSegments)
                .HasForeignKey(x => x.WorkTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.WorkSegmentType)
                .WithMany(x => x.WorkTemplateSegments)
                .HasForeignKey(x => x.WorkSegmentTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }




}
