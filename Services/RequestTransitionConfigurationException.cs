namespace HRM.Services;

public sealed class RequestTransitionConfigurationException(string detail)
    : InvalidOperationException("This action could not be completed right now. Please contact your organisation's HR team for assistance. No changes have been made.")
{
    public string Detail { get; } = detail;
}
