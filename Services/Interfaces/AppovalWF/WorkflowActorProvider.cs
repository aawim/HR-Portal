namespace HRM.Services.Interfaces.AppovalWF
{
    public class WorkflowActorProvider : IWorkflowActorProvider
    {
        private readonly IUserAccessService _userAccess;

        public WorkflowActorProvider(IUserAccessService userAccess)
        {
            _userAccess = userAccess;
        }

        public async Task<int> GetCurrentUserIdAsync(
            CancellationToken cancellationToken = default)
        {
            var context = await _userAccess.RequireContextAsync();

            // Replace UserId with the actual property
            // in your UserContextDto.
            var userId = context.UserId;

            if (userId <= 0)
            {
                throw new UnauthorizedAccessException(
                    "Authenticated UserID is not available.");
            }

            return userId;
        }

    }
}
