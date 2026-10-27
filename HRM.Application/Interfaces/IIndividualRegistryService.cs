using HRM.DTOs.Registry;
namespace HRM.Services.Interfaces;
public interface IIndividualRegistryService
{
    Task<List<RegistryIndividualDto>> SearchAsync(string searchTerm, string searchType, CancellationToken ct = default);
}
