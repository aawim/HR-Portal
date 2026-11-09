using HRM.DTOs.Registry;
namespace HRM.Services.Interfaces;
public interface IIndividualRegistryService
{
    Task<List<RegistryIndividualDto>> SearchAsync(string searchTerm, string searchType, CancellationToken ct = default);
    Task<string?> GetPhotoAsync(int individualId, CancellationToken ct = default);
    Task UpdatePhotoAsync(int individualId, byte[]? imageData, CancellationToken ct = default);
}
