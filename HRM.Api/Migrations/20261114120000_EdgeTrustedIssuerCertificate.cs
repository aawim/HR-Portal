using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace HRM.Migrations;
[DbContext(typeof(HrmTeContext))]
[Migration("20261114120000_EdgeTrustedIssuerCertificate")]
public partial class EdgeTrustedIssuerCertificate : Migration
{ protected override void Up(MigrationBuilder m)=>m.Sql("ALTER TABLE dbo.EA_TrustedIssuers ADD CertificateRawData varbinary(max) NULL;"); protected override void Down(MigrationBuilder m)=>m.Sql("ALTER TABLE dbo.EA_TrustedIssuers DROP COLUMN CertificateRawData;"); }
