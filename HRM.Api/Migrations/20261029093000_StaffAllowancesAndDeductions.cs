using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class StaffAllowancesAndDeductions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AD_FinanceRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AD_FinanceRequests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_AD_FinanceRequests_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AD_FinanceRequests_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AD_PayrollBatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SelectionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayrollCycleRequestId = table.Column<int>(type: "int", nullable: false),
                    ClientBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PostedByUserId = table.Column<int>(type: "int", nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AD_PayrollBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AD_PayrollBatches_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AD_PayrollBatches_PayrollCycles_PayrollCycleRequestId",
                        column: x => x.PayrollCycleRequestId,
                        principalTable: "PayrollCycles",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AL_AllowanceTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AL_AllowanceTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AL_AllowanceTypes_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DA_DeductionAgreements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    CreditorBusinessEntityId = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CurrencyTypeId = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FirstDueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InstalmentCount = table.Column<int>(type: "int", nullable: false),
                    FrequencyMonths = table.Column<int>(type: "int", nullable: false),
                    ScheduleVersion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DA_DeductionAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DA_DeductionAgreements_BE_BusinessEntities_CreditorBusinessEntityId",
                        column: x => x.CreditorBusinessEntityId,
                        principalTable: "BE_BusinessEntities",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DA_DeductionAgreements_BE_Individuals_IndividualId",
                        column: x => x.IndividualId,
                        principalTable: "BE_Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DA_DeductionAgreements_CurrencyTypes_CurrencyTypeId",
                        column: x => x.CurrencyTypeId,
                        principalTable: "CurrencyTypes",
                        principalColumn: "CurrencyTypeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DA_DeductionAgreements_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DA_DeductionAgreements_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DA_DeductionPolicies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    CurrencyTypeId = table.Column<int>(type: "int", nullable: false),
                    MaximumPerMonth = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaximumPercentOfBasic = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinimumTakeHome = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaximumOutstanding = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaximumAgreements = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DA_DeductionPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DA_DeductionPolicies_CurrencyTypes_CurrencyTypeId",
                        column: x => x.CurrencyTypeId,
                        principalTable: "CurrencyTypes",
                        principalColumn: "CurrencyTypeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DA_DeductionPolicies_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AD_RequestDocuments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AD_RequestDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AD_RequestDocuments_AD_FinanceRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "AD_FinanceRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AL_PositionAllowanceRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    AllowanceTypeId = table.Column<long>(type: "bigint", nullable: false),
                    CurrencyTypeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FrequencyMonths = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MultiplePositionPolicy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AL_PositionAllowanceRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AL_PositionAllowanceRules_AL_AllowanceTypes_AllowanceTypeId",
                        column: x => x.AllowanceTypeId,
                        principalTable: "AL_AllowanceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AL_PositionAllowanceRules_CurrencyTypes_CurrencyTypeId",
                        column: x => x.CurrencyTypeId,
                        principalTable: "CurrencyTypes",
                        principalColumn: "CurrencyTypeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AL_PositionAllowanceRules_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "PositionID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AL_StaffAllowances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    AllowanceTypeId = table.Column<long>(type: "bigint", nullable: false),
                    CurrencyTypeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FrequencyMonths = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AL_StaffAllowances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AL_StaffAllowances_AL_AllowanceTypes_AllowanceTypeId",
                        column: x => x.AllowanceTypeId,
                        principalTable: "AL_AllowanceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AL_StaffAllowances_CurrencyTypes_CurrencyTypeId",
                        column: x => x.CurrencyTypeId,
                        principalTable: "CurrencyTypes",
                        principalColumn: "CurrencyTypeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AL_StaffAllowances_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AL_StaffAllowances_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DA_Instalments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgreementId = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsSuperseded = table.Column<bool>(type: "bit", nullable: false),
                    SourceRequestId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DA_Instalments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DA_Instalments_DA_DeductionAgreements_AgreementId",
                        column: x => x.AgreementId,
                        principalTable: "DA_DeductionAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DA_Instalments_Requests_SourceRequestId",
                        column: x => x.SourceRequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AD_PayrollEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<long>(type: "bigint", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    IndividualId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CurrencyTypeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InstalmentId = table.Column<long>(type: "bigint", nullable: true),
                    AllowanceId = table.Column<long>(type: "bigint", nullable: true),
                    PositionRuleId = table.Column<long>(type: "bigint", nullable: true),
                    JobPositionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AD_PayrollEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AD_PayrollEntries_AD_PayrollBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "AD_PayrollBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AD_PayrollEntries_AL_PositionAllowanceRules_PositionRuleId",
                        column: x => x.PositionRuleId,
                        principalTable: "AL_PositionAllowanceRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AD_PayrollEntries_AL_StaffAllowances_AllowanceId",
                        column: x => x.AllowanceId,
                        principalTable: "AL_StaffAllowances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AD_PayrollEntries_DA_Instalments_InstalmentId",
                        column: x => x.InstalmentId,
                        principalTable: "DA_Instalments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AD_PayrollEntries_JobPositions_JobPositionId",
                        column: x => x.JobPositionId,
                        principalTable: "JobPositions",
                        principalColumn: "JobPositionID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AD_PayrollEntries_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "JobID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DA_Payments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgreementId = table.Column<long>(type: "bigint", nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: true),
                    PayrollEntryId = table.Column<long>(type: "bigint", nullable: true),
                    PayerBusinessEntityId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RecordedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DA_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DA_Payments_AD_PayrollEntries_PayrollEntryId",
                        column: x => x.PayrollEntryId,
                        principalTable: "AD_PayrollEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DA_Payments_BE_BusinessEntities_PayerBusinessEntityId",
                        column: x => x.PayerBusinessEntityId,
                        principalTable: "BE_BusinessEntities",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DA_Payments_DA_DeductionAgreements_AgreementId",
                        column: x => x.AgreementId,
                        principalTable: "DA_DeductionAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DA_Payments_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DA_PaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<long>(type: "bigint", nullable: false),
                    InstalmentId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DA_PaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DA_PaymentAllocations_DA_Instalments_InstalmentId",
                        column: x => x.InstalmentId,
                        principalTable: "DA_Instalments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DA_PaymentAllocations_DA_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "DA_Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AD_FinanceRequests_JobId",
                table: "AD_FinanceRequests",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_AD_FinanceRequests_OrganisationId_ClientRequestId",
                table: "AD_FinanceRequests",
                columns: new[] { "OrganisationId", "ClientRequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AD_PayrollBatches_OrganisationId_ClientBatchId",
                table: "AD_PayrollBatches",
                columns: new[] { "OrganisationId", "ClientBatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AD_PayrollBatches_PayrollCycleRequestId",
                table: "AD_PayrollBatches",
                column: "PayrollCycleRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AD_PayrollEntries_AllowanceId",
                table: "AD_PayrollEntries",
                column: "AllowanceId");

            migrationBuilder.CreateIndex(
                name: "IX_AD_PayrollEntries_BatchId",
                table: "AD_PayrollEntries",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_AD_PayrollEntries_InstalmentId",
                table: "AD_PayrollEntries",
                column: "InstalmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AD_PayrollEntries_JobId",
                table: "AD_PayrollEntries",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_AD_PayrollEntries_JobPositionId",
                table: "AD_PayrollEntries",
                column: "JobPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_AD_PayrollEntries_OrganisationId_Month_SourceKey",
                table: "AD_PayrollEntries",
                columns: new[] { "OrganisationId", "Month", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AD_PayrollEntries_PositionRuleId",
                table: "AD_PayrollEntries",
                column: "PositionRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AD_RequestDocuments_RequestId",
                table: "AD_RequestDocuments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AL_AllowanceTypes_OrganisationId_Name",
                table: "AL_AllowanceTypes",
                columns: new[] { "OrganisationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AL_PositionAllowanceRules_AllowanceTypeId",
                table: "AL_PositionAllowanceRules",
                column: "AllowanceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AL_PositionAllowanceRules_CurrencyTypeId",
                table: "AL_PositionAllowanceRules",
                column: "CurrencyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AL_PositionAllowanceRules_OrganisationId_PositionId_AllowanceTypeId_FromDate",
                table: "AL_PositionAllowanceRules",
                columns: new[] { "OrganisationId", "PositionId", "AllowanceTypeId", "FromDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AL_PositionAllowanceRules_PositionId",
                table: "AL_PositionAllowanceRules",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_AL_StaffAllowances_AllowanceTypeId",
                table: "AL_StaffAllowances",
                column: "AllowanceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AL_StaffAllowances_CurrencyTypeId",
                table: "AL_StaffAllowances",
                column: "CurrencyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AL_StaffAllowances_JobId",
                table: "AL_StaffAllowances",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_AL_StaffAllowances_RequestId",
                table: "AL_StaffAllowances",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DA_DeductionAgreements_CreditorBusinessEntityId",
                table: "DA_DeductionAgreements",
                column: "CreditorBusinessEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_DA_DeductionAgreements_CurrencyTypeId",
                table: "DA_DeductionAgreements",
                column: "CurrencyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DA_DeductionAgreements_IndividualId",
                table: "DA_DeductionAgreements",
                column: "IndividualId");

            migrationBuilder.CreateIndex(
                name: "IX_DA_DeductionAgreements_JobId",
                table: "DA_DeductionAgreements",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_DA_DeductionAgreements_OrganisationId_CreditorBusinessEntityId_Reference",
                table: "DA_DeductionAgreements",
                columns: new[] { "OrganisationId", "CreditorBusinessEntityId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DA_DeductionAgreements_RequestId",
                table: "DA_DeductionAgreements",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DA_DeductionPolicies_CurrencyTypeId",
                table: "DA_DeductionPolicies",
                column: "CurrencyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DA_DeductionPolicies_OrganisationId_CurrencyTypeId_FromDate",
                table: "DA_DeductionPolicies",
                columns: new[] { "OrganisationId", "CurrencyTypeId", "FromDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DA_Instalments_AgreementId_Version_DueDate",
                table: "DA_Instalments",
                columns: new[] { "AgreementId", "Version", "DueDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DA_Instalments_SourceRequestId",
                table: "DA_Instalments",
                column: "SourceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DA_PaymentAllocations_InstalmentId",
                table: "DA_PaymentAllocations",
                column: "InstalmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DA_PaymentAllocations_PaymentId_InstalmentId",
                table: "DA_PaymentAllocations",
                columns: new[] { "PaymentId", "InstalmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DA_Payments_AgreementId",
                table: "DA_Payments",
                column: "AgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_DA_Payments_PayerBusinessEntityId",
                table: "DA_Payments",
                column: "PayerBusinessEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_DA_Payments_PayrollEntryId",
                table: "DA_Payments",
                column: "PayrollEntryId",
                unique: true,
                filter: "[PayrollEntryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DA_Payments_RequestId",
                table: "DA_Payments",
                column: "RequestId",
                unique: true,
                filter: "[RequestId] IS NOT NULL");
            migrationBuilder.Sql("""
                DECLARE @ContextId int=(SELECT TOP(1) ContextId FROM dbo.Roles WHERE RoleKey='Admin');
                IF @ContextId IS NULL THROW 51000,'Admin permission context is required.',1;
                INSERT dbo.Roles(RoleKey,Name,Description,ContextId,IsSystemRole)
                SELECT k.RoleKey,k.Name,k.Description,@ContextId,1 FROM (VALUES
                  ('StaffFinance.Configure','Configure allowances and deductions','Manage tenant allowance types, position allowance rules and salary deduction limits.'),
                  ('StaffFinance.Manage','Manage staff allowances and deductions','View staff financial records and submit requests on behalf of staff in the current organisation.'),
                  ('StaffFinance.Payroll','Post allowance and deduction payroll','Preview and post approved allowances and deductions to a payroll cycle.'))k(RoleKey,Name,Description)
                WHERE NOT EXISTS(SELECT 1 FROM dbo.Roles r WHERE r.RoleKey=k.RoleKey);
                DECLARE @Name nvarchar(150),@Prefix nvarchar(20),@Sequence int;
                DECLARE types CURSOR LOCAL FAST_FORWARD FOR SELECT * FROM (VALUES
                  (N'Staff Allowance Request',N'ALW'),(N'Salary Deduction Agreement Request',N'DAG'),
                  (N'Deduction Deferral Request',N'DDF'),(N'Deduction External Payment Request',N'DPY'),
                  (N'Allowance End Request',N'ALE'))v(Name,Prefix);
                OPEN types; FETCH NEXT FROM types INTO @Name,@Prefix;
                WHILE @@FETCH_STATUS=0
                BEGIN
                  IF NOT EXISTS(SELECT 1 FROM dbo.RequestTypes WHERE TypeName=@Name)
                  BEGIN
                    INSERT dbo.SequenceNumberTypes(TypeName,Prefix,FormatString,IsResetAnnually,StartingNumber,NextNumber)
                    VALUES(@Name,@Prefix,N'{PREFIX}-{YEAR}-{NUMBER}',0,1,1);
                    SET @Sequence=CONVERT(int,SCOPE_IDENTITY());
                    INSERT dbo.RequestTypes(TypeName,SequenceNumberTypeID,ViewURL,IsSystemType)
                    VALUES(@Name,@Sequence,N'/online/allowances-deductions',1);
                  END;
                  FETCH NEXT FROM types INTO @Name,@Prefix;
                END;
                CLOSE types; DEALLOCATE types;
                """);
            foreach (var table in new[] { "AD_PayrollBatches", "AD_PayrollEntries", "DA_Payments", "DA_PaymentAllocations", "AD_RequestDocuments", "AD_FinanceRequests", "DA_DeductionPolicies" })
                migrationBuilder.Sql($"CREATE OR ALTER TRIGGER dbo.TR_{table}_Immutable ON dbo.{table} INSTEAD OF UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51000, 'Financial history is immutable. Use an approved adjustment.', 1; END;");
            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER dbo.TR_DA_Instalments_Lock ON dbo.DA_Instalments AFTER UPDATE, DELETE AS
                BEGIN
                  SET NOCOUNT ON;
                  IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id
                    WHERE i.Id IS NULL OR i.AgreementId<>d.AgreementId OR i.Version<>d.Version OR i.DueDate<>d.DueDate OR i.Amount<>d.Amount OR i.SourceRequestId<>d.SourceRequestId OR d.IsSuperseded=1 AND i.IsSuperseded=0)
                    THROW 51000,'Repayment schedules are versioned; only unpaid schedule supersession is permitted.',1;
                END;
                """);
            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER dbo.TR_DA_Agreements_Lock ON dbo.DA_DeductionAgreements AFTER UPDATE, DELETE AS
                BEGIN
                  SET NOCOUNT ON;
                  IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id WHERE i.Id IS NULL OR
                    i.OrganisationId<>d.OrganisationId OR i.IndividualId<>d.IndividualId OR i.JobId<>d.JobId OR i.RequestId<>d.RequestId OR
                    i.CreditorBusinessEntityId<>d.CreditorBusinessEntityId OR i.Reference<>d.Reference OR i.Category<>d.Category OR
                    i.CurrencyTypeId<>d.CurrencyTypeId OR i.TotalAmount<>d.TotalAmount OR i.FirstDueDate<>d.FirstDueDate OR
                    i.InstalmentCount<>d.InstalmentCount OR i.FrequencyMonths<>d.FrequencyMonths OR i.ScheduleVersion<>d.ScheduleVersion+1)
                    THROW 51000,'Approved agreement terms are immutable. Use an approved schedule adjustment.',1;
                END;
                """);
            migrationBuilder.Sql("""
                CREATE OR ALTER TRIGGER dbo.TR_AD_RequestStateLock ON dbo.Requests AFTER UPDATE, DELETE AS
                BEGIN
                  SET NOCOUNT ON;
                  IF EXISTS(SELECT 1 FROM deleted d JOIN dbo.AD_FinanceRequests f ON f.RequestId=d.RequestID
                    JOIN dbo.RequestStates s ON s.RequestStateID=d.RequestStateID LEFT JOIN inserted i ON i.RequestID=d.RequestID
                    WHERE i.RequestID IS NULL OR s.IsFinalState=1 AND i.RequestStateID<>d.RequestStateID)
                    THROW 51000,'Final finance requests cannot change state. Submit an adjustment request.',1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Financial records must be retained. Use a reviewed forward migration.");
        }
    }
}
