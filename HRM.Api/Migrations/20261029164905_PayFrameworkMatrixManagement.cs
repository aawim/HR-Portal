using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Migrations
{
    /// <inheritdoc />
    public partial class PayFrameworkMatrixManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Extend the existing PF tables in place. Historical definitions remain unclassified
            // until an authorised officer explicitly selects their framework and currency.
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.PF_JobMatrix','FrameworkID') IS NULL
                    ALTER TABLE dbo.PF_JobMatrix ADD FrameworkID int NULL;
                IF COL_LENGTH('dbo.PF_JobMatrix','CurrencyTypeID') IS NULL
                    ALTER TABLE dbo.PF_JobMatrix ADD CurrencyTypeID int NULL;
                IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_PF_JobMatrix_Framework')
                    ALTER TABLE dbo.PF_JobMatrix WITH CHECK ADD CONSTRAINT FK_PF_JobMatrix_Framework
                    FOREIGN KEY(FrameworkID) REFERENCES dbo.PF_Frameworks(FrameworkID);
                IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_PF_JobMatrix_Currency')
                    ALTER TABLE dbo.PF_JobMatrix WITH CHECK ADD CONSTRAINT FK_PF_JobMatrix_Currency
                    FOREIGN KEY(CurrencyTypeID) REFERENCES dbo.CurrencyTypes(CurrencyTypeID);
                IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Positions') AND name='UX_Positions_PositionMatrixID')
                BEGIN
                    IF EXISTS(SELECT PositionMatrixID FROM dbo.Positions WHERE PositionMatrixID IS NOT NULL GROUP BY PositionMatrixID HAVING COUNT(*)>1)
                        THROW 51063,'Review duplicate position matrix links before applying this migration. No records have been removed.',1;
                    CREATE UNIQUE INDEX UX_Positions_PositionMatrixID ON dbo.Positions(PositionMatrixID) WHERE PositionMatrixID IS NOT NULL;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new System.NotSupportedException("Preserve framework/currency links and position identities. Use a reviewed forward migration.");
        }
    }
}
