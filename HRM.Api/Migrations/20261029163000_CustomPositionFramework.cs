using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261029163000_CustomPositionFramework")]
public sealed class CustomPositionFramework : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        IF NOT EXISTS(SELECT 1 FROM dbo.PF_Frameworks WITH(UPDLOCK,HOLDLOCK) WHERE Code=N'CUSTOM')
        BEGIN
            DECLARE @FrameworkId int=(SELECT ISNULL(MAX(FrameworkID),0)+1 FROM dbo.PF_Frameworks WITH(UPDLOCK,HOLDLOCK));
            INSERT dbo.PF_Frameworks(FrameworkID,Code,Name,IsActive)
            VALUES(@FrameworkId,N'CUSTOM',N'Non CSC / Custom',1);
        END;
        """);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Preserve custom position framework references. Use a reviewed forward migration.");
}
