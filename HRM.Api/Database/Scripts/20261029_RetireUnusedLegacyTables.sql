-- Reviewed scope: eighteen user-named legacy tables. No operational records are discarded.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @Migration nvarchar(150)=N'RetireUnusedLegacyTables';
    DECLARE @Renames TABLE(OldName sysname PRIMARY KEY,NewName sysname);
    INSERT @Renames VALUES
      (N'AdditionOrDeductionTypes',N'PY_AdditionOrDeductionTypes'),
      (N'BreakTimes',N'BT_BreakTimes'),(N'BudgetItems',N'BG_BudgetItems'),
      (N'BusinessEntitiesDocuments',N'BE_BusinessEntityDocuments'),
      (N'BusinessEntityCalendars',N'BE_BusinessEntityCalendars'),
      (N'BusinessEntityLocations',N'BE_BusinessEntityLocations'),
      (N'BusinessEntityLocationTypes',N'BE_BusinessEntityLocationTypes'),
      (N'BusinessEntitySchedules',N'BE_BusinessEntitySchedules'),
      (N'BusinessEntityTypes',N'BE_BusinessEntityTypes'),
      (N'BussinessEntityContactInformations',N'BE_BusinessEntityContacts');
    DECLARE @Retire TABLE(Name sysname PRIMARY KEY,DropOrder int,AllowLookupRows bit);
    INSERT @Retire VALUES
      (N'AdditionDeductionTypeDependencyTimePeriodAmounts',1,0),
      (N'BudgetTransactions',2,0),(N'BudgetTransactionTypes',3,1),
      (N'BusinessEntityRelatedLocationTypes',4,0),
      (N'BusinessEntityRelationAssignedRoles',5,0),(N'BusinessEntityRequests',6,0),
      (N'BusinessEntityRelations',7,0),(N'BusinessEntityRelationStates',8,1);

    IF EXISTS(SELECT 1 FROM @Renames WHERE OBJECT_ID(N'dbo.'+OldName,N'U') IS NOT NULL AND OBJECT_ID(N'dbo.'+NewName,N'U') IS NOT NULL)
      THROW 51040,'Both old and new table names exist. Resolve the collision before cleanup.',1;
    IF EXISTS(SELECT 1 FROM @Renames WHERE OBJECT_ID(N'dbo.'+OldName,N'U') IS NULL AND OBJECT_ID(N'dbo.'+NewName,N'U') IS NULL)
      THROW 51040,'A retained table is missing. Cleanup was not committed.',1;
    IF EXISTS(SELECT 1 FROM sys.foreign_keys f JOIN @Retire r ON OBJECT_ID(N'dbo.'+r.Name)=f.referenced_object_id
      WHERE NOT EXISTS(SELECT 1 FROM @Retire c WHERE OBJECT_ID(N'dbo.'+c.Name)=f.parent_object_id))
      THROW 51040,'An outside table references a retirement candidate. Review the dependency before cleanup.',1;
    IF EXISTS(SELECT 1 FROM sys.sql_expression_dependencies d WHERE d.referenced_id IN
      (SELECT OBJECT_ID(N'dbo.'+Name) FROM @Retire UNION ALL SELECT OBJECT_ID(N'dbo.'+OldName) FROM @Renames))
      THROW 51040,'A SQL module references a target table. Review the dependency before cleanup.',1;
    IF EXISTS(SELECT 1 FROM sys.sql_modules m CROSS JOIN
      (SELECT Name FROM @Retire UNION ALL SELECT OldName FROM @Renames)n
      WHERE PATINDEX(N'%[^A-Za-z0-9_]'+n.Name+N'[^A-Za-z0-9_]%',N' '+m.definition+N' ')>0)
      THROW 51040,'A SQL module contains a target table reference. Cleanup was not committed.',1;
    IF EXISTS(SELECT 1 FROM sys.triggers t JOIN @Retire r ON t.parent_id=OBJECT_ID(N'dbo.'+r.Name))
      THROW 51040,'A retirement candidate has a trigger. Review it before cleanup.',1;

    IF OBJECT_ID(N'dbo.SC_TableRetirementAudits',N'U') IS NULL
      CREATE TABLE dbo.SC_TableRetirementAudits(
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_SC_TableRetirementAudits PRIMARY KEY,
        MigrationId nvarchar(150) NOT NULL,OriginalTableName nvarchar(128) NOT NULL,
        RecordCount bigint NOT NULL,SchemaJson nvarchar(max) NOT NULL,DataJson nvarchar(max) NOT NULL,
        ArchivedAtUtc datetime2 NOT NULL,Reason nvarchar(500) NOT NULL);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.SC_TableRetirementAudits') AND name=N'IX_SC_TableRetirementAudits_MigrationId_OriginalTableName')
      CREATE UNIQUE INDEX IX_SC_TableRetirementAudits_MigrationId_OriginalTableName ON dbo.SC_TableRetirementAudits(MigrationId,OriginalTableName);

    DECLARE @Name sysname,@Id int,@Allow bit,@Count bigint,@Data nvarchar(max),@Schema nvarchar(max),@Sql nvarchar(max),@Qualified nvarchar(300);
    DECLARE RetireAudit CURSOR LOCAL FAST_FORWARD FOR SELECT Name,AllowLookupRows FROM @Retire ORDER BY DropOrder;
    OPEN RetireAudit;FETCH NEXT FROM RetireAudit INTO @Name,@Allow;
    WHILE @@FETCH_STATUS=0
    BEGIN
      SET @Id=OBJECT_ID(N'dbo.'+@Name,N'U');
      IF @Id IS NOT NULL
      BEGIN
        SET @Qualified=N'dbo.'+QUOTENAME(@Name);
        SET @Sql=N'SELECT @Count=COUNT_BIG(*) FROM '+@Qualified+N' WITH(TABLOCKX,HOLDLOCK); SELECT @Data=(SELECT * FROM '+@Qualified+N' FOR JSON PATH,INCLUDE_NULL_VALUES);';
        EXEC sys.sp_executesql @Sql,N'@Count bigint OUTPUT,@Data nvarchar(max) OUTPUT',@Count=@Count OUTPUT,@Data=@Data OUTPUT;
        IF @Allow=0 AND @Count<>0 THROW 51040,'An operational retirement candidate contains records. Reassess usage before deleting it.',1;
        IF EXISTS(SELECT 1 FROM dbo.SC_TableRetirementAudits WHERE MigrationId=@Migration AND OriginalTableName=@Name)
          THROW 51040,'An archived table has reappeared. Review the existing archive before retiring it again.',1;
        SET @Schema=(SELECT @Name AS [table],
          JSON_QUERY((SELECT c.column_id,c.name,SCHEMA_NAME(t.schema_id) TypeSchema,t.name TypeName,c.max_length,c.precision,c.scale,c.is_nullable,c.collation_name,c.is_identity,
             CONVERT(nvarchar(100),i.seed_value) IdentitySeed,CONVERT(nvarchar(100),i.increment_value) IdentityIncrement,CONVERT(nvarchar(100),i.last_value) IdentityLast,
             d.name DefaultName,d.definition DefaultDefinition,cc.definition ComputedDefinition,cc.is_persisted
             FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id LEFT JOIN sys.identity_columns i ON i.object_id=c.object_id AND i.column_id=c.column_id
             LEFT JOIN sys.default_constraints d ON d.object_id=c.default_object_id LEFT JOIN sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id
             WHERE c.object_id=@Id ORDER BY c.column_id FOR JSON PATH,INCLUDE_NULL_VALUES)) AS Columns,
          JSON_QUERY((SELECT i.name,i.type_desc,i.is_unique,i.is_primary_key,i.is_unique_constraint,i.has_filter,i.filter_definition,
             JSON_QUERY((SELECT c.name,ic.key_ordinal,ic.is_descending_key,ic.is_included_column FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id ORDER BY ic.index_column_id FOR JSON PATH)) AS Columns
             FROM sys.indexes i WHERE i.object_id=@Id AND i.index_id>0 FOR JSON PATH,INCLUDE_NULL_VALUES)) AS Indexes,
          JSON_QUERY((SELECT f.name,OBJECT_SCHEMA_NAME(f.referenced_object_id) ReferenceSchema,OBJECT_NAME(f.referenced_object_id) ReferenceTable,f.delete_referential_action_desc,f.update_referential_action_desc,f.is_disabled,f.is_not_trusted,
             JSON_QUERY((SELECT COL_NAME(fc.parent_object_id,fc.parent_column_id) ParentColumn,COL_NAME(fc.referenced_object_id,fc.referenced_column_id) ReferencedColumn FROM sys.foreign_key_columns fc WHERE fc.constraint_object_id=f.object_id ORDER BY fc.constraint_column_id FOR JSON PATH)) AS Columns
             FROM sys.foreign_keys f WHERE f.parent_object_id=@Id FOR JSON PATH)) AS ForeignKeys,
          JSON_QUERY((SELECT name,definition,is_disabled,is_not_trusted FROM sys.check_constraints WHERE parent_object_id=@Id FOR JSON PATH)) AS Checks,
          JSON_QUERY((SELECT p.state_desc,p.permission_name,USER_NAME(p.grantee_principal_id) Grantee,p.minor_id FROM sys.database_permissions p WHERE p.class=1 AND p.major_id=@Id FOR JSON PATH)) AS Permissions
          FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
        IF ISJSON(@Schema)<>1 OR ISJSON(COALESCE(@Data,N'[]'))<>1 OR (SELECT COUNT_BIG(*) FROM OPENJSON(COALESCE(@Data,N'[]')))<>@Count
          THROW 51040,'Retirement evidence is incomplete. Cleanup was rolled back.',1;
        INSERT dbo.SC_TableRetirementAudits(MigrationId,OriginalTableName,RecordCount,SchemaJson,DataJson,ArchivedAtUtc,Reason)
        VALUES(@Migration,@Name,@Count,@Schema,COALESCE(@Data,N'[]'),SYSUTCDATETIME(),N'User-authorised cleanup: no current application consumer or outside FK/SQL module dependency. Lookup records and schema retained for recovery.');
      END;
      FETCH NEXT FROM RetireAudit INTO @Name,@Allow;
    END;
    CLOSE RetireAudit;DEALLOCATE RetireAudit;

    DECLARE @New sysname,@Before bigint,@After bigint;
    DECLARE RetainedNames CURSOR LOCAL FAST_FORWARD FOR SELECT OldName,NewName FROM @Renames;
    OPEN RetainedNames;FETCH NEXT FROM RetainedNames INTO @Name,@New;
    WHILE @@FETCH_STATUS=0
    BEGIN
      SET @Id=COALESCE(OBJECT_ID(N'dbo.'+@Name,N'U'),OBJECT_ID(N'dbo.'+@New,N'U'));
      SET @Qualified=N'dbo.'+QUOTENAME(CASE WHEN OBJECT_ID(N'dbo.'+@Name,N'U') IS NOT NULL THEN @Name ELSE @New END);
      SET @Sql=N'SELECT @Count=COUNT_BIG(*) FROM '+@Qualified+N' WITH(TABLOCKX,HOLDLOCK);';
      EXEC sys.sp_executesql @Sql,N'@Count bigint OUTPUT',@Count=@Before OUTPUT;
      IF OBJECT_ID(N'dbo.'+@Name,N'U') IS NOT NULL EXEC sys.sp_rename @objname=@Qualified,@newname=@New,@objtype=N'OBJECT';
      SET @Sql=N'SELECT @Count=COUNT_BIG(*) FROM dbo.'+QUOTENAME(@New);
      EXEC sys.sp_executesql @Sql,N'@Count bigint OUTPUT',@Count=@After OUTPUT;
      IF @Id<>OBJECT_ID(N'dbo.'+@New,N'U') OR @Before<>@After THROW 51040,'A retained table changed identity or record count. Cleanup was rolled back.',1;
      FETCH NEXT FROM RetainedNames INTO @Name,@New;
    END;
    CLOSE RetainedNames;DEALLOCATE RetainedNames;

    DECLARE DropUnused CURSOR LOCAL FAST_FORWARD FOR SELECT Name FROM @Retire ORDER BY DropOrder;
    OPEN DropUnused;FETCH NEXT FROM DropUnused INTO @Name;
    WHILE @@FETCH_STATUS=0
    BEGIN
      IF OBJECT_ID(N'dbo.'+@Name,N'U') IS NOT NULL BEGIN SET @Sql=N'DROP TABLE dbo.'+QUOTENAME(@Name);EXEC sys.sp_executesql @Sql;END;
      FETCH NEXT FROM DropUnused INTO @Name;
    END;
    CLOSE DropUnused;DEALLOCATE DropUnused;
    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_SC_TableRetirementAudits_Immutable ON dbo.SC_TableRetirementAudits INSTEAD OF UPDATE,DELETE AS BEGIN SET NOCOUNT ON; THROW 51040,''Table retirement evidence is immutable.'',1; END;');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
