using System;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    [DbContext(typeof(HrmTeContext))]
    [Migration("20261107150000_EnterprisePayrollManagement")]
    public sealed class EnterprisePayrollManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollPeriods')
                BEGIN
                    CREATE TABLE dbo.PAY_PayrollPeriods (
                        PayrollPeriodId INT IDENTITY(1,1) NOT NULL,
                        OrganisationId INT NOT NULL,
                        PayrollGroupId INT NULL,
                        PeriodYear INT NOT NULL,
                        PeriodMonth INT NOT NULL,
                        PeriodStartDate DATE NOT NULL,
                        PeriodEndDate DATE NOT NULL,
                        AttendanceCutoffDate DATE NOT NULL,
                        AdjustmentCutoffDate DATE NOT NULL,
                        PaymentDate DATE NOT NULL,
                        PeriodStatus INT NOT NULL CONSTRAINT DF_PAY_PayrollPeriods_PeriodStatus DEFAULT (1),
                        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PAY_PayrollPeriods_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PAY_PayrollPeriods_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        OperationLogId INT NULL,
                        CONSTRAINT PK_PAY_PayrollPeriods PRIMARY KEY CLUSTERED (PayrollPeriodId),
                        CONSTRAINT FK_PAY_PayrollPeriods_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID)
                    );

                    CREATE UNIQUE NONCLUSTERED INDEX UX_PAY_PayrollPeriods_Org_Year_Month_Group ON dbo.PAY_PayrollPeriods (OrganisationId, PeriodYear, PeriodMonth, PayrollGroupId);
                    CREATE NONCLUSTERED INDEX IX_PAY_PayrollPeriods_Org_Status ON dbo.PAY_PayrollPeriods (OrganisationId, PeriodStatus);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_SalaryComponents')
                BEGIN
                    CREATE TABLE dbo.PAY_SalaryComponents (
                        SalaryComponentId INT IDENTITY(1,1) NOT NULL,
                        OrganisationId INT NOT NULL,
                        ComponentCode NVARCHAR(50) NOT NULL,
                        ComponentName NVARCHAR(150) NOT NULL,
                        ComponentType INT NOT NULL,
                        CalculationMethod INT NOT NULL,
                        IsRecurring BIT NOT NULL CONSTRAINT DF_PAY_SalaryComponents_IsRecurring DEFAULT (1),
                        IsTaxable BIT NOT NULL CONSTRAINT DF_PAY_SalaryComponents_IsTaxable DEFAULT (1),
                        IsPensionable BIT NOT NULL CONSTRAINT DF_PAY_SalaryComponents_IsPensionable DEFAULT (1),
                        IsProratable BIT NOT NULL CONSTRAINT DF_PAY_SalaryComponents_IsProratable DEFAULT (1),
                        EffectiveFrom DATE NOT NULL,
                        EffectiveTo DATE NULL,
                        IsActive BIT NOT NULL CONSTRAINT DF_PAY_SalaryComponents_IsActive DEFAULT (1),
                        DefaultAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_PAY_SalaryComponents_DefaultAmount DEFAULT (0),
                        DefaultPercentage DECIMAL(18,4) NOT NULL CONSTRAINT DF_PAY_SalaryComponents_DefaultPercentage DEFAULT (0),
                        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PAY_SalaryComponents_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT PK_PAY_SalaryComponents PRIMARY KEY CLUSTERED (SalaryComponentId),
                        CONSTRAINT FK_PAY_SalaryComponents_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID)
                    );

                    CREATE UNIQUE NONCLUSTERED INDEX UX_PAY_SalaryComponents_Org_Code ON dbo.PAY_SalaryComponents (OrganisationId, ComponentCode);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_SalaryStructures')
                BEGIN
                    CREATE TABLE dbo.PAY_SalaryStructures (
                        SalaryStructureId INT IDENTITY(1,1) NOT NULL,
                        OrganisationId INT NOT NULL,
                        StructureCode NVARCHAR(50) NOT NULL,
                        StructureName NVARCHAR(150) NOT NULL,
                        GradeCode NVARCHAR(50) NULL,
                        Description NVARCHAR(500) NULL,
                        MinimumSalary DECIMAL(18,2) NOT NULL CONSTRAINT DF_PAY_SalaryStructures_MinimumSalary DEFAULT (0),
                        MaximumSalary DECIMAL(18,2) NOT NULL CONSTRAINT DF_PAY_SalaryStructures_MaximumSalary DEFAULT (0),
                        IsActive BIT NOT NULL CONSTRAINT DF_PAY_SalaryStructures_IsActive DEFAULT (1),
                        EffectiveFrom DATE NOT NULL,
                        EffectiveTo DATE NULL,
                        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PAY_SalaryStructures_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT PK_PAY_SalaryStructures PRIMARY KEY CLUSTERED (SalaryStructureId),
                        CONSTRAINT FK_PAY_SalaryStructures_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID)
                    );

                    CREATE UNIQUE NONCLUSTERED INDEX UX_PAY_SalaryStructures_Org_Code ON dbo.PAY_SalaryStructures (OrganisationId, StructureCode);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_EmployeeCompensation')
                BEGIN
                    CREATE TABLE dbo.PAY_EmployeeCompensation (
                        EmployeeCompensationId INT IDENTITY(1,1) NOT NULL,
                        OrganisationId INT NOT NULL,
                        JobId INT NOT NULL,
                        IndividualId INT NOT NULL,
                        SalaryStructureId INT NULL,
                        BasicSalary DECIMAL(18,2) NOT NULL CONSTRAINT DF_PAY_EmployeeCompensation_BasicSalary DEFAULT (0),
                        GradeCode NVARCHAR(50) NULL,
                        StepNumber INT NULL,
                        CurrencyTypeId INT NOT NULL CONSTRAINT DF_PAY_EmployeeCompensation_CurrencyTypeId DEFAULT (1),
                        BankName NVARCHAR(100) NOT NULL CONSTRAINT DF_PAY_EmployeeCompensation_BankName DEFAULT (''),
                        BankBranch NVARCHAR(100) NOT NULL CONSTRAINT DF_PAY_EmployeeCompensation_BankBranch DEFAULT (''),
                        BankAccountNumber NVARCHAR(50) NOT NULL CONSTRAINT DF_PAY_EmployeeCompensation_BankAccountNumber DEFAULT (''),
                        AccountHolderName NVARCHAR(150) NOT NULL CONSTRAINT DF_PAY_EmployeeCompensation_AccountHolderName DEFAULT (''),
                        EffectiveFrom DATE NOT NULL,
                        EffectiveTo DATE NULL,
                        IsActive BIT NOT NULL CONSTRAINT DF_PAY_EmployeeCompensation_IsActive DEFAULT (1),
                        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PAY_EmployeeCompensation_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        CreatedByUserId INT NULL,
                        CONSTRAINT PK_PAY_EmployeeCompensation PRIMARY KEY CLUSTERED (EmployeeCompensationId),
                        CONSTRAINT FK_PAY_EmployeeCompensation_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
                        CONSTRAINT FK_PAY_EmployeeCompensation_Jobs FOREIGN KEY (JobId) REFERENCES dbo.Jobs (JobID)
                    );

                    CREATE NONCLUSTERED INDEX IX_PAY_EmployeeCompensation_Org_Job_Dates ON dbo.PAY_EmployeeCompensation (OrganisationId, JobId, EffectiveFrom, EffectiveTo);
                    CREATE NONCLUSTERED INDEX IX_PAY_EmployeeCompensation_Org_Individual ON dbo.PAY_EmployeeCompensation (OrganisationId, IndividualId);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_CalculationRules')
                BEGIN
                    CREATE TABLE dbo.PAY_CalculationRules (
                        CalculationRuleId INT IDENTITY(1,1) NOT NULL,
                        OrganisationId INT NOT NULL,
                        RuleCode NVARCHAR(50) NOT NULL,
                        RuleName NVARCHAR(150) NOT NULL,
                        RuleType NVARCHAR(50) NOT NULL,
                        PercentageValue DECIMAL(18,4) NOT NULL CONSTRAINT DF_PAY_CalculationRules_PercentageValue DEFAULT (0),
                        FlatThresholdValue DECIMAL(18,2) NOT NULL CONSTRAINT DF_PAY_CalculationRules_FlatThresholdValue DEFAULT (0),
                        EffectiveFrom DATE NOT NULL,
                        EffectiveTo DATE NULL,
                        IsActive BIT NOT NULL CONSTRAINT DF_PAY_CalculationRules_IsActive DEFAULT (1),
                        ConfigurationJson NVARCHAR(MAX) NULL,
                        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PAY_CalculationRules_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT PK_PAY_CalculationRules PRIMARY KEY CLUSTERED (CalculationRuleId),
                        CONSTRAINT FK_PAY_CalculationRules_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID)
                    );

                    CREATE NONCLUSTERED INDEX IX_PAY_CalculationRules_Org_Code ON dbo.PAY_CalculationRules (OrganisationId, RuleCode);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollRuns')
                BEGIN
                    CREATE TABLE dbo.PAY_PayrollRuns (
                        PayrollRunId INT IDENTITY(1,1) NOT NULL,
                        OrganisationId INT NOT NULL,
                        PayrollPeriodId INT NOT NULL,
                        RunType INT NOT NULL CONSTRAINT DF_PAY_PayrollRuns_RunType DEFAULT (1),
                        Status INT NOT NULL CONSTRAINT DF_PAY_PayrollRuns_Status DEFAULT (1),
                        Title NVARCHAR(150) NOT NULL,
                        Description NVARCHAR(500) NULL,
                        TotalEmployees INT NOT NULL CONSTRAINT DF_PAY_PayrollRuns_TotalEmployees DEFAULT (0),
                        ProcessedEmployees INT NOT NULL CONSTRAINT DF_PAY_PayrollRuns_ProcessedEmployees DEFAULT (0),
                        TotalGrossEarnings DECIMAL(18,2) NOT NULL CONSTRAINT DF_PAY_PayrollRuns_TotalGross DEFAULT (0),
                        TotalDeductions DECIMAL(18,2) NOT NULL CONSTRAINT DF_PAY_PayrollRuns_TotalDeductions DEFAULT (0),
                        TotalEmployerContributions DECIMAL(18,2) NOT NULL CONSTRAINT DF_PAY_PayrollRuns_TotalEmployerContrib DEFAULT (0),
                        TotalNetPayable DECIMAL(18,2) NOT NULL CONSTRAINT DF_PAY_PayrollRuns_TotalNet DEFAULT (0),
                        PreparedByUserId INT NULL,
                        PreparedAtUtc DATETIME2 NULL,
                        ApprovedByUserId INT NULL,
                        ApprovedAtUtc DATETIME2 NULL,
                        FinalisedByUserId INT NULL,
                        FinalisedAtUtc DATETIME2 NULL,
                        RowVersion ROWVERSION NOT NULL,
                        CONSTRAINT PK_PAY_PayrollRuns PRIMARY KEY CLUSTERED (PayrollRunId),
                        CONSTRAINT FK_PAY_PayrollRuns_Periods FOREIGN KEY (PayrollPeriodId) REFERENCES dbo.PAY_PayrollPeriods (PayrollPeriodId),
                        CONSTRAINT FK_PAY_PayrollRuns_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID)
                    );

                    CREATE NONCLUSTERED INDEX IX_PAY_PayrollRuns_Org_Period ON dbo.PAY_PayrollRuns (OrganisationId, PayrollPeriodId);
                    CREATE NONCLUSTERED INDEX IX_PAY_PayrollRuns_Org_Status ON dbo.PAY_PayrollRuns (OrganisationId, Status);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollRunBatches')
                BEGIN
                    CREATE TABLE dbo.PAY_PayrollRunBatches (
                        PayrollRunBatchId INT IDENTITY(1,1) NOT NULL,
                        PayrollRunId INT NOT NULL,
                        BatchNumber INT NOT NULL,
                        TotalEmployees INT NOT NULL,
                        Status INT NOT NULL CONSTRAINT DF_PAY_PayrollRunBatches_Status DEFAULT (1),
                        StartedAtUtc DATETIME2 NULL,
                        CompletedAtUtc DATETIME2 NULL,
                        ErrorMessage NVARCHAR(1000) NULL,
                        RetryCount INT NOT NULL CONSTRAINT DF_PAY_PayrollRunBatches_RetryCount DEFAULT (0),
                        CONSTRAINT PK_PAY_PayrollRunBatches PRIMARY KEY CLUSTERED (PayrollRunBatchId),
                        CONSTRAINT FK_PAY_PayrollRunBatches_Runs FOREIGN KEY (PayrollRunId) REFERENCES dbo.PAY_PayrollRuns (PayrollRunId) ON DELETE CASCADE
                    );

                    CREATE UNIQUE NONCLUSTERED INDEX UX_PAY_PayrollRunBatches_Run_BatchNum ON dbo.PAY_PayrollRunBatches (PayrollRunId, BatchNumber);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollEmployees')
                BEGIN
                    CREATE TABLE dbo.PAY_PayrollEmployees (
                        PayrollEmployeeId INT IDENTITY(1,1) NOT NULL,
                        PayrollRunId INT NOT NULL,
                        PayrollRunBatchId INT NULL,
                        JobId INT NOT NULL,
                        IndividualId INT NOT NULL,
                        EmployeeNumber NVARCHAR(50) NOT NULL,
                        EmployeeName NVARCHAR(150) NOT NULL,
                        DepartmentName NVARCHAR(150) NOT NULL CONSTRAINT DF_PAY_PayrollEmployees_Dept DEFAULT (''),
                        PositionTitle NVARCHAR(150) NOT NULL CONSTRAINT DF_PAY_PayrollEmployees_Pos DEFAULT (''),
                        GradeCode NVARCHAR(50) NULL,
                        StepNumber INT NULL,
                        BaseSalary DECIMAL(18,2) NOT NULL,
                        GrossEarnings DECIMAL(18,2) NOT NULL,
                        TotalDeductions DECIMAL(18,2) NOT NULL,
                        EmployerContributions DECIMAL(18,2) NOT NULL CONSTRAINT DF_PAY_PayrollEmployees_EmployerContrib DEFAULT (0),
                        NetSalary DECIMAL(18,2) NOT NULL,
                        CalculationStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_PAY_PayrollEmployees_Status DEFAULT ('Calculated'),
                        ErrorMessage NVARCHAR(1000) NULL,
                        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PAY_PayrollEmployees_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT PK_PAY_PayrollEmployees PRIMARY KEY CLUSTERED (PayrollEmployeeId),
                        CONSTRAINT FK_PAY_PayrollEmployees_Runs FOREIGN KEY (PayrollRunId) REFERENCES dbo.PAY_PayrollRuns (PayrollRunId) ON DELETE CASCADE,
                        CONSTRAINT FK_PAY_PayrollEmployees_Jobs FOREIGN KEY (JobId) REFERENCES dbo.Jobs (JobID)
                    );

                    CREATE UNIQUE NONCLUSTERED INDEX UX_PAY_PayrollEmployees_Run_Individual ON dbo.PAY_PayrollEmployees (PayrollRunId, IndividualId);
                    CREATE NONCLUSTERED INDEX IX_PAY_PayrollEmployees_Run_Batch ON dbo.PAY_PayrollEmployees (PayrollRunId, PayrollRunBatchId);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollLines')
                BEGIN
                    CREATE TABLE dbo.PAY_PayrollLines (
                        PayrollLineId INT IDENTITY(1,1) NOT NULL,
                        PayrollEmployeeId INT NOT NULL,
                        PayrollRunId INT NOT NULL,
                        ComponentCode NVARCHAR(50) NOT NULL,
                        ComponentName NVARCHAR(150) NOT NULL,
                        ComponentType INT NOT NULL,
                        Rate DECIMAL(18,4) NOT NULL CONSTRAINT DF_PAY_PayrollLines_Rate DEFAULT (0),
                        Units DECIMAL(18,4) NOT NULL CONSTRAINT DF_PAY_PayrollLines_Units DEFAULT (1),
                        CalculatedAmount DECIMAL(18,2) NOT NULL,
                        IsTaxable BIT NOT NULL CONSTRAINT DF_PAY_PayrollLines_IsTaxable DEFAULT (1),
                        IsPensionable BIT NOT NULL CONSTRAINT DF_PAY_PayrollLines_IsPensionable DEFAULT (1),
                        SourceReference NVARCHAR(200) NULL,
                        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PAY_PayrollLines_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT PK_PAY_PayrollLines PRIMARY KEY CLUSTERED (PayrollLineId),
                        CONSTRAINT FK_PAY_PayrollLines_Employees FOREIGN KEY (PayrollEmployeeId) REFERENCES dbo.PAY_PayrollEmployees (PayrollEmployeeId) ON DELETE CASCADE
                    );

                    CREATE NONCLUSTERED INDEX IX_PAY_PayrollLines_Employee_Component ON dbo.PAY_PayrollLines (PayrollEmployeeId, ComponentCode);
                    CREATE NONCLUSTERED INDEX IX_PAY_PayrollLines_RunId ON dbo.PAY_PayrollLines (PayrollRunId);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollInputSnapshots')
                BEGIN
                    CREATE TABLE dbo.PAY_PayrollInputSnapshots (
                        PayrollInputSnapshotId INT IDENTITY(1,1) NOT NULL,
                        PayrollRunId INT NOT NULL,
                        JobId INT NOT NULL,
                        IndividualId INT NOT NULL,
                        SnapshotVersion INT NOT NULL CONSTRAINT DF_PAY_PayrollInputSnapshots_Version DEFAULT (1),
                        SnapshotHash NVARCHAR(100) NOT NULL,
                        SnapshotJson NVARCHAR(MAX) NOT NULL,
                        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PAY_PayrollInputSnapshots_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT PK_PAY_PayrollInputSnapshots PRIMARY KEY CLUSTERED (PayrollInputSnapshotId),
                        CONSTRAINT FK_PAY_PayrollInputSnapshots_Runs FOREIGN KEY (PayrollRunId) REFERENCES dbo.PAY_PayrollRuns (PayrollRunId) ON DELETE CASCADE
                    );

                    CREATE UNIQUE NONCLUSTERED INDEX UX_PAY_PayrollInputSnapshots_Run_Job ON dbo.PAY_PayrollInputSnapshots (PayrollRunId, JobId);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollExceptions')
                BEGIN
                    CREATE TABLE dbo.PAY_PayrollExceptions (
                        PayrollExceptionId INT IDENTITY(1,1) NOT NULL,
                        PayrollRunId INT NOT NULL,
                        JobId INT NULL,
                        IndividualId INT NULL,
                        Severity INT NOT NULL CONSTRAINT DF_PAY_PayrollExceptions_Severity DEFAULT (1),
                        Code NVARCHAR(50) NOT NULL,
                        Message NVARCHAR(500) NOT NULL,
                        Details NVARCHAR(2000) NULL,
                        IsAcknowledged BIT NOT NULL CONSTRAINT DF_PAY_PayrollExceptions_IsAcknowledged DEFAULT (0),
                        AcknowledgedByUserId INT NULL,
                        AcknowledgedAtUtc DATETIME2 NULL,
                        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PAY_PayrollExceptions_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT PK_PAY_PayrollExceptions PRIMARY KEY CLUSTERED (PayrollExceptionId),
                        CONSTRAINT FK_PAY_PayrollExceptions_Runs FOREIGN KEY (PayrollRunId) REFERENCES dbo.PAY_PayrollRuns (PayrollRunId) ON DELETE CASCADE
                    );

                    CREATE NONCLUSTERED INDEX IX_PAY_PayrollExceptions_Run_Severity ON dbo.PAY_PayrollExceptions (PayrollRunId, Severity);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollAdjustments')
                BEGIN
                    CREATE TABLE dbo.PAY_PayrollAdjustments (
                        PayrollAdjustmentId INT IDENTITY(1,1) NOT NULL,
                        OrganisationId INT NOT NULL,
                        JobId INT NOT NULL,
                        IndividualId INT NOT NULL,
                        PayrollRunId INT NULL,
                        ComponentCode NVARCHAR(50) NOT NULL,
                        Description NVARCHAR(250) NOT NULL,
                        Amount DECIMAL(18,2) NOT NULL,
                        IsAddition BIT NOT NULL CONSTRAINT DF_PAY_PayrollAdjustments_IsAddition DEFAULT (1),
                        ApprovedByUserId INT NULL,
                        ApprovedAtUtc DATETIME2 NULL,
                        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PAY_PayrollAdjustments_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT PK_PAY_PayrollAdjustments PRIMARY KEY CLUSTERED (PayrollAdjustmentId),
                        CONSTRAINT FK_PAY_PayrollAdjustments_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID)
                    );

                    CREATE NONCLUSTERED INDEX IX_PAY_PayrollAdjustments_Org_Job_Run ON dbo.PAY_PayrollAdjustments (OrganisationId, JobId, PayrollRunId);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PaymentBatches')
                BEGIN
                    CREATE TABLE dbo.PAY_PaymentBatches (
                        PaymentBatchId INT IDENTITY(1,1) NOT NULL,
                        OrganisationId INT NOT NULL,
                        PayrollRunId INT NOT NULL,
                        BatchReference NVARCHAR(100) NOT NULL,
                        BankCode NVARCHAR(50) NOT NULL CONSTRAINT DF_PAY_PaymentBatches_BankCode DEFAULT ('BML'),
                        FormatCode NVARCHAR(50) NOT NULL CONSTRAINT DF_PAY_PaymentBatches_FormatCode DEFAULT ('CSV'),
                        TotalAmount DECIMAL(18,2) NOT NULL,
                        TotalCount INT NOT NULL,
                        Status INT NOT NULL CONSTRAINT DF_PAY_PaymentBatches_Status DEFAULT (1),
                        ExportedContent NVARCHAR(MAX) NULL,
                        CreatedByUserId INT NOT NULL,
                        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PAY_PaymentBatches_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        ApprovedByUserId INT NULL,
                        ApprovedAtUtc DATETIME2 NULL,
                        CONSTRAINT PK_PAY_PaymentBatches PRIMARY KEY CLUSTERED (PaymentBatchId),
                        CONSTRAINT FK_PAY_PaymentBatches_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID),
                        CONSTRAINT FK_PAY_PaymentBatches_Runs FOREIGN KEY (PayrollRunId) REFERENCES dbo.PAY_PayrollRuns (PayrollRunId)
                    );

                    CREATE UNIQUE NONCLUSTERED INDEX UX_PAY_PaymentBatches_Org_Ref ON dbo.PAY_PaymentBatches (OrganisationId, BatchReference);
                    CREATE NONCLUSTERED INDEX IX_PAY_PaymentBatches_Org_Run ON dbo.PAY_PaymentBatches (OrganisationId, PayrollRunId);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PaymentTransactions')
                BEGIN
                    CREATE TABLE dbo.PAY_PaymentTransactions (
                        PaymentTransactionId INT IDENTITY(1,1) NOT NULL,
                        PaymentBatchId INT NOT NULL,
                        PayrollEmployeeId INT NOT NULL,
                        IndividualId INT NOT NULL,
                        EmployeeName NVARCHAR(150) NOT NULL,
                        BankAccountNumber NVARCHAR(50) NOT NULL,
                        BankName NVARCHAR(100) NOT NULL,
                        BankBranch NVARCHAR(100) NOT NULL,
                        Amount DECIMAL(18,2) NOT NULL,
                        CurrencyTypeId INT NOT NULL CONSTRAINT DF_PAY_PaymentTransactions_Currency DEFAULT (1),
                        PaymentReference NVARCHAR(100) NOT NULL,
                        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_PAY_PaymentTransactions_Status DEFAULT ('Prepared'),
                        TransactionReference NVARCHAR(100) NULL,
                        FailureReason NVARCHAR(500) NULL,
                        ProcessedAtUtc DATETIME2 NULL,
                        CONSTRAINT PK_PAY_PaymentTransactions PRIMARY KEY CLUSTERED (PaymentTransactionId),
                        CONSTRAINT FK_PAY_PaymentTransactions_Batches FOREIGN KEY (PaymentBatchId) REFERENCES dbo.PAY_PaymentBatches (PaymentBatchId) ON DELETE CASCADE,
                        CONSTRAINT FK_PAY_PaymentTransactions_Employees FOREIGN KEY (PayrollEmployeeId) REFERENCES dbo.PAY_PayrollEmployees (PayrollEmployeeId)
                    );

                    CREATE NONCLUSTERED INDEX IX_PAY_PaymentTransactions_Batch_Employee ON dbo.PAY_PaymentTransactions (PaymentBatchId, PayrollEmployeeId);
                    CREATE UNIQUE NONCLUSTERED INDEX UX_PAY_PaymentTransactions_PaymentRef ON dbo.PAY_PaymentTransactions (PaymentReference);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollAuditEvents')
                BEGIN
                    CREATE TABLE dbo.PAY_PayrollAuditEvents (
                        PayrollAuditEventId INT IDENTITY(1,1) NOT NULL,
                        OrganisationId INT NOT NULL,
                        PayrollRunId INT NULL,
                        UserId INT NOT NULL,
                        ActionType NVARCHAR(100) NOT NULL,
                        OldState NVARCHAR(100) NULL,
                        NewState NVARCHAR(100) NULL,
                        Description NVARCHAR(1000) NOT NULL,
                        TimestampUtc DATETIME2 NOT NULL CONSTRAINT DF_PAY_PayrollAuditEvents_TimestampUtc DEFAULT (SYSUTCDATETIME()),
                        IpAddress NVARCHAR(50) NULL,
                        CONSTRAINT PK_PAY_PayrollAuditEvents PRIMARY KEY CLUSTERED (PayrollAuditEventId),
                        CONSTRAINT FK_PAY_PayrollAuditEvents_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (BusinessEntityID)
                    );

                    CREATE NONCLUSTERED INDEX IX_PAY_PayrollAuditEvents_Org_Run_Time ON dbo.PAY_PayrollAuditEvents (OrganisationId, PayrollRunId, TimestampUtc);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_BackgroundJobs')
                BEGIN
                    CREATE TABLE dbo.PAY_BackgroundJobs (
                        JobId BIGINT IDENTITY(1,1) NOT NULL,
                        PayrollRunId INT NULL,
                        OrganisationId INT NOT NULL,
                        JobType INT NOT NULL,
                        Status INT NOT NULL CONSTRAINT DF_PAY_BackgroundJobs_Status DEFAULT (1),
                        Priority INT NOT NULL CONSTRAINT DF_PAY_BackgroundJobs_Priority DEFAULT (100),
                        AttemptCount INT NOT NULL CONSTRAINT DF_PAY_BackgroundJobs_AttemptCount DEFAULT (0),
                        MaxAttempts INT NOT NULL CONSTRAINT DF_PAY_BackgroundJobs_MaxAttempts DEFAULT (3),
                        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PAY_BackgroundJobs_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        StartedAtUtc DATETIME2 NULL,
                        CompletedAtUtc DATETIME2 NULL,
                        LeaseOwner NVARCHAR(150) NULL,
                        LeaseExpiresAtUtc DATETIME2 NULL,
                        FencingToken BIGINT NOT NULL CONSTRAINT DF_PAY_BackgroundJobs_FencingToken DEFAULT (0),
                        LastHeartbeatAtUtc DATETIME2 NULL,
                        ErrorMessage NVARCHAR(2000) NULL,
                        PayloadJson NVARCHAR(MAX) NULL,
                        CONSTRAINT PK_PAY_BackgroundJobs PRIMARY KEY CLUSTERED (JobId)
                    );

                    CREATE NONCLUSTERED INDEX IX_PAY_BackgroundJobs_Status_Priority_Created ON dbo.PAY_BackgroundJobs (Status, Priority, CreatedAtUtc);
                    CREATE NONCLUSTERED INDEX IX_PAY_BackgroundJobs_Org_Run ON dbo.PAY_BackgroundJobs (OrganisationId, PayrollRunId);
                    CREATE NONCLUSTERED INDEX IX_PAY_BackgroundJobs_Status_LeaseExpires ON dbo.PAY_BackgroundJobs (Status, LeaseExpiresAtUtc);
                END;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_BackgroundJobs') DROP TABLE dbo.PAY_BackgroundJobs;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollAuditEvents') DROP TABLE dbo.PAY_PayrollAuditEvents;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PaymentTransactions') DROP TABLE dbo.PAY_PaymentTransactions;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PaymentBatches') DROP TABLE dbo.PAY_PaymentBatches;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollAdjustments') DROP TABLE dbo.PAY_PayrollAdjustments;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollExceptions') DROP TABLE dbo.PAY_PayrollExceptions;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollInputSnapshots') DROP TABLE dbo.PAY_PayrollInputSnapshots;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollLines') DROP TABLE dbo.PAY_PayrollLines;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollEmployees') DROP TABLE dbo.PAY_PayrollEmployees;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollRunBatches') DROP TABLE dbo.PAY_PayrollRunBatches;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollRuns') DROP TABLE dbo.PAY_PayrollRuns;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_CalculationRules') DROP TABLE dbo.PAY_CalculationRules;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_EmployeeCompensation') DROP TABLE dbo.PAY_EmployeeCompensation;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_SalaryStructures') DROP TABLE dbo.PAY_SalaryStructures;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_SalaryComponents') DROP TABLE dbo.PAY_SalaryComponents;
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PAY_PayrollPeriods') DROP TABLE dbo.PAY_PayrollPeriods;
            ");
        }
    }
}
