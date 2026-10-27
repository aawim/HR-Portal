using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace HRM.Application.Transport;

public sealed record ApiOperationRequest(string Contract,string Method,IReadOnlyList<JsonElement> Arguments);
public sealed record ApiOperationProblem(string Code,string Message);
public sealed record ApiOperationResponse(JsonElement? Value,ApiOperationProblem? Error=null,IReadOnlyList<HRM.Services.ToastMessage>? Notifications=null);
public readonly record struct ApiArgument(Type DeclaredType,object? Value)
{
    public static ApiArgument From<T>(T value)=>new(typeof(T),value);
}
public interface IApiOperationClient
{
    Task<T> InvokeAsync<T>(Type contract,string method,IReadOnlyList<ApiArgument> arguments,CancellationToken ct=default);
    Task InvokeAsync(Type contract,string method,IReadOnlyList<ApiArgument> arguments,CancellationToken ct=default);
}
public static class ApiOperationKeys
{
    public static string Method(MethodInfo method)=>method.Name+"("+string.Join(",",method.GetParameters().Where(p=>p.ParameterType!=typeof(CancellationToken)).Select(p=>p.ParameterType.FullName))+")";
    public static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web){IncludeFields=true,ReferenceHandler=ReferenceHandler.IgnoreCycles,MaxDepth=64};
}
