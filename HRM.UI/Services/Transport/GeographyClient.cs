using HRM.Application.Contracts.Pages;
using HRM.Models.Archives;
using HRM.Models;
using HRM.Services.Interfaces;

namespace HRM.UI.Services.Transport;

/// <summary>Adapts scalar directory options to the existing address form's local view models.</summary>
public sealed class GeographyClient(IEmployeeAddresses addresses) : IGeographyService
{
    public async Task<List<Country>> GetCountriesAsync() => (await addresses.GetDirectoryAsync()).Countries
        .Select(x => new Country { CountryId = x.Id, NameEnglish = x.Name }).ToList();
    public async Task<List<Atoll>> GetAtollsAsync() => (await addresses.GetDirectoryAsync()).Atolls
        .Select(x => new Atoll { AtollId = x.Id, NameEnglish = x.Name }).ToList();
    public async Task<List<Island>> GetIslandsAsync(int atollId) => (await addresses.GetDirectoryAsync(atollId)).Islands
        .Select(x => new Island { IslandId = x.Id, AtollId = atollId, NameEnglish = x.Name }).ToList();
}
