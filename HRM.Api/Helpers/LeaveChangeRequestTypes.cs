using HRM.DTOs.Leave;

namespace HRM.Helpers;

public static class LeaveChangeRequestTypes
{
    public const int Legacy = 48;
    public const int EarlyReturn = 105;
    public const int Extension = 106;
    public static bool IsChange(int requestTypeId) => requestTypeId is Legacy or EarlyReturn or Extension;
    public static int ForKind(LeaveChangeKind kind) => kind switch
    {
        LeaveChangeKind.EarlyReturn => EarlyReturn,
        LeaveChangeKind.Extension => Extension,
        _ => throw new InvalidOperationException("Select a valid leave change.")
    };
}
