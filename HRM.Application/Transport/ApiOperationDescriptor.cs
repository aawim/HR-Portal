using System.Reflection;
using System.Text.Json;

namespace HRM.Application.Transport;

/// <summary>Describes an explicitly registered contract, without loading types named by a caller.</summary>
public sealed class ApiOperationDescriptor
{
    private readonly IReadOnlyDictionary<string, MethodInfo> methods;
    public Type Contract { get; }
    public string Key => Contract.FullName!;

    public ApiOperationDescriptor(Type contract)
    {
        if (!contract.IsInterface || contract.ContainsGenericParameters || contract.FullName is null)
            throw new ArgumentException("An operation contract must be a closed interface.", nameof(contract));
        Contract = contract;
        var entries = contract.GetInterfaces().Append(contract).SelectMany(t => t.GetMethods()).Distinct();
        var map = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
        foreach (var method in entries)
        {
            var result = method.ReturnType;
            if (method.IsGenericMethod || method.IsSpecialName || method.GetParameters().Any(p => p.ParameterType.IsByRef) ||
                !(result == typeof(Task) || result.IsGenericType && result.GetGenericTypeDefinition() == typeof(Task<>)))
                throw new ArgumentException($"Operation {method.Name} must be asynchronous and have serializable parameters.", nameof(contract));
            var key = ApiOperationKeys.Method(method);
            if (!map.TryAdd(key, method)) throw new ArgumentException($"Duplicate operation {key}.", nameof(contract));
        }
        methods = map;
    }

    public MethodInfo Resolve(string key) => methods.TryGetValue(key, out var method)
        ? method : throw new KeyNotFoundException("The requested operation is not registered.");

    public MethodInfo Resolve(string name, IReadOnlyList<ApiArgument> arguments)
    {
        var matches = methods.Values.Where(m => m.Name == name &&
            m.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).Select(p => p.ParameterType)
                .SequenceEqual(arguments.Select(a => a.DeclaredType))).ToArray();
        return matches.Length == 1 ? matches[0] : throw new ArgumentException("The operation signature does not match the registered contract.");
    }

    public object?[] Deserialize(MethodInfo method, IReadOnlyList<JsonElement> arguments, CancellationToken cancellationToken)
    {
        if (!methods.Values.Contains(method)) throw new ArgumentException("Unregistered operation.", nameof(method));
        var parameters = method.GetParameters();
        if (parameters.Count(p => p.ParameterType != typeof(CancellationToken)) != arguments.Count)
            throw new ArgumentException("The operation argument count is invalid.", nameof(arguments));
        var values = new object?[parameters.Length];
        var index = 0;
        for (var i = 0; i < parameters.Length; i++)
        {
            var type = parameters[i].ParameterType;
            if (type == typeof(CancellationToken)) { values[i] = cancellationToken; continue; }
            var value = arguments[index++];
            if (value.ValueKind == JsonValueKind.Null && type.IsValueType && Nullable.GetUnderlyingType(type) is null)
                throw new ArgumentException("A required operation argument is missing.", nameof(arguments));
            values[i] = value.Deserialize(type, ApiOperationKeys.Json);
        }
        return values;
    }
}
