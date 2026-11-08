using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class SequenceNumberService : ISequenceNumberService
    {

        //private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly HrmTeContext _db;
        public SequenceNumberService(HrmTeContext db)
        {
            _db = db;
        }

        public async Task<string> GenerateAsync(int sequenceNumberTypeId)
        {
          

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            try
            {
                var sequence = await _db.SequenceNumberTypes
                    .FirstOrDefaultAsync(x =>
                        x.SequenceNumberTypeId == sequenceNumberTypeId);

                if (sequence == null)
                    throw new Exception(
                        $"Sequence Number Type {sequenceNumberTypeId} not found.");

                //---------------------------------------
                // Reset every year (optional)
                //---------------------------------------

                if (sequence.IsResetAnnually == true)
                {
                    // Optional implementation
                    // Requires a LastResetYear column
                }

                var number = sequence.NextNumber ?? throw new InvalidOperationException("The request numbering sequence is not configured.");

                sequence.NextNumber++;

                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return BuildReference(sequence, number);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static string BuildReference(SequenceNumberType sequence,
            int number)
        {
            if (!string.IsNullOrWhiteSpace(sequence.FormatString))
            {
                return sequence.FormatString
                    .Replace("{PREFIX}", sequence.Prefix)
                    .Replace("{YEAR}", DateTime.Now.Year.ToString())
                    .Replace("{YY}", DateTime.Now.ToString("yy"))
                    .Replace("{NUMBER}", number.ToString("D6"));
            }

            return $"{sequence.Prefix}-{number:D6}";
        }

        public async Task<string> GenerateRequestNumberAsync(int requestTypeId)
        {
            await using var transaction =
                await _db.Database.BeginTransactionAsync();
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
                    .FirstOrDefaultAsync(x =>
                        x.RequestTypeId == requestTypeId);

            if (requestType == null)
            {
                throw new InvalidOperationException(
                    $"RequestType {requestTypeId} was not found.");
            }

            if (requestType.SequenceNumberTypeId == null)
            {
                throw new InvalidOperationException(
                    $"RequestType '{requestType.TypeName}' has no Sequence Number Type configured.");
            }



            var sequence =
                await db.SequenceNumberTypes
                    .FromSqlInterpolated($"SELECT * FROM dbo.SequenceNumberTypes WITH (UPDLOCK, HOLDLOCK) WHERE SequenceNumberTypeID = {requestType.SequenceNumberTypeId.Value}")
                    .AsNoTracking()
                    .SingleOrDefaultAsync();

            if (sequence == null)
            {
                throw new InvalidOperationException(
                    $"Sequence Number Type {requestType.SequenceNumberTypeId} was not found.");
            }

            if (sequence.NextNumber == null)
            {
                throw new InvalidOperationException(
                    $"Sequence '{sequence.TypeName}' has no NextNumber configured.");
            }



            var number = sequence.NextNumber.Value;

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE dbo.SequenceNumberTypes SET NextNumber = NextNumber + 1 WHERE SequenceNumberTypeID = {sequence.SequenceNumberTypeId}");

            return Format(sequence, number);
        }

        private static string Format(SequenceNumberType sequence,int number)
        {
            if (string.IsNullOrWhiteSpace(sequence.FormatString))
            {
                return $"{sequence.Prefix}/{DateTime.Now.Year}/{number}";
            }

            return sequence.FormatString
                .Replace("{PREFIX}", sequence.Prefix ?? "")
                .Replace("{YEAR}", DateTime.Now.Year.ToString())
                .Replace("{YY}", DateTime.Now.ToString("yy"))
                .Replace("{NUMBER}", number.ToString());
        }
    }
}
