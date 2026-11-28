using System.Data;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HRM.Services
{
    public class SequenceNumberService : ISequenceNumberService
    {
        private readonly HrmTeContext _db;
        private readonly IUserAccessService? _access;

        public SequenceNumberService(HrmTeContext db, IUserAccessService? access = null)
        {
            _db = db;
            _access = access;
        }

        public async Task<string> GenerateAsync(int sequenceNumberTypeId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var orgId = await ResolveCurrentOrgIdAsync();
                if (orgId > 0)
                {
                    var orgRef = await TryGenerateOrganisationOverrideAsync(_db, orgId, sequenceNumberTypeId);
                    if (!string.IsNullOrEmpty(orgRef))
                    {
                        await transaction.CommitAsync();
                        return orgRef;
                    }
                }

                var sequence = await _db.SequenceNumberTypes
                    .FirstOrDefaultAsync(x => x.SequenceNumberTypeId == sequenceNumberTypeId);

                if (sequence == null)
                    throw new Exception($"Sequence Number Type {sequenceNumberTypeId} not found.");

                var number = sequence.NextNumber ?? throw new InvalidOperationException("The request numbering sequence is not configured.");
                sequence.NextNumber++;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return BuildReference(sequence, number, padLength: 6, orgCode: null);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<int> ResolveCurrentOrgIdAsync()
        {
            if (_access == null) return 0;
            try { return await _access.GetCurrentOrganisationIdAsync(); }
            catch { return 0; }
        }

        private static async Task<string?> TryGenerateOrganisationOverrideAsync(HrmTeContext db, int organisationId, int sequenceNumberTypeId)
        {
            try
            {
                var conn = db.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open) await conn.OpenAsync();
                var dbTx = db.Database.CurrentTransaction?.GetDbTransaction();

                await using var checkCmd = conn.CreateCommand();
                checkCmd.Transaction = dbTx;
                checkCmd.CommandText = "SELECT OBJECT_ID(N'dbo.NF_OrganisationNumberFormats', N'U')";
                if (await checkCmd.ExecuteScalarAsync() is null or DBNull)
                    return null;

                await using var readCmd = conn.CreateCommand();
                readCmd.Transaction = dbTx;
                readCmd.CommandText = """
                    SELECT TOP 1 f.NumberFormatId, f.Prefix, f.FormatString, f.PaddingLength,
                           f.StartingNumber, f.NextNumber, f.IsResetAnnually, f.LastResetYear,
                           o.RegistrationNumber
                    FROM dbo.NF_OrganisationNumberFormats f WITH (UPDLOCK, HOLDLOCK)
                    LEFT JOIN dbo.Organisations o ON o.BusinessEntityID = f.OrganisationId
                    WHERE f.OrganisationId = @orgId AND f.SequenceNumberTypeId = @seqId AND f.IsActive = 1
                    ORDER BY f.NumberFormatId
                    """;
                readCmd.Parameters.Add(new SqlParameter("@orgId", organisationId));
                readCmd.Parameters.Add(new SqlParameter("@seqId", sequenceNumberTypeId));

                int fmtId;
                string prefix, fmtStr;
                int pad, startNum, nextNum;
                bool resetAnnually;
                int? lastResetYear;
                string? orgCode;

                await using (var reader = await readCmd.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync()) return null;
                    fmtId = reader.GetInt32(0);
                    prefix = reader.GetString(1);
                    fmtStr = reader.GetString(2);
                    pad = Math.Clamp(reader.GetInt32(3), 1, 10);
                    startNum = Math.Max(1, reader.GetInt32(4));
                    nextNum = Math.Max(1, reader.GetInt32(5));
                    resetAnnually = reader.GetBoolean(6);
                    lastResetYear = reader.IsDBNull(7) ? null : reader.GetInt32(7);
                    orgCode = reader.IsDBNull(8) ? null : reader.GetString(8);
                }

                var currentYear = DateTime.UtcNow.AddHours(5).Year;
                if (resetAnnually && lastResetYear.HasValue && lastResetYear.Value != currentYear)
                {
                    nextNum = startNum;
                }

                await using var updCmd = conn.CreateCommand();
                updCmd.Transaction = dbTx;
                updCmd.CommandText = """
                    UPDATE dbo.NF_OrganisationNumberFormats
                    SET NextNumber = @nextVal, LastResetYear = @year, UpdatedAtUtc = SYSUTCDATETIME()
                    WHERE NumberFormatId = @id
                    """;
                updCmd.Parameters.Add(new SqlParameter("@nextVal", nextNum + 1));
                updCmd.Parameters.Add(new SqlParameter("@year", currentYear));
                updCmd.Parameters.Add(new SqlParameter("@id", fmtId));
                await updCmd.ExecuteNonQueryAsync();

                var now = DateTime.UtcNow.AddHours(5);
                return (string.IsNullOrWhiteSpace(fmtStr) ? "{PREFIX}/{YEAR}/{NUMBER}" : fmtStr)
                    .Replace("{ORG}", string.IsNullOrWhiteSpace(orgCode) ? $"ORG{organisationId}" : orgCode)
                    .Replace("{PREFIX}", prefix ?? "")
                    .Replace("{YEAR}", now.Year.ToString())
                    .Replace("{YY}", now.ToString("yy"))
                    .Replace("{MM}", now.ToString("MM"))
                    .Replace("{NUMBER}", nextNum.ToString($"D{pad}"));
            }
            catch
            {
                return null;
            }
        }

        private static string BuildReference(SequenceNumberType sequence, int number, int padLength, string? orgCode)
        {
            var now = DateTime.UtcNow.AddHours(5);
            if (!string.IsNullOrWhiteSpace(sequence.FormatString))
            {
                return sequence.FormatString
                    .Replace("{ORG}", string.IsNullOrWhiteSpace(orgCode) ? "ORG" : orgCode)
                    .Replace("{PREFIX}", sequence.Prefix ?? "")
                    .Replace("{YEAR}", now.Year.ToString())
                    .Replace("{YY}", now.ToString("yy"))
                    .Replace("{MM}", now.ToString("MM"))
                    .Replace("{NUMBER}", padLength > 0 ? number.ToString($"D{padLength}") : number.ToString());
            }

            return $"{sequence.Prefix}-{number.ToString($"D{Math.Max(1, padLength)}")}";
        }

        public async Task<string> GenerateRequestNumberAsync(int requestTypeId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var reference = await GenerateRequestNumberAsync(_db, requestTypeId);
                await transaction.CommitAsync();
                return reference;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<string> GenerateRequestNumberAsync(HrmTeContext db, int requestTypeId)
        {
            if (db.Database.CurrentTransaction == null)
                throw new InvalidOperationException("Request numbering requires an active transaction.");

            var requestType =
                await db.RequestTypes.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.RequestTypeId == requestTypeId);

            if (requestType == null)
                throw new InvalidOperationException($"RequestType {requestTypeId} was not found.");

            if (requestType.SequenceNumberTypeId == null)
                throw new InvalidOperationException($"RequestType '{requestType.TypeName}' has no Sequence Number Type configured.");

            // Check per-organisation number format override first (with annual reset & custom padding)
            var orgId = await ResolveCurrentOrgIdAsync();
            if (orgId > 0)
            {
                var orgRef = await TryGenerateOrganisationOverrideAsync(db, orgId, requestType.SequenceNumberTypeId.Value);
                if (!string.IsNullOrEmpty(orgRef))
                    return orgRef;
            }

            var sequence =
                await db.SequenceNumberTypes
                    .FromSqlInterpolated($"SELECT * FROM dbo.SequenceNumberTypes WITH (UPDLOCK, HOLDLOCK) WHERE SequenceNumberTypeID = {requestType.SequenceNumberTypeId.Value}")
                    .AsNoTracking()
                    .SingleOrDefaultAsync();

            if (sequence == null)
                throw new InvalidOperationException($"Sequence Number Type {requestType.SequenceNumberTypeId} was not found.");

            if (sequence.NextNumber == null)
                throw new InvalidOperationException($"Sequence '{sequence.TypeName}' has no NextNumber configured.");

            var number = sequence.NextNumber.Value;

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE dbo.SequenceNumberTypes SET NextNumber = NextNumber + 1 WHERE SequenceNumberTypeID = {sequence.SequenceNumberTypeId}");

            return Format(sequence, number);
        }

        private static string Format(SequenceNumberType sequence, int number)
        {
            var now = DateTime.UtcNow.AddHours(5);
            if (string.IsNullOrWhiteSpace(sequence.FormatString))
            {
                return $"{sequence.Prefix}/{now.Year}/{number:D4}";
            }

            return sequence.FormatString
                .Replace("{ORG}", "ORG")
                .Replace("{PREFIX}", sequence.Prefix ?? "")
                .Replace("{YEAR}", now.Year.ToString())
                .Replace("{YY}", now.ToString("yy"))
                .Replace("{MM}", now.ToString("MM"))
                .Replace("{NUMBER}", number.ToString("D4"));
        }
    }
}
