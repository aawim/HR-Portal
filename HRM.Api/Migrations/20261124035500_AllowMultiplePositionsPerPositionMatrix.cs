using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261124035500_AllowMultiplePositionsPerPositionMatrix")]
public partial class AllowMultiplePositionsPerPositionMatrix : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.Sql("""
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Positions') AND name = 'UX_Positions_PositionMatrixID')
                DROP INDEX UX_Positions_PositionMatrixID ON dbo.Positions;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Positions') AND name = 'IX_Positions_PositionMatrixID')
                CREATE NONCLUSTERED INDEX IX_Positions_PositionMatrixID ON dbo.Positions(PositionMatrixID) WHERE PositionMatrixID IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("""
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Positions') AND name = 'IX_Positions_PositionMatrixID')
                DROP INDEX IX_Positions_PositionMatrixID ON dbo.Positions;
            """);
    }
}
