using System;
using System.Collections.Generic;
using HRM.Models.Archives;
using HRM.Models.LeaveTypes;
using HRM.Models.Workflow;
using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;

namespace HRM.Models;

public partial class HrmTeContext : DbContext
{
    public DbSet<RequestNotification> RequestNotifications => Set<RequestNotification>();
    public DbSet<JobTypeLeaveAccrualRule> JobTypeLeaveAccrualRules => Set<JobTypeLeaveAccrualRule>();
    public DbSet<LeaveAccrualPosting> LeaveAccrualPostings => Set<LeaveAccrualPosting>();
    public DbSet<LeaveCarryForward> LeaveCarryForwards => Set<LeaveCarryForward>();
    public HrmTeContext()
    {
    }
    public DbSet<LeaveHandoverReview> LeaveHandoverReviews => Set<LeaveHandoverReview>();
    public DbSet<LeaveHandoverSupervisor> LeaveHandoverSupervisors => Set<LeaveHandoverSupervisor>();

    public HrmTeContext(DbContextOptions<HrmTeContext> options) : base(options)
    {
    }

    public DbSet<WfWorkflowDefinition> WfWorkflowDefinitions
    => Set<WfWorkflowDefinition>();

    public DbSet<WfWorkflowStage> WfWorkflowStages
        => Set<WfWorkflowStage>();

    public DbSet<WfStageApprover> WfStageApprovers
        => Set<WfStageApprover>();

    public DbSet<WfRequestInstance> WfRequestInstances
        => Set<WfRequestInstance>();

    public DbSet<WfRequestStageInstance> WfRequestStageInstances
        => Set<WfRequestStageInstance>();

    public DbSet<WfRequestApproval> WfRequestApprovals
        => Set<WfRequestApproval>();

    public DbSet<WfRequestAction> WfRequestActions
        => Set<WfRequestAction>();


    public virtual DbSet<CheckListItem> CheckListItems { get; set; }

    public virtual DbSet<WfWorkflowJobType> WfWorkflowJobTypes
    { get; set; }


    public virtual DbSet<LeavePolicy> LeavePolicies { get; set; }

    public virtual DbSet<LeavePolicyAccrualRule> LeavePolicyAccrualRules { get; set; }

    public DbSet<LeaveTypeMigrationAudit> LeaveTypeMigrationAudits => Set<LeaveTypeMigrationAudit>();



    public DbSet<JobWorkTemplate> JobWorkTemplates { get; set; }

    public virtual DbSet<PlanningProvider> PlanningProviders { get; set; }

    public virtual DbSet<JobWorkTemplateAssignment> JobWorkTemplateAssignments { get; set; }
    public virtual DbSet<OrganisationWorkPlanningSetting>OrganisationWorkPlanningSettings{ get; set; }

    public virtual DbSet<WorkPlanSegment> WorkPlanSegments { get; set; }
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

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<AddressBaseType> AddressBaseTypes { get; set; }

    public virtual DbSet<AddressBasis> AddressBases { get; set; }

    public virtual DbSet<AddressInstance> AddressInstances { get; set; }

    public virtual DbSet<AddressInstanceType> AddressInstanceTypes { get; set; }

    public virtual DbSet<Atoll> Atolls { get; set; }

    public virtual DbSet<AttachedBreakTime> AttachedBreakTimes { get; set; }

    public virtual DbSet<AttachedBreakTimesDay> AttachedBreakTimesDays { get; set; }

    public virtual DbSet<AttendanceDevice> AttendanceDevices { get; set; }

    public virtual DbSet<AttendanceLog> AttendanceLogs { get; set; }

    public virtual DbSet<AttendanceLogChangeRequest> AttendanceLogChangeRequests { get; set; }

    public virtual DbSet<AttendanceLogRequest> AttendanceLogRequests { get; set; }

    public virtual DbSet<AttendanceLogState> AttendanceLogStates { get; set; }


    public virtual DbSet<BasicSalary> BasicSalaries { get; set; }

    public virtual DbSet<BreakTime> BreakTimes { get; set; }

    public virtual DbSet<BulkUploadedDocument> BulkUploadedDocuments { get; set; }


    public virtual DbSet<BusinessEntitiesDocument> BusinessEntitiesDocuments { get; set; }

    public virtual DbSet<BusinessEntity> BusinessEntities { get; set; }

