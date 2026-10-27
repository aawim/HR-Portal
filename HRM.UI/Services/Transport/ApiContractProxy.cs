using System.Reflection;
using HRM.Application.Transport;
namespace HRM.UI.Services.Transport;

/// <summary>Creates clients only for explicitly registered, asynchronous application interfaces.</summary>
public class ApiContractProxy : DispatchProxy
{
    private IApiOperationClient client = null!;
    private Type contract = null!;
    private static readonly MethodInfo GenericInvoke = typeof(IApiOperationClient).GetMethods().Single(m => m.IsGenericMethodDefinition);

    public static T Create<T>(IApiOperationClient client) where T : class
    {
        _ = new ApiOperationDescriptor(typeof(T));
        var proxy = Create<T, ApiContractProxy>();
        var state = (ApiContractProxy)(object)proxy;
        state.client = client;
        state.contract = typeof(T);
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);
        args ??= [];
        var parameters = targetMethod.GetParameters();
        var values = new List<ApiArgument>();
        var cancellation = CancellationToken.None;
        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType == typeof(CancellationToken)) cancellation = (CancellationToken)args[i]!;
            else values.Add(new(parameters[i].ParameterType, args[i]));
        }
        if (targetMethod.ReturnType == typeof(Task))
            return client.InvokeAsync(contract, targetMethod.Name, values, cancellation);
        return GenericInvoke.MakeGenericMethod(targetMethod.ReturnType.GenericTypeArguments[0])
            .Invoke(client, [contract, targetMethod.Name, values, cancellation]);
    }
}
