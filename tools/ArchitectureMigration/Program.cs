using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;
var root=Path.GetFullPath(args.FirstOrDefault()??".");
var source=Path.Combine(root,"Models");
var result=new List<object>();
foreach(var file in Directory.EnumerateFiles(source,"*.cs",SearchOption.AllDirectories))
{
 var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(file));var unit=tree.GetCompilationUnitRoot();
 var declarations=unit.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Where(t=>!t.Ancestors().OfType<BaseTypeDeclarationSyntax>().Any()).ToArray();
 result.Add(new{File=Path.GetRelativePath(root,file),Types=declarations.Select(t=>new{Name=t.Identifier.ValueText,Backend=t.Identifier.ValueText is "HrmTeContext"||t.BaseList?.Types.Any(b=>b.Type.ToString().Contains("EntityTypeConfiguration")||b.Type.ToString().Contains("DbContext"))==true}).ToArray()});
}
if(!args.Contains("--inspect-ui"))Console.WriteLine(JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));

if(args.Contains("--extract-models"))
{
 foreach(var file in Directory.EnumerateFiles(source,"*.cs",SearchOption.AllDirectories).ToArray())
 {
  var unit=CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetCompilationUnitRoot();
  var declarations=unit.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Where(t=>!t.Ancestors().OfType<BaseTypeDeclarationSyntax>().Any()).ToArray();
  foreach(var group in declarations.GroupBy(t=>new{Namespace=string.Join(".",t.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(n=>n.Name.ToString())),Backend=t.Identifier.ValueText is "HrmTeContext"||t.BaseList?.Types.Any(b=>b.Type.ToString().Contains("EntityTypeConfiguration")||b.Type.ToString().Contains("DbContext"))==true}))
  {
   var folder=group.Key.Backend?"HRM.Api/Data/EntityConfigurations":"HRM.Core/Models";
   var target=Path.GetFullPath(Path.Combine(root,folder,Path.GetRelativePath(source,file)));
   if(!target.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Outside workspace.");
   var usings=unit.Usings.Concat(group.SelectMany(t=>t.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().SelectMany(n=>n.Usings))).Select(u=>u.ToString()).Distinct();
   if(!group.Key.Backend)usings=usings.Where(u=>!u.Contains("Microsoft.EntityFrameworkCore"));
   var content=string.Join("\n",usings)+"\nnamespace "+group.Key.Namespace+";\n"+string.Join("\n",group.Select(t=>t.ToFullString()));
   Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.WriteAllText(target,content);
  }
  if(declarations.Length>0)File.Delete(file);
 }
}

if(args.Contains("--inspect-ui"))
{
 var directory=Path.Combine(root,"bin","AttendanceRefresh");
 System.Runtime.Loader.AssemblyLoadContext.Default.Resolving+=(c,n)=>File.Exists(Path.Combine(directory,n.Name+".dll"))?c.LoadFromAssemblyPath(Path.Combine(directory,n.Name+".dll")):null;
 var assembly=System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(directory,"HRM.dll"));
 var bindings=assembly.GetTypes().Where(t=>t.Namespace?.StartsWith("HRM.Components")==true).SelectMany(t=>t.GetProperties(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).Where(p=>p.GetCustomAttributesData().Any(a=>a.AttributeType.FullName=="Microsoft.AspNetCore.Components.InjectAttribute")).Select(p=>new{Component=t.FullName,Property=p.Name,Service=p.PropertyType.FullName,Assembly=p.PropertyType.Assembly.GetName().Name})).ToList();
 Console.WriteLine(JsonSerializer.Serialize(bindings,new JsonSerializerOptions{WriteIndented=true}));
}

if(args.Contains("--extract-contracts"))ContractExtraction.Run(root);
