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

                var number = sequence.NextNumber;

                sequence.NextNumber++;

                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return BuildReference(sequence, (int)number);
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
            

            using var transaction =
                await _db.Database.BeginTransactionAsync();

            var requestType =
                await _db.RequestTypes
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
                await _db.SequenceNumberTypes
                    .FirstOrDefaultAsync(x =>
                        x.SequenceNumberTypeId ==
                        requestType.SequenceNumberTypeId);

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

            sequence.NextNumber++;

            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

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
