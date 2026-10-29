namespace HRM.Application.Transport;

public static class ApiProblemCodes
{
    public const string Header="X-HRM-Problem-Code";
    public const string WorkflowUnavailable="workflow_unavailable";
    public const string WorkflowUnavailableMessage="This request cannot be processed until HR finishes setting up its approval process. No changes have been saved. Please contact your organisation's HR team.";
}
