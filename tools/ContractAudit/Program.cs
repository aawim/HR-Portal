using System.Reflection;
using System.Text.Json;
using HRM.Application.Transport;
var assembly=typeof(IApiOperationClient).Assembly;
var contracts=HRM.Application.Contracts.ApplicationContractCatalog.All;
var rows=new List<object>();
foreach(var contract in contracts)
{
 var leaks=new HashSet<string>();var visited=new HashSet<Type>();
 void Visit(Type type,string path)
 {
  if(type==typeof(CancellationToken)||type==typeof(void)||!visited.Add(type))return;
  if(type.Namespace?.StartsWith("HRM.Models")==true){leaks.Add(path+" -> "+type.FullName);return;}
  if(type.IsArray){Visit(type.GetElementType()!,path+"[]");return;}
  if(type.IsGenericType)foreach(var argument in type.GenericTypeArguments)Visit(argument,path+"<"+argument.Name+">");
  if(type.Assembly!=assembly)return;
  foreach(var property in type.GetProperties(BindingFlags.Public|BindingFlags.Instance))Visit(property.PropertyType,path+"."+property.Name);
  foreach(var field in type.GetFields(BindingFlags.Public|BindingFlags.Instance))Visit(field.FieldType,path+"."+field.Name);
 }
 foreach(var method in contract.GetMethods())
 {
  Visit(method.ReturnType,method.Name+" return");
  foreach(var parameter in method.GetParameters())Visit(parameter.ParameterType,method.Name+"("+parameter.Name+")");
 }
 rows.Add(new{Contract=contract.FullName,Operations=contract.GetMethods().Length,EntityDependencies=leaks.Order().ToArray()});
}
Console.WriteLine(JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
