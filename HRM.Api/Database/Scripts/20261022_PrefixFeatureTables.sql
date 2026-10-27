-- Rename feature tables in place. Existing IDs, data, keys, indexes and references stay attached.
-- Existing deployments: run this before starting the application with the updated EF mappings.
SET XACT_ABORT ON;
DECLARE @Revert bit = 0;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @Names TABLE(OldName sysname NOT NULL, NewName sysname NOT NULL);
    INSERT @Names VALUES
    (N'LateCheckInRequests', N'LC_LateCheckInRequests'),
    (N'DutyChangeRequests', N'DC_DutyChangeRequests'),
    (N'AdminAccessGrantRequests', N'AA_AdminAccessGrantRequests'),
    (N'StaffOnboardingRequests', N'SO_StaffOnboardingRequests'),
    (N'StaffNotifications', N'SN_StaffNotifications'),
    (N'RequestNotifications', N'RN_RequestNotifications'),
    (N'LeaveCarryForwards', N'LCF_LeaveCarryForwards'),
    (N'JobTypeLeaveAccrualRules', N'LA_JobTypeLeaveAccrualRules'),
    (N'LeaveAccrualPostings', N'LA_LeaveAccrualPostings'),
    (N'LeaveHandoverReviews', N'LH_LeaveHandoverReviews'),
    (N'LeaveHandoverSupervisors', N'LH_LeaveHandoverSupervisors'),
    (N'UserTodos', N'UT_UserTodos');
    IF @Revert = 1
        UPDATE @Names SET OldName = NewName, NewName = OldName;
    IF EXISTS(SELECT 1 FROM @Names n WHERE OBJECT_ID(N'dbo.' + n.OldName,N'U') IS NOT NULL AND OBJECT_ID(N'dbo.' + n.NewName,N'U') IS NOT NULL)
        THROW 51000, 'Both the original and prefixed table exist. Reconcile them before applying the rename.', 1;
    -- SQL Server updates foreign keys by object ID, but cannot rewrite module source text.
    IF EXISTS(SELECT 1 FROM sys.sql_modules m JOIN @Names n ON CHARINDEX(n.OldName,m.definition)>0
              WHERE OBJECT_ID(N'dbo.' + n.OldName,N'U') IS NOT NULL)
        THROW 51000, 'A SQL module references an original feature-table name. Update that module before applying the rename.', 1;
    DECLARE @Audit TABLE(OldName sysname, NewName sysname, ObjectId int, RowCountBefore bigint, RowCountAfter bigint);
    DECLARE @Old sysname, @New sysname, @Qualified nvarchar(300), @Sql nvarchar(max), @ObjectId int, @Rows bigint, @After bigint;
    DECLARE FeatureTables CURSOR LOCAL FAST_FORWARD FOR SELECT OldName,NewName FROM @Names ORDER BY OldName;
    OPEN FeatureTables;
    FETCH NEXT FROM FeatureTables INTO @Old,@New;
    WHILE @@FETCH_STATUS=0
    BEGIN
        SET @ObjectId=COALESCE(OBJECT_ID(N'dbo.'+@Old,N'U'),OBJECT_ID(N'dbo.'+@New,N'U'));
        IF @ObjectId IS NOT NULL
        BEGIN
            SET @Qualified=QUOTENAME(N'dbo')+N'.'+QUOTENAME(CASE WHEN OBJECT_ID(N'dbo.'+@Old,N'U') IS NOT NULL THEN @Old ELSE @New END);
            SET @Sql=N'SELECT @Count=COUNT_BIG(*) FROM '+@Qualified+N' WITH (TABLOCKX,HOLDLOCK)';
            EXEC sys.sp_executesql @Sql,N'@Count bigint OUTPUT',@Count=@Rows OUTPUT;
            IF OBJECT_ID(N'dbo.'+@Old,N'U') IS NOT NULL
                EXEC sys.sp_rename @objname=@Qualified,@newname=@New,@objtype=N'OBJECT';
            IF OBJECT_ID(N'dbo.'+@New,N'U')<>@ObjectId
                THROW 51000, 'Table identity changed unexpectedly during rename.', 1;
            SET @Sql=N'SELECT @Count=COUNT_BIG(*) FROM '+QUOTENAME(N'dbo')+N'.'+QUOTENAME(@New);
            EXEC sys.sp_executesql @Sql,N'@Count bigint OUTPUT',@Count=@After OUTPUT;
            IF @Rows<>@After THROW 51000, 'Row count changed unexpectedly during rename.', 1;
            INSERT @Audit VALUES(@Old,@New,@ObjectId,@Rows,@After);
        END;
        FETCH NEXT FROM FeatureTables INTO @Old,@New;
    END;
    CLOSE FeatureTables;
    DEALLOCATE FeatureTables;
    COMMIT;
    SELECT OldName,NewName,ObjectId,RowCountBefore,RowCountAfter FROM @Audit ORDER BY NewName;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