    public virtual DbSet<BusinessEntityCalendar> BusinessEntityCalendars { get; set; }

    public virtual DbSet<BusinessEntityLocation> BusinessEntityLocations { get; set; }

    public virtual DbSet<BusinessEntityLocationType> BusinessEntityLocationTypes { get; set; }

    public virtual DbSet<BusinessEntitySchedule> BusinessEntitySchedules { get; set; }


    public virtual DbSet<BusinessEntityType> BusinessEntityTypes { get; set; }

    public virtual DbSet<BussinessEntityContactInformation> BussinessEntityContactInformations { get; set; }

    public virtual DbSet<Calendar> Calendars { get; set; }

    public virtual DbSet<CalendarInstance> CalendarInstances { get; set; }

    public virtual DbSet<CalendarMonth> CalendarMonths { get; set; }

    public virtual DbSet<CalendarSchedule> CalendarSchedules { get; set; }

    public virtual DbSet<CalendarType> CalendarTypes { get; set; }


    public virtual DbSet<City> Cities { get; set; }

    public virtual DbSet<ContactInformationType> ContactInformationTypes { get; set; }

    public virtual DbSet<Context> Contexts { get; set; }

    public virtual DbSet<Country> Countries { get; set; }

    public virtual DbSet<DataCorrectionRequest> DataCorrectionRequests { get; set; }

    //public virtual DbSet<DayOfWeek> DayOfWeeks { get; set; }

    public virtual DbSet<DeductionType> DeductionTypes { get; set; }

    public virtual DbSet<Document> Documents { get; set; }


    public virtual DbSet<DocumentType> DocumentTypes { get; set; }

    public virtual DbSet<Element> Elements { get; set; }


    public virtual DbSet<FacilityRegistrationType> FacilityRegistrationTypes { get; set; }


    public virtual DbSet<Group> Groups { get; set; }


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

    public virtual DbSet<Leaf> Leaves { get; set; }

    public virtual DbSet<LeaveChangeRequest> LeaveChangeRequests { get; set; }

    public virtual DbSet<LeaveChangeRequestType> LeaveChangeRequestTypes { get; set; }

    public virtual DbSet<LeaveConfigurableValue> LeaveConfigurableValues { get; set; }

    public virtual DbSet<LeaveConfigurableValueType> LeaveConfigurableValueTypes { get; set; }

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

    public virtual DbSet<LocalUserState> LocalUserStates { get; set; }

    public virtual DbSet<Location> Locations { get; set; }

    public virtual DbSet<LocationType> LocationTypes { get; set; }


    public virtual DbSet<Module> Modules { get; set; }

    public virtual DbSet<Month> Months { get; set; }

    public virtual DbSet<Note> Notes { get; set; }


    public virtual DbSet<OfficialTripDetail> OfficialTripDetails { get; set; }

    public virtual DbSet<OfficialTripLocation> OfficialTripLocations { get; set; }

    public virtual DbSet<OfficialTripType> OfficialTripTypes { get; set; }

    public virtual DbSet<OnlineSignInOrganisation> OnlineSignInOrganisations { get; set; }

    public virtual DbSet<OperationLog> OperationLogs { get; set; }

    public virtual DbSet<OperationLogActionType> OperationLogActionTypes { get; set; }

    public virtual DbSet<Organisation> Organisations { get; set; }

    public virtual DbSet<OrganisationPayrollPeriod> OrganisationPayrollPeriods { get; set; }

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

    public virtual DbSet<OtherSalaryRateDefinition> OtherSalaryRateDefinitions { get; set; }

    public virtual DbSet<OtpreApproval> OtpreApprovals { get; set; }

    public virtual DbSet<OutOfOfficeRequest> OutOfOfficeRequests { get; set; }

    public virtual DbSet<OutOfficeRequestType> OutOfficeRequestTypes { get; set; }


    public virtual DbSet<Passport> Passports { get; set; }

