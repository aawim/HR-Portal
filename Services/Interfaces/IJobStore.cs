namespace HRM.Services.Interfaces
{
    public interface IJobStore
    {
        int? CurrentJobId { get; }
        void SetJobId(int jobId);
    }
}
