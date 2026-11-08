using HRM.Components.Shared;

namespace HRM.Helpers;

internal static class LeaveRequestStateMapper
{
    public static int ToLeaveState(int requestState, bool isProcessing = false) => requestState switch
    {
        SharedConfig.RequestStates.INCOMPLETE or
        SharedConfig.RequestStates.PENDING_STAFF_COMPLETION => SharedConfig.LeaveStates.INCOMPLETE,
        SharedConfig.RequestStates.PENDING_VERIFICATION or
        SharedConfig.RequestStates.PENDING_SUPERVISOR_VERIFICATION or
        SharedConfig.RequestStates.PENDING_HR_VERIFICATION => SharedConfig.LeaveStates.PENDING_VERIFICATION,
        SharedConfig.RequestStates.PENDING_APPROVAL or
        SharedConfig.RequestStates.PENDING_SUPERVISOR_APPROVAL or
        SharedConfig.RequestStates.PENDING_HR_APPROVAL or
        SharedConfig.RequestStates.PENDING_APPROVAL_PERSON_1 or
        SharedConfig.RequestStates.PENDING_APPROVAL_PERSON_2 => SharedConfig.LeaveStates.PENDING_APPROVAL,
        SharedConfig.RequestStates.PENDING_HOD_APPROVAL => SharedConfig.LeaveStates.PENDING_HOD_APPROVAL,
        SharedConfig.RequestStates.PENDING_HR_HEAD_APPROVAL => SharedConfig.LeaveStates.PENDING_HR_HEAD_APPROVAL,
        SharedConfig.RequestStates.APPROVED => SharedConfig.LeaveStates.APPROVED,
        SharedConfig.RequestStates.REJECTED => SharedConfig.LeaveStates.REJECTED,
        SharedConfig.RequestStates.CANCELLED => SharedConfig.LeaveStates.CANCELLED,
        _ when isProcessing => SharedConfig.LeaveStates.PENDING_APPROVAL,
        _ => throw new InvalidOperationException(
            $"Request state {requestState} is not supported for leave applications.")
    };
}
