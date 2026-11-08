using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace HRM.Migrations;
[DbContext(typeof(HrmTeContext))]
[Migration("20261029094000_FinancePayrollCycleRequests")]
public sealed class FinancePayrollCycleRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)=>migrationBuilder.Sql("""
        IF NOT EXISTS(SELECT 1 FROM dbo.RequestTypes WHERE TypeName=N'Finance Payroll Cycle Request')
        BEGIN
          INSERT dbo.SequenceNumberTypes(TypeName,Prefix,FormatString,IsResetAnnually,StartingNumber,NextNumber)
          VALUES(N'Finance Payroll Cycle Request',N'FPC',N'{PREFIX}-{YEAR}-{NUMBER}',0,1,1);
          DECLARE @Sequence int=CONVERT(int,SCOPE_IDENTITY());
          INSERT dbo.RequestTypes(TypeName,SequenceNumberTypeID,ViewURL,IsSystemType)
          VALUES(N'Finance Payroll Cycle Request',@Sequence,N'/online/allowances-deductions',1);
        END;
        """);
    protected override void Down(MigrationBuilder migrationBuilder)=>throw new NotSupportedException("Payroll cycle request history must be retained.");
}
