using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class IndividualImageAndForeigner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "Individuals",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "Individuals",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsForeigner",
                table: "Individuals",
                type: "bit",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Individuals_Image",
                table: "Individuals",
                sql: "([ImageData] IS NULL AND [ImageContentType] IS NULL) OR ([ImageData] IS NOT NULL AND DATALENGTH([ImageData]) BETWEEN 1 AND 2097152 AND [ImageContentType] IS NOT NULL AND [ImageContentType] IN ('image/png','image/jpeg'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new System.NotSupportedException("Individual images and identity classification must be preserved. Use a reviewed forward migration.");
        }
    }
}
