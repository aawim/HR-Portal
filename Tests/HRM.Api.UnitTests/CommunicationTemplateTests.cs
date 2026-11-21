using HRM.Communications;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class CommunicationTemplateTests
{
    [Theory]
    [InlineData("{{UnsupportedVariable}}")]
    [InlineData("{{ Title }}")]
    [InlineData("{{Title}")]
    [InlineData(" ")]
    public void InvalidTemplatesAreRejected(string template) =>
        Assert.Throws<InvalidOperationException>(() => CommunicationMessageRenderer.Validate(template));

    [Fact]
    public void EventContentCannotInjectAnotherVariable()
    {
        var values = new Dictionary<string, string?> { ["Title"] = "Literal {{Description}}", ["Description"] = "Private content" };
        Assert.Equal("Literal {{Description}}", CommunicationMessageRenderer.Render("{{Title}}", values));
    }

    [Fact]
    public void MissingValuesAreRejected() => Assert.Throws<InvalidOperationException>(() =>
        CommunicationMessageRenderer.Render("{{Title}}", new Dictionary<string, string?>()));

    [Fact]
    public void ExpansionBeyondStorageLimitIsRejected() => Assert.Throws<InvalidOperationException>(() =>
        CommunicationMessageRenderer.Render("{{Description}}", new Dictionary<string, string?> { ["Description"] = new string('a', 4001) }));

    [Fact]
    public void UnicodeAndRepeatedVariablesArePreserved()
    {
        var values = new Dictionary<string, string?> { ["Title"] = "ސަލާމް" };
        Assert.Equal("ސަލާމް / ސަލާމް", CommunicationMessageRenderer.Render("{{Title}} / {{Title}}", values));
    }

    [Fact]
    public void EmployeeScheduleVariablesAreRendered() => Assert.Equal("Employee: 2026-11-21",
        CommunicationMessageRenderer.Render("{{EmployeeName}}: {{EffectiveDate}}", new Dictionary<string, string?> { ["EmployeeName"] = "Employee", ["EffectiveDate"] = "2026-11-21" }));
}
