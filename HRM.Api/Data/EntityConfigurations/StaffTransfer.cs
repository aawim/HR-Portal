using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class StaffTransferMapping :
    IEntityTypeConfiguration<StaffTransfer>,
    IEntityTypeConfiguration<StaffTransferJobDetail>,
    IEntityTypeConfiguration<StaffTransferWorkDetail>,
    IEntityTypeConfiguration<StaffTransferAccessChange>,
    IEntityTypeConfiguration<StaffTransferDoorAccess>,
    IEntityTypeConfiguration<StaffTransferReportingDetail>,
    IEntityTypeConfiguration<StaffTransferComment>,
    IEntityTypeConfiguration<StaffTransferConfirmation>,
    IEntityTypeConfiguration<StaffTransferActivationLog>,
    IEntityTypeConfiguration<StaffTransferDocument>,
    IEntityTypeConfiguration<JobOrganisationHistory>,
    IEntityTypeConfiguration<StaffTransferClearanceTask>,
    IEntityTypeConfiguration<StaffTransferAssetDecision>,
    IEntityTypeConfiguration<StaffTransferFinanceSettlement>,
    IEntityTypeConfiguration<StaffTransferEmploymentExecution>
{
    public void Configure(EntityTypeBuilder<StaffTransfer> b)
    {
        b.ToTable("ST_StaffTransfers", t =>
        {
            t.HasCheckConstraint("CK_ST_StaffTransfers_Status",
                "[Status] IN ('Draft','Submitted','PendingStaffConfirmation','CorrectionRequested','PendingReceivingApproval','Rejected','Approved','Scheduled','ActivationInProgress','Active','ActivationFailed','Cancelled')");
            t.HasCheckConstraint("CK_ST_StaffTransfers_Versions",
                "[CurrentVersion] >= 1 AND ([ConfirmedVersion] IS NULL OR ([ConfirmedVersion] >= 1 AND [ConfirmedVersion] <= [CurrentVersion]))");
            t.HasCheckConstraint("CK_ST_StaffTransfers_TransferType",
                "[TransferTypeId] BETWEEN 1 AND 7");
        });

        b.HasKey(x => x.StaffTransferId);
        b.Property(x => x.TransferReason).HasMaxLength(1000);
        b.Property(x => x.RequestedDate).HasColumnType("datetime2");
        b.Property(x => x.EffectiveDate).HasColumnType("date");
        b.Property(x => x.ReportingDate).HasColumnType("date");
        b.Property(x => x.SecondmentEndDate).HasColumnType("date");
        b.Property(x => x.ServiceContinuityTreatment).HasMaxLength(50);
        b.Property(x => x.LeaveBalanceTreatment).HasMaxLength(50);
        b.Property(x => x.AppointingAuthorityRef).HasMaxLength(100);
        b.Property(x => x.SourceHrReleasedAt).HasColumnType("datetime2");
        b.Property(x => x.Status).HasMaxLength(40);
        b.Property(x => x.MaterialStateFingerprint).HasMaxLength(64);
        b.Property(x => x.FullNameSnapshot).HasMaxLength(250);
        b.Property(x => x.EmployeeNumberSnapshot).HasMaxLength(50);
        b.Property(x => x.ProtectedIdCardSnapshot).HasMaxLength(100);
        b.Property(x => x.CurrentEmploymentStatusSnapshot).HasMaxLength(100);
        b.Property(x => x.EmploymentCategorySnapshot).HasMaxLength(100);
        b.Property(x => x.DateJoinedSnapshot).HasColumnType("date");
        b.Property(x => x.OfficialEmailSnapshot).HasMaxLength(128);
        b.Property(x => x.OfficialPhoneSnapshot).HasMaxLength(50);
        b.Property(x => x.StaffSnapshotJson).HasMaxLength(4000);
        b.Property(x => x.FinalDecisionRemarks).HasMaxLength(2000);
        b.Property(x => x.FinalApprovedAt).HasColumnType("datetime2");
        b.Property(x => x.ActivatedAt).HasColumnType("datetime2");
        b.Property(x => x.CreatedDate).HasColumnType("datetime2");
        b.Property(x => x.UpdatedDate).HasColumnType("datetime2");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.RequestId).IsUnique().HasFilter("[RequestId] IS NOT NULL");
        b.HasIndex(x => x.CurrentJobId).IsUnique().HasFilter("[IsOpen] = 1");
        b.HasIndex(x => new { x.FromOrganisationId, x.Status, x.EffectiveDate });
        b.HasIndex(x => new { x.ToOrganisationId, x.Status, x.EffectiveDate });
        b.HasIndex(x => new { x.IndividualId, x.EffectiveDate });

        b.HasOne(x => x.Request).WithOne().HasForeignKey<StaffTransfer>(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Individual).WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CurrentJob).WithMany().HasForeignKey(x => x.CurrentJobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.FromOrganisation).WithMany().HasForeignKey(x => x.FromOrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.FromOrganisationStructure).WithMany().HasForeignKey(x => x.FromOrganisationStructureId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ToOrganisation).WithMany().HasForeignKey(x => x.ToOrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ToOrganisationStructure).WithMany().HasForeignKey(x => x.ToOrganisationStructureId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SourceEmployer).WithMany().HasForeignKey(x => x.SourceEmployerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DestinationEmployer).WithMany().HasForeignKey(x => x.DestinationEmployerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SourceHrReleaseUser).WithMany().HasForeignKey(x => x.SourceHrReleaseUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.FinalApprovedByUser).WithMany().HasForeignKey(x => x.FinalApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CurrentSupervisorIndividual).WithMany().HasForeignKey(x => x.CurrentSupervisorIndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CurrentHodIndividual).WithMany().HasForeignKey(x => x.CurrentHodIndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OperationLog).WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<StaffTransferJobDetail> b)
    {
        b.ToTable("ST_StaffTransferJobDetails", t =>
            t.HasCheckConstraint("CK_ST_StaffTransferJobDetails_Mode",
                "[TransferAppointmentMode] IN ('OrganisationalReassignment','SameJobPositionChange','NewEmploymentAppointment')"));

        b.HasKey(x => x.StaffTransferJobDetailId);
        b.Property(x => x.ExistingPositionTitleSnapshot).HasMaxLength(250);
        b.Property(x => x.ExistingGradeLevelSnapshot).HasMaxLength(150);
        b.Property(x => x.ExistingBasicSalarySnapshot).HasPrecision(18, 2);
        b.Property(x => x.ExistingAllowanceSnapshot).HasPrecision(18, 2);
        b.Property(x => x.DestinationPositionTitleSnapshot).HasMaxLength(250);
        b.Property(x => x.DestinationGradeLevelSnapshot).HasMaxLength(150);
        b.Property(x => x.TransferAppointmentMode).HasMaxLength(40);
        b.Property(x => x.ProposedBasicSalary).HasPrecision(18, 2);
        b.Property(x => x.ProposedAllowance).HasPrecision(18, 2);
        b.Property(x => x.SalaryBenefitChangeNotes).HasMaxLength(1000);

        b.HasIndex(x => x.StaffTransferId).IsUnique();
        b.HasIndex(x => x.DestinationPositionMatrixId);

        b.HasOne(x => x.StaffTransfer).WithOne(x => x.JobDetail).HasForeignKey<StaffTransferJobDetail>(x => x.StaffTransferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ExistingJob).WithMany().HasForeignKey(x => x.ExistingJobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ExistingJobPosition).WithMany().HasForeignKey(x => x.ExistingJobPositionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ExistingPosition).WithMany().HasForeignKey(x => x.ExistingPositionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DestinationOrganisation).WithMany().HasForeignKey(x => x.DestinationOrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DestinationOrganisationStructure).WithMany().HasForeignKey(x => x.DestinationOrganisationStructureId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DestinationJobType).WithMany().HasForeignKey(x => x.DestinationJobTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DestinationPosition).WithMany().HasForeignKey(x => x.DestinationPositionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.NewReportingSupervisorIndividual).WithMany().HasForeignKey(x => x.NewReportingSupervisorIndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ResultingJob).WithMany().HasForeignKey(x => x.ResultingJobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ResultingJobPosition).WithMany().HasForeignKey(x => x.ResultingJobPositionId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<StaffTransferWorkDetail> b)
    {
        b.ToTable("ST_StaffTransferWorkDetails", t =>
            t.HasCheckConstraint("CK_ST_StaffTransferWorkDetails_Hours",
                "[WeeklyWorkingHours] > 0 AND [WeeklyWorkingHours] <= 168 AND [WorkingDaysMask] BETWEEN 0 AND 127"));

        b.HasKey(x => x.StaffTransferWorkDetailId);
        b.Property(x => x.CurrentLocationNameSnapshot).HasMaxLength(250);
        b.Property(x => x.ProposedLocationName).HasMaxLength(250);
        b.Property(x => x.CurrentScheduleMode).HasMaxLength(30);
        b.Property(x => x.ProposedScheduleMode).HasMaxLength(30);
        b.Property(x => x.StandardStartTime).HasColumnType("time");
        b.Property(x => x.StandardEndTime).HasColumnType("time");
        b.Property(x => x.WeeklyWorkingHours).HasPrecision(6, 2);
        b.Property(x => x.RequiredAttendanceMethod).HasMaxLength(50);
        b.Property(x => x.AttendanceDeviceLocationNotes).HasMaxLength(250);
        b.Property(x => x.WorkArrangementMode).HasMaxLength(30);
        b.Property(x => x.ScheduleEffectiveDate).HasColumnType("date");
        b.Property(x => x.WorkNotes).HasMaxLength(1000);

        b.HasIndex(x => x.StaffTransferId).IsUnique();

        b.HasOne(x => x.StaffTransfer).WithOne(x => x.WorkDetail).HasForeignKey<StaffTransferWorkDetail>(x => x.StaffTransferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CurrentLocation).WithMany().HasForeignKey(x => x.CurrentLocationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProposedLocation).WithMany().HasForeignKey(x => x.ProposedLocationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CurrentPlanningProvider).WithMany().HasForeignKey(x => x.CurrentPlanningProviderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProposedPlanningProvider).WithMany().HasForeignKey(x => x.ProposedPlanningProviderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CurrentWorkTemplate).WithMany().HasForeignKey(x => x.CurrentWorkTemplateId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProposedWorkTemplate).WithMany().HasForeignKey(x => x.ProposedWorkTemplateId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AttendanceDevice).WithMany().HasForeignKey(x => x.AttendanceDeviceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AttendanceVerificationSupervisorIndividual).WithMany().HasForeignKey(x => x.AttendanceVerificationSupervisorIndividualId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<StaffTransferAccessChange> b)
    {
        b.ToTable("ST_StaffTransferAccessChanges", t =>
            t.HasCheckConstraint("CK_ST_StaffTransferAccessChanges_Action",
                "[TransitionAction] IN ('Retain','Revoke','Grant','RevokeOldGrantNew','ReviewAndAssign','NoChange') AND [ProvisioningStatus] IN ('Pending','Applied','RequiresManualAuth','Revoked','Failed')"));

        b.HasKey(x => x.StaffTransferAccessChangeId);
        b.Property(x => x.AccessAreaCode).HasMaxLength(60);
        b.Property(x => x.AccessAreaName).HasMaxLength(150);
        b.Property(x => x.CurrentScopeSummary).HasMaxLength(500);
        b.Property(x => x.ProposedScopeSummary).HasMaxLength(500);
        b.Property(x => x.TransitionAction).HasMaxLength(30);
        b.Property(x => x.ProvisioningStatus).HasMaxLength(30);
        b.Property(x => x.AppliedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.Remarks).HasMaxLength(500);

        b.HasIndex(x => new { x.StaffTransferId, x.AccessAreaCode });

        b.HasOne(x => x.StaffTransfer).WithMany(x => x.AccessChanges).HasForeignKey(x => x.StaffTransferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.UserGroup).WithMany().HasForeignKey(x => x.UserGroupId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AppliedByUser).WithMany().HasForeignKey(x => x.AppliedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<StaffTransferDoorAccess> b)
    {
        b.ToTable("ST_StaffTransferDoorAccess", t =>
            t.HasCheckConstraint("CK_ST_StaffTransferDoorAccess_State",
                "[TransitionAction] IN ('RetainCommon','RevokeOld','GrantNew','Replace') AND [ProvisioningState] IN ('Pending','TaskGenerated','InProgress','Provisioned','Revoked','Verified','Failed')"));

        b.HasKey(x => x.StaffTransferDoorAccessId);
        b.Property(x => x.BuildingName).HasMaxLength(150);
        b.Property(x => x.FloorName).HasMaxLength(100);
        b.Property(x => x.DepartmentOfficeArea).HasMaxLength(150);
        b.Property(x => x.RestrictedRoomName).HasMaxLength(150);
        b.Property(x => x.StaffEntranceName).HasMaxLength(150);
        b.Property(x => x.AccessCardReference).HasMaxLength(100);
        b.Property(x => x.ExistingAccessGroups).HasMaxLength(500);
        b.Property(x => x.ProposedAccessGroups).HasMaxLength(500);
        b.Property(x => x.TransitionAction).HasMaxLength(30);
        b.Property(x => x.AccessStartDate).HasColumnType("date");
        b.Property(x => x.PreviousAccessExpiryDate).HasColumnType("date");
        b.Property(x => x.ConnectorReference).HasMaxLength(150);
        b.Property(x => x.ProvisioningState).HasMaxLength(30);
        b.Property(x => x.ProvisionedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.ProvisioningNotes).HasMaxLength(1000);

        b.HasIndex(x => new { x.StaffTransferId, x.ProvisioningState });

        b.HasOne(x => x.StaffTransfer).WithMany(x => x.DoorAccessChanges).HasForeignKey(x => x.StaffTransferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AttendanceDevice).WithMany().HasForeignKey(x => x.AttendanceDeviceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SecurityOfficerUser).WithMany().HasForeignKey(x => x.SecurityOfficerUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<StaffTransferReportingDetail> b)
    {
        b.ToTable("ST_StaffTransferReportingDetails", t =>
            t.HasCheckConstraint("CK_ST_StaffTransferReportingDetails_Routing",
                "[OutstandingApprovalsRoutingRule] IN ('RetainHistoricalReassignOpen','ReassignAllToSuccessor','EscalateToSendingHod','ManualReview')"));

        b.HasKey(x => x.StaffTransferReportingDetailId);
        b.Property(x => x.ApprovedTransferEffectiveDate).HasColumnType("date");
        b.Property(x => x.ReportingDate).HasColumnType("date");
        b.Property(x => x.ReportingTime).HasColumnType("time");
        b.Property(x => x.ReportingLocation).HasMaxLength(250);
        b.Property(x => x.ReportingToNameSnapshot).HasMaxLength(250);
        b.Property(x => x.HandoverDeadline).HasColumnType("date");
        b.Property(x => x.OutstandingAssignmentsSummary).HasMaxLength(2000);
        b.Property(x => x.AssetsToReturnSummary).HasMaxLength(2000);
        b.Property(x => x.AssetsToReceiveSummary).HasMaxLength(2000);
        b.Property(x => x.OutstandingApprovalsRoutingRule).HasMaxLength(40);
        b.Property(x => x.OutstandingApprovalsSummary).HasMaxLength(2000);
        b.Property(x => x.SpecialInstructions).HasMaxLength(2000);

        b.HasIndex(x => x.StaffTransferId).IsUnique();

        b.HasOne(x => x.StaffTransfer).WithOne(x => x.ReportingDetail).HasForeignKey<StaffTransferReportingDetail>(x => x.StaffTransferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReportingToIndividual).WithMany().HasForeignKey(x => x.ReportingToIndividualId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<StaffTransferComment> b)
    {
        b.ToTable("ST_StaffTransferComments", t =>
            t.HasCheckConstraint("CK_ST_StaffTransferComments_Kind",
                "[CommentKind] IN ('AdvisoryComment','Clarification','CorrectionRequest','ApprovalRemark','SystemAudit')"));

        b.HasKey(x => x.StaffTransferCommentId);
        b.Property(x => x.ParticipantRole).HasMaxLength(40);
        b.Property(x => x.CommentKind).HasMaxLength(30);
        b.Property(x => x.CommentText).HasMaxLength(2000);
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => new { x.StaffTransferId, x.CreatedAtUtc });

        b.HasOne(x => x.StaffTransfer).WithMany(x => x.Comments).HasForeignKey(x => x.StaffTransferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AuthorUser).WithMany().HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AuthorIndividual).WithMany().HasForeignKey(x => x.AuthorIndividualId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<StaffTransferConfirmation> b)
    {
        b.ToTable("ST_StaffTransferConfirmations", t =>
            t.HasCheckConstraint("CK_ST_StaffTransferConfirmations_Response",
                "[ResponseKind] IN ('Confirmed','CorrectionRequested','ClarificationSubmitted') AND [ConfirmedVersion] >= 1"));

        b.HasKey(x => x.StaffTransferConfirmationId);
        b.Property(x => x.ResponseKind).HasMaxLength(30);
        b.Property(x => x.ConfirmationStatement).HasMaxLength(1000);
        b.Property(x => x.EmployeeRemarks).HasMaxLength(2000);
        b.Property(x => x.InvalidatedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.ConfirmedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => new { x.StaffTransferId, x.ConfirmedVersion });

        b.HasOne(x => x.StaffTransfer).WithMany(x => x.Confirmations).HasForeignKey(x => x.StaffTransferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ConfirmedByUser).WithMany().HasForeignKey(x => x.ConfirmedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ConfirmedByIndividual).WithMany().HasForeignKey(x => x.ConfirmedByIndividualId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<StaffTransferActivationLog> b)
    {
        b.ToTable("ST_StaffTransferActivationLogs", t =>
            t.HasCheckConstraint("CK_ST_StaffTransferActivationLogs_Status",
                "[StepStatus] IN ('Pending','Succeeded','Skipped','Failed') AND [ExecutionAttempt] >= 1"));

        b.HasKey(x => x.StaffTransferActivationLogId);
        b.Property(x => x.StepCode).HasMaxLength(50);
        b.Property(x => x.StepStatus).HasMaxLength(25);
        b.Property(x => x.IdempotencyKey).HasMaxLength(120);
        b.Property(x => x.DetailsJson).HasMaxLength(4000);
        b.Property(x => x.ErrorMessage).HasMaxLength(2000);
        b.Property(x => x.ExecutedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => new { x.StaffTransferId, x.StepCode, x.IdempotencyKey })
            .IsUnique()
            .HasFilter("[StepStatus] = 'Succeeded'");

        b.HasOne(x => x.StaffTransfer).WithMany(x => x.ActivationLogs).HasForeignKey(x => x.StaffTransferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ExecutedByUser).WithMany().HasForeignKey(x => x.ExecutedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<StaffTransferDocument> b)
    {
        b.ToTable("ST_StaffTransferDocuments", t =>
            t.HasCheckConstraint("CK_ST_StaffTransferDocuments_Variant",
                "[DocumentVariant] IN ('EmployeeNotice','RestrictedAdminRecord')"));

        b.HasKey(x => x.StaffTransferDocumentId);
        b.Property(x => x.DocumentNumber).HasMaxLength(60);
        b.Property(x => x.DocumentVariant).HasMaxLength(30);
        b.Property(x => x.Sha256Hash).HasMaxLength(64);
        b.Property(x => x.ContentType).HasMaxLength(60);
        b.Property(x => x.GeneratedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => x.DocumentNumber).IsUnique();
        b.HasIndex(x => new { x.StaffTransferId, x.DocumentVariant, x.RequestVersion });

        b.HasOne(x => x.StaffTransfer).WithMany(x => x.Documents).HasForeignKey(x => x.StaffTransferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.GeneratedByUser).WithMany().HasForeignKey(x => x.GeneratedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<JobOrganisationHistory> b)
    {
        b.ToTable("ST_JobOrganisationHistories", t =>
            t.HasCheckConstraint("CK_ST_JobOrganisationHistories_Dates",
                "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]"));

        b.HasKey(x => x.JobOrganisationHistoryId);
        b.Property(x => x.EffectiveFrom).HasColumnType("date");
        b.Property(x => x.EffectiveTo).HasColumnType("date");
        b.Property(x => x.ChangeReason).HasMaxLength(500);
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => new { x.JobId, x.EffectiveFrom });
        b.HasIndex(x => new { x.OrganisationId, x.OrganisationStructureId, x.EffectiveFrom });

        b.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Individual).WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OrganisationStructure).WithMany().HasForeignKey(x => x.OrganisationStructureId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StaffTransfer).WithMany().HasForeignKey(x => x.StaffTransferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OperationLog).WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<StaffTransferClearanceTask> b)
    {
        b.ToTable("ST_StaffTransferClearanceTasks");
        b.HasKey(x => x.ClearanceTaskId);
        b.Property(x => x.ClearanceDomain).HasMaxLength(50);
        b.Property(x => x.TaskTitle).HasMaxLength(250);
        b.Property(x => x.TaskDescription).HasMaxLength(1000);
        b.Property(x => x.Status).HasMaxLength(40);
        b.Property(x => x.WaiverReason).HasMaxLength(1000);
        b.Property(x => x.Remarks).HasMaxLength(1000);
        b.Property(x => x.ClearedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.StaffTransferId, x.ClearanceDomain, x.Status });

        b.HasOne(x => x.StaffTransfer).WithMany(x => x.ClearanceTasks).HasForeignKey(x => x.StaffTransferId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.AssignedToUser).WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssignedToOrganisationStructure).WithMany().HasForeignKey(x => x.AssignedToOrganisationStructureId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ClearedByUser).WithMany().HasForeignKey(x => x.ClearedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<StaffTransferAssetDecision> b)
    {
        b.ToTable("ST_StaffTransferAssetDecisions");
        b.HasKey(x => x.TransferAssetDecisionId);
        b.Property(x => x.AssetDecision).HasMaxLength(50);
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.VerifiedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.StaffTransferId, x.AssetCustodyId });

        b.HasOne(x => x.StaffTransfer).WithMany(x => x.AssetDecisions).HasForeignKey(x => x.StaffTransferId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.VerifiedByUser).WithMany().HasForeignKey(x => x.VerifiedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<StaffTransferFinanceSettlement> b)
    {
        b.ToTable("ST_StaffTransferFinanceSettlements");
        b.HasKey(x => x.FinanceSettlementId);
        b.Property(x => x.LastWorkingDateAtSource).HasColumnType("date");
        b.Property(x => x.LastPayrollCutoffDate).HasColumnType("date");
        b.Property(x => x.OutstandingAdvanceDeduction).HasPrecision(18, 2);
        b.Property(x => x.UnencashedLeaveDays).HasPrecision(9, 2);
        b.Property(x => x.LeaveEncashmentAmount).HasPrecision(18, 2);
        b.Property(x => x.GratuitySeveranceAmount).HasPrecision(18, 2);
        b.Property(x => x.SettlementMode).HasMaxLength(50);
        b.Property(x => x.ClearedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.Remarks).HasMaxLength(1000);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.StaffTransferId).IsUnique();

        b.HasOne(x => x.StaffTransfer).WithOne(x => x.FinanceSettlement).HasForeignKey<StaffTransferFinanceSettlement>(x => x.StaffTransferId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.FinanceOfficerUser).WithMany().HasForeignKey(x => x.FinanceOfficerUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<StaffTransferEmploymentExecution> b)
    {
        b.ToTable("ST_StaffTransferEmploymentExecutions");
        b.HasKey(x => x.ExecutionId);
        b.Property(x => x.EffectiveDate).HasColumnType("date");
        b.Property(x => x.IdempotencyKey).HasMaxLength(120);
        b.Property(x => x.ExecutedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => new { x.StaffTransferId, x.ExecutedAtUtc });

        b.HasOne(x => x.StaffTransfer).WithMany(x => x.EmploymentExecutions).HasForeignKey(x => x.StaffTransferId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.SourceJob).WithMany().HasForeignKey(x => x.SourceJobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DestinationJob).WithMany().HasForeignKey(x => x.DestinationJobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DestinationJobPosition).WithMany().HasForeignKey(x => x.DestinationJobPositionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public partial class HrmTeContext
{
    public DbSet<StaffTransfer> StaffTransfers => Set<StaffTransfer>();
    public DbSet<StaffTransferJobDetail> StaffTransferJobDetails => Set<StaffTransferJobDetail>();
    public DbSet<StaffTransferWorkDetail> StaffTransferWorkDetails => Set<StaffTransferWorkDetail>();
    public DbSet<StaffTransferAccessChange> StaffTransferAccessChanges => Set<StaffTransferAccessChange>();
    public DbSet<StaffTransferDoorAccess> StaffTransferDoorAccesses => Set<StaffTransferDoorAccess>();
    public DbSet<StaffTransferReportingDetail> StaffTransferReportingDetails => Set<StaffTransferReportingDetail>();
    public DbSet<StaffTransferComment> StaffTransferComments => Set<StaffTransferComment>();
    public DbSet<StaffTransferConfirmation> StaffTransferConfirmations => Set<StaffTransferConfirmation>();
    public DbSet<StaffTransferActivationLog> StaffTransferActivationLogs => Set<StaffTransferActivationLog>();
    public DbSet<StaffTransferDocument> StaffTransferDocuments => Set<StaffTransferDocument>();
    public DbSet<JobOrganisationHistory> JobOrganisationHistories => Set<JobOrganisationHistory>();
    public DbSet<StaffTransferClearanceTask> StaffTransferClearanceTasks => Set<StaffTransferClearanceTask>();
    public DbSet<StaffTransferAssetDecision> StaffTransferAssetDecisions => Set<StaffTransferAssetDecision>();
    public DbSet<StaffTransferFinanceSettlement> StaffTransferFinanceSettlements => Set<StaffTransferFinanceSettlement>();
    public DbSet<StaffTransferEmploymentExecution> StaffTransferEmploymentExecutions => Set<StaffTransferEmploymentExecution>();
}
