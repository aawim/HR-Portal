namespace HRM.Services.Interfaces.AppovalWF
{
    public interface IWorkflowEngine
    {
        Task<long> StartAsync(
           int requestId,
           int subjectJobId,
           CancellationToken cancellationToken = default);

        // Starts inside the caller's transaction without committing it.
        Task<long> StartAsync(
            HRM.Models.HrmTeContext db,
            int requestId,
            int subjectJobId,
            CancellationToken cancellationToken = default);

        Task CancelAsync(
            int requestId,
            string? reason,
            CancellationToken cancellationToken = default);

    }
}
