namespace HRM.Application.Transport;

/// <summary>Verified signed-in identity for the UI host. Contains no cookie, access token or identity-provider token.</summary>
public sealed record ApiIdentityDto(bool IsAuthenticated, string? Username, string? DisplayName);
