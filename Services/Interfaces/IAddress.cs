using HRM.DTOs;
using HRM.Models;
using HRM.Models.Archives;
namespace HRM.Services.Interfaces
{
    public interface IAddress
    {
        Task<List<AddressCardDto>> GetLeaveAddressesAsync();
        Task<List<Country>> GetCountriesAsync();

        Task<List<Atoll>> GetAtollsAsync();

        Task<List<Island>> GetIslandsAsync(int atollId);

        Task<List<AddressInstanceType>> GetAddressInstanceTypesAsync();
        Task<List<AddressBaseType>> GetAddressBaseTypesAsync();

        Task<List<BusinessEntityLocationType>> GetBusinessEntityLocationTypesAsync();

        Task<ServiceResult> CreateAddressAsync(AddressCreateDto model);
    }
}
