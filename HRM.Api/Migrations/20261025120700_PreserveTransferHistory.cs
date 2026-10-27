using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace HRM.Migrations;
public partial class PreserveTransferHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Legacy installations used different constraint names. Identify by table/column.
        migrationBuilder.Sql(@"
DECLARE @name sysname, @sql nvarchar(max);
DECLARE transfer_keys CURSOR LOCAL FAST_FORWARD FOR
 SELECT DISTINCT f.name FROM sys.foreign_keys f
 JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=f.object_id
 JOIN sys.columns c ON c.object_id=fc.parent_object_id AND c.column_id=fc.parent_column_id
 WHERE f.parent_object_id=OBJECT_ID('dbo.WorkAssignmentTransfers')
 AND c.name IN ('FromIndividualID','ToIndividualID','FromJobID','ToJobID','WorkAssignmentID');
OPEN transfer_keys; FETCH NEXT FROM transfer_keys INTO @name;
WHILE @@FETCH_STATUS=0 BEGIN
 SET @sql=N'ALTER TABLE dbo.WorkAssignmentTransfers DROP CONSTRAINT '+QUOTENAME(@name); EXEC sp_executesql @sql;
 FETCH NEXT FROM transfer_keys INTO @name;
END;
CLOSE transfer_keys; DEALLOCATE transfer_keys;
");
        migrationBuilder.AddForeignKey(name: "FK_WorkAssignmentTransfers_Individuals_FromIndividualID", table: "WorkAssignmentTransfers", column: "FromIndividualID", principalTable: "Individuals", principalColumn: "BusinessEntityID", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_WorkAssignmentTransfers_Individuals_ToIndividualID", table: "WorkAssignmentTransfers", column: "ToIndividualID", principalTable: "Individuals", principalColumn: "BusinessEntityID", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_WorkAssignmentTransfers_Jobs_FromJobID", table: "WorkAssignmentTransfers", column: "FromJobID", principalTable: "Jobs", principalColumn: "JobID", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_WorkAssignmentTransfers_Jobs_ToJobID", table: "WorkAssignmentTransfers", column: "ToJobID", principalTable: "Jobs", principalColumn: "JobID", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_WorkAssignmentTransfers_WorkAssignments_WorkAssignmentID", table: "WorkAssignmentTransfers", column: "WorkAssignmentID", principalTable: "WorkAssignments", principalColumn: "WorkAssignmentID", onDelete: ReferentialAction.Restrict);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
        => throw new System.NotSupportedException("Transfer history protection cannot safely restore multiple cascading delete paths.");
}
