using System.Net.Http.Json;
using HRM.Application.Contracts.Recruitment;
namespace HRM.UI.Services.Recruitment;
public interface IRecruitmentApiClient {
    Task<IReadOnlyList<RecruitmentRequisitionRow>> GetRequisitionsAsync(CancellationToken ct = default);
    Task<HRM.DTOs.ServiceResult> CreateRequisitionAsync(RecruitmentRequisitionInput input, CancellationToken ct = default);
    Task<HRM.DTOs.ServiceResult> ApproveRequisitionAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<RecruitmentVacancyRow>> GetVacanciesAsync();
    Task<HRM.DTOs.ServiceResult> CreateVacancyAsync(RecruitmentVacancyInput input);
    Task<HRM.DTOs.ServiceResult> PublishVacancyAsync(int id);
    Task<IReadOnlyList<RecruitmentScreeningApplicationRow>> GetScreeningApplicationsAsync(int vacancyId);
    Task<HRM.DTOs.ServiceResult> RecordScreeningAsync(RecordScreeningInput input);
    Task<HRM.DTOs.ServiceResult> DeclareConflictAsync(DeclareRecruitmentConflictInput input);
}
public sealed class RecruitmentApiClient(HttpClient http) : IRecruitmentApiClient
{
    public async Task<IReadOnlyList<RecruitmentScreeningApplicationRow>> GetScreeningApplicationsAsync(int vacancyId)=>await http.GetFromJsonAsync<List<RecruitmentScreeningApplicationRow>>($"/api/v1/recruitment/screening/applications/{vacancyId}")??[];
    public Task<HRM.DTOs.ServiceResult> RecordScreeningAsync(RecordScreeningInput input)=>PostAsync("/api/v1/recruitment/screening",input,default);
    public Task<HRM.DTOs.ServiceResult> DeclareConflictAsync(DeclareRecruitmentConflictInput input)=>PostAsync("/api/v1/recruitment/screening/conflicts",input,default);
    public async Task<IReadOnlyList<RecruitmentVacancyRow>> GetVacanciesAsync()=>await http.GetFromJsonAsync<List<RecruitmentVacancyRow>>("/api/v1/recruitment/vacancies")??[];
    public Task<HRM.DTOs.ServiceResult> CreateVacancyAsync(RecruitmentVacancyInput input)=>PostAsync("/api/v1/recruitment/requisitions/vacancies",new{input},default);
    public Task<HRM.DTOs.ServiceResult> PublishVacancyAsync(int id)=>PostAsync($"/api/v1/recruitment/requisitions/vacancies/{id}/publish",new{},default);
    public Task<HRM.DTOs.ServiceResult> CreateRequisitionAsync(RecruitmentRequisitionInput input, CancellationToken ct = default) => PostAsync("/api/v1/recruitment/requisitions", new { input }, ct);
    public Task<HRM.DTOs.ServiceResult> ApproveRequisitionAsync(int id, CancellationToken ct = default) => PostAsync($"/api/v1/recruitment/requisitions/{id}/approve", new { }, ct);
    private async Task<HRM.DTOs.ServiceResult> PostAsync<T>(string path, T input, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync(path, input, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden) return HRM.DTOs.ServiceResult.Fail("Recruitment.Manage permission is required for this action.");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HRM.DTOs.ServiceResult>(cancellationToken: ct) ?? HRM.DTOs.ServiceResult.Fail("The server returned no action result.");
    }
    public async Task<IReadOnlyList<RecruitmentRequisitionRow>> GetRequisitionsAsync(CancellationToken ct = default)
    {
        using var response = await http.GetAsync("/api/v1/recruitment/requisitions", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Your session has expired. Sign out and sign in again.");
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException("Recruitment access is not assigned for your current organisation. An authorised administrator must assign Recruitment.View or Recruitment.Manage.");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<RecruitmentRequisitionRow>>(cancellationToken: ct) ?? [];
    }
}
