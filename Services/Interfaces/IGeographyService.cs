using HRM.Models;
using HRM.Models.Archives;
namespace HRM.Services.Interfaces
{
    public interface IGeographyService
    {
        Task<List<Country>> GetCountriesAsync();

        Task<List<Atoll>> GetAtollsAsync();

        Task<List<Island>> GetIslandsAsync(int atollId);
    }
}
