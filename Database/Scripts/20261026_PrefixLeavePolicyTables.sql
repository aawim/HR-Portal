-- Idempotent, in-place leave policy table renames. Existing rows, IDs and FK objects are retained.
SET XACT_ABORT ON;
DECLARE @ownTransaction bit=CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;
IF @ownTransaction=1 BEGIN TRANSACTION;
BEGIN TRY
 DECLARE @names TABLE(OldName sysname,NewName sysname);
 INSERT @names VALUES(N'LeavePolicies',N'LT_LeavePolicies'),(N'LeavePolicyAccrualRules',N'LT_LeavePolicyAccrualRules');
 DECLARE @old sysname,@new sysname,@object int,@before bigint,@after bigint,@sql nvarchar(max),@qualified nvarchar(300);
 DECLARE names_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT OldName,NewName FROM @names;
 OPEN names_cursor; FETCH NEXT FROM names_cursor INTO @old,@new;
 WHILE @@FETCH_STATUS=0
 BEGIN
  IF OBJECT_ID(N'dbo.'+QUOTENAME(@old),N'U') IS NOT NULL
  BEGIN
   IF OBJECT_ID(N'dbo.'+QUOTENAME(@new),N'U') IS NOT NULL THROW 51000,'Both old and prefixed leave policy tables exist. Review the collision before deployment.',1;
   SET @object=OBJECT_ID(N'dbo.'+QUOTENAME(@old),N'U');
   IF EXISTS(SELECT 1 FROM sys.sql_expression_dependencies d JOIN sys.sql_modules m ON m.object_id=d.referencing_id WHERE d.referenced_id=@object)
    THROW 51000,'A SQL module references an old leave policy table. Update its definition before deployment.',1;
   SET @sql=N'SELECT @count=COUNT_BIG(*) FROM dbo.'+QUOTENAME(@old);
   EXEC sys.sp_executesql @sql,N'@count bigint OUTPUT',@count=@before OUTPUT;
   SET @qualified=N'dbo.'+QUOTENAME(@old);
   EXEC sys.sp_rename @qualified,@new,N'OBJECT';
   IF OBJECT_ID(N'dbo.'+QUOTENAME(@new),N'U')<>@object THROW 51000,'A table object ID changed during rename.',1;
   SET @sql=N'SELECT @count=COUNT_BIG(*) FROM dbo.'+QUOTENAME(@new);
   EXEC sys.sp_executesql @sql,N'@count bigint OUTPUT',@count=@after OUTPUT;
   IF @before<>@after THROW 51000,'A leave policy row count changed during rename.',1;
  END
  ELSE IF OBJECT_ID(N'dbo.'+QUOTENAME(@new),N'U') IS NULL
   THROW 51000,'A required leave policy table is missing.',1;
  FETCH NEXT FROM names_cursor INTO @old,@new;
 END;
 CLOSE names_cursor; DEALLOCATE names_cursor;
 IF @ownTransaction=1 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF @ownTransaction=1 AND XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
