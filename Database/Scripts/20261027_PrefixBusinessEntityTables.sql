-- Data-preserving business entity registry table prefix upgrade.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    -- These two catalogues existed in legacy databases but were missing from the EF model.
    IF OBJECT_ID(N'dbo.VerifiedStates',N'U') IS NULL AND OBJECT_ID(N'dbo.BE_VerifiedStates',N'U') IS NULL
        CREATE TABLE dbo.BE_VerifiedStates(VerifiedStateID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_VerifiedStates PRIMARY KEY,StateName varchar(250) NOT NULL);
    IF OBJECT_ID(N'dbo.BusinessEntityStates',N'U') IS NULL AND OBJECT_ID(N'dbo.BE_BusinessEntityStates',N'U') IS NULL
        CREATE TABLE dbo.BE_BusinessEntityStates(BusinessEntityStateID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_BusinessEntityStates PRIMARY KEY,StateName varchar(250) NOT NULL);
    DECLARE @Names TABLE(OldName sysname,NewName sysname);
    INSERT @Names VALUES(N'Individuals',N'BE_Individuals'),(N'BusinessEntities',N'BE_BusinessEntities'),(N'VerifiedStates',N'BE_VerifiedStates'),(N'BusinessEntityStates',N'BE_BusinessEntityStates');
    IF EXISTS(SELECT 1 FROM @Names n WHERE OBJECT_ID(N'dbo.'+n.OldName,N'U') IS NOT NULL AND OBJECT_ID(N'dbo.'+n.NewName,N'U') IS NOT NULL)
        THROW 51030,'Both registry table names exist. Resolve the collision before renaming.',1;
    IF EXISTS(SELECT 1 FROM @Names n WHERE OBJECT_ID(N'dbo.'+n.OldName,N'U') IS NULL AND OBJECT_ID(N'dbo.'+n.NewName,N'U') IS NULL)
        THROW 51030,'A required registry table is missing. No rename has been committed.',1;
    -- The reviewed legacy procedure is rewritten in place, preserving its signature/body/permissions.
    -- Unreviewed dependent SQL modules require explicit review rather than a blind global rewrite.
    IF EXISTS(SELECT 1 FROM sys.sql_modules m CROSS JOIN @Names n
        WHERE PATINDEX(N'%[^A-Za-z0-9_]'+n.OldName+N'[^A-Za-z0-9_]%',N' '+m.definition+N' ')>0
        AND m.object_id<>COALESCE(OBJECT_ID(N'dbo.UserGetInfo',N'P'),-1))
        THROW 51030,'An unreviewed SQL module references a registry table. Review the dependency before renaming.',1;
    IF EXISTS(SELECT 1 FROM sys.crypt_properties WHERE major_id=OBJECT_ID(N'dbo.UserGetInfo',N'P'))
        THROW 51030,'UserGetInfo is signed. Review and re-sign it before the registry table rename.',1;
    DECLARE @Old sysname,@New sysname,@Qualified nvarchar(300),@Id int,@Before bigint,@After bigint,@Sql nvarchar(max);
    DECLARE RegistryTables CURSOR LOCAL FAST_FORWARD FOR SELECT OldName,NewName FROM @Names;
    OPEN RegistryTables;FETCH NEXT FROM RegistryTables INTO @Old,@New;
    WHILE @@FETCH_STATUS=0
    BEGIN
        SET @Id=COALESCE(OBJECT_ID(N'dbo.'+@Old,N'U'),OBJECT_ID(N'dbo.'+@New,N'U'));
        SET @Qualified=N'dbo.'+QUOTENAME(CASE WHEN OBJECT_ID(N'dbo.'+@Old,N'U') IS NOT NULL THEN @Old ELSE @New END);
        SET @Sql=N'SELECT @Count=COUNT_BIG(*) FROM '+@Qualified+N' WITH(TABLOCKX,HOLDLOCK)';
        EXEC sys.sp_executesql @Sql,N'@Count bigint OUTPUT',@Count=@Before OUTPUT;
        IF OBJECT_ID(N'dbo.'+@Old,N'U') IS NOT NULL EXEC sys.sp_rename @objname=@Qualified,@newname=@New,@objtype=N'OBJECT';
        SET @Sql=N'SELECT @Count=COUNT_BIG(*) FROM dbo.'+QUOTENAME(@New);
        EXEC sys.sp_executesql @Sql,N'@Count bigint OUTPUT',@Count=@After OUTPUT;
        IF OBJECT_ID(N'dbo.'+@New,N'U')<>@Id OR @Before<>@After THROW 51030,'Registry table identity/count changed unexpectedly.',1;
        FETCH NEXT FROM RegistryTables INTO @Old,@New;
    END;
    CLOSE RegistryTables;DEALLOCATE RegistryTables;
    DECLARE @Procedure nvarchar(max)=OBJECT_DEFINITION(OBJECT_ID(N'dbo.UserGetInfo',N'P'));
    IF @Procedure IS NOT NULL
    BEGIN
        DECLARE ProcedureNames CURSOR LOCAL FAST_FORWARD FOR SELECT OldName,NewName FROM @Names;
        OPEN ProcedureNames;FETCH NEXT FROM ProcedureNames INTO @Old,@New;
        WHILE @@FETCH_STATUS=0
        BEGIN
            IF PATINDEX(N'%[^A-Za-z0-9_]'+@Old+N'[^A-Za-z0-9_]%',N' '+@Procedure+N' ')>0 AND CHARINDEX(@New,@Procedure)=0
                SET @Procedure=REPLACE(@Procedure,@Old,@New);
            FETCH NEXT FROM ProcedureNames INTO @Old,@New;
        END;
        CLOSE ProcedureNames;DEALLOCATE ProcedureNames;
        SET @Procedure=REPLACE(REPLACE(@Procedure,N'CREATE PROCEDURE',N'ALTER PROCEDURE'),N'CREATE PROC ',N'ALTER PROC ');
        EXEC sys.sp_executesql @Procedure;
    END;
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_BusinessEntities_VerifiedStates')
        ALTER TABLE dbo.BE_BusinessEntities WITH CHECK ADD CONSTRAINT FK_BusinessEntities_VerifiedStates FOREIGN KEY(VerifiedStateID) REFERENCES dbo.BE_VerifiedStates(VerifiedStateID);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_BusinessEntities_BusinessEntityStates')
        ALTER TABLE dbo.BE_BusinessEntities WITH CHECK ADD CONSTRAINT FK_BusinessEntities_BusinessEntityStates FOREIGN KEY(BusinessEntityStateID) REFERENCES dbo.BE_BusinessEntityStates(BusinessEntityStateID);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.BE_BusinessEntities') AND name=N'IX_BusinessEntities_VerifiedStateID')
        CREATE INDEX IX_BusinessEntities_VerifiedStateID ON dbo.BE_BusinessEntities(VerifiedStateID);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.BE_BusinessEntities') AND name=N'IX_BusinessEntities_BusinessEntityStateID')
        CREATE INDEX IX_BusinessEntities_BusinessEntityStateID ON dbo.BE_BusinessEntities(BusinessEntityStateID);
    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
