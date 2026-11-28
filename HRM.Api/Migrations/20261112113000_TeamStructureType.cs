using HRM.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations;

[DbContext(typeof(HrmTeContext))]
[Migration("20261112113000_TeamStructureType")]
public sealed class TeamStructureType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "OrganisationStructureTypeID", table: "Teams", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Teams_OrganisationStructureTypeID", table: "Teams", column: "OrganisationStructureTypeID");
        migrationBuilder.AddForeignKey(name: "FK_Teams_OrganisationStructureTypes", table: "Teams", column: "OrganisationStructureTypeID", principalTable: "OrganisationStructureTypes", principalColumn: "OrganisationStructureTypeID", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Teams_OrganisationStructureTypes", table: "Teams");
        migrationBuilder.DropIndex(name: "IX_Teams_OrganisationStructureTypeID", table: "Teams");
        migrationBuilder.DropColumn(name: "OrganisationStructureTypeID", table: "Teams");
    }
}
