using System.Text.Json;
using HRM.Api.Controllers.V1;
using HRM.Application.Transport;
using HRM.DTOs.Helpdesk;
using HRM.Models;
using HRM.Services.Contracts;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class HelpdeskTransportTests
{
    [Fact]
    public void SaveBotSettingsEnvelopeDeserializesDisabledAndTenantScope()
    {
        // Simulate exact serialization performed by HttpApiOperationClient.SendAsync
        var method = typeof(HelpdeskServiceContracts.IOperations).GetMethod(nameof(HelpdeskServiceContracts.IOperations.SaveBotSettingsAsync))!;
        var paramName = method.GetParameters()[0].Name!;
        var payload = new SaveHelpdeskBotSettingsDto
        {
            IsEnabled = false,
            ScopeMode = HelpdeskScopeModes.SelectedOrganisations,
            EnabledOrganisationIds = [5, 12]
        };

        var body = new Dictionary<string, JsonElement>
        {
            [paramName] = JsonSerializer.SerializeToElement(payload, typeof(SaveHelpdeskBotSettingsDto), ApiOperationKeys.Json)
        };
        var wireJson = JsonSerializer.Serialize(body, ApiOperationKeys.Json);

        // Simulate ASP.NET Core MVC JSON deserialization (ApiEndpointRegistration options)
        var apiOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            IncludeFields = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };

        var request = JsonSerializer.Deserialize<HelpdeskController.SaveBotSettingsRequest>(wireJson, apiOptions)!;
        Assert.NotNull(request);
        Assert.NotNull(request.model);
        Assert.False(request.model.IsEnabled);
        Assert.Equal(HelpdeskScopeModes.SelectedOrganisations, request.model.ScopeMode);
        Assert.Equal([5, 12], request.model.EnabledOrganisationIds);
    }

    [Fact]
    public void SaveArticleEnvelopeDeserializesAllFields()
    {
        var method = typeof(HelpdeskServiceContracts.IOperations).GetMethod(nameof(HelpdeskServiceContracts.IOperations.SaveArticleAsync))!;
        var paramName = method.GetParameters()[0].Name!;
        var article = new HelpdeskArticleDto
        {
            Id = 0,
            Category = "Leave",
            Title = "How to apply for sick leave",
            Keywords = "sick, medical",
            Answer = "Go to /online/leaves",
            IsActive = true,
            SortOrder = 20
        };

        var body = new Dictionary<string, JsonElement>
        {
            [paramName] = JsonSerializer.SerializeToElement(article, typeof(HelpdeskArticleDto), ApiOperationKeys.Json)
        };
        var wireJson = JsonSerializer.Serialize(body, ApiOperationKeys.Json);

        var apiOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            IncludeFields = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };

        var request = JsonSerializer.Deserialize<HelpdeskController.SaveArticleRequest>(wireJson, apiOptions)!;
        Assert.NotNull(request);
        Assert.NotNull(request.model);
        Assert.Equal("Leave", request.model.Category);
        Assert.Equal("How to apply for sick leave", request.model.Title);
        Assert.Equal("Go to /online/leaves", request.model.Answer);
    }

    [Fact]
    public void AvailabilityAndBotSettingsRoundTripPreserveDisabledState()
    {
        var apiOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            IncludeFields = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };

        var availability = new HelpdeskAvailabilityDto(false, false, HelpdeskScopeModes.Global, "Disabled");
        var availabilityWire = JsonSerializer.Serialize(availability, apiOptions);
        var availabilityBack = JsonSerializer.Deserialize<HelpdeskAvailabilityDto>(availabilityWire, ApiOperationKeys.Json)!;
        Assert.False(availabilityBack.IsAvailable);
        Assert.False(availabilityBack.GlobalEnabled);

        var settings = new HelpdeskBotSettingsDto
        {
            IsEnabled = false,
            ScopeMode = HelpdeskScopeModes.SelectedOrganisations,
            Organisations = [new HelpdeskOrganisationToggleDto { OrganisationId = 3, OrganisationName = "Org A", IsEnabled = false }]
        };
        var settingsWire = JsonSerializer.Serialize(settings, apiOptions);
        var settingsBack = JsonSerializer.Deserialize<HelpdeskBotSettingsDto>(settingsWire, ApiOperationKeys.Json)!;
        Assert.False(settingsBack.IsEnabled);
        Assert.Equal(HelpdeskScopeModes.SelectedOrganisations, settingsBack.ScopeMode);
        Assert.Single(settingsBack.Organisations);
        Assert.False(settingsBack.Organisations[0].IsEnabled);
    }
}
