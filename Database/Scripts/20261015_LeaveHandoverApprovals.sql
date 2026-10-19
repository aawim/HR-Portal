SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.LeaveWorkHandOvers', 'TaskDetails') IS NULL
    ALTER TABLE dbo.LeaveWorkHandOvers ADD TaskDetails nvarchar(250) NULL;
IF COL_LENGTH('dbo.LeaveWorkHandOvers', 'Decision') IS NULL
    ALTER TABLE dbo.LeaveWorkHandOvers ADD Decision nvarchar(20) NOT NULL CONSTRAINT DF_LeaveWorkHandOvers_Decision DEFAULT(N'Pending');
IF COL_LENGTH('dbo.LeaveWorkHandOvers', 'Comments') IS NULL
    ALTER TABLE dbo.LeaveWorkHandOvers ADD Comments nvarchar(250) NULL;
IF COL_LENGTH('dbo.LeaveWorkHandOvers', 'DecisionAt') IS NULL
    ALTER TABLE dbo.LeaveWorkHandOvers ADD DecisionAt datetime2 NULL;
IF OBJECT_ID('dbo.LeaveHandoverReviews', 'U') IS NULL
    CREATE TABLE dbo.LeaveHandoverReviews (
        LeaveId int NOT NULL PRIMARY KEY REFERENCES dbo.Leaves(LeaveID),
        Revision int NOT NULL DEFAULT(1),
        SupervisorDecision nvarchar(20) NOT NULL DEFAULT(N'Pending'),
        SupervisorApprovedByIndividualId int NULL REFERENCES dbo.Staffs(IndividualID),
        Comments nvarchar(250) NULL,
        DecisionAt datetime2 NULL,
        OperationLogId int NULL REFERENCES dbo.OperationLogs(OperationLogID));
IF OBJECT_ID('dbo.LeaveHandoverSupervisors', 'U') IS NULL
    CREATE TABLE dbo.LeaveHandoverSupervisors (
        LeaveId int NOT NULL REFERENCES dbo.LeaveHandoverReviews(LeaveId),
        IndividualId int NOT NULL REFERENCES dbo.Staffs(IndividualID),
        PRIMARY KEY(LeaveId, IndividualId));
COMMIT;
