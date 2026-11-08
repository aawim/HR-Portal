using System;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(HrmTeContext))]
    [Migration("20261103001800_InternalStaffTransferManagement")]
    public partial class InternalStaffTransferManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ST_StaffTransfers",
                columns: table => new
                {
                    StaffTransferId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: true),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    CurrentJobId = table.Column<int>(type: "int", nullable: false),
                    FromOrganisationId = table.Column<int>(type: "int", nullable: false),
                    FromOrganisationStructureId = table.Column<int>(type: "int", nullable: false),
                    ToOrganisationId = table.Column<int>(type: "int", nullable: false),
                    ToOrganisationStructureId = table.Column<int>(type: "int", nullable: false),
                    TransferTypeId = table.Column<int>(type: "int", nullable: false),
                    TransferReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "date", nullable: false),
                    ReportingDate = table.Column<DateTime>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false),
                    CurrentVersion = table.Column<int>(type: "int", nullable: false),
                    ConfirmedVersion = table.Column<int>(type: "int", nullable: true),
                    MaterialStateFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FullNameSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    EmployeeNumberSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProtectedIdCardSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CurrentEmploymentStatusSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmploymentCategorySnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateJoinedSnapshot = table.Column<DateTime>(type: "date", nullable: false),
                    OfficialEmailSnapshot = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    OfficialPhoneSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CurrentSupervisorIndividualId = table.Column<int>(type: "int", nullable: true),
                    CurrentHodIndividualId = table.Column<int>(type: "int", nullable: true),
                    StaffSnapshotJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FinalApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    FinalApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalDecisionRemarks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OperationLogId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ST_StaffTransfers", x => x.StaffTransferId);
                    table.CheckConstraint("CK_ST_StaffTransfers_Status", "[Status] IN ('Draft','Submitted','PendingStaffConfirmation','CorrectionRequested','PendingReceivingApproval','Rejected','Approved','Scheduled','ActivationInProgress','Active','ActivationFailed','Cancelled')");
                    table.CheckConstraint("CK_ST_StaffTransfers_TransferType", "[TransferTypeId] BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_ST_StaffTransfers_Versions", "[CurrentVersion] >= 1 AND ([ConfirmedVersion] IS NULL OR ([ConfirmedVersion] >= 1 AND [ConfirmedVersion] <= [CurrentVersion]))");
                    table.ForeignKey(
                        name: "FK_ST_StaffTransfers_BE_Individuals_CurrentHodIndividualId",
                        column: x => x.CurrentHodIndividualId,
                        principalTable: "BE_Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransfers_BE_Individuals_CurrentSupervisorIndividualId",
                        column: x => x.CurrentSupervisorIndividualId,
                        principalTable: "BE_Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransfers_BE_Individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalTable: "BE_Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransfers_Jobs_CurrentJobId",
                        column: x => x.CurrentJobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransfers_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransfers_OrganisationStructure_FromOrganisationStructureId",
                        column: x => x.FromOrganisationStructureId,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransfers_OrganisationStructure_ToOrganisationStructureId",
                        column: x => x.ToOrganisationStructureId,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransfers_Organisations_FromOrganisationId",
                        column: x => x.FromOrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransfers_Organisations_ToOrganisationId",
                        column: x => x.ToOrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransfers_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransfers_Users_FinalApprovedByUserId",
                        column: x => x.FinalApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransfers_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ST_StaffTransferJobDetails",
                columns: table => new
                {
                    StaffTransferJobDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffTransferId = table.Column<int>(type: "int", nullable: false),
                    ExistingJobId = table.Column<int>(type: "int", nullable: false),
                    ExistingJobPositionId = table.Column<int>(type: "int", nullable: true),
                    ExistingPositionId = table.Column<int>(type: "int", nullable: true),
                    ExistingPositionTitleSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ExistingPositionFrameworkId = table.Column<int>(type: "int", nullable: true),
                    ExistingPositionMatrixId = table.Column<int>(type: "int", nullable: true),
                    ExistingJobMatrixId = table.Column<int>(type: "int", nullable: true),
                    ExistingJobMatrixStepId = table.Column<int>(type: "int", nullable: true),
                    ExistingGradeLevelSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ExistingBasicSalarySnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ExistingAllowanceSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DestinationOrganisationId = table.Column<int>(type: "int", nullable: false),
                    DestinationOrganisationStructureId = table.Column<int>(type: "int", nullable: false),
                    DestinationJobTypeId = table.Column<int>(type: "int", nullable: true),
                    DestinationPositionId = table.Column<int>(type: "int", nullable: true),
                    DestinationPositionTitleSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DestinationPositionFrameworkId = table.Column<int>(type: "int", nullable: true),
                    DestinationPositionMatrixId = table.Column<int>(type: "int", nullable: true),
                    DestinationJobMatrixId = table.Column<int>(type: "int", nullable: true),
                    DestinationJobMatrixStepId = table.Column<int>(type: "int", nullable: true),
                    DestinationGradeLevelSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TransferAppointmentMode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    HasSalaryBenefitChange = table.Column<bool>(type: "bit", nullable: false),
                    ProposedBasicSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ProposedAllowance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SalaryBenefitChangeNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NewReportingSupervisorIndividualId = table.Column<int>(type: "int", nullable: true),
                    ResultingJobId = table.Column<int>(type: "int", nullable: true),
                    ResultingJobPositionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ST_StaffTransferJobDetails", x => x.StaffTransferJobDetailId);
                    table.CheckConstraint("CK_ST_StaffTransferJobDetails_Mode", "[TransferAppointmentMode] IN ('OrganisationalReassignment','SameJobPositionChange','NewEmploymentAppointment')");
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferJobDetails_BE_Individuals_NewReportingSupervisorIndividualId",
                        column: x => x.NewReportingSupervisorIndividualId,
                        principalTable: "BE_Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferJobDetails_JobPositions_ExistingJobPositionId",
                        column: x => x.ExistingJobPositionId,
                        principalTable: "JobPositions",
                        principalColumn: "JobPositionID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferJobDetails_JobPositions_ResultingJobPositionId",
                        column: x => x.ResultingJobPositionId,
                        principalTable: "JobPositions",
                        principalColumn: "JobPositionID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferJobDetails_JobTypes_DestinationJobTypeId",
                        column: x => x.DestinationJobTypeId,
                        principalTable: "JobTypes",
                        principalColumn: "JobTypeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferJobDetails_Jobs_ExistingJobId",
                        column: x => x.ExistingJobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferJobDetails_Jobs_ResultingJobId",
                        column: x => x.ResultingJobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferJobDetails_OrganisationStructure_DestinationOrganisationStructureId",
                        column: x => x.DestinationOrganisationStructureId,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferJobDetails_Organisations_DestinationOrganisationId",
                        column: x => x.DestinationOrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferJobDetails_Positions_DestinationPositionId",
                        column: x => x.DestinationPositionId,
                        principalTable: "Positions",
                        principalColumn: "PositionID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferJobDetails_Positions_ExistingPositionId",
                        column: x => x.ExistingPositionId,
                        principalTable: "Positions",
                        principalColumn: "PositionID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferJobDetails_ST_StaffTransfers_StaffTransferId",
                        column: x => x.StaffTransferId,
                        principalTable: "ST_StaffTransfers",
                        principalColumn: "StaffTransferId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ST_StaffTransferWorkDetails",
                columns: table => new
                {
                    StaffTransferWorkDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffTransferId = table.Column<int>(type: "int", nullable: false),
                    CurrentLocationId = table.Column<int>(type: "int", nullable: true),
                    CurrentLocationNameSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ProposedLocationId = table.Column<int>(type: "int", nullable: true),
                    ProposedLocationName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CurrentPlanningProviderId = table.Column<int>(type: "int", nullable: true),
                    ProposedPlanningProviderId = table.Column<int>(type: "int", nullable: true),
                    CurrentWorkTemplateId = table.Column<int>(type: "int", nullable: true),
                    ProposedWorkTemplateId = table.Column<int>(type: "int", nullable: true),
                    CurrentScheduleMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ProposedScheduleMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WorkingDaysMask = table.Column<int>(type: "int", nullable: false),
                    StandardStartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    StandardEndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    WeeklyWorkingHours = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    RequiredAttendanceMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AttendanceDeviceId = table.Column<int>(type: "int", nullable: true),
                    AttendanceDeviceLocationNotes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsOvertimeEligible = table.Column<bool>(type: "bit", nullable: false),
                    WorkArrangementMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AttendanceVerificationSupervisorIndividualId = table.Column<int>(type: "int", nullable: true),
                    ScheduleEffectiveDate = table.Column<DateTime>(type: "date", nullable: false),
                    ReconcileFutureWorkPlans = table.Column<bool>(type: "bit", nullable: false),
                    WorkNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ST_StaffTransferWorkDetails", x => x.StaffTransferWorkDetailId);
                    table.CheckConstraint("CK_ST_StaffTransferWorkDetails_Hours", "[WeeklyWorkingHours] > 0 AND [WeeklyWorkingHours] <= 168 AND [WorkingDaysMask] BETWEEN 0 AND 127");
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferWorkDetails_AttendanceDevices_AttendanceDeviceId",
                        column: x => x.AttendanceDeviceId,
                        principalTable: "AttendanceDevices",
                        principalColumn: "AttendanceDeviceID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferWorkDetails_BE_Individuals_AttendanceVerificationSupervisorIndividualId",
                        column: x => x.AttendanceVerificationSupervisorIndividualId,
                        principalTable: "BE_Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferWorkDetails_Locations_CurrentLocationId",
                        column: x => x.CurrentLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferWorkDetails_Locations_ProposedLocationId",
                        column: x => x.ProposedLocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferWorkDetails_PlanningProviders_CurrentPlanningProviderId",
                        column: x => x.CurrentPlanningProviderId,
                        principalTable: "PlanningProviders",
                        principalColumn: "PlanningProviderID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferWorkDetails_PlanningProviders_ProposedPlanningProviderId",
                        column: x => x.ProposedPlanningProviderId,
                        principalTable: "PlanningProviders",
                        principalColumn: "PlanningProviderID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferWorkDetails_ST_StaffTransfers_StaffTransferId",
                        column: x => x.StaffTransferId,
                        principalTable: "ST_StaffTransfers",
                        principalColumn: "StaffTransferId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferWorkDetails_WorkTemplates_CurrentWorkTemplateId",
                        column: x => x.CurrentWorkTemplateId,
                        principalTable: "WorkTemplates",
                        principalColumn: "WorkTemplateID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferWorkDetails_WorkTemplates_ProposedWorkTemplateId",
                        column: x => x.ProposedWorkTemplateId,
                        principalTable: "WorkTemplates",
                        principalColumn: "WorkTemplateID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ST_StaffTransferAccessChanges",
                columns: table => new
                {
                    StaffTransferAccessChangeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffTransferId = table.Column<int>(type: "int", nullable: false),
                    AccessAreaCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    AccessAreaName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CurrentScopeSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProposedScopeSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TransitionAction = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    UserGroupId = table.Column<int>(type: "int", nullable: true),
                    RemainsActiveInOldDepartmentWarning = table.Column<bool>(type: "bit", nullable: false),
                    RequiresSeparateSecurityAuth = table.Column<bool>(type: "bit", nullable: false),
                    ProvisioningStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppliedByUserId = table.Column<int>(type: "int", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ST_StaffTransferAccessChanges", x => x.StaffTransferAccessChangeId);
                    table.CheckConstraint("CK_ST_StaffTransferAccessChanges_Action", "[TransitionAction] IN ('Retain','Revoke','Grant','RevokeOldGrantNew','ReviewAndAssign','NoChange') AND [ProvisioningStatus] IN ('Pending','Applied','RequiresManualAuth','Revoked','Failed')");
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferAccessChanges_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferAccessChanges_ST_StaffTransfers_StaffTransferId",
                        column: x => x.StaffTransferId,
                        principalTable: "ST_StaffTransfers",
                        principalColumn: "StaffTransferId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferAccessChanges_UserGroups_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "UserGroups",
                        principalColumn: "UserGroupID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferAccessChanges_Users_AppliedByUserId",
                        column: x => x.AppliedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ST_StaffTransferDoorAccess",
                columns: table => new
                {
                    StaffTransferDoorAccessId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffTransferId = table.Column<int>(type: "int", nullable: false),
                    BuildingName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FloorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DepartmentOfficeArea = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RestrictedRoomName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IncludesServerRoom = table.Column<bool>(type: "bit", nullable: false),
                    StaffEntranceName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AttendanceDeviceId = table.Column<int>(type: "int", nullable: true),
                    AccessCardReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExistingAccessGroups = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProposedAccessGroups = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TransitionAction = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AccessStartDate = table.Column<DateTime>(type: "date", nullable: false),
                    PreviousAccessExpiryDate = table.Column<DateTime>(type: "date", nullable: true),
                    SecurityOfficerUserId = table.Column<int>(type: "int", nullable: true),
                    ConnectorReference = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ProvisioningState = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProvisionedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProvisioningNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ST_StaffTransferDoorAccess", x => x.StaffTransferDoorAccessId);
                    table.CheckConstraint("CK_ST_StaffTransferDoorAccess_State", "[TransitionAction] IN ('RetainCommon','RevokeOld','GrantNew','Replace') AND [ProvisioningState] IN ('Pending','TaskGenerated','InProgress','Provisioned','Revoked','Verified','Failed')");
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferDoorAccess_AttendanceDevices_AttendanceDeviceId",
                        column: x => x.AttendanceDeviceId,
                        principalTable: "AttendanceDevices",
                        principalColumn: "AttendanceDeviceID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferDoorAccess_ST_StaffTransfers_StaffTransferId",
                        column: x => x.StaffTransferId,
                        principalTable: "ST_StaffTransfers",
                        principalColumn: "StaffTransferId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferDoorAccess_Users_SecurityOfficerUserId",
                        column: x => x.SecurityOfficerUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ST_StaffTransferReportingDetails",
                columns: table => new
                {
                    StaffTransferReportingDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffTransferId = table.Column<int>(type: "int", nullable: false),
                    ApprovedTransferEffectiveDate = table.Column<DateTime>(type: "date", nullable: false),
                    ReportingDate = table.Column<DateTime>(type: "date", nullable: false),
                    ReportingTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    ReportingLocation = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ReportingToIndividualId = table.Column<int>(type: "int", nullable: true),
                    ReportingToNameSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    HandoverRequired = table.Column<bool>(type: "bit", nullable: false),
                    HandoverDeadline = table.Column<DateTime>(type: "date", nullable: true),
                    OutstandingAssignmentsSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AssetsToReturnSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AssetsToReceiveSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OutstandingApprovalsRoutingRule = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OutstandingApprovalsSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SpecialInstructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ST_StaffTransferReportingDetails", x => x.StaffTransferReportingDetailId);
                    table.CheckConstraint("CK_ST_StaffTransferReportingDetails_Routing", "[OutstandingApprovalsRoutingRule] IN ('RetainHistoricalReassignOpen','ReassignAllToSuccessor','EscalateToSendingHod','ManualReview')");
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferReportingDetails_BE_Individuals_ReportingToIndividualId",
                        column: x => x.ReportingToIndividualId,
                        principalTable: "BE_Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferReportingDetails_ST_StaffTransfers_StaffTransferId",
                        column: x => x.StaffTransferId,
                        principalTable: "ST_StaffTransfers",
                        principalColumn: "StaffTransferId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ST_StaffTransferComments",
                columns: table => new
                {
                    StaffTransferCommentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffTransferId = table.Column<int>(type: "int", nullable: false),
                    AuthorUserId = table.Column<int>(type: "int", nullable: false),
                    AuthorIndividualId = table.Column<int>(type: "int", nullable: true),
                    ParticipantRole = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CommentKind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestVersion = table.Column<int>(type: "int", nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsVisibleToEmployee = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ST_StaffTransferComments", x => x.StaffTransferCommentId);
                    table.CheckConstraint("CK_ST_StaffTransferComments_Kind", "[CommentKind] IN ('AdvisoryComment','Clarification','CorrectionRequest','ApprovalRemark','SystemAudit')");
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferComments_BE_Individuals_AuthorIndividualId",
                        column: x => x.AuthorIndividualId,
                        principalTable: "BE_Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferComments_ST_StaffTransfers_StaffTransferId",
                        column: x => x.StaffTransferId,
                        principalTable: "ST_StaffTransfers",
                        principalColumn: "StaffTransferId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferComments_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ST_StaffTransferConfirmations",
                columns: table => new
                {
                    StaffTransferConfirmationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffTransferId = table.Column<int>(type: "int", nullable: false),
                    ConfirmedVersion = table.Column<int>(type: "int", nullable: false),
                    ResponseKind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ConfirmedByUserId = table.Column<int>(type: "int", nullable: false),
                    ConfirmedByIndividualId = table.Column<int>(type: "int", nullable: false),
                    ConfirmationStatement = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EmployeeRemarks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReviewedSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsInvalidatedByMaterialEdit = table.Column<bool>(type: "bit", nullable: false),
                    InvalidatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ST_StaffTransferConfirmations", x => x.StaffTransferConfirmationId);
                    table.CheckConstraint("CK_ST_StaffTransferConfirmations_Response", "[ResponseKind] IN ('Confirmed','CorrectionRequested','ClarificationSubmitted') AND [ConfirmedVersion] >= 1");
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferConfirmations_BE_Individuals_ConfirmedByIndividualId",
                        column: x => x.ConfirmedByIndividualId,
                        principalTable: "BE_Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferConfirmations_ST_StaffTransfers_StaffTransferId",
                        column: x => x.StaffTransferId,
                        principalTable: "ST_StaffTransfers",
                        principalColumn: "StaffTransferId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferConfirmations_Users_ConfirmedByUserId",
                        column: x => x.ConfirmedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ST_StaffTransferActivationLogs",
                columns: table => new
                {
                    StaffTransferActivationLogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffTransferId = table.Column<int>(type: "int", nullable: false),
                    ExecutionAttempt = table.Column<int>(type: "int", nullable: false),
                    StepCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StepStatus = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DetailsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExecutedByUserId = table.Column<int>(type: "int", nullable: true),
                    ExecutedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ST_StaffTransferActivationLogs", x => x.StaffTransferActivationLogId);
                    table.CheckConstraint("CK_ST_StaffTransferActivationLogs_Status", "[StepStatus] IN ('Pending','Succeeded','Skipped','Failed') AND [ExecutionAttempt] >= 1");
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferActivationLogs_ST_StaffTransfers_StaffTransferId",
                        column: x => x.StaffTransferId,
                        principalTable: "ST_StaffTransfers",
                        principalColumn: "StaffTransferId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferActivationLogs_Users_ExecutedByUserId",
                        column: x => x.ExecutedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ST_StaffTransferDocuments",
                columns: table => new
                {
                    StaffTransferDocumentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffTransferId = table.Column<int>(type: "int", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    DocumentVariant = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestVersion = table.Column<int>(type: "int", nullable: false),
                    ConfirmedVersion = table.Column<int>(type: "int", nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sha256Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PdfBytes = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    GeneratedByUserId = table.Column<int>(type: "int", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ST_StaffTransferDocuments", x => x.StaffTransferDocumentId);
                    table.CheckConstraint("CK_ST_StaffTransferDocuments_Variant", "[DocumentVariant] IN ('EmployeeNotice','RestrictedAdminRecord')");
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferDocuments_ST_StaffTransfers_StaffTransferId",
                        column: x => x.StaffTransferId,
                        principalTable: "ST_StaffTransfers",
                        principalColumn: "StaffTransferId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_StaffTransferDocuments_Users_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ST_JobOrganisationHistories",
                columns: table => new
                {
                    JobOrganisationHistoryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    OrganisationStructureId = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "date", nullable: true),
                    StaffTransferId = table.Column<int>(type: "int", nullable: true),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ST_JobOrganisationHistories", x => x.JobOrganisationHistoryId);
                    table.CheckConstraint("CK_ST_JobOrganisationHistories_Dates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_ST_JobOrganisationHistories_BE_Individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalTable: "BE_Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_JobOrganisationHistories_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_JobOrganisationHistories_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_JobOrganisationHistories_OrganisationStructure_OrganisationStructureId",
                        column: x => x.OrganisationStructureId,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_JobOrganisationHistories_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ST_JobOrganisationHistories_ST_StaffTransfers_StaffTransferId",
                        column: x => x.StaffTransferId,
                        principalTable: "ST_StaffTransfers",
                        principalColumn: "StaffTransferId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ST_StaffTransfers_CurrentJobId",
                table: "ST_StaffTransfers",
                column: "CurrentJobId",
                unique: true,
                filter: "[IsOpen] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ST_StaffTransfers_FromOrganisationId_Status_EffectiveDate",
                table: "ST_StaffTransfers",
                columns: new[] { "FromOrganisationId", "Status", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ST_StaffTransfers_IndividualId_EffectiveDate",
                table: "ST_StaffTransfers",
                columns: new[] { "IndividualId", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ST_StaffTransfers_RequestId",
                table: "ST_StaffTransfers",
                column: "RequestId",
                unique: true,
                filter: "[RequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ST_StaffTransfers_ToOrganisationId_Status_EffectiveDate",
                table: "ST_StaffTransfers",
                columns: new[] { "ToOrganisationId", "Status", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ST_StaffTransferJobDetails_DestinationPositionMatrixId",
                table: "ST_StaffTransferJobDetails",
                column: "DestinationPositionMatrixId");

            migrationBuilder.CreateIndex(
                name: "IX_ST_StaffTransferJobDetails_StaffTransferId",
                table: "ST_StaffTransferJobDetails",
                column: "StaffTransferId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ST_StaffTransferWorkDetails_StaffTransferId",
                table: "ST_StaffTransferWorkDetails",
                column: "StaffTransferId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ST_StaffTransferAccessChanges_StaffTransferId_AccessAreaCode",
                table: "ST_StaffTransferAccessChanges",
                columns: new[] { "StaffTransferId", "AccessAreaCode" });

            migrationBuilder.CreateIndex(
                name: "IX_ST_StaffTransferDoorAccess_StaffTransferId_ProvisioningState",
                table: "ST_StaffTransferDoorAccess",
                columns: new[] { "StaffTransferId", "ProvisioningState" });

            migrationBuilder.CreateIndex(
                name: "IX_ST_StaffTransferReportingDetails_StaffTransferId",
                table: "ST_StaffTransferReportingDetails",
                column: "StaffTransferId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ST_StaffTransferComments_StaffTransferId_CreatedAtUtc",
                table: "ST_StaffTransferComments",
                columns: new[] { "StaffTransferId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ST_StaffTransferConfirmations_StaffTransferId_ConfirmedVersion",
                table: "ST_StaffTransferConfirmations",
                columns: new[] { "StaffTransferId", "ConfirmedVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_ST_StaffTransferActivationLogs_StaffTransferId_StepCode_IdempotencyKey",
                table: "ST_StaffTransferActivationLogs",
                columns: new[] { "StaffTransferId", "StepCode", "IdempotencyKey" },
                unique: true,
                filter: "[StepStatus] = 'Succeeded'");

            migrationBuilder.CreateIndex(
                name: "IX_ST_StaffTransferDocuments_DocumentNumber",
                table: "ST_StaffTransferDocuments",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ST_StaffTransferDocuments_StaffTransferId_DocumentVariant_RequestVersion",
                table: "ST_StaffTransferDocuments",
                columns: new[] { "StaffTransferId", "DocumentVariant", "RequestVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_ST_JobOrganisationHistories_JobId_EffectiveFrom",
                table: "ST_JobOrganisationHistories",
                columns: new[] { "JobId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ST_JobOrganisationHistories_OrganisationId_OrganisationStructureId_EffectiveFrom",
                table: "ST_JobOrganisationHistories",
                columns: new[] { "OrganisationId", "OrganisationStructureId", "EffectiveFrom" });

            migrationBuilder.Sql("""
                IF NOT EXISTS(SELECT 1 FROM dbo.RequestTypes WHERE TypeName=N'Internal Staff Transfer Request')
                BEGIN
                    INSERT dbo.SequenceNumberTypes(TypeName,Prefix,FormatString,IsResetAnnually,StartingNumber,NextNumber)
                    VALUES(N'Staff Transfers',N'STR',N'{PREFIX}-{YEAR}-{NUMBER}',0,1,1);
                    DECLARE @TransferSequence int=CONVERT(int,SCOPE_IDENTITY());
                    INSERT dbo.RequestTypes(TypeName,SequenceNumberTypeID,ViewURL,IsSystemType)
                    VALUES(N'Internal Staff Transfer Request',@TransferSequence,N'/admin/hr/staff-transfers',1);
                END;
                INSERT dbo.RequestTypesAllowedRequestStateTransitions(RequestTypeID,FromRequestStateID,ToRequestStateID)
                SELECT t.RequestTypeID, f.RequestStateID, s.RequestStateID
                FROM dbo.RequestTypes t CROSS JOIN dbo.RequestStates f CROSS JOIN dbo.RequestStates s
                WHERE t.TypeName = N'Internal Staff Transfer Request'
                  AND (f.RequestStateID=1 OR f.IsProcessingState=1 AND f.IsFinalState=0)
                  AND (s.IsProcessingState=1 AND s.IsFinalState=0 OR s.RequestStateID IN (4,5,6))
                  AND f.RequestStateID<>s.RequestStateID
                  AND NOT EXISTS(SELECT 1 FROM dbo.RequestTypesAllowedRequestStateTransitions x
                      WHERE x.RequestTypeID=t.RequestTypeID AND x.FromRequestStateID=f.RequestStateID AND x.ToRequestStateID=s.RequestStateID);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Internal staff transfer history and confirmed employment records must be retained.");
        }
    }
}
