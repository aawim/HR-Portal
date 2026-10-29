using HRM.Application.Contracts.Pages;
using HRM.Components.Admin.Settings.Positions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
namespace HRM.UI.UnitTests;
public sealed class JobMatrixRequirementRenderingTests
{
    [Fact]public async Task Identical_empty_requirement_rows_render_independently_and_pathway_rules_are_explained()
    {
        var profile=new JobMatrixProfile{CredentialRequirements=[new(),new()],QualificationPathways=[new(){Name="Route",Qualifications=[new(),new()]}]};
        using var provider=new ServiceCollection().AddLogging().BuildServiceProvider();await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
        var html=await renderer.Dispatcher.InvokeAsync(async()=> (await renderer.RenderComponentAsync<JobMatrixRequirementsEditor>(ParameterView.FromDictionary(new Dictionary<string,object?>{{"Profile",profile}}))).ToHtmlString());
        Assert.Equal(2,html.Split("Credential / training name").Length-1);Assert.Equal(2,html.Split("Qualification type / description").Length-1);Assert.Contains("MNQF Level 10",html);Assert.Contains("Complete any one pathway (OR)",html);Assert.Contains("Post-qualification experience",html);
    }
    [Fact]public async Task Requirement_sources_are_rendered_as_text_not_executable_markup()
    {
        var profile=new JobMatrixProfile{CredentialRequirements=[new(){Name="Practice",Issuer="Council",SourceReference="<script>alert(1)</script>"}]};
        using var provider=new ServiceCollection().AddLogging().BuildServiceProvider();await using var renderer=new HtmlRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
        var html=await renderer.Dispatcher.InvokeAsync(async()=> (await renderer.RenderComponentAsync<JobMatrixRequirementsView>(ParameterView.FromDictionary(new Dictionary<string,object?>{{"Profile",profile}}))).ToHtmlString());
        Assert.DoesNotContain("<script>",html);Assert.Contains("&lt;script&gt;",html);
    }
}