    public virtual DbSet<PayrollCycle> PayrollCycles { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<PositionBasicSalary> PositionBasicSalaries { get; set; }

    public virtual DbSet<PositionClassification> PositionClassifications { get; set; }

    public virtual DbSet<PositionRank> PositionRanks { get; set; }

    public virtual DbSet<PositionType> PositionTypes { get; set; }

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


    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SalaryRateDefinition> SalaryRateDefinitions { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<ScheduleElement> ScheduleElements { get; set; }

    public virtual DbSet<ScheduleOwner> ScheduleOwners { get; set; }

    public virtual DbSet<ScheduleType> ScheduleTypes { get; set; }

    public virtual DbSet<SequenceNumberType> SequenceNumberTypes { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ShiftSchedule> ShiftSchedules { get; set; }

    public virtual DbSet<ShiftScheduleDay> ShiftScheduleDays { get; set; }

    public virtual DbSet<SickLeaveForm> SickLeaveForms { get; set; }


    public virtual DbSet<SpecialDay> SpecialDays { get; set; }


    public virtual DbSet<SpecialDayType> SpecialDayTypes { get; set; }

    public virtual DbSet<SsouserState> SsouserStates { get; set; }

    public virtual DbSet<Staff> Staffs { get; set; }

    public virtual DbSet<StaffAssignedDeduction> StaffAssignedDeductions { get; set; }

    public virtual DbSet<StaffEnrollmentNumber> StaffEnrollmentNumbers { get; set; }


    public virtual DbSet<StaffPayrollItemType> StaffPayrollItemTypes { get; set; }

    public virtual DbSet<StaffPayrollItemTypeAmount> StaffPayrollItemTypeAmounts { get; set; }

    public virtual DbSet<StaffPayrollItemTypeSchedule> StaffPayrollItemTypeSchedules { get; set; }

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

    public virtual DbSet<Ward> Wards { get; set; }


    public virtual DbSet<WorkShift> WorkShifts { get; set; }

    public virtual DbSet<WorkingDay> WorkingDays { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            throw new InvalidOperationException("Configure HrmTeContext through dependency injection or a design-time factory. A database connection is never inferred from source code.");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LeaveCarryForward>(e =>
        {
            e.ToTable("LCF_LeaveCarryForwards"); e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RuleId, x.JobId, x.RenewalDate }).IsUnique();
            e.Property(x => x.RenewalDate).HasColumnType("date");
            e.Property(x => x.PreviousBalance).HasPrecision(10, 2);
            e.Property(x => x.CarriedDays).HasPrecision(10, 2);
            e.Property(x => x.PeriodStart).HasColumnType("date");
            e.Property(x => x.PeriodEnd).HasColumnType("date");
            e.Property(x => x.ClosingGrant).HasPrecision(10, 2);
            e.Property(x => x.RenewalGrant).HasPrecision(10, 2);
            e.Property(x => x.NewBalance).HasPrecision(10, 2);
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.HasOne<JobTypeLeaveAccrualRule>().WithMany().HasForeignKey(x => x.RuleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<JobLeaveType>().WithMany().HasForeignKey(x => x.JobLeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<JobTypeLeaveAccrualRule>(e =>
        {
            e.ToTable("LA_JobTypeLeaveAccrualRules"); e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganisationId, x.JobTypeId, x.LeaveTypeId }).IsUnique();
            e.Property(x => x.GrantDays).HasPrecision(10, 2);
            e.Property(x => x.CarryForwardLimit).HasPrecision(10, 2);
            e.Property(x => x.MaximumBalance).HasPrecision(10, 2);
            e.Property(x => x.CarryForwardMode).HasMaxLength(20);
            e.Property(x => x.EffectiveFrom).HasColumnType("date");
            e.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<JobType>().WithMany().HasForeignKey(x => x.JobTypeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<LeaveType>().WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<LeaveAccrualPosting>(e =>
        {
            e.ToTable("LA_LeaveAccrualPostings"); e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RuleId, x.JobId, x.DueDate }).IsUnique();
            e.Property(x => x.DueDate).HasColumnType("date");
            e.Property(x => x.GrantedDays).HasPrecision(10, 2); e.Property(x => x.CarryAdjustment).HasPrecision(10, 2);
            e.Property(x => x.PreviousBalance).HasPrecision(10, 2); e.Property(x => x.NewBalance).HasPrecision(10, 2);
            e.HasOne<JobTypeLeaveAccrualRule>().WithMany().HasForeignKey(x => x.RuleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<JobLeaveType>().WithMany().HasForeignKey(x => x.JobLeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<RequestNotification>(entity =>
        {
            entity.ToTable("RN_RequestNotifications");
            entity.HasKey(x => x.RequestNotificationId);
            entity.Property(x => x.ReferenceNumber).HasMaxLength(512);
            entity.Property(x => x.NotificationKind).HasMaxLength(32).HasDefaultValue("StateChanged");
            entity.HasIndex(x => new { x.UserId, x.RequestId }).IsUnique()
                .HasDatabaseName("UX_RequestNotifications_DutyAcceptanceRecipient")
                .HasFilter("[NotificationKind] = 'DutyAcceptance'");
            entity.HasIndex(x => new { x.UserId, x.RequestStageInstanceId }).IsUnique()
                .HasDatabaseName("UX_RequestNotifications_ActionRecipientStage")
                .HasFilter("[NotificationKind] = 'ActionRequired' AND [RequestStageInstanceId] IS NOT NULL");
            entity.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");
            entity.Property(x => x.ReadAtUtc).HasColumnType("datetime2");
            entity.HasIndex(x => new { x.UserId, x.NotificationKind, x.LeaveId, x.HandoverRevision }).IsUnique()
                .HasDatabaseName("UX_RequestNotifications_HandoverRecipientRevision")
                .HasFilter("[LeaveId] IS NOT NULL AND [HandoverRevision] IS NOT NULL");
            entity.HasOne<Leaf>().WithMany().HasForeignKey(x => x.LeaveId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.UserId, x.ReadAtUtc, x.CreatedAtUtc });
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<RequestState>().WithMany().HasForeignKey(x => x.FromStateId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<RequestState>().WithMany().HasForeignKey(x => x.ToStateId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<LeaveHandoverReview>(entity =>
        {
            entity.ToTable("LH_LeaveHandoverReviews"); entity.HasKey(x => x.LeaveId);
            entity.Property(x => x.LeaveId).ValueGeneratedNever();
            entity.Property(x => x.SupervisorDecision).HasMaxLength(20);
            entity.Property(x => x.Comments).HasMaxLength(250);
            entity.HasOne<Leaf>().WithMany().HasForeignKey(x => x.LeaveId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Staff>().WithMany().HasForeignKey(x => x.SupervisorApprovedByIndividualId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<LeaveHandoverSupervisor>(entity =>
        {
            entity.ToTable("LH_LeaveHandoverSupervisors"); entity.HasKey(x => new { x.LeaveId, x.IndividualId });
            entity.HasOne<LeaveHandoverReview>().WithMany().HasForeignKey(x => x.LeaveId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Staff>().WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        });

        // Preserve the deployed legacy lookup table name; convention singularised it incorrectly.
        modelBuilder.Entity<HRM.Models.Archives.DayOfWeek>().ToTable("DayOfWeeks");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HrmTeContext).Assembly);

        modelBuilder.Entity<JobWorkTemplate>(entity =>
        {
            entity.ToTable("JobWorkTemplates");

            entity.HasKey(x => x.JobWorkTemplateId);

            entity.Property(x => x.EffectiveFrom)
                .HasColumnType("date");

            entity.Property(x => x.EffectiveTo)
                .HasColumnType("date");

            entity.HasOne(x => x.Job)
                .WithMany()
                .HasForeignKey(x => x.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.WorkTemplate)
                .WithMany()
                .HasForeignKey(x => x.WorkTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new
            {
                x.JobId,
                x.IsActive,
                x.EffectiveFrom,
                x.EffectiveTo
            });
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


        });

        //// This forces EF Core to understand there is only ONE relationship
        //modelBuilder.Entity<Job>()
        //    .HasOne(j => j.User)         // 1. A Job has one User
        //    .WithMany(u => u.Jobs)       // 2. A User has many Jobs (Make sure 'Jobs' matches the exact name of the list in your User.cs file!)
        //    .HasForeignKey(j => j.IndividualID); // 3. They are linked specifically by IndividualID

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

            entity.HasOne(d => d.OperationLog).WithMany(p => p.AttendanceDevices)
                .HasForeignKey(d => d.OperationLogId)
                .HasConstraintName("FK_AttendanceDevices_OperationLogs");
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

            entity.HasOne(d => d.OperationLog).WithMany(p => p.BulkUploadedDocuments)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BulkUploadedDocuments_OperationLogs");

            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.BulkUploadedDocuments)
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BulkUploadedDocuments_Users");
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

        modelBuilder.Entity<City>(entity =>
        {
            entity.Property(e => e.CityId).HasColumnName("CityID");
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .IsUnicode(false);
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

        //modelBuilder.Entity<DayOfWeek>(entity =>
        //{
        //    entity.Property(e => e.DayOfWeekId).HasColumnName("DayOfWeekID");
        //    entity.Property(e => e.Name).HasMaxLength(50);
        //});

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
            entity.Property(e => e.OrganisationId).HasColumnName("OwnerOrganisationID");
            entity.Property(e => e.RequestId).HasColumnName("RequestID");
            entity.Property(e => e.TypeNameDhivehi).HasMaxLength(50);
            entity.Property(e => e.TypeNameEnglish).HasMaxLength(50);

            entity.HasOne(d => d.OwnerOrganisation).WithMany(p => p.DeductionTypes)
                .HasForeignKey(d => d.OrganisationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DeductionTypes_Organisations");

            entity.HasOne(d => d.Request).WithMany(p => p.DeductionTypes)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("FK_DeductionTypes_Requests");
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
            entity.ToTable("Jobs");

            entity.HasKey(e => e.JobId)
                .HasName("PK_Jobs");

            entity.Property(e => e.JobId)
                .HasColumnName("JobID")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.IndividualID)
                .HasColumnName("IndividualID");

            entity.Property(e => e.OrganisationID)
                .HasColumnName("OrganisationID");

            entity.Property(e => e.OrganisationStructureId)
                .HasColumnName("OrganisationStructureID");

            entity.Property(e => e.JobStateId)
                .HasColumnName("JobStateID");

            entity.Property(e => e.JobTypeId)
                .HasColumnName("JobTypeID");

            entity.Property(e => e.JoinedDate)
                .HasColumnType("datetime");

            entity.Property(e => e.TerminatedDate)
                .HasColumnType("datetime");

            entity.Property(e => e.BasicSalary)
                .HasColumnType("decimal(38, 8)");

            entity.Property(e => e.OperationLogId)
                .HasColumnName("OperationLogID");

            entity.Property(e => e.Sapnumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SAPNumber");

            entity.Property(e => e.ServiceId)
                .HasColumnName("ServiceID");

            entity.HasOne(e => e.Staff)
                .WithMany(e => e.Jobs)
                .HasForeignKey(e => e.IndividualID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Jobs_Staffs");

            entity.HasOne(e => e.Individual)
                .WithMany()
                .HasForeignKey(e => e.IndividualID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Jobs_Individuals");

            entity.HasOne(e => e.JobType)
                .WithMany(e => e.Jobs)
                .HasForeignKey(e => e.JobTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Jobs_JobTypes");

            entity.HasOne(e => e.Organisation)
                .WithMany(e => e.Jobs)
                .HasForeignKey(e => e.OrganisationID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Jobs_Organisations");

            entity.HasOne(e => e.OrganisationStructure)
                .WithMany(e => e.Jobs)
                .HasForeignKey(e => e.OrganisationStructureId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Jobs_OrganisationStructure");

            entity.HasOne(e => e.Service)
                .WithMany(e => e.Jobs)
                .HasForeignKey(e => e.ServiceId)
                .HasConstraintName("FK_Jobs_Services");
        });

        modelBuilder.Entity<JobLeaveType>(entity =>
        {
            entity.ToTable("JobLeaveTypes", t => t.HasCheckConstraint("CK_JobLeaveTypes_LeaveType", "[LeaveTypeID] IS NOT NULL"));

            entity.HasKey(e => e.JobLeaveTypeId)
                .HasName("PK_JobLeaveTypes");

            // Canonical type lookup index
            entity.HasIndex(
                    e => new
                    {
                        e.JobId,
                        e.LeaveTypeId
                    },
                    "IX_JobLeaveTypes");

            entity.Property(e => e.JobLeaveTypeId)
                .HasColumnName("JobLeaveTypeID");

            entity.Property(e => e.JobId)
                .HasColumnName("JobID");

            entity.Property(e => e.LeaveTypeId)
                .HasColumnName("LeaveTypeID");



            entity.Property(e => e.RemainingDays).HasPrecision(10, 2);

            entity.Property(e => e.LastLeaveTakenDate)
                .HasColumnType("datetime");

            entity.Property(e => e.RenewedDate)
                .HasColumnType("datetime");

            entity.Property(e => e.OperationLogId)
                .HasColumnName("OperationLogID");

            entity.Property(e => e.EffectiveFromDate)
                .HasColumnType("datetime");

            entity.Property(e => e.EffectiveToDate)
                .HasColumnType("datetime");

            entity.Property(e => e.AutoCorrectDate)
                .HasColumnType("datetime");

            entity.Property(e => e.AutoCorrectRemark)
                .HasMaxLength(500);

            // Job relationship
            entity.HasOne(e => e.Job)
                .WithMany(e => e.JobLeaveTypes)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_JobLeaves_Jobs");

            // Canonical type relationship
            entity.HasOne(e => e.LeaveType)
                .WithMany(e => e.JobLeaveTypes)
                .HasForeignKey(e => e.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_JobLeaves_LeaveTypes");

            // Operation log relationship
            entity.HasOne(e => e.OperationLog)
                .WithMany(e => e.JobLeaveTypes)
                .HasForeignKey(e => e.OperationLogId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName(
                    "FK_JobLeaves_OperationLogs");
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
            entity.ToTable("LT_LeaveTypes");
            entity.Property(e => e.Code).HasMaxLength(50).IsUnicode(false);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasMany(e => e.LeavePolicies).WithOne(e => e.LeaveType).HasForeignKey(e => e.LeaveTypeId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_LeavePolicies_LT_LeaveTypes_LeaveTypeID");
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
            entity.Property(x => x.TaskDetails).HasMaxLength(250);
            entity.Property(x => x.Decision).HasMaxLength(20);
            entity.Property(x => x.Comments).HasMaxLength(250);
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

        modelBuilder.Entity<Note>(entity =>
        {
            entity.Property(e => e.NoteId).HasColumnName("NoteID");
            entity.Property(e => e.NoteReasonId).HasColumnName("NoteReasonID");
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.Text).HasColumnType("text");

            entity.HasOne(d => d.OperationLog).WithMany(p => p.Notes)
                .HasForeignKey(d => d.OperationLogId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notes_OperationLogs1");
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

        modelBuilder.Entity<SpecialDayType>(entity =>
        {
            entity.Property(e => e.SpecialDayTypeId).HasColumnName("SpecialDayTypeID");
            entity.Property(e => e.DayTypeId).HasColumnName("DayTypeID");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.OperationLogId).HasColumnName("OperationLogID");
            entity.Property(e => e.OrganisationId).HasColumnName("OrganisationID");
            entity.Property(e => e.OrganisationStructureId).HasColumnName("OrganisationStructureID");

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
            entity.Property(e => e.AdminPortalAccessEnabled).HasDefaultValue(false);
            entity.Property(e => e.AdminPortalAccessOrganisationId);
            entity.HasKey(e => e.UserId);

            entity.Property(e => e.UserId)
                .HasColumnName("UserID")
                .ValueGeneratedOnAdd();


            entity.HasIndex(e => e.Username, "IX_UserName").IsUnique();

            //entity.Property(e => e.UserId).HasColumnName("UserID");
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
            entity.Property(e => e.UserOrganisationRoleID).ValueGeneratedOnAdd();

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
            entity.Property(e => e.ThemeMode).HasMaxLength(16).HasDefaultValue("dark").IsRequired();
            entity.Property(e => e.RecentWorkspaceSearchesJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.UpdatedAtUtc).HasColumnType("datetime2");
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

        modelBuilder.Entity<WorkAssignment>().Property(x => x.SchedulePurpose).HasMaxLength(20).HasDefaultValue("Official");
        modelBuilder.Entity<WorkPlanSegment>().Property(x => x.SchedulePurpose).HasMaxLength(20).HasDefaultValue("Official");
        modelBuilder.Entity<JobWorkTemplateAssignment>(entity =>
        {
            entity.Property(x => x.SchedulePurpose).HasMaxLength(20).HasDefaultValue("Official");
            entity.Property(x => x.ScheduleMode).HasMaxLength(20).HasDefaultValue("Standard");
            entity.Property(x => x.WorkingDaysMask).HasDefaultValue(31);
            entity.Property(x => x.RotationAnchorDate).HasColumnType("date");
            entity.Property(x => x.RotationTemplateIds).HasMaxLength(2000);
            entity.ToTable("JobWorkTemplateAssignments");

            entity.HasKey(e => e.JobWorkTemplateAssignmentID);

            entity.Property(e => e.JobWorkTemplateAssignmentID)
                .HasColumnName("JobWorkTemplateAssignmentID")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.JobID)
                .HasColumnName("JobID")
                .IsRequired();

            entity.Property(e => e.WorkTemplateID)
                .HasColumnName("WorkTemplateID")
                .IsRequired();

            entity.Property(e => e.EffectiveFrom)
                .HasColumnName("EffectiveFrom")
                .HasColumnType("datetime")
                .IsRequired();

            entity.Property(e => e.EffectiveTo)
                .HasColumnName("EffectiveTo")
                .HasColumnType("datetime");

            entity.Property(e => e.IsActive)
                .HasColumnName("IsActive")
                .HasDefaultValue(true)
                .IsRequired();

            entity.Property(e => e.CreatedDate)
                .HasColumnName("CreatedDate")
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getdate())")
                .ValueGeneratedOnAdd();

            entity.HasIndex(e => new
            {
                e.JobID,
                e.WorkTemplateID,
                e.EffectiveFrom
            })
            .HasDatabaseName(
                "IX_JobWorkTemplateAssignment_Job_Template");

            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobID)
                .OnDelete(DeleteBehavior.Restrict);

            //entity.HasOne(e => e.WorkTemplate)
            //    .WithMany(e => e.JobWorkTemplateAssignments)
            //    .HasForeignKey(e => e.WorkTemplateID)
            //    .OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.ConfigureWorkflow();

        ConfigurePlanningProvider(modelBuilder);
        ConfigureOrganisationWorkPlanningSetting(modelBuilder);
        ConfigureWorkTemplateType(modelBuilder);
        ConfigureWorkSegmentType(modelBuilder);
        ConfigureWorkTemplate(modelBuilder);
        ConfigureWorkTemplateSegment(modelBuilder);
        //ConfigureWorkPlan(modelBuilder);
        ConfigureWorkAssignment(modelBuilder);

        ConfigureWorkflowJobType(modelBuilder);
        ConfigureWorkflowDefinition(modelBuilder);
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

            //entity.HasKey(x => x.WorkPlanId)
            //    .HasName("PK_WorkPlans");

            entity.Property(x => x.WorkPlanId)
                .HasColumnName("WorkPlanID");

            entity.Property(x => x.IndividualId)
                .HasColumnName("IndividualID");

            entity.Property(x => x.JobId)
                .HasColumnName("JobID");

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

            entity.Property(e => e.GenerationSource)
                   .HasConversion<string>()
                   .HasMaxLength(50);

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
 

            entity.HasOne(x => x.Individual)
                .WithMany()
                .HasForeignKey(x => x.IndividualId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WorkPlans_Individuals");

            entity.HasOne(e => e.WorkTemplate)
                .WithMany(e => e.WorkPlans)
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

            entity.HasIndex(x => new
            {
                x.JobId,
                x.WorkDate
            })
            .IsUnique()
            .HasFilter("[IsValid] = 1")
            .HasDatabaseName("UX_WorkPlans_Job_WorkDate");
        });

        modelBuilder.Entity<LeavePolicy>(entity =>
        {
            entity.ToTable("LT_LeavePolicies");

            entity.HasKey(x =>
                x.LeavePolicyId).HasName("PK_LeavePolicies");
            entity.HasIndex(x => x.LeaveTypeId).HasDatabaseName("IX_LeavePolicies_LeaveTypeID");

            entity.Property(x =>
                x.LeavePolicyId)
                .HasColumnName("LeavePolicyID");

            entity.Property(x =>
                x.LeaveTypeId)
                .HasColumnName("LeaveTypeID");

            entity.Property(x =>
                x.OrganisationId)
                .HasColumnName("OrganisationID");

            entity.Property(x =>
                x.RequestTypeId)
                .HasColumnName("RequestTypeID");

            entity.Property(x =>
                x.OperationLogId)
                .HasColumnName("OperationLogID");

            entity.Property(x =>
                x.DefaultEntitlementDays)
                .HasColumnType("decimal(8,2)");

            entity.Property(x =>
                x.PayPercentage)
                .HasColumnType("decimal(5,2)");
        });


        modelBuilder.Entity<LeavePolicyAccrualRule>(entity =>
        {
            entity.ToTable("LT_LeavePolicyAccrualRules");

            entity.HasKey(x =>
                x.LeavePolicyAccrualRuleId).HasName("PK_LeavePolicyAccrualRules");
            entity.HasIndex(x => x.LeavePolicyId).HasDatabaseName("IX_LeavePolicyAccrualRules_LeavePolicyID");

            entity.Property(x =>
                x.LeavePolicyAccrualRuleId)
                .HasColumnName("LeavePolicyAccrualRuleID");

            entity.Property(x =>
                x.LeavePolicyId)
                .HasColumnName("LeavePolicyID");

            entity.Property(x =>
                x.AccrualType)
                .HasConversion<string>();

            entity.Property(x =>
                x.AccrualAmount)
                .HasColumnType("decimal(8,2)");

            entity.Property(x =>
                x.MaximumBalance)
                .HasColumnType("decimal(8,2)");

            entity.Property(x =>
                x.MaximumCarryForward)
                .HasColumnType("decimal(8,2)");

            entity.HasOne(x =>
                x.LeavePolicy)
                .WithMany(x =>
                    x.AccrualRules)
                .HasForeignKey(x =>
                    x.LeavePolicyId).HasConstraintName("FK_LeavePolicyAccrualRules_LeavePolicies_LeavePolicyID");
        });

        modelBuilder.Entity<LeaveTypeMigrationAudit>(entity =>
        {
            entity.ToTable("LT_TypeMigrationAudits");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SourceEntity).HasMaxLength(80);
            entity.Property(x => x.SnapshotJson).IsRequired();
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


            entity.HasIndex(e => new {
                e.OrganisationBusinessEntityID,
                e.Code
            }).IsUnique();

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


            entity.HasOne(e => e.OrganisationBusinessEntity)
                .WithMany()
                .HasForeignKey(e => e.OrganisationBusinessEntityID)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PlanningProviders_Organisations");


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

            entity.Property(x => x.EffectiveFrom)
                .HasColumnType("date");

            entity.Property(x => x.EffectiveTo)
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

    private static void ConfigureWorkTemplateSegment(ModelBuilder modelBuilder)
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


    private static void ConfigureWorkflowDefinition(
    ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WfWorkflowDefinition>(entity =>
        {
            entity.ToTable("WF_WorkflowDefinitions");

            entity.HasKey(x => x.WorkflowDefinitionId);

            entity.Property(x => x.WorkflowDefinitionId)
                .HasColumnName("WorkflowDefinitionID")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.RequestTypeId)
                .HasColumnName("RequestTypeID");

            entity.Property(x => x.OrganisationBusinessEntityId)
                .HasColumnName("OrganisationBusinessEntityID");

            entity.Property(x => x.VersionNumber)
                .HasColumnName("VersionNumber");

            entity.Property(x => x.Name)
                .HasColumnName("Name");

            entity.Property(x => x.IsActive)
                .HasColumnName("IsActive");

            entity.Property(x => x.EffectiveFrom)
                .HasColumnName("EffectiveFrom")
                .HasColumnType("date");

            entity.Property(x => x.EffectiveTo)
                .HasColumnName("EffectiveTo")
                .HasColumnType("date");

            entity.Property(x => x.CreatedAt)
                .HasColumnName("CreatedAt");

            entity.HasMany(x => x.WorkflowJobTypes)
                .WithOne(x => x.WorkflowDefinition)
                .HasForeignKey(x => x.WorkflowDefinitionId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureWorkflowJobType(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WfWorkflowJobType>(entity =>
        {
            entity.ToTable("WF_WorkflowJobTypes");

            entity.HasKey(x => x.WorkflowJobTypeId);

            entity.Property(x => x.WorkflowJobTypeId)
                .HasColumnName("WorkflowJobTypeID")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.WorkflowDefinitionId)
                .HasColumnName("WorkflowDefinitionID")
                .IsRequired();

            entity.Property(x => x.JobTypeId)
                .HasColumnName("JobTypeID")
                .IsRequired();

            entity.Property(x => x.IsActive)
                .HasColumnName("IsActive")
                .HasDefaultValue(true)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.WorkflowDefinitionId,
                x.JobTypeId
            })
            .IsUnique();

            entity.HasOne(x => x.WorkflowDefinition)
                .WithMany(x => x.WorkflowJobTypes)
                .HasForeignKey(x => x.WorkflowDefinitionId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.JobType)
                .WithMany()
                .HasForeignKey(x => x.JobTypeId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

}
