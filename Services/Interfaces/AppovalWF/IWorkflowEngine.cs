namespace HRM.Services.Interfaces.AppovalWF
{
    public interface IWorkflowEngine
    {
        Task<long> StartAsync(
           int requestId,
           int subjectJobId,
           CancellationToken cancellationToken = default);
    }
}
