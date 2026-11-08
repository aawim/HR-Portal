using System;
using System.IO;
using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    [DbContext(typeof(HrmTeContext))]
    [Migration("20261103110000_ItemStaffRequestLineDetails")]
    public sealed class ItemStaffRequestLineDetails : Migration
    {
        private static string Script()
        {
            using var stream = typeof(ItemStaffRequestLineDetails).Assembly.GetManifestResourceStream("HRM.Database.Scripts.20261103_ItemStaffRequestLineDetails.sql")
                ?? throw new InvalidOperationException("Item staff request line details SQL script is unavailable as an embedded resource.");
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
            throw new NotSupportedException("Item staff request line details and history must be retained.");
        }
    }
}
