using System;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    [DbContext(typeof(HrmTeContext))]
    [Migration("20261108110000_MaldivesCivilServiceAttendanceDeductions")]
    public sealed class MaldivesCivilServiceAttendanceDeductions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ATD_AttendanceDeductionPolicies')
                BEGIN
                    CREATE TABLE dbo.ATD_AttendanceDeductionPolicies (
                        AttendanceDeductionPolicyID BIGINT IDENTITY(1,1) NOT NULL,
                        OrganisationID INT NOT NULL,
                        PolicyCode NVARCHAR(50) NOT NULL,
                        PolicyName NVARCHAR(150) NOT NULL,
                        LegalReference NVARCHAR(250) NOT NULL,
                        EffectiveFrom DATE NOT NULL,
                        EffectiveTo DATE NULL,
                        IsActive BIT NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_IsActive DEFAULT (1),
                        IsVerified BIT NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_IsVerified DEFAULT (1),
                        SalaryDivisor INT NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_SalaryDivisor DEFAULT (30),
                        OfficialMinutesSource NVARCHAR(50) NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_OfficialMinutesSource DEFAULT ('ScheduledWorkPlan'),
                        DefaultDailyWorkingMinutes INT NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_DefaultDailyWorkingMinutes DEFAULT (360),
                        ArrivalBufferMinutes INT NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_ArrivalBufferMinutes DEFAULT (30),
                        BufferEligibilityCategory NVARCHAR(100) NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_BufferEligibilityCategory DEFAULT ('AllNonShift'),
                        BufferMakeUpRequired BIT NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_BufferMakeUpRequired DEFAULT (1),
                        BufferMakeUpWindow NVARCHAR(50) NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_BufferMakeUpWindow DEFAULT ('SameWeek'),
                        BufferExceededRule NVARCHAR(50) NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_BufferExceededRule DEFAULT ('RequireReview'),
                        LateDeductionFormula NVARCHAR(50) NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_LateDeductionFormula DEFAULT ('PerMinuteBasic'),
                        AbsenceDeductionFormula NVARCHAR(50) NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_AbsenceDeductionFormula DEFAULT ('BasicPlusServiceAllowance'),
                        DeductServiceAllowanceOnAbsence BIT NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_DeductServiceAllowanceOnAbsence DEFAULT (1),
                        ServiceAllowanceComponentCode NVARCHAR(50) NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_ServiceAllowanceComponentCode DEFAULT ('SERV_ALLOW'),
                        DeductFromPensionableSalary BIT NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_DeductFromPensionableSalary DEFAULT (1),
                        RoundingMethod NVARCHAR(50) NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_RoundingMethod DEFAULT ('AwayFromZero'),
                        RequiresVerification BIT NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_RequiresVerification DEFAULT (1),
                        ApprovedByUserID INT NULL,
                        ApprovedAtUtc DATETIME2(0) NULL,
                        CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        UpdatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ATD_AttendanceDeductionPolicies_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT PK_ATD_AttendanceDeductionPolicies PRIMARY KEY CLUSTERED (AttendanceDeductionPolicyID),
                        CONSTRAINT FK_ATD_AttendanceDeductionPolicies_Organisations FOREIGN KEY (OrganisationID) REFERENCES dbo.Organisations (BusinessEntityID)
                    );

                    CREATE UNIQUE NONCLUSTERED INDEX UX_ATD_AttendanceDeductionPolicies_Org_Code_Date ON dbo.ATD_AttendanceDeductionPolicies (OrganisationID, PolicyCode, EffectiveFrom);
                    CREATE NONCLUSTERED INDEX IX_ATD_AttendanceDeductionPolicies_Org_Active ON dbo.ATD_AttendanceDeductionPolicies (OrganisationID, IsActive);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ATD_AttendanceAdjustments')
                BEGIN
                    CREATE TABLE dbo.ATD_AttendanceAdjustments (
                        AttendanceAdjustmentID BIGINT IDENTITY(1,1) NOT NULL,
                        OrganisationID INT NOT NULL,
                        IndividualID INT NOT NULL,
                        JobID INT NOT NULL,
                        WorkDate DATE NOT NULL,
                        AdjustmentType NVARCHAR(50) NOT NULL,
                        ScheduledMinutes INT NOT NULL CONSTRAINT DF_ATD_AttendanceAdjustments_ScheduledMinutes DEFAULT (0),
                        ActualMinutes INT NOT NULL CONSTRAINT DF_ATD_AttendanceAdjustments_ActualMinutes DEFAULT (0),
                        DeductibleMinutes INT NOT NULL CONSTRAINT DF_ATD_AttendanceAdjustments_DeductibleMinutes DEFAULT (0),
                        EstimatedDeductionAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_ATD_AttendanceAdjustments_EstimatedDeductionAmount DEFAULT (0),
                        BasicSalaryDeductionAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_ATD_AttendanceAdjustments_BasicSalaryDeductionAmount DEFAULT (0),
                        ServiceAllowanceDeductionAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_ATD_AttendanceAdjustments_ServiceAllowanceDeductionAmount DEFAULT (0),
                        SourceAttendanceReference NVARCHAR(100) NOT NULL,
                        SourceObligationId BIGINT NULL,
                        AttendanceOutcome NVARCHAR(50) NOT NULL CONSTRAINT DF_ATD_AttendanceAdjustments_AttendanceOutcome DEFAULT ('PendingVerification'),
                        VerificationStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_ATD_AttendanceAdjustments_VerificationStatus DEFAULT ('PendingVerification'),
                        VerifiedByUserID INT NULL,
                        VerifiedAtUtc DATETIME2(0) NULL,
                        ReviewerComments NVARCHAR(1000) NULL,
                        PolicyId BIGINT NOT NULL,
                        PolicyCode NVARCHAR(50) NOT NULL,
                        PolicyVersion NVARCHAR(50) NOT NULL,
                        RegulatorySource NVARCHAR(250) NOT NULL,
                        IsRegulatoryAssumptionVerified BIT NOT NULL CONSTRAINT DF_ATD_AttendanceAdjustments_IsRegulatoryAssumptionVerified DEFAULT (1),
                        PayrollProcessingStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_ATD_AttendanceAdjustments_PayrollProcessingStatus DEFAULT ('Pending'),
                        PayrollRunID INT NULL,
                        PayrollEmployeeID INT NULL,
                        PayrollLineID INT NULL,
                        CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ATD_AttendanceAdjustments_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        UpdatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ATD_AttendanceAdjustments_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
                        CONSTRAINT PK_ATD_AttendanceAdjustments PRIMARY KEY CLUSTERED (AttendanceAdjustmentID),
                        CONSTRAINT FK_ATD_AttendanceAdjustments_Organisations FOREIGN KEY (OrganisationID) REFERENCES dbo.Organisations (BusinessEntityID),
                        CONSTRAINT FK_ATD_AttendanceAdjustments_Individuals FOREIGN KEY (IndividualID) REFERENCES dbo.BE_Individuals (BusinessEntityID),
                        CONSTRAINT FK_ATD_AttendanceAdjustments_Jobs FOREIGN KEY (JobID) REFERENCES dbo.Jobs (JobID)
                    );

                    CREATE UNIQUE NONCLUSTERED INDEX UX_ATD_AttendanceAdjustments_Org_Job_Date_Type ON dbo.ATD_AttendanceAdjustments (OrganisationID, JobID, WorkDate, AdjustmentType) WHERE [VerificationStatus] <> 'Cancelled';
                    CREATE NONCLUSTERED INDEX IX_ATD_AttendanceAdjustments_Org_Date_Status ON dbo.ATD_AttendanceAdjustments (OrganisationID, WorkDate, VerificationStatus);
                    CREATE NONCLUSTERED INDEX IX_ATD_AttendanceAdjustments_Org_PayrollStatus ON dbo.ATD_AttendanceAdjustments (OrganisationID, PayrollProcessingStatus, PayrollRunID);
                    CREATE NONCLUSTERED INDEX IX_ATD_AttendanceAdjustments_SourceRef ON dbo.ATD_AttendanceAdjustments (SourceAttendanceReference);
                END;

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ATD_AttendanceExceptionReviews')
                BEGIN
                    CREATE TABLE dbo.ATD_AttendanceExceptionReviews (
                        AttendanceExceptionReviewID BIGINT IDENTITY(1,1) NOT NULL,
                        AttendanceAdjustmentID BIGINT NOT NULL,
                        OrganisationID INT NOT NULL,
                        Action NVARCHAR(50) NOT NULL,
                        Reason NVARCHAR(1000) NOT NULL,
                        PerformedByUserID INT NOT NULL,
                        PerformedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ATD_AttendanceExceptionReviews_PerformedAtUtc DEFAULT (SYSUTCDATETIME()),
                        PreviousStatus NVARCHAR(50) NOT NULL,
                        NewStatus NVARCHAR(50) NOT NULL,
                        CONSTRAINT PK_ATD_AttendanceExceptionReviews PRIMARY KEY CLUSTERED (AttendanceExceptionReviewID),
                        CONSTRAINT FK_ATD_AttendanceExceptionReviews_Adjustments FOREIGN KEY (AttendanceAdjustmentID) REFERENCES dbo.ATD_AttendanceAdjustments (AttendanceAdjustmentID) ON DELETE CASCADE,
                        CONSTRAINT FK_ATD_AttendanceExceptionReviews_Organisations FOREIGN KEY (OrganisationID) REFERENCES dbo.Organisations (BusinessEntityID)
                    );

                    CREATE NONCLUSTERED INDEX IX_ATD_AttendanceExceptionReviews_Adj_Time ON dbo.ATD_AttendanceExceptionReviews (AttendanceAdjustmentID, PerformedAtUtc);
                END;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS dbo.ATD_AttendanceExceptionReviews;
                DROP TABLE IF EXISTS dbo.ATD_AttendanceAdjustments;
                DROP TABLE IF EXISTS dbo.ATD_AttendanceDeductionPolicies;
            ");
        }
    }
}
