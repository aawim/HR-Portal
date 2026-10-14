namespace HRM.Services.Interfaces.AppovalWF
{
    public interface IWorkflowEngine
    {
        Task<long> StartAsync(
           int requestId,
           int subjectJobId,
           CancellationToken cancellationToken = default);

        Task CancelAsync(
            int requestId,
            string? reason,
            CancellationToken cancellationToken = default);

    }
}
