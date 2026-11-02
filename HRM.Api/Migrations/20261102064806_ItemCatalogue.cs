using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class ItemCatalogue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IM_ItemCategories",
                columns: table => new
                {
                    ItemCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IM_ItemCategories", x => x.ItemCategoryId);
                    table.ForeignKey(
                        name: "FK_IM_ItemCategories_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IM_ItemCategories_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IM_Items",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    ItemCategoryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    QuantityOnHand = table.Column<int>(type: "int", nullable: false),
                    ReorderLevel = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IM_Items", x => x.ItemId);
                    table.CheckConstraint("CK_IM_Items_Kind", "[Kind] IN ('Asset','Consumable') AND [QuantityOnHand] >= 0 AND [ReorderLevel] >= 0");
                    table.ForeignKey(
                        name: "FK_IM_Items_IM_ItemCategories_ItemCategoryId",
                        column: x => x.ItemCategoryId,
                        principalTable: "IM_ItemCategories",
                        principalColumn: "ItemCategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IM_Items_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IM_Items_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IM_ItemUnits",
                columns: table => new
                {
                    ItemUnitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    InventoryNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IssuedToIndividualId = table.Column<int>(type: "int", nullable: true),
                    IssuedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CollectedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    OperationLogId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IM_ItemUnits", x => x.ItemUnitId);
                    table.CheckConstraint("CK_IM_ItemUnits_Status", "[Status] IN ('Available','Issued','Returned','Damaged','Lost','Retired') AND [Condition] IN ('Good','Fair','Damaged','Unusable')");
                    table.ForeignKey(
                        name: "FK_IM_ItemUnits_BE_Individuals_IssuedToIndividualId",
                        column: x => x.IssuedToIndividualId,
                        principalTable: "BE_Individuals",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IM_ItemUnits_IM_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "IM_Items",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IM_ItemUnits_OperationLogs_OperationLogId",
                        column: x => x.OperationLogId,
                        principalTable: "OperationLogs",
                        principalColumn: "OperationLogID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IM_ItemUnits_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "BusinessEntityID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IM_ItemCategories_OperationLogId",
                table: "IM_ItemCategories",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_IM_ItemCategories_OrganisationId_Name",
                table: "IM_ItemCategories",
                columns: new[] { "OrganisationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IM_Items_ItemCategoryId",
                table: "IM_Items",
                column: "ItemCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_IM_Items_OperationLogId",
                table: "IM_Items",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_IM_Items_OrganisationId_ItemCategoryId_Name",
                table: "IM_Items",
                columns: new[] { "OrganisationId", "ItemCategoryId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IM_ItemUnits_IssuedToIndividualId",
                table: "IM_ItemUnits",
                column: "IssuedToIndividualId");

            migrationBuilder.CreateIndex(
                name: "IX_IM_ItemUnits_ItemId_Status",
                table: "IM_ItemUnits",
                columns: new[] { "ItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_IM_ItemUnits_OperationLogId",
                table: "IM_ItemUnits",
                column: "OperationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_IM_ItemUnits_OrganisationId_InventoryNumber",
                table: "IM_ItemUnits",
                columns: new[] { "OrganisationId", "InventoryNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IM_ItemUnits");

            migrationBuilder.DropTable(
                name: "IM_Items");

            migrationBuilder.DropTable(
                name: "IM_ItemCategories");
        }
    }
}
