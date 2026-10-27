using HRM.Application.Contracts.Pages;
using HRM.DTOs;
using HRM.Services.Interfaces;
namespace HRM.Services.Queries;
public sealed class EmployeeAddresses(IAddress addresses, IUserAccessService access) : IEmployeeAddresses
{
    public async Task<AddressDirectory> GetDirectoryAsync(int? atollId = null, CancellationToken ct = default)
    {
        await access.RequireContextAsync(); ct.ThrowIfCancellationRequested();
        var countries = await addresses.GetCountriesAsync();
        var atolls = await addresses.GetAtollsAsync();
        var islands = atollId.HasValue ? await addresses.GetIslandsAsync(atollId.Value) : [];
        var instances = await addresses.GetAddressInstanceTypesAsync();
        var bases = await addresses.GetAddressBaseTypesAsync();
        var locations = await addresses.GetBusinessEntityLocationTypesAsync();
        ct.ThrowIfCancellationRequested();
        return new(countries.Select(x => new DirectoryOption(x.CountryId, x.NameEnglish)).ToList(),
            atolls.Select(x => new DirectoryOption(x.AtollId, x.NameEnglish)).ToList(),
            islands.Select(x => new DirectoryOption(x.IslandId, x.NameEnglish)).ToList(),
            instances.Select(x => new DirectoryOption(x.AddressInstanceTypeId, x.TypeName)).ToList(),
            bases.Select(x => new DirectoryOption(x.AddressBaseTypeId, x.TypeName)).ToList(),
            locations.Select(x => new DirectoryOption(x.BusinessEntityLocationTypeId, x.TypeName ?? "Address")).ToList());
    }
    public Task<List<AddressCardDto>> GetLeaveAddressesAsync(int? applicantJobId = null) => addresses.GetLeaveAddressesAsync(applicantJobId);
    public Task<ServiceResult> CreateAddressAsync(AddressCreateDto application, int? applicantJobId = null) => addresses.CreateAddressAsync(application, applicantJobId);
}
