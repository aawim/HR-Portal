using HRM.DTOs.Profile;

namespace HRM.Services.Interfaces.Profile
{
    public interface IProfileService
    {
        Task<ProfileOverviewDto?> GetProfileAsync(
              int individualId,
              CancellationToken cancellationToken = default);
    }
}
