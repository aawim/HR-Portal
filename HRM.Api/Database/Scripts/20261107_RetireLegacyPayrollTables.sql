-- User-authorised retirement of legacy payroll system tables.
-- All data and schemas are archived into SC_TableRetirementAudits before dropping.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Migration nvarchar(150) = N'RetireLegacyPayrollTables';

    -- 1. Ensure retirement audit table exists
    IF OBJECT_ID(N'dbo.SC_TableRetirementAudits', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.SC_TableRetirementAudits(
            Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_SC_TableRetirementAudits PRIMARY KEY,
            MigrationId nvarchar(150) NOT NULL,
            OriginalTableName nvarchar(128) NOT NULL,
            RecordCount bigint NOT NULL,
            SchemaJson nvarchar(max) NOT NULL,
            DataJson nvarchar(max) NOT NULL,
            ArchivedAtUtc datetime2 NOT NULL,
            Reason nvarchar(500) NOT NULL
        );
        CREATE UNIQUE INDEX IX_SC_TableRetirementAudits_MigrationId_OriginalTableName
            ON dbo.SC_TableRetirementAudits(MigrationId, OriginalTableName);
    END;

    -- 2. Drop legacy stored procedures that reference these tables
    IF OBJECT_ID(N'dbo.CheckIfAnyIssuesInState', N'P') IS NOT NULL DROP PROCEDURE dbo.CheckIfAnyIssuesInState;
    IF OBJECT_ID(N'dbo.GetPayrollRequestsProcessingStateForStaff', N'P') IS NOT NULL DROP PROCEDURE dbo.GetPayrollRequestsProcessingStateForStaff;
    IF OBJECT_ID(N'dbo.PopulateAllPayrollCycleLinkedRequests', N'P') IS NOT NULL DROP PROCEDURE dbo.PopulateAllPayrollCycleLinkedRequests;
    IF OBJECT_ID(N'dbo.PopulateAllPayrollCycleLinkedRequests1', N'P') IS NOT NULL DROP PROCEDURE dbo.PopulateAllPayrollCycleLinkedRequests1;
    IF OBJECT_ID(N'dbo.PopulateAllPayrollCycleLinkedRequestsDateRange', N'P') IS NOT NULL DROP PROCEDURE dbo.PopulateAllPayrollCycleLinkedRequestsDateRange;

    -- 3. Drop incoming foreign keys from outside legacy tables
    IF OBJECT_ID(N'dbo.FK_AD_PayrollBatches_PayrollCycles_PayrollCycleRequestId', N'F') IS NOT NULL
        ALTER TABLE dbo.AD_PayrollBatches DROP CONSTRAINT FK_AD_PayrollBatches_PayrollCycles_PayrollCycleRequestId;

    IF OBJECT_ID(N'dbo.FK_StaffSalaries_PayrollCycles1', N'F') IS NOT NULL
        ALTER TABLE dbo.StaffSalaries DROP CONSTRAINT FK_StaffSalaries_PayrollCycles1;

    IF OBJECT_ID(N'dbo.FK_StaffPayrollItemTypeSchedules_PayrollCycles', N'F') IS NOT NULL
        ALTER TABLE dbo.StaffPayrollItemTypeSchedules DROP CONSTRAINT FK_StaffPayrollItemTypeSchedules_PayrollCycles;

    IF OBJECT_ID(N'dbo.FK_StaffDailyAttandanceSummaryIssues_PayrollCycleProcessingStates', N'F') IS NOT NULL
        ALTER TABLE dbo.StaffDailyAttandanceSummaryIssues DROP CONSTRAINT FK_StaffDailyAttandanceSummaryIssues_PayrollCycleProcessingStates;

    IF OBJECT_ID(N'dbo.FK_AttachedPayrollItemsProcessingWhiteLists_PayrollItemsProcessingWhiteLists', N'F') IS NOT NULL
        ALTER TABLE dbo.AttachedPayrollItemsProcessingWhiteLists DROP CONSTRAINT FK_AttachedPayrollItemsProcessingWhiteLists_PayrollItemsProcessingWhiteLists;

    IF OBJECT_ID(N'dbo.FK_DerivedOnPayrollItemTypes_PayrollItemTypes', N'F') IS NOT NULL
        ALTER TABLE dbo.DerivedOnPayrollItemTypes DROP CONSTRAINT FK_DerivedOnPayrollItemTypes_PayrollItemTypes;

    IF OBJECT_ID(N'dbo.FK_DerivedOnPayrollItemTypes_PayrollItemTypes1', N'F') IS NOT NULL
        ALTER TABLE dbo.DerivedOnPayrollItemTypes DROP CONSTRAINT FK_DerivedOnPayrollItemTypes_PayrollItemTypes1;

    IF OBJECT_ID(N'dbo.FK_OrganisationAllowanceTypes_Allowances', N'F') IS NOT NULL
        ALTER TABLE dbo.OrganisationPayrollItemTypes DROP CONSTRAINT FK_OrganisationAllowanceTypes_Allowances;

    IF OBJECT_ID(N'dbo.FK_PrimaryPayrollItemTypeSAPWageTypes_PayrollItemTypes', N'F') IS NOT NULL
        ALTER TABLE dbo.PrimaryPayrollItemTypeSAPWageTypes DROP CONSTRAINT FK_PrimaryPayrollItemTypeSAPWageTypes_PayrollItemTypes;

    IF OBJECT_ID(N'dbo.FK_SAPExceptions_NewPayrollItemTypes', N'F') IS NOT NULL
        ALTER TABLE dbo.SAPExceptions DROP CONSTRAINT FK_SAPExceptions_NewPayrollItemTypes;

    IF OBJECT_ID(N'dbo.FK_StaffAllowances_Allowances', N'F') IS NOT NULL
        ALTER TABLE dbo.StaffPayrollItemTypes DROP CONSTRAINT FK_StaffAllowances_Allowances;

    IF OBJECT_ID(N'dbo.FK_StaffSalaryPayrollItems_PayrollItemTypes', N'F') IS NOT NULL
        ALTER TABLE dbo.StaffSalaryPayrollItems DROP CONSTRAINT FK_StaffSalaryPayrollItems_PayrollItemTypes;

    IF OBJECT_ID(N'dbo.FK_PayrollItemTypeDependencyTimePeriodAmounts_PayrollItemTypes', N'F') IS NOT NULL
        ALTER TABLE dbo.PayrollItemTypeDependencyTimePeriodAmounts DROP CONSTRAINT FK_PayrollItemTypeDependencyTimePeriodAmounts_PayrollItemTypes;

    -- 4. Define retirement manifest in child-to-parent order
    DECLARE @Retire TABLE(Name sysname PRIMARY KEY, DropOrder int);
    INSERT @Retire VALUES
        (N'PayrollItemDailySAPSheetDetails', 1),
        (N'PayrollItemProcessingFlows', 2),
        (N'PayrollItemsProcessingWhiteLists', 3),
        (N'PayrollItemTypeAmounts', 4),
        (N'PayrollItemTypeDependencyAmounts', 5),
        (N'PayrollItemTypeProcessingCodes', 6),
        (N'PayrollConfigurableValues', 7),
        (N'PayrollConfigurableValueTypes', 8),
        (N'PayrollCycleLinkedRequests', 9),
        (N'PayrollCycleProcessingStates', 10),
        (N'PayrollItemTypes', 11),
        (N'PayrollCycles', 12);

    -- 5. Archive each table into SC_TableRetirementAudits
    DECLARE @Name sysname, @Id int, @Count bigint, @Data nvarchar(max), @Schema nvarchar(max), @Sql nvarchar(max), @Qualified nvarchar(300);
    DECLARE RetireCursor CURSOR LOCAL FAST_FORWARD FOR SELECT Name FROM @Retire ORDER BY DropOrder;
    OPEN RetireCursor;
    FETCH NEXT FROM RetireCursor INTO @Name;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Id = OBJECT_ID(N'dbo.' + @Name, N'U');
        IF @Id IS NOT NULL
        BEGIN
            SET @Qualified = N'dbo.' + QUOTENAME(@Name);
            SET @Sql = N'SELECT @Count = COUNT_BIG(*) FROM ' + @Qualified + N' WITH(TABLOCKX, HOLDLOCK); SELECT @Data = (SELECT * FROM ' + @Qualified + N' FOR JSON PATH, INCLUDE_NULL_VALUES);';
            EXEC sys.sp_executesql @Sql, N'@Count bigint OUTPUT, @Data nvarchar(max) OUTPUT', @Count = @Count OUTPUT, @Data = @Data OUTPUT;

            IF NOT EXISTS(SELECT 1 FROM dbo.SC_TableRetirementAudits WHERE MigrationId = @Migration AND OriginalTableName = @Name)
            BEGIN
                SET @Schema = (
                    SELECT @Name AS [table],
                    JSON_QUERY((SELECT c.column_id, c.name, SCHEMA_NAME(t.schema_id) TypeSchema, t.name TypeName, c.max_length, c.precision, c.scale, c.is_nullable
                                FROM sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id
                                WHERE c.object_id = @Id ORDER BY c.column_id FOR JSON PATH)) AS Columns
                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
                );

                INSERT dbo.SC_TableRetirementAudits(MigrationId, OriginalTableName, RecordCount, SchemaJson, DataJson, ArchivedAtUtc, Reason)
                VALUES(@Migration, @Name, @Count, ISNULL(@Schema, N'{}'), COALESCE(@Data, N'[]'), SYSUTCDATETIME(), N'User-authorised retirement of legacy payroll system tables replaced by Enterprise Payroll module.');
            END;
        END;
        FETCH NEXT FROM RetireCursor INTO @Name;
    END;
    CLOSE RetireCursor;
    DEALLOCATE RetireCursor;

    -- 6. Drop the retired tables in safe order
    DECLARE DropCursor CURSOR LOCAL FAST_FORWARD FOR SELECT Name FROM @Retire ORDER BY DropOrder;
    OPEN DropCursor;
    FETCH NEXT FROM DropCursor INTO @Name;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF OBJECT_ID(N'dbo.' + @Name, N'U') IS NOT NULL
        BEGIN
            SET @Sql = N'DROP TABLE dbo.' + QUOTENAME(@Name) + N';';
            EXEC sys.sp_executesql @Sql;
        END;
        FETCH NEXT FROM DropCursor INTO @Name;
    END;
    CLOSE DropCursor;
    DEALLOCATE DropCursor;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
