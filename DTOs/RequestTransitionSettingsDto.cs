namespace HRM.DTOs;

public sealed record RequestTransitionOption(int Id, string Name);
public sealed record AllowedRequestTransitionDto(int FromStateId, string FromState, int ToStateId, string ToState);
public sealed class RequestTransitionSettingsDto
{
    public List<RequestTransitionOption> RequestTypes { get; set; } = [];
    public List<RequestTransitionOption> States { get; set; } = [];
}
