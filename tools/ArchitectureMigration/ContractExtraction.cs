using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;
internal static class ContractExtraction
{
    private static string Namespace(SyntaxNode t)=>string.Join(".",t.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(n=>n.Name.ToString()));
    private static string Imports(CompilationUnitSyntax unit)=>string.Join("\n",unit.Usings.Select(u=>u.ToString()).Where(u=>u.StartsWith("using System")||u.StartsWith("using HRM.DTOs")||u.StartsWith("using HRM.Models")||u.StartsWith("using HRM.Enum")||u.StartsWith("using HRM.Components.Shared")||u.StartsWith("using HRM.Services.Interfaces")||u.StartsWith("using HRM.WorkPlanning.DTOs")).Distinct());
    private static bool Public(MemberDeclarationSyntax t)=>t.Modifiers.Any(SyntaxKind.PublicKeyword);
    private static bool Dto(BaseTypeDeclarationSyntax t)=>t is EnumDeclarationSyntax or RecordDeclarationSyntax || t is ClassDeclarationSyntax c && Public(c) && c.ParameterList==null && c.Members.All(m=>m is PropertyDeclarationSyntax or FieldDeclarationSyntax or ConstructorDeclarationSyntax or EnumDeclarationSyntax) && !(c.BaseList?.Types.Any(b=>b.ToString().Contains("Exception"))??false);
    public static void Run(string root)
    {
        var sourceFiles=new[]{"Services","WorkPlanning"}.SelectMany(d=>Directory.EnumerateFiles(Path.Combine(root,d),"*.cs",SearchOption.AllDirectories)).ToList();
        // Move data contracts and pure interfaces out of implementation files.
        foreach(var file in sourceFiles)
        {
            var unit=CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetCompilationUnitRoot();
            var types=unit.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Where(t=>!t.Ancestors().OfType<BaseTypeDeclarationSyntax>().Any()&&Public(t)).ToList();
            var selected=types.Where(t=>Dto(t)||t is InterfaceDeclarationSyntax&&!t.ToString().Contains("HrmTeContext")&&!t.ToString().Contains("DbContext")).ToList();
            if(selected.Count==0)continue;
            foreach(var group in selected.GroupBy(Namespace))
            {
                var relative=Path.GetRelativePath(root,file);var target=Path.Combine(root,"HRM.Application","Features",relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllText(target,Imports(unit)+"\nnamespace "+group.Key+";\n"+string.Join("\n",group.Select(t=>t.ToFullString())));
            }
            unit=unit.RemoveNodes(selected,SyntaxRemoveOptions.KeepExteriorTrivia)!;
            File.WriteAllText(file,unit.ToFullString());
        }
        using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"architecture-ui-services.json")));
        var excluded=new HashSet<string>{"HRM.Services.ToastService","HRM.Services.PortalContext","HRM.Services.State.LeaveApplicationState","HRM.Services.Stores.LeaveStore","HRM.Models.HrmTeContext"};
        var classes=manifest.RootElement.EnumerateArray().Where(x=>x.GetProperty("Assembly").GetString()=="HRM").Select(x=>x.GetProperty("Service").GetString()!).Where(n=>!excluded.Contains(n)&&!n.Contains('`')).Distinct().ToList();
        var descriptors=new List<object>();
        foreach(var fullName in classes)
        {
            var found=new List<(string File,CompilationUnitSyntax Unit,ClassDeclarationSyntax Type)>();
            foreach(var file in sourceFiles)
            {
                var unit=CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetCompilationUnitRoot();
                found.AddRange(unit.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(t=>!t.Ancestors().OfType<TypeDeclarationSyntax>().Any()&&Namespace(t)+"."+t.Identifier.ValueText==fullName).Select(t=>(file,unit,t)));
            }
            if(found.Count==0)continue;
            var methods=found.SelectMany(x=>x.Type.Members.OfType<MethodDeclarationSyntax>()).Where(m=>Public(m)&&!m.Modifiers.Any(SyntaxKind.StaticKeyword)&&m.TypeParameterList==null&&(m.ReturnType.ToString().StartsWith("Task")||m.ReturnType.ToString().StartsWith("ValueTask"))&&!m.ParameterList.ToString().Contains("HrmTeContext")&&!m.ParameterList.ToString().Contains("DbContext")).ToList();
            if(methods.Count==0)continue;
            var shared=found.SelectMany(x=>x.Type.Members).Where(m=>m is BaseTypeDeclarationSyntax&&Public(m)||m is FieldDeclarationSyntax f&&Public(f)&&(f.Modifiers.Any(SyntaxKind.ConstKeyword)||f.Modifiers.Any(SyntaxKind.StaticKeyword))).ToList();
            var name=found[0].Type.Identifier.ValueText;var ns=Namespace(found[0].Type);var contractName=name+"Contracts";var contractFull=ns+".Contracts."+contractName;
            var signatures=methods.Select(m=>m.WithBody(null).WithExpressionBody(null).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)).WithModifiers(SyntaxFactory.TokenList()).ToString());
            var target=Path.Combine(root,"HRM.Application","Contracts",ns.Replace('.','_')+"_"+contractName+".cs");Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllText(target,string.Join("\n",found.Select(x=>Imports(x.Unit)).Distinct())+"\nnamespace "+ns+".Contracts;\npublic abstract class "+contractName+"\n{\n"+string.Join("\n",shared.Select(m=>m.ToFullString()))+"\npublic interface IOperations\n{\n"+string.Join("\n",signatures)+"\n}\n}\n");
            var canonical=found.OrderByDescending(x=>x.Type.ParameterList!=null).ThenByDescending(x=>Path.GetFileName(x.File)==name+".cs").First();
            foreach(var part in found)
            {
                var modified=part.Type.RemoveNodes(part.Type.Members.Where(m=>m is BaseTypeDeclarationSyntax&&Public(m)||m is FieldDeclarationSyntax f&&Public(f)&&(f.Modifiers.Any(SyntaxKind.ConstKeyword)||f.Modifiers.Any(SyntaxKind.StaticKeyword))),SyntaxRemoveOptions.KeepExteriorTrivia)!;
                if(part==canonical)
                {
                    var bases=new[]{SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("global::"+contractFull)),SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("global::"+contractFull+".IOperations"))}.Concat(modified.BaseList?.Types??SyntaxFactory.SeparatedList<BaseTypeSyntax>());
                    modified=modified.WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SeparatedList<BaseTypeSyntax>(bases)));
                }
                File.WriteAllText(part.File,part.Unit.ReplaceNode(part.Type,modified).ToFullString());
            }
            // Client is strongly typed to the shared contract and has no implementation/database reference.
            var code="using "+ns+".Contracts;\n"+string.Join("\n",found.Select(x=>Imports(x.Unit)).Distinct())+"\nnamespace HRM.UI.Services.Generated;\npublic sealed class "+name+"ApiClient(HRM.Application.Transport.IApiOperationClient api): "+contractFull+", "+contractFull+".IOperations\n{\n";
            foreach(var method in methods)
            {
                var args=method.ParameterList.Parameters.Where(p=>!p.Type!.ToString().Contains("CancellationToken")).Select(p=>p.Identifier.ValueText).ToArray();
                var ct=method.ParameterList.Parameters.FirstOrDefault(p=>p.Type!.ToString().Contains("CancellationToken"))?.Identifier.ValueText??"default";
                var returnType=method.ReturnType.ToString();var inner=returnType=="Task"?null:returnType.Substring(returnType.IndexOf('<')+1,returnType.LastIndexOf('>')-returnType.IndexOf('<')-1);
                code+="public "+returnType+" "+method.Identifier+method.ParameterList+" => api."+(inner==null?"InvokeAsync":"InvokeAsync<"+inner+">")+"(typeof("+contractFull+".IOperations),nameof("+method.Identifier+"),["+string.Join(",",args)+"],"+ct+");\n";
            }
            code+="}\n";var client=Path.Combine(root,"HRM.UI","Services","Generated",name+"ApiClient.cs");Directory.CreateDirectory(Path.GetDirectoryName(client)!);File.WriteAllText(client,code);
            descriptors.Add(new{Implementation=fullName,Contract=contractFull+".IOperations",Client="HRM.UI.Services.Generated."+name+"ApiClient",Class=name,Methods=methods.Select(m=>m.Identifier.ValueText).ToArray()});
        }
        File.WriteAllText(Path.Combine(root,"architecture-operation-contracts.json"),JsonSerializer.Serialize(descriptors,new JsonSerializerOptions{WriteIndented=true}));
    }
}
