using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class ItemRequestLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ItemId",
                table: "IM_ItemStaffRequests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "IM_ItemStaffRequestLines",
                columns: table => new
                {
                    ItemStaffRequestLineId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IM_ItemStaffRequestLines", x => x.ItemStaffRequestLineId);
                    table.CheckConstraint("CK_IM_ItemStaffRequestLines_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_IM_ItemStaffRequestLines_IM_ItemStaffRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "IM_ItemStaffRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IM_ItemStaffRequestLines_RequestId_ItemStaffRequestLineId",
                table: "IM_ItemStaffRequestLines",
                columns: new[] { "RequestId", "ItemStaffRequestLineId" });
            migrationBuilder.Sql("""
                INSERT dbo.IM_ItemStaffRequestLines(RequestId,ItemName,Quantity,IssuedAtUtc)
                SELECT r.RequestId,i.Name,r.Quantity,r.IssuedAtUtc
                FROM dbo.IM_ItemStaffRequests r
                JOIN dbo.IM_Items i ON i.ItemId=r.ItemId
                WHERE NOT EXISTS(SELECT 1 FROM dbo.IM_ItemStaffRequestLines l WHERE l.RequestId=r.RequestId);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Item request lines and approved issue history must be retained.");
        }
    }
}
