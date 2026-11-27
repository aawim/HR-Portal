using System.Data;
using HRM.Api.Infrastructure;
using HRM.Application.Contracts.SuperAdmin;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/superadmin/number-formatting")]
public sealed class NumberFormattingController(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access) : HrmApiController
{
    private static readonly string[] AllowedCategories =
        ["Request", "Document", "Certificate", "Asset", "Recruitment", "Custom"];

    private async Task RequireSuperAdminAsync()
    {
        var actor = await access.RequireContextAsync();
        if (!actor.IsSuperAdministrator)
            throw new UnauthorizedAccessException("Only Super Administrators can configure organisation number formats.");
    }

    private static async Task EnsureTableAsync(HrmTeContext db, CancellationToken ct)
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.NF_OrganisationNumberFormats', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.NF_OrganisationNumberFormats
                (
                    NumberFormatId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NF_OrganisationNumberFormats PRIMARY KEY,
                    OrganisationId        INT NOT NULL,
                    Category              NVARCHAR(40) NOT NULL,
                    SequenceNumberTypeId  INT NULL,
                    Code                  NVARCHAR(60) NOT NULL,
                    Name                  NVARCHAR(150) NOT NULL,
                    Prefix                NVARCHAR(30) NOT NULL,
                    FormatString          NVARCHAR(150) NOT NULL,
                    PaddingLength         INT NOT NULL CONSTRAINT DF_NF_OrgNumFmt_Pad DEFAULT (4),
                    StartingNumber        INT NOT NULL CONSTRAINT DF_NF_OrgNumFmt_Start DEFAULT (1),
                    NextNumber            INT NOT NULL CONSTRAINT DF_NF_OrgNumFmt_Next DEFAULT (1),
                    IsResetAnnually       BIT NOT NULL CONSTRAINT DF_NF_OrgNumFmt_Reset DEFAULT (1),
                    LastResetYear         INT NULL,
                    IsActive              BIT NOT NULL CONSTRAINT DF_NF_OrgNumFmt_Active DEFAULT (1),
                    UpdatedAtUtc          DATETIME2 NOT NULL CONSTRAINT DF_NF_OrgNumFmt_Updated DEFAULT (SYSUTCDATETIME())
                );
                CREATE INDEX IX_NF_OrganisationNumberFormats_Org ON dbo.NF_OrganisationNumberFormats (OrganisationId, IsActive);
            END
            """;
        await db.Database.ExecuteSqlRawAsync(sql, ct);
    }

    private static string BuildPreview(string formatString, string prefix, string orgCode, int nextNumber, int paddingLength)
    {
        var pad = Math.Clamp(paddingLength, 1, 10);
        var now = DateTime.UtcNow.AddHours(5);
        var fmt = string.IsNullOrWhiteSpace(formatString) ? "{PREFIX}/{YEAR}/{NUMBER}" : formatString;
        return fmt
            .Replace("{ORG}", string.IsNullOrWhiteSpace(orgCode) ? "ORG" : orgCode)
            .Replace("{PREFIX}", prefix ?? "")
            .Replace("{YEAR}", now.Year.ToString())
            .Replace("{YY}", now.ToString("yy"))
            .Replace("{MM}", now.ToString("MM"))
            .Replace("{NUMBER}", nextNumber.ToString($"D{pad}"));
    }

    [HttpGet("load")]
    public async Task<ActionResult<NumberFormattingWorkspaceDto>> Load([FromQuery] int? organisationId = null, CancellationToken ct = default)
    {
        await RequireSuperAdminAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTableAsync(db, ct);

        var inactiveStateIds = await HRM.Helpers.ActiveOrganisationFilter.InactiveEntityStateIdsAsync(db, ct);
        var orgs = await HRM.Helpers.ActiveOrganisationFilter.ActiveOrganisations(db, inactiveStateIds)
            .OrderBy(o => o.OrganisationName)
            .Select(o => new NumberFormatOptionDto(o.BusinessEntityID, o.OrganisationName, o.RegistrationNumber))
            .ToListAsync(ct);

        var selectedOrg = organisationId is > 0
            ? orgs.FirstOrDefault(o => o.Id == organisationId.Value)
            : orgs.FirstOrDefault();

        var orgId = selectedOrg?.Id ?? 0;
        var orgCode = selectedOrg?.Code ?? (orgId > 0 ? $"ORG{orgId}" : "ORG");

        var globalSequences = await db.SequenceNumberTypes.AsNoTracking()
            .Include(s => s.RequestTypes)
            .OrderBy(s => s.TypeName)
            .Select(s => new GlobalSequenceNumberDto
            {
                Id = s.SequenceNumberTypeId,
                TypeName = s.TypeName ?? "",
                Prefix = s.Prefix ?? "",
                FormatString = s.FormatString ?? "",
                IsResetAnnually = s.IsResetAnnually == true,
                StartingNumber = s.StartingNumber ?? 1,
                NextNumber = s.NextNumber ?? 1,
                LinkedRequestTypes = s.RequestTypes.Select(rt => rt.TypeName).OrderBy(n => n).ToList()
            })
            .ToListAsync(ct);

        var seqNameById = globalSequences.ToDictionary(s => s.Id, s => s.TypeName);
        var orgFormats = new List<OrganisationNumberFormatDto>();

        if (orgId > 0)
        {
            var conn = db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT NumberFormatId, OrganisationId, Category, SequenceNumberTypeId, Code, Name, Prefix, FormatString,
                       PaddingLength, StartingNumber, NextNumber, IsResetAnnually, LastResetYear, IsActive
                FROM dbo.NF_OrganisationNumberFormats
                WHERE OrganisationId = @orgId AND IsActive = 1
                ORDER BY Category, Name
                """;
            cmd.Parameters.Add(new SqlParameter("@orgId", orgId));
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var seqId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                var dto = new OrganisationNumberFormatDto
                {
                    NumberFormatId = reader.GetInt32(0),
                    OrganisationId = reader.GetInt32(1),
                    Category = reader.GetString(2),
                    SequenceNumberTypeId = seqId,
                    SequenceNumberTypeName = seqId.HasValue ? seqNameById.GetValueOrDefault(seqId.Value) : null,
                    Code = reader.GetString(4),
                    Name = reader.GetString(5),
                    Prefix = reader.GetString(6),
                    FormatString = reader.GetString(7),
                    PaddingLength = reader.GetInt32(8),
                    StartingNumber = reader.GetInt32(9),
                    NextNumber = reader.GetInt32(10),
                    IsResetAnnually = reader.GetBoolean(11),
                    LastResetYear = reader.IsDBNull(12) ? (int?)null : reader.GetInt32(12),
                    IsActive = reader.GetBoolean(13)
                };
                dto.Preview = BuildPreview(dto.FormatString, dto.Prefix, orgCode, dto.NextNumber, dto.PaddingLength);
                orgFormats.Add(dto);
            }
        }

        return Ok(new NumberFormattingWorkspaceDto
        {
            OrganisationId = orgId,
            OrganisationName = selectedOrg?.Name ?? "",
            OrganisationCode = orgCode,
            Organisations = orgs,
            OrganisationFormats = orgFormats,
            GlobalSequences = globalSequences
        });
    }

    [HttpPost("save-organisation-format")]
    public async Task<ActionResult<ServiceResult>> SaveOrganisationFormat([FromBody] OrganisationNumberFormatDto model, CancellationToken ct = default)
    {
        await RequireSuperAdminAsync();
        if (model.OrganisationId <= 0)
            return Ok(ServiceResult.Fail("Select an organisation first."));
        if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Trim().Length > 150)
            return Ok(ServiceResult.Fail("Enter a format name (up to 150 characters)."));
        if (string.IsNullOrWhiteSpace(model.Code) || model.Code.Trim().Length > 60)
            return Ok(ServiceResult.Fail("Enter a unique format code (up to 60 characters)."));
        if (string.IsNullOrWhiteSpace(model.Prefix) || model.Prefix.Trim().Length > 30)
            return Ok(ServiceResult.Fail("Enter a prefix (up to 30 characters)."));
        if (string.IsNullOrWhiteSpace(model.FormatString) || model.FormatString.Trim().Length > 150 || !model.FormatString.Contains("{NUMBER}"))
            return Ok(ServiceResult.Fail("Format string is required and must include the {NUMBER} token."));
        if (!AllowedCategories.Contains(model.Category, StringComparer.OrdinalIgnoreCase))
            return Ok(ServiceResult.Fail("Select a valid category."));

        var pad = Math.Clamp(model.PaddingLength, 1, 10);
        var startNum = Math.Max(1, model.StartingNumber);
        var nextNum = Math.Max(startNum, model.NextNumber);

        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTableAsync(db, ct);

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        // Check duplicate active code within same organisation
        await using (var dupCmd = conn.CreateCommand())
        {
            dupCmd.CommandText = """
                SELECT COUNT(1) FROM dbo.NF_OrganisationNumberFormats
                WHERE OrganisationId = @orgId AND IsActive = 1 AND UPPER(Code) = UPPER(@code) AND NumberFormatId <> @id
                """;
            dupCmd.Parameters.Add(new SqlParameter("@orgId", model.OrganisationId));
            dupCmd.Parameters.Add(new SqlParameter("@code", model.Code.Trim()));
            dupCmd.Parameters.Add(new SqlParameter("@id", model.NumberFormatId));
            var count = Convert.ToInt32(await dupCmd.ExecuteScalarAsync(ct));
            if (count > 0)
                return Ok(ServiceResult.Fail($"An active format with code '{model.Code.Trim()}' already exists for this organisation."));
        }

        await using var cmd = conn.CreateCommand();
        if (model.NumberFormatId == 0)
        {
            cmd.CommandText = """
                INSERT INTO dbo.NF_OrganisationNumberFormats
                    (OrganisationId, Category, SequenceNumberTypeId, Code, Name, Prefix, FormatString, PaddingLength, StartingNumber, NextNumber, IsResetAnnually, LastResetYear, IsActive, UpdatedAtUtc)
                VALUES
                    (@orgId, @cat, @seqId, @code, @name, @prefix, @fmt, @pad, @start, @next, @reset, @resetYear, 1, SYSUTCDATETIME())
                """;
        }
        else
        {
            cmd.CommandText = """
                UPDATE dbo.NF_OrganisationNumberFormats
                SET Category = @cat,
                    SequenceNumberTypeId = @seqId,
                    Code = @code,
                    Name = @name,
                    Prefix = @prefix,
                    FormatString = @fmt,
                    PaddingLength = @pad,
                    StartingNumber = @start,
                    NextNumber = @next,
                    IsResetAnnually = @reset,
                    UpdatedAtUtc = SYSUTCDATETIME()
                WHERE NumberFormatId = @id AND OrganisationId = @orgId AND IsActive = 1
                """;
            cmd.Parameters.Add(new SqlParameter("@id", model.NumberFormatId));
        }

        cmd.Parameters.Add(new SqlParameter("@orgId", model.OrganisationId));
        cmd.Parameters.Add(new SqlParameter("@cat", model.Category.Trim()));
        cmd.Parameters.Add(new SqlParameter("@seqId", (object?)model.SequenceNumberTypeId ?? DBNull.Value));
        cmd.Parameters.Add(new SqlParameter("@code", model.Code.Trim().ToUpperInvariant()));
        cmd.Parameters.Add(new SqlParameter("@name", model.Name.Trim()));
        cmd.Parameters.Add(new SqlParameter("@prefix", model.Prefix.Trim()));
        cmd.Parameters.Add(new SqlParameter("@fmt", model.FormatString.Trim()));
        cmd.Parameters.Add(new SqlParameter("@pad", pad));
        cmd.Parameters.Add(new SqlParameter("@start", startNum));
        cmd.Parameters.Add(new SqlParameter("@next", nextNum));
        cmd.Parameters.Add(new SqlParameter("@reset", model.IsResetAnnually));
        cmd.Parameters.Add(new SqlParameter("@resetYear", DateTime.UtcNow.AddHours(5).Year));
        await cmd.ExecuteNonQueryAsync(ct);

        return Ok(ServiceResult.Ok("Organisation number format saved."));
    }

    [HttpPost("delete-organisation-format")]
    public async Task<ActionResult<ServiceResult>> DeleteOrganisationFormat([FromQuery] int organisationId, [FromQuery] int numberFormatId, CancellationToken ct = default)
    {
        await RequireSuperAdminAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTableAsync(db, ct);

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE dbo.NF_OrganisationNumberFormats
            SET IsActive = 0, UpdatedAtUtc = SYSUTCDATETIME()
            WHERE NumberFormatId = @id AND OrganisationId = @orgId AND IsActive = 1
            """;
        cmd.Parameters.Add(new SqlParameter("@id", numberFormatId));
        cmd.Parameters.Add(new SqlParameter("@orgId", organisationId));
        var affected = await cmd.ExecuteNonQueryAsync(ct);
        return Ok(affected > 0 ? ServiceResult.Ok("Number format soft-deleted.") : ServiceResult.Fail("Number format not found."));
    }

    [HttpPost("save-global-sequence")]
    public async Task<ActionResult<ServiceResult>> SaveGlobalSequence([FromBody] GlobalSequenceNumberDto model, CancellationToken ct = default)
    {
        await RequireSuperAdminAsync();
        if (string.IsNullOrWhiteSpace(model.TypeName) || model.TypeName.Trim().Length > 100)
            return Ok(ServiceResult.Fail("Enter a sequence name (up to 100 characters)."));
        if (string.IsNullOrWhiteSpace(model.Prefix) || model.Prefix.Trim().Length > 30)
            return Ok(ServiceResult.Fail("Enter a prefix (up to 30 characters)."));
        if (!string.IsNullOrWhiteSpace(model.FormatString) && !model.FormatString.Contains("{NUMBER}"))
            return Ok(ServiceResult.Fail("Format string must contain the {NUMBER} token."));

        await using var db = await factory.CreateDbContextAsync(ct);
        SequenceNumberType entity;
        if (model.Id == 0)
        {
            entity = new SequenceNumberType();
            db.SequenceNumberTypes.Add(entity);
        }
        else
        {
            var found = await db.SequenceNumberTypes.FirstOrDefaultAsync(x => x.SequenceNumberTypeId == model.Id, ct);
            if (found == null) return Ok(ServiceResult.Fail("Global sequence was not found."));
            entity = found;
        }

        entity.TypeName = model.TypeName.Trim();
        entity.Prefix = model.Prefix.Trim();
        entity.FormatString = string.IsNullOrWhiteSpace(model.FormatString) ? "{PREFIX}/{YEAR}/{NUMBER}" : model.FormatString.Trim();
        entity.IsResetAnnually = model.IsResetAnnually;
        entity.StartingNumber = Math.Max(1, model.StartingNumber);
        entity.NextNumber = Math.Max(entity.StartingNumber.Value, model.NextNumber);
        await db.SaveChangesAsync(ct);
        return Ok(ServiceResult.Ok("Global sequence format saved."));
    }
}
