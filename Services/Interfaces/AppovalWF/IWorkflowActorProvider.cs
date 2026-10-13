namespace HRM.Services.Interfaces.AppovalWF
{
    public interface IWorkflowActorProvider
    {
        Task<int> GetCurrentUserIdAsync(CancellationToken cancellationToken = default);
    }
}
