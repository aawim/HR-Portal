using System.Reflection;
using System.Text.Json;
using HRM.Application.Contracts;

namespace HRM.Application.Transport;

/// <summary>Generated routes for the reviewed contracts; never accepts a destination supplied by a user.</summary>
public sealed record ApiEndpoint(string Contract, string Operation, string Verb, string Path);

public static class ApiEndpointCatalog
{
    public static IReadOnlyList<ApiEndpoint> All { get; } = Load();
    private static readonly IReadOnlyDictionary<(string, string), ApiEndpoint> Index =
        All.ToDictionary(e => (e.Contract, e.Operation));

    public static ApiEndpoint Resolve(Type contract, MethodInfo operation)
    {
        if (!ApplicationContractCatalog.All.Contains(contract) ||
            !Index.TryGetValue((contract.FullName!, ApiOperationKeys.Method(operation)), out var endpoint))
            throw new ArgumentException("This operation has no registered API endpoint.");
        return endpoint;
    }

    private static IReadOnlyList<ApiEndpoint> Load()
    {
        using var stream = typeof(ApiEndpointCatalog).Assembly.GetManifestResourceStream("HRM.Application.ApiEndpoints.json")
            ?? throw new InvalidOperationException("The API route catalog is missing.");
        var entries = JsonSerializer.Deserialize<List<ApiEndpoint>>(stream)!;
        if (entries.Any(e => e.Verb is not ("GET" or "POST") || !e.Path.StartsWith("/api/v1/", StringComparison.Ordinal) ||
            e.Path.Contains('?') || e.Path.Contains('#') || e.Path.Contains("..")))
            throw new InvalidOperationException("The API route catalog is invalid.");
        return entries.AsReadOnly();
    }
}
