namespace HRM.Communications;

public static class CommunicationApprovalPolicy
{
    public static bool CanDispatch(string? status) => status is "NotRequired" or "Approved";
}
