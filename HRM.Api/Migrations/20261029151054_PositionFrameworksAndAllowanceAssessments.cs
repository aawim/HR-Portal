using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class PositionFrameworksAndAllowanceAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.Positions','PositionFrameworkID') IS NULL ALTER TABLE dbo.Positions ADD PositionFrameworkID int NULL;
                IF COL_LENGTH('dbo.Positions','PositionMatrixID') IS NULL ALTER TABLE dbo.Positions ADD PositionMatrixID int NULL;
                """);

            migrationBuilder.CreateTable(
                name: "AL_AllowanceAssessments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    RequestStateId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyTypeId = table.Column<int>(type: "int", nullable: false),
                    AssessedByUserId = table.Column<int>(type: "int", nullable: false),
                    AssessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AL_AllowanceAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AL_AllowanceAssessments_AD_FinanceRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "AD_FinanceRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AL_AllowanceAssessments_CurrencyTypes_CurrencyTypeId",
                        column: x => x.CurrencyTypeId,
                        principalTable: "CurrencyTypes",
                        principalColumn: "CurrencyTypeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AL_AllowanceAssessments_Users_AssessedByUserId",
                        column: x => x.AssessedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AL_AllowanceAssessments_AssessedByUserId",
                table: "AL_AllowanceAssessments",
                column: "AssessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AL_AllowanceAssessments_CurrencyTypeId",
                table: "AL_AllowanceAssessments",
                column: "CurrencyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AL_AllowanceAssessments_RequestId_Id",
                table: "AL_AllowanceAssessments",
                columns: new[] { "RequestId", "Id" });
            migrationBuilder.Sql("""
                DECLARE @ContextId int=(SELECT TOP(1) ContextId FROM dbo.Roles WHERE RoleKey='Admin');
                IF @ContextId IS NULL THROW 51000,'Admin permission context is required.',1;
                INSERT dbo.Roles(RoleKey,Name,Description,ContextId,IsSystemRole)
                SELECT k.RoleKey,k.Name,k.Description,@ContextId,1 FROM (VALUES
                  ('Positions.Configure','Configure positions','Manage current-tenant custom positions and government position matrix links.'),
                  ('StaffFinance.Assess','Assess staff allowance amounts','Independently assess the amount and currency of pending staff allowance requests.'))k(RoleKey,Name,Description)
                WHERE NOT EXISTS(SELECT 1 FROM dbo.Roles r WHERE r.RoleKey=k.RoleKey);
                """);
            migrationBuilder.Sql("CREATE OR ALTER TRIGGER dbo.TR_AL_AllowanceAssessments_Immutable ON dbo.AL_AllowanceAssessments INSTEAD OF UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51000,'Allowance assessments are immutable; add a new assessment before final approval.',1; END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Existing position framework links and financial assessment history must be retained.");
        }
    }
}
