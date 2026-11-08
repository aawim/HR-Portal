using System;
using System.IO;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    [DbContext(typeof(HrmTeContext))]
    [Migration("20261103093000_AssetLifecycleManagement")]
    public sealed class AssetLifecycleManagement : Migration
    {
        private static string Script()
        {
            using var stream = typeof(AssetLifecycleManagement).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261103_AssetLifecycleManagement.sql")
                ?? throw new InvalidOperationException("Asset Lifecycle Management SQL script is unavailable as an embedded resource.");
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
            throw new NotSupportedException("Asset lifecycle, maintenance, component history and audit records must be retained.");
        }
    }
}
