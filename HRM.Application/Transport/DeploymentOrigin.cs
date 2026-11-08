namespace HRM.Application.Transport;

public static class DeploymentOrigin
{
    public static Uri Require(string? value, string name, bool production = false)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo) ||
            production && (uri.IsLoopback || uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"{name} must be an explicit HTTPS origin without a path or credentials; production cannot use localhost.");
        return uri;
    }
}
