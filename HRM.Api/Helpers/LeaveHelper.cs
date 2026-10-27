using HRM.Components.Shared;
namespace HRM.Helpers
{
    public static class LeaveHelper
    {
        private static readonly HashSet<int> NonCancellableStates =
        [
            SharedConfig.LeaveStates.CANCELLED,
            SharedConfig.LeaveStates.REJECTED,
            SharedConfig.LeaveStates.APPROVED
        ];

        public static bool CanCancel(int leaveStateId)
        {
            return !NonCancellableStates.Contains(leaveStateId);
        }


    }
}
