using System;
using System.IO;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    [DbContext(typeof(HrmTeContext))]
    [Migration("20261104090000_ItemCustodyLifecycle")]
    public sealed class ItemCustodyLifecycle : Migration
    {
        private static string Script()
        {
            using var stream = typeof(ItemCustodyLifecycle).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261104_ItemCustodyLifecycle.sql")
                ?? throw new InvalidOperationException("Item custody lifecycle SQL script is unavailable as an embedded resource.");
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
            throw new NotSupportedException("Item custody history must be retained.");
        }
    }
}
