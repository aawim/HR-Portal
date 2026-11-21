$ErrorActionPreference = 'Stop'
$source = Get-Content -Raw 'HRM.Api/Migrations/20261115150000_OrganisationalAccessScopeFramework.cs'
$secretPath = Join-Path $env:APPDATA 'Microsoft/UserSecrets/hrm-local-development-20261022/secrets.json'
$connection = (Get-Content -Raw -LiteralPath $secretPath | ConvertFrom-Json).'ConnectionStrings:DefaultConnection'
$db = [System.Data.SqlClient.SqlConnection]::new($connection)
$db.Open()
$transaction = $db.BeginTransaction()
try {
    function Execute([string]$sql) { $command=$db.CreateCommand();$command.Transaction=$transaction;$command.CommandText=$sql;[void]$command.ExecuteNonQuery() }
    foreach($match in [regex]::Matches($source,'name: "(AC_[^"]+)",\s+columns: table => new\s+\{([\s\S]*?)\},\s+constraints: table =>\s+\{([\s\S]*?)\}\);')) {
        $table=$match.Groups[1].Value
        foreach($column in [regex]::Matches($match.Groups[2].Value,'(\w+) = table.Column<(\w+)>\(([^)]*)\)')) {
            $name=$column.Groups[1].Value;$kind=$column.Groups[2].Value;$argsText=$column.Groups[3].Value
            $type=@{int='int';long='bigint';bool='bit';DateTime='datetime2';string='nvarchar'}[$kind]
            $nullable=if($argsText.Contains('nullable: true')){1}else{0}
            $length=if($argsText -match 'maxLength: (\d+)'){[int]$Matches[1]*2}else{0}
            Execute "IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.$table') AND name='$name' AND TYPE_NAME(user_type_id)='$type' AND is_nullable=$nullable AND ($length=0 OR max_length >= $length)) THROW 51000,'Access scope schema mismatch: $table.$name',1;"
        }
        $id=if($table -eq 'AC_UserAccessScopes'){'UserAccessScopeId'}else{'UserAccessScopeAuditId'}
        Execute "IF COLUMNPROPERTY(OBJECT_ID('dbo.$table'),'$id','IsIdentity')<>1 OR NOT EXISTS(SELECT 1 FROM sys.indexes i JOIN sys.index_columns ic ON i.object_id=ic.object_id AND i.index_id=ic.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE i.object_id=OBJECT_ID('dbo.$table') AND i.is_primary_key=1 AND c.name='$id') THROW 51000,'Access scope primary key mismatch',1;"
        foreach($fk in [regex]::Matches($match.Groups[3].Value,'table.ForeignKey\("([^"]+)", x => x.(\w+), "([^"]+)", "([^"]+)"')) {
            $name=$fk.Groups[1].Value;$column=$fk.Groups[2].Value;$target=$fk.Groups[3].Value;$targetColumn=$fk.Groups[4].Value
            Execute "IF NOT EXISTS(SELECT 1 FROM sys.foreign_key_columns f JOIN sys.columns c ON c.object_id=f.parent_object_id AND c.column_id=f.parent_column_id JOIN sys.columns r ON r.object_id=f.referenced_object_id AND r.column_id=f.referenced_column_id JOIN sys.foreign_keys k ON k.object_id=f.constraint_object_id WHERE f.parent_object_id=OBJECT_ID('dbo.$table') AND c.name='$column' AND f.referenced_object_id=OBJECT_ID('dbo.$target') AND r.name='$targetColumn' AND k.delete_referential_action=0 AND k.is_disabled=0 AND k.is_not_trusted=0) ALTER TABLE dbo.[$table] WITH CHECK ADD CONSTRAINT [$name] FOREIGN KEY ([$column]) REFERENCES dbo.[$target]([$targetColumn]);"
        }
    }
    foreach($index in [regex]::Matches($source,'migrationBuilder.CreateIndex\("([^"]+)", "([^"]+)", new\[\] \{ ([^}]+) \}\)')) {
        $name=$index.Groups[1].Value;$table=$index.Groups[2].Value;$columns=($index.Groups[3].Value -replace '"','')
        Execute "IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.$table') AND name='$name') CREATE INDEX [$name] ON dbo.[$table]($columns);"
    }
    Execute "IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.AC_UserAccessScopes') AND name='CK_AC_UserAccessScopes_Subject') ALTER TABLE dbo.AC_UserAccessScopes WITH CHECK ADD CONSTRAINT CK_AC_UserAccessScopes_Subject CHECK ((UserId IS NOT NULL AND UserGroupId IS NULL) OR (UserId IS NULL AND UserGroupId IS NOT NULL)); ALTER TABLE dbo.AC_UserAccessScopes WITH CHECK CHECK CONSTRAINT CK_AC_UserAccessScopes_Subject;"
    Execute "IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.AC_UserAccessScopes') AND name='CK_AC_UserAccessScopes_ScopeType') ALTER TABLE dbo.AC_UserAccessScopes WITH CHECK ADD CONSTRAINT CK_AC_UserAccessScopes_ScopeType CHECK (ScopeType IN ('Self','Team','Department','Division','DivisionAndDescendants','Organisation','Custom')); ALTER TABLE dbo.AC_UserAccessScopes WITH CHECK CHECK CONSTRAINT CK_AC_UserAccessScopes_ScopeType;"
    $seed=[regex]::Match($source,'migrationBuilder.Sql\("""([\s\S]*?)"""\)').Groups[1].Value
    if([string]::IsNullOrWhiteSpace($seed)){throw 'Permission seed SQL not found'}
    Execute $seed
    Execute "IF NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261115150000_OrganisationalAccessScopeFramework') INSERT dbo.__EFMigrationsHistory(MigrationId,ProductVersion) VALUES('20261115150000_OrganisationalAccessScopeFramework','8.0.27');"
    $transaction.Commit()
    Write-Output 'Access-scope schema validated, missing constraints/indexes/permissions reconciled, migration history recorded.'
} catch { $transaction.Rollback(); throw } finally { $db.Dispose() }
