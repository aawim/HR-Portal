using System;
using System.IO;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    [DbContext(typeof(HrmTeContext))]
    [Migration("20261107120000_EmploymentCertificates")]
    public sealed class EmploymentCertificates : Migration
    {
        private static string Script()
        {
            using var stream = typeof(EmploymentCertificates).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261107_EmploymentCertificates.sql")
                ?? throw new InvalidOperationException("Employment certificates SQL script is unavailable as an embedded resource.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Script());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS dbo.EC_CertificateAuditLogs;
                DROP TABLE IF EXISTS dbo.EC_IssuedCertificates;
                DROP TABLE IF EXISTS dbo.EC_CertificateTemplates;
                DROP TABLE IF EXISTS dbo.EC_CertificateRequests;
            ");
        }
    }
}
