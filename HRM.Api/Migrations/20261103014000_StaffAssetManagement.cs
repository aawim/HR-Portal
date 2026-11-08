using System;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(HrmTeContext))]
    [Migration("20261103014000_StaffAssetManagement")]
    public partial class StaffAssetManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AM_AssetCategories",
                columns: table => new
                {
                    AssetCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RequiresSerialNumber = table.Column<bool>(type: "bit", nullable: false),
                    RequiresReturn = table.Column<bool>(type: "bit", nullable: false),
                    IsSecurityItem = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_AssetCategories", x => x.AssetCategoryId);
                    table.ForeignKey(
                        name: "FK_AM_AssetCategories_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetCategories_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AM_AssetConditions",
                columns: table => new
                {
                    AssetConditionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequiresMaintenanceOnReturn = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_AssetConditions", x => x.AssetConditionId);
                });

            migrationBuilder.CreateTable(
                name: "AM_AssetItems",
                columns: table => new
                {
                    AssetItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    AssetCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    OwnerOrganisationId = table.Column<int>(type: "int", nullable: false),
                    OwnerStructureId = table.Column<int>(type: "int", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrentConditionId = table.Column<int>(type: "int", nullable: false),
                    WarrantyExpiryDate = table.Column<DateTime>(type: "date", nullable: true),
                    DefaultAccessories = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_AssetItems", x => x.AssetItemId);
                    table.CheckConstraint("CK_AM_AssetItems_Status", "[Status] IN ('Available','Reserved','PendingHandover','Assigned','ReturnPending','Maintenance','Lost','Disposed')");
                    table.ForeignKey(
                        name: "FK_AM_AssetItems_AM_AssetCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "AM_AssetCategories",
                        principalColumn: "AssetCategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetItems_AM_AssetConditions_CurrentConditionId",
                        column: x => x.CurrentConditionId,
                        principalTable: "AM_AssetConditions",
                        principalColumn: "AssetConditionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetItems_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetItems_OrganisationStructure_OwnerStructureId",
                        column: x => x.OwnerStructureId,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetItems_Organisations_OwnerOrganisationId",
                        column: x => x.OwnerOrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AM_AssetIssuances",
                columns: table => new
                {
                    AssetIssuanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    StructureId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "date", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalizedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalizedByUserId = table.Column<int>(type: "int", nullable: true),
                    FinalizationIdempotencyKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    EmployeeNameSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    StaffNumberSnapshot = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    PositionTitleSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DepartmentNameSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OrganisationNameSnapshot = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_AssetIssuances", x => x.AssetIssuanceId);
                    table.CheckConstraint("CK_AM_AssetIssuances_Status", "[Status] IN ('Draft','AwaitingHandover','PendingStaffAcknowledgement','PendingHRApproval','ReturnedForCorrection','Completed','PartiallyCompleted','Cancelled')");
                    table.ForeignKey(
                        name: "FK_AM_AssetIssuances_BE_Individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalTable: "BE_Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetIssuances_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetIssuances_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetIssuances_OrganisationStructure_StructureId",
                        column: x => x.StructureId,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetIssuances_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetIssuances_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetIssuances_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetIssuances_Users_FinalizedByUserId",
                        column: x => x.FinalizedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AM_AssetIssuanceItems",
                columns: table => new
                {
                    AssetIssuanceItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssuanceId = table.Column<int>(type: "int", nullable: false),
                    AssetItemId = table.Column<int>(type: "int", nullable: false),
                    ConditionAtIssueId = table.Column<int>(type: "int", nullable: false),
                    Accessories = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HandedOverAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HandedOverByUserId = table.Column<int>(type: "int", nullable: true),
                    HandoverNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsActiveReservation = table.Column<bool>(type: "bit", nullable: false),
                    ReplacedFromIssuanceItemId = table.Column<int>(type: "int", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ExpectedReturnDate = table.Column<DateTime>(type: "date", nullable: true),
                    AssetCodeSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SerialNumberSnapshot = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CategoryNameSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BrandSnapshot = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ModelSnapshot = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ConditionNameSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsSecurityItemSnapshot = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_AssetIssuanceItems", x => x.AssetIssuanceItemId);
                    table.CheckConstraint("CK_AM_AssetIssuanceItems_Status", "[Status] IN ('Proposed','Reserved','HandedOver','Accepted','Rejected','HRApproved','ReturnedForCorrection','Closed')");
                    table.CheckConstraint("CK_AM_AssetIssuanceItems_Version", "[Version] >= 1");
                    table.ForeignKey(
                        name: "FK_AM_AssetIssuanceItems_AM_AssetConditions_ConditionAtIssueId",
                        column: x => x.ConditionAtIssueId,
                        principalTable: "AM_AssetConditions",
                        principalColumn: "AssetConditionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetIssuanceItems_AM_AssetIssuanceItems_ReplacedFromIssuanceItemId",
                        column: x => x.ReplacedFromIssuanceItemId,
                        principalTable: "AM_AssetIssuanceItems",
                        principalColumn: "AssetIssuanceItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetIssuanceItems_AM_AssetIssuances_IssuanceId",
                        column: x => x.IssuanceId,
                        principalTable: "AM_AssetIssuances",
                        principalColumn: "AssetIssuanceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetIssuanceItems_AM_AssetItems_AssetItemId",
                        column: x => x.AssetItemId,
                        principalTable: "AM_AssetItems",
                        principalColumn: "AssetItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetIssuanceItems_Users_HandedOverByUserId",
                        column: x => x.HandedOverByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AM_AssetAcknowledgements",
                columns: table => new
                {
                    AcknowledgementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssuanceItemId = table.Column<int>(type: "int", nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RejectionReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AcknowledgementStatement = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EvidenceAttachmentId = table.Column<int>(type: "int", nullable: true),
                    ConfirmedByUserId = table.Column<int>(type: "int", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ItemVersion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_AssetAcknowledgements", x => x.AcknowledgementId);
                    table.CheckConstraint("CK_AM_AssetAcknowledgements_Decision", "[Decision] IN ('Accepted','Rejected') AND ([Decision] = 'Accepted' OR ([Remarks] IS NOT NULL AND LEN(LTRIM(RTRIM([Remarks]))) > 0))");
                    table.ForeignKey(
                        name: "FK_AM_AssetAcknowledgements_AM_AssetIssuanceItems_IssuanceItemId",
                        column: x => x.IssuanceItemId,
                        principalTable: "AM_AssetIssuanceItems",
                        principalColumn: "AssetIssuanceItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetAcknowledgements_BE_Individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalTable: "BE_Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetAcknowledgements_Users_ConfirmedByUserId",
                        column: x => x.ConfirmedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AM_AssetHrDecisions",
                columns: table => new
                {
                    HrDecisionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssuanceItemId = table.Column<int>(type: "int", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DecidedByUserId = table.Column<int>(type: "int", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ItemVersion = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_AssetHrDecisions", x => x.HrDecisionId);
                    table.CheckConstraint("CK_AM_AssetHrDecisions_Decision", "[Decision] IN ('HRApproved','ReturnedForCorrection','Closed')");
                    table.ForeignKey(
                        name: "FK_AM_AssetHrDecisions_AM_AssetIssuanceItems_IssuanceItemId",
                        column: x => x.IssuanceItemId,
                        principalTable: "AM_AssetIssuanceItems",
                        principalColumn: "AssetIssuanceItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetHrDecisions_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AM_AssetCustodies",
                columns: table => new
                {
                    AssetCustodyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetItemId = table.Column<int>(type: "int", nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    StructureId = table.Column<int>(type: "int", nullable: true),
                    IssuanceItemId = table.Column<int>(type: "int", nullable: false),
                    CustodyFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustodyToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedReturnDate = table.Column<DateTime>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ClosedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClosedByUserId = table.Column<int>(type: "int", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_AssetCustodies", x => x.AssetCustodyId);
                    table.CheckConstraint("CK_AM_AssetCustodies_Dates", "[CustodyToUtc] IS NULL OR [CustodyToUtc] >= [CustodyFromUtc]");
                    table.CheckConstraint("CK_AM_AssetCustodies_Status", "[Status] IN ('Active','ReturnPending','Returned','Lost','Transferred','Closed')");
                    table.ForeignKey(
                        name: "FK_AM_AssetCustodies_AM_AssetIssuanceItems_IssuanceItemId",
                        column: x => x.IssuanceItemId,
                        principalTable: "AM_AssetIssuanceItems",
                        principalColumn: "AssetIssuanceItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetCustodies_AM_AssetItems_AssetItemId",
                        column: x => x.AssetItemId,
                        principalTable: "AM_AssetItems",
                        principalColumn: "AssetItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetCustodies_BE_Individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalTable: "BE_Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetCustodies_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetCustodies_OrganisationStructure_StructureId",
                        column: x => x.StructureId,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetCustodies_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetCustodies_Users_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AM_AssetReturns",
                columns: table => new
                {
                    AssetReturnId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: true),
                    StaffTransferId = table.Column<int>(type: "int", nullable: true),
                    ReturnType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    ReturnedToUserId = table.Column<int>(type: "int", nullable: true),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_AssetReturns", x => x.AssetReturnId);
                    table.CheckConstraint("CK_AM_AssetReturns_Status", "[Status] IN ('ReturnPending','Inspected','Closed','Cancelled') AND [ReturnType] IN ('StandardReturn','DamageReport','LossReport','TransferReturn','TerminationClearance')");
                    table.ForeignKey(
                        name: "FK_AM_AssetReturns_BE_Individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalTable: "BE_Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetReturns_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetReturns_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetReturns_ST_StaffTransfers_StaffTransferId",
                        column: x => x.StaffTransferId,
                        principalTable: "ST_StaffTransfers",
                        principalColumn: "StaffTransferId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetReturns_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetReturns_Users_ReturnedToUserId",
                        column: x => x.ReturnedToUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AM_AssetReturnItems",
                columns: table => new
                {
                    AssetReturnItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetReturnId = table.Column<int>(type: "int", nullable: false),
                    AssetCustodyId = table.Column<int>(type: "int", nullable: false),
                    AssetItemId = table.Column<int>(type: "int", nullable: false),
                    ConditionAtReturnId = table.Column<int>(type: "int", nullable: true),
                    AccessoryChecklist = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MissingAccessories = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DamageOrLossEvidence = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InspectionResult = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PostReturnDisposition = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ReceivedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_AssetReturnItems", x => x.AssetReturnItemId);
                    table.CheckConstraint("CK_AM_AssetReturnItems_Status", "[Status] IN ('ReturnPending','Inspected','Closed') AND [InspectionResult] IN ('Pending','GoodAvailable','DamagedMaintenance','LostConfirmed','Disposed','ReplacementRequired')");
                    table.ForeignKey(
                        name: "FK_AM_AssetReturnItems_AM_AssetConditions_ConditionAtReturnId",
                        column: x => x.ConditionAtReturnId,
                        principalTable: "AM_AssetConditions",
                        principalColumn: "AssetConditionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetReturnItems_AM_AssetCustodies_AssetCustodyId",
                        column: x => x.AssetCustodyId,
                        principalTable: "AM_AssetCustodies",
                        principalColumn: "AssetCustodyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetReturnItems_AM_AssetItems_AssetItemId",
                        column: x => x.AssetItemId,
                        principalTable: "AM_AssetItems",
                        principalColumn: "AssetItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetReturnItems_AM_AssetReturns_AssetReturnId",
                        column: x => x.AssetReturnId,
                        principalTable: "AM_AssetReturns",
                        principalColumn: "AssetReturnId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetReturnItems_Users_ReceivedByUserId",
                        column: x => x.ReceivedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AM_AssetTransferDispositions",
                columns: table => new
                {
                    AssetTransferDispositionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffTransferId = table.Column<int>(type: "int", nullable: true),
                    AssetCustodyId = table.Column<int>(type: "int", nullable: false),
                    AssetItemId = table.Column<int>(type: "int", nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    FromJobId = table.Column<int>(type: "int", nullable: false),
                    ToJobId = table.Column<int>(type: "int", nullable: true),
                    FromOrganisationId = table.Column<int>(type: "int", nullable: false),
                    ToOrganisationId = table.Column<int>(type: "int", nullable: false),
                    FromStructureId = table.Column<int>(type: "int", nullable: true),
                    ToStructureId = table.Column<int>(type: "int", nullable: true),
                    Decision = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DecidedByUserId = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_AssetTransferDispositions", x => x.AssetTransferDispositionId);
                    table.CheckConstraint("CK_AM_AssetTransferDispositions_Decision", "[Decision] IN ('Retain','Return','ChangeOwningDepartment','Replace') AND [Status] IN ('Pending','Approved','Executed','Cancelled')");
                    table.ForeignKey(
                        name: "FK_AM_AssetTransferDispositions_AM_AssetCustodies_AssetCustodyId",
                        column: x => x.AssetCustodyId,
                        principalTable: "AM_AssetCustodies",
                        principalColumn: "AssetCustodyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetTransferDispositions_AM_AssetItems_AssetItemId",
                        column: x => x.AssetItemId,
                        principalTable: "AM_AssetItems",
                        principalColumn: "AssetItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetTransferDispositions_BE_Individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalTable: "BE_Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetTransferDispositions_Jobs_FromJobId",
                        column: x => x.FromJobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetTransferDispositions_Jobs_ToJobId",
                        column: x => x.ToJobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetTransferDispositions_OrganisationStructure_FromStructureId",
                        column: x => x.FromStructureId,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetTransferDispositions_OrganisationStructure_ToStructureId",
                        column: x => x.ToStructureId,
                        principalTable: "OrganisationStructure",
                        principalColumn: "OrganisationStructureID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetTransferDispositions_Organisations_FromOrganisationId",
                        column: x => x.FromOrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetTransferDispositions_Organisations_ToOrganisationId",
                        column: x => x.ToOrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetTransferDispositions_ST_StaffTransfers_StaffTransferId",
                        column: x => x.StaffTransferId,
                        principalTable: "ST_StaffTransfers",
                        principalColumn: "StaffTransferId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetTransferDispositions_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetTransferDispositions_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AM_AssetEvents",
                columns: table => new
                {
                    AssetEventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    AssetItemId = table.Column<int>(type: "int", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ToStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ActorUserId = table.Column<int>(type: "int", nullable: false),
                    AtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_AssetEvents", x => x.AssetEventId);
                    table.ForeignKey(
                        name: "FK_AM_AssetEvents_AM_AssetItems_AssetItemId",
                        column: x => x.AssetItemId,
                        principalTable: "AM_AssetItems",
                        principalColumn: "AssetItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetEvents_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetEvents_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AM_AssetAttachments",
                columns: table => new
                {
                    AttachmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileBytes = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccessScope = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_AssetAttachments", x => x.AttachmentId);
                    table.CheckConstraint("CK_AM_AssetAttachments_Scope", "[AccessScope] IN ('EmployeeAndHr','HrOnly','RestrictedSecurity')");
                    table.ForeignKey(
                        name: "FK_AM_AssetAttachments_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetAttachments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AM_AssetDocuments",
                columns: table => new
                {
                    AssetDocumentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetIssuanceId = table.Column<int>(type: "int", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    DocumentVariant = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DocumentVersion = table.Column<int>(type: "int", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sha256Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PdfBytes = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    GeneratedByUserId = table.Column<int>(type: "int", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AM_AssetDocuments", x => x.AssetDocumentId);
                    table.CheckConstraint("CK_AM_AssetDocuments_Variant", "[DocumentVariant] IN ('StandardHandoverRecord','RestrictedSecurityRecord')");
                    table.ForeignKey(
                        name: "FK_AM_AssetDocuments_AM_AssetIssuances_AssetIssuanceId",
                        column: x => x.AssetIssuanceId,
                        principalTable: "AM_AssetIssuances",
                        principalColumn: "AssetIssuanceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AM_AssetDocuments_Users_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetCategories_OrganisationId_Code",
                table: "AM_AssetCategories",
                columns: new[] { "OrganisationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetCategories_OrganisationId_Name",
                table: "AM_AssetCategories",
                columns: new[] { "OrganisationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetConditions_Code",
                table: "AM_AssetConditions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetConditions_Name",
                table: "AM_AssetConditions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AM_AssetItems_AssetCode",
                table: "AM_AssetItems",
                column: "AssetCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AM_AssetItems_SerialNumber",
                table: "AM_AssetItems",
                columns: new[] { "OwnerOrganisationId", "SerialNumber" },
                unique: true,
                filter: "[SerialNumber] IS NOT NULL AND [SerialNumber] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetItems_OwnerOrganisationId_Status_CategoryId",
                table: "AM_AssetItems",
                columns: new[] { "OwnerOrganisationId", "Status", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetIssuances_ReferenceNumber",
                table: "AM_AssetIssuances",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetIssuances_RequestId",
                table: "AM_AssetIssuances",
                column: "RequestId",
                unique: true,
                filter: "[RequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetIssuances_Staff_Status",
                table: "AM_AssetIssuances",
                columns: new[] { "IndividualId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetIssuances_OrganisationId_Status_IssueDate",
                table: "AM_AssetIssuances",
                columns: new[] { "OrganisationId", "Status", "IssueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetIssuanceItems_Asset",
                table: "AM_AssetIssuanceItems",
                column: "AssetItemId");

            migrationBuilder.CreateIndex(
                name: "UX_AM_AssetIssuanceItems_ActiveReservation",
                table: "AM_AssetIssuanceItems",
                column: "AssetItemId",
                unique: true,
                filter: "[IsActiveReservation] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetIssuanceItems_IssuanceId_Status",
                table: "AM_AssetIssuanceItems",
                columns: new[] { "IssuanceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetAcknowledgements_IssuanceItemId_ItemVersion",
                table: "AM_AssetAcknowledgements",
                columns: new[] { "IssuanceItemId", "ItemVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetAcknowledgements_IndividualId_ConfirmedAtUtc",
                table: "AM_AssetAcknowledgements",
                columns: new[] { "IndividualId", "ConfirmedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetHrDecisions_IssuanceItemId_ItemVersion_IdempotencyKey",
                table: "AM_AssetHrDecisions",
                columns: new[] { "IssuanceItemId", "ItemVersion", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AM_AssetCustodies_Active",
                table: "AM_AssetCustodies",
                column: "AssetItemId",
                unique: true,
                filter: "[CustodyToUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_AM_AssetCustodies_IssuanceItem",
                table: "AM_AssetCustodies",
                column: "IssuanceItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AM_AssetCustodies_Idempotency",
                table: "AM_AssetCustodies",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetCustodies_IndividualId_Status",
                table: "AM_AssetCustodies",
                columns: new[] { "IndividualId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetCustodies_OrganisationId_Status",
                table: "AM_AssetCustodies",
                columns: new[] { "OrganisationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetReturns_ReferenceNumber",
                table: "AM_AssetReturns",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetReturns_OrganisationId_Status_ReturnDate",
                table: "AM_AssetReturns",
                columns: new[] { "OrganisationId", "Status", "ReturnDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetReturns_IndividualId_Status",
                table: "AM_AssetReturns",
                columns: new[] { "IndividualId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetReturnItems_AssetReturnId_AssetCustodyId",
                table: "AM_AssetReturnItems",
                columns: new[] { "AssetReturnId", "AssetCustodyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetReturnItems_AssetCustodyId_Status",
                table: "AM_AssetReturnItems",
                columns: new[] { "AssetCustodyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetTransferDispositions_StaffTransferId_AssetCustodyId",
                table: "AM_AssetTransferDispositions",
                columns: new[] { "StaffTransferId", "AssetCustodyId" });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetTransferDispositions_IndividualId_Status",
                table: "AM_AssetTransferDispositions",
                columns: new[] { "IndividualId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetEvents_AssetItemId_AtUtc",
                table: "AM_AssetEvents",
                columns: new[] { "AssetItemId", "AtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetEvents_RelatedEntityType_RelatedEntityId_AtUtc",
                table: "AM_AssetEvents",
                columns: new[] { "RelatedEntityType", "RelatedEntityId", "AtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetEvents_OrganisationId_AtUtc",
                table: "AM_AssetEvents",
                columns: new[] { "OrganisationId", "AtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetAttachments_StorageKey",
                table: "AM_AssetAttachments",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetAttachments_EntityType_EntityId",
                table: "AM_AssetAttachments",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetDocuments_DocumentNumber",
                table: "AM_AssetDocuments",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AM_AssetDocuments_AssetIssuanceId_DocumentVariant_DocumentVersion",
                table: "AM_AssetDocuments",
                columns: new[] { "AssetIssuanceId", "DocumentVariant", "DocumentVersion" });

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM dbo.AM_AssetConditions WHERE Code = N'NEW')
                    INSERT INTO dbo.AM_AssetConditions (Code, Name, Description, RequiresMaintenanceOnReturn, SortOrder, IsActive)
                    VALUES
                        (N'NEW', N'New', N'Brand new, unused equipment in original factory condition.', 0, 1, 1),
                        (N'GOOD', N'Good', N'Fully functional equipment in good working order.', 0, 2, 1),
                        (N'FAIR', N'Fair', N'Functional with minor cosmetic wear.', 0, 3, 1),
                        (N'DAMAGED', N'Damaged', N'Damaged or faulty; requires maintenance or repair.', 1, 4, 1),
                        (N'UNSERVICEABLE', N'Unserviceable', N'Non-functional beyond normal use; candidate for repair or disposal.', 1, 5, 1);

                IF NOT EXISTS (SELECT 1 FROM dbo.AM_AssetCategories WHERE OrganisationId IS NULL AND Code = N'LAPTOP')
                    INSERT INTO dbo.AM_AssetCategories (OrganisationId, Code, Name, Description, RequiresSerialNumber, RequiresReturn, IsSecurityItem, IsActive, CreatedAtUtc)
                    VALUES
                        (NULL, N'LAPTOP', N'Laptops & Notebooks', N'Portable computers issued to staff.', 1, 1, 0, 1, SYSUTCDATETIME()),
                        (NULL, N'MONITOR', N'Displays & Monitors', N'External monitors and docking displays.', 1, 1, 0, 1, SYSUTCDATETIME()),
                        (NULL, N'PHONE', N'Mobile Phones & Tablets', N'Official handsets, SIM-enabled tablets and communication devices.', 1, 1, 0, 1, SYSUTCDATETIME()),
                        (NULL, N'ACCESS_CARD', N'Security & Access Cards', N'Smart cards, building badges, and cryptographic tokens.', 1, 1, 1, 1, SYSUTCDATETIME()),
                        (NULL, N'EQUIPMENT', N'Specialized Office & Field Equipment', N'Peripherals, radios, cameras, and technical instruments.', 1, 1, 0, 1, SYSUTCDATETIME());

                IF NOT EXISTS(SELECT 1 FROM dbo.RequestTypes WHERE TypeName = N'Staff Asset Issuance Request')
                BEGIN
                    INSERT dbo.SequenceNumberTypes(TypeName,Prefix,FormatString,IsResetAnnually,StartingNumber,NextNumber)
                    VALUES(N'Staff Asset Issuances',N'SAI',N'{PREFIX}-{YEAR}-{NUMBER}',0,1,1);
                    DECLARE @AssetSequence int = CONVERT(int, SCOPE_IDENTITY());
                    INSERT dbo.RequestTypes(TypeName,SequenceNumberTypeID,ViewURL,IsSystemType)
                    VALUES(N'Staff Asset Issuance Request',@AssetSequence,N'/admin/hr/items/asset-custody',1);
                END;

                INSERT dbo.RequestTypesAllowedRequestStateTransitions(RequestTypeID,FromRequestStateID,ToRequestStateID)
                SELECT t.RequestTypeID, f.RequestStateID, s.RequestStateID
                FROM dbo.RequestTypes t CROSS JOIN dbo.RequestStates f CROSS JOIN dbo.RequestStates s
                WHERE t.TypeName = N'Staff Asset Issuance Request'
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
            throw new NotSupportedException("Staff asset assignment, acknowledgement, custody and audit records must be retained.");
        }
    }
}
