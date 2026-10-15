namespace HRM.Services.Interfaces
{
    public interface ISequenceNumberService
    {
        Task<string> GenerateAsync(int sequenceNumberTypeId);
        Task<string> GenerateRequestNumberAsync(int requestTypeId);
        Task<string> GenerateRequestNumberAsync(HRM.Models.HrmTeContext db, int requestTypeId);
    }
}
