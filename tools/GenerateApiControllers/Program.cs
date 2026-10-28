using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HRM.Application.Contracts;
using HRM.Application.Transport;
if (args is not ["--write", var directory]) throw new ArgumentException("Pass --write and the repository path explicitly.");
var root=Path.GetFullPath(directory);
var target=Path.Combine(root,"HRM.Api/Controllers/V1");Directory.CreateDirectory(target);
var nulls=new NullabilityInfoContext();
string Name(Type t,NullabilityInfo? n=null)
{
 string s;
 if(t.IsArray)s=Name(t.GetElementType()!,n?.ElementType)+"[]";
 else if(t.IsGenericType)
 {
  if(t.GetGenericTypeDefinition()==typeof(Nullable<>))return Name(t.GenericTypeArguments[0])+"?";
  s="global::"+t.GetGenericTypeDefinition().FullName!.Split('`')[0].Replace('+','.')+"<"+string.Join(", ",t.GenericTypeArguments.Select((a,i)=>Name(a,n?.GenericTypeArguments.ElementAtOrDefault(i))))+">";
 }
 else s="global::"+t.FullName!.Replace('+','.');
 return !t.IsValueType&&n?.ReadState==NullabilityState.Nullable?s+"?":s;
}
string Kebab(string s)=>Regex.Replace(Regex.Replace(s,"([A-Z]+)([A-Z][a-z])","$1-$2"),"([a-z0-9])([A-Z])","$1-$2").ToLowerInvariant();
bool Simple(Type t)=>t.IsEnum||t==typeof(string)||t.IsValueType&&(!t.IsGenericType||t.GetGenericTypeDefinition()==typeof(Nullable<>)&&Simple(t.GenericTypeArguments[0]));
string Default(ParameterInfo p)
{
 if(!p.HasDefaultValue)return "";
 var value=p.DefaultValue;
 return " = "+(value is null?"null":value is bool b?b?"true":"false":value is string s?JsonSerializer.Serialize(s):p.ParameterType.IsEnum?"("+Name(p.ParameterType)+")"+Convert.ToInt32(value):Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture));
}
var entries=new List<object>();var paths=new HashSet<string>();var count=0;
foreach(var contract in ApplicationContractCatalog.All)
{
 var baseName=contract.DeclaringType?.Name.Replace("ServiceContracts","")??(contract.Name.StartsWith("I", StringComparison.Ordinal) ? contract.Name[1..] : contract.Name);
 if(baseName.EndsWith("Service"))baseName=baseName[..^7];
 var controller=baseName+"Controller";var prefix="api/v1/"+Kebab(baseName);
 var file=new StringBuilder("// Typed endpoints delegate to canonical application services.\nusing Microsoft.AspNetCore.Mvc;\nusing HRM.Api.Infrastructure;\nnamespace HRM.Api.Controllers.V1;\n\n");
 if(baseName is "CurrentEmployeeSession" or "UserContext")file.AppendLine("[AllowMissingEmployee]");
 file.AppendLine($"[Route(\"{prefix}\")]\npublic sealed class {controller}({Name(contract)} service) : HrmApiController\n{{");
 foreach(var method in contract.GetMethods().Where(m => m.GetCustomAttribute<ObsoleteAttribute>() is null))
 {
  var parameters=method.GetParameters().Where(p=>p.ParameterType!=typeof(CancellationToken)).ToArray();
  var stem=method.Name.EndsWith("Async")?method.Name[..^5]:method.Name;
  if(contract.GetMethods().Count(m=>m.Name==method.Name)>1)stem+=parameters.Length==0?"Default":"By"+string.Join("And",parameters.Select(p=>char.ToUpperInvariant(p.Name![0])+p.Name[1..]));
  var reads=Regex.IsMatch(stem,@"^(Get|Search|Load|Resolve|Preview|Pending|History|Detail|Can|Has|Is|Require|Validate)")&&!stem.StartsWith("GetOrGenerate");
  var get=reads&&parameters.All(p=>Simple(p.ParameterType));
  var route=(reads&&!get?"query/":"")+Kebab(stem);var verb=get?"GET":"POST";
  if(!paths.Add(verb+" "+prefix+"/"+route))throw new Exception("Duplicate route.");
  var requestName=controller.Replace("Controller","")+stem+"Request";
  var request=get?string.Join(", ",parameters.Select(p=>"[FromQuery] "+Name(p.ParameterType,nulls.Create(p))+" "+p.Name+Default(p))):parameters.Length==0?"":"[FromBody] "+requestName+" request";
  string[] permissions=baseName switch
  {
   "LeaveConfigurationSettings" or "LeaveAccrualSettings" or "LeaveFlowConfiguration" => ["Admin"],
   "JobTypeConfiguration" => reads?["Configuration.View"]:["Admin"],
   "PositionOverview" => ["Position.Details","Staff.Positions.Manage"],
   "IndividualRegistry" => ["Individual.View","Employee.Create"],
   "IndividualRegistration" => ["Individual.Create"],
   "FormerStaff" => ["Employee.View","Staff.Details"],
   "WorkspaceDirectory" or "WorkspaceRecentSearch" => ["Employee.View"],
   "StaffOnboarding" when method.Name=="SubmitAsync" => ["Employee.Create"],
   "StaffOnboarding" when method.Name=="LoadAsync" => ["Employee.View","Employee.Create"],
   _=>[]
  };
  if(permissions.Length>0)file.AppendLine("    [ApiPermission("+string.Join(", ",permissions.Select(k=>JsonSerializer.Serialize(k)))+")]");
  file.AppendLine($"    [Http{(get?"Get":"Post")}(\"{route}\")]");
  var result=method.ReturnType==typeof(Task)?null:method.ReturnType.GenericTypeArguments[0];
  var resultName=result is null?"IActionResult":"ActionResult<"+Name(result,nulls.Create(method.ReturnParameter).GenericTypeArguments[0])+">";
  file.AppendLine($"    public async Task<{resultName}> {stem}({request})\n    {{");
  var call="service."+method.Name+"("+string.Join(", ",method.GetParameters().Select(p=>p.ParameterType==typeof(CancellationToken)?"HttpContext.RequestAborted":(get?"":"request.")+p.Name))+ ")";
  if(result is null)file.AppendLine($"        await {call};\n        return NoContent();");
  else file.AppendLine($"        return Ok(await {call});");
  file.AppendLine("    }\n");
  if(!get&&parameters.Length>0)
  {
   file.AppendLine($"    public sealed class {requestName}\n    {{");
   foreach(var p in parameters)
   {
    var type=Name(p.ParameterType,nulls.Create(p));var def=Default(p);
    var initializer=def.Length>0?def+";":p.ParameterType.IsValueType||type.EndsWith('?')?"":" = default!;";
    file.AppendLine($"        public {type} {p.Name} {{ get; set; }}{initializer}");
   }
   file.AppendLine("    }\n");
  }
  entries.Add(new{Contract=contract.FullName,Operation=ApiOperationKeys.Method(method),Verb=verb,Path="/"+prefix+"/"+route,Controller=controller,Action=stem});count++;
 }
 file.AppendLine("}");File.WriteAllText(Path.Combine(target,controller+".cs"),file.ToString());
}
File.WriteAllText(Path.Combine(root,"docs/api-endpoints.json"),JsonSerializer.Serialize(entries,new JsonSerializerOptions{WriteIndented=true})+"\n");
File.WriteAllText(Path.Combine(root,"HRM.Application/Transport/api-endpoints.json"),JsonSerializer.Serialize(entries,new JsonSerializerOptions{WriteIndented=true})+"\n");
Console.WriteLine($"Generated {ApplicationContractCatalog.All.Count} typed controllers / {count} actions.");
