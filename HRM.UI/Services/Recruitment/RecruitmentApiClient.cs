using System.Net.Http.Json;
using HRM.Application.Contracts.Recruitment;
namespace HRM.UI.Services.Recruitment;
public interface IRecruitmentApiClient { Task<IReadOnlyList<RecruitmentRequisitionRow>> GetRequisitionsAsync(CancellationToken ct = default); }
public sealed class RecruitmentApiClient(HttpClient http) : IRecruitmentApiClient { public async Task<IReadOnlyList<RecruitmentRequisitionRow>> GetRequisitionsAsync(CancellationToken ct = default) => await http.GetFromJsonAsync<List<RecruitmentRequisitionRow>>("/api/v1/recruitment/requisitions", ct) ?? []; }
