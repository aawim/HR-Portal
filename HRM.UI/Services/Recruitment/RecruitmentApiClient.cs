using System.Net.Http.Json;
using HRM.Application.Contracts.Recruitment;
namespace HRM.UI.Services.Recruitment;
public interface IRecruitmentApiClient {
    Task<IReadOnlyList<RecruitmentRequisitionRow>> GetRequisitionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AvailablePositionMatrixOption>> GetAvailablePositionsAsync(CancellationToken ct = default);
    Task<VacancyDraftContextDto> GetVacancyDraftContextAsync(int requisitionId, CancellationToken ct = default);
    Task<HRM.DTOs.ServiceResult> CreateRequisitionAsync(RecruitmentRequisitionInput input, CancellationToken ct = default);
    Task<HRM.DTOs.ServiceResult> ApproveRequisitionAsync(int id, CancellationToken ct = default);
    Task<RecruitmentPolicyWorkspaceDto> GetPoliciesAsync(CancellationToken ct = default);
    Task<HRM.DTOs.ServiceResult> CreatePolicyAsync(CreateRecruitmentPolicyInput input, CancellationToken ct = default);
    Task<HRM.DTOs.ServiceResult> VerifyApprovePolicyAsync(int versionId, CancellationToken ct = default);
    Task<HRM.DTOs.ServiceResult> AssignPolicyAsync(AssignRecruitmentPolicyInput input, CancellationToken ct = default);
    Task<HRM.DTOs.ServiceResult> TogglePolicyAssignmentAsync(int assignmentId, CancellationToken ct = default);
    Task<IReadOnlyList<RecruitmentVacancyRow>> GetVacanciesAsync();
    Task<HRM.DTOs.ServiceResult> CreateVacancyAsync(RecruitmentVacancyInput input);
    Task<HRM.DTOs.ServiceResult> PublishVacancyAsync(int id);
    Task<IReadOnlyList<RecruitmentScreeningApplicationRow>> GetScreeningApplicationsAsync(int vacancyId);
    Task<ScreeningResultSheetDto?> GetScreeningResultSheetAsync(int vacancyId, CancellationToken ct = default);
    Task<int> GetPendingScreeningCountAsync(CancellationToken ct = default);
    Task<HrRecruitmentApplicationDetail?> GetHrApplicationDetailAsync(int applicationId);
    Task<(bool Success, string FileName, string ContentType, byte[] Content, string? Error)> DownloadScreeningDocumentAsync(int documentId);
    Task<HRM.DTOs.ServiceResult> ReturnApplicationForCorrectionAsync(ReturnRecruitmentApplicationInput input);
    Task<IReadOnlyList<RecruitmentConflictDeclarationRow>> GetConflictDeclarationsAsync(int vacancyId);
    Task<HRM.DTOs.ServiceResult> RecordScreeningAsync(RecordScreeningInput input);
    Task<HRM.DTOs.ServiceResult> DeclareConflictAsync(DeclareRecruitmentConflictInput input);
    Task<RecruitmentAssessmentWorkspace> GetAssessmentWorkspaceAsync(int vacancyId);
    Task<HRM.DTOs.ServiceResult> CreateAssessmentAsync(CreateRecruitmentAssessmentInput input);
    Task<HRM.DTOs.ServiceResult> SubmitScorecardAsync(SubmitPanelScorecardInput input);
    Task<RecruitmentResultWorkspace> GetResultWorkspaceAsync(int vacancyId);
    Task<HRM.DTOs.ServiceResult> PublishResultsAsync(PublishRecruitmentResultsInput input);
    Task<HRM.DTOs.ServiceResult> ResolveObjectionAsync(ResolveRecruitmentObjectionInput input);
    Task<HRM.DTOs.ServiceResult> FinalizeResultsAsync(int id);
    Task<RecruitmentOfferWorkspace> GetOfferWorkspaceAsync(int vacancyId);
    Task<HRM.DTOs.ServiceResult> CreateOfferAsync(CreateRecruitmentOfferInput input);
    Task<HRM.DTOs.ServiceResult> SubmitOnboardingAsync(SubmitRecruitmentOnboardingInput input);
    Task<MyRecruitmentWorkspace> GetMyWorkspaceAsync();
    Task<MyPublishedResultDetail?> GetMyPublishedResultAsync(int publicationId, int applicationId);
    Task<HRM.DTOs.ServiceResult> AcceptMyOfferAsync(int id);
    Task<HRM.DTOs.ServiceResult> DeclineMyOfferAsync(int id, string? reason = null);
    Task<HRM.DTOs.ServiceResult> SubmitMyObjectionAsync(SubmitRecruitmentObjectionInput input);
    Task<IReadOnlyList<PublishedRecruitmentVacancyRow>> BrowseVacanciesAsync();
    Task<IReadOnlyList<PublishedRecruitmentVacancyRow>> BrowsePublicVacanciesAsync();
    Task<OfficeVacanciesResponse?> GetOfficeVacanciesAsync(string officeSlug);
    Task<PublicMeritSheetResponse?> GetPublicMeritSheetAsync(int publicationId);
    Task<ScreeningResultSheetDto?> GetPublicScreeningResultSheetAsync(int vacancyId, CancellationToken ct = default);
    Task<HRM.DTOs.ServiceResult> RegisterApplicantAsync(ApplicantRegistrationInput input);
    Task<HRM.DTOs.ServiceResult> ApplyAsync(ApplyToRecruitmentVacancyInput input);
    Task<RecruitmentDraftDetail?> GetDraftAsync(int vacancy);
    Task<HRM.DTOs.ServiceResult> SaveDraftAsync(SaveRecruitmentDraftInput input);
    Task<HRM.DTOs.ServiceResult> SubmitDraftAsync(int id,RecruitmentLifecycleActionInput input);
    Task<HRM.DTOs.ServiceResult> WithdrawAsync(int id,RecruitmentLifecycleActionInput input);
    Task<HRM.DTOs.ServiceResult> UploadAsync(int id,string category,string fileName,byte[] content);
    Task<IReadOnlyList<RecruitmentOfficialTemplateRow>> GetOfficialTemplatesAsync(int policyVersionId, CancellationToken ct = default);
    Task<HRM.DTOs.ServiceResult> RegisterOfficialTemplateAsync(RegisterRecruitmentOfficialTemplateInput input, CancellationToken ct = default);
    Task<HRM.DTOs.ServiceResult> UploadOfficialTemplateAsync(UploadRecruitmentOfficialTemplateInput input, CancellationToken ct = default);
    Task<HRM.DTOs.ServiceResult> AttachOfficialTemplateFileAsync(int templateId, string fileName, string? contentType, byte[] content, CancellationToken ct = default);
    Task<HRM.DTOs.ServiceResult> VerifyOfficialTemplateAsync(int templateId, CancellationToken ct = default);
    Task<RecruitmentOfficialTemplateFileResult> DownloadOfficialTemplateFileAsync(int templateId, CancellationToken ct = default);
}
public sealed class RecruitmentApiClient(HttpClient http, HttpClient? publicHttp = null) : IRecruitmentApiClient
{
    private HttpClient PublicClient => publicHttp ?? http;
    public async Task<IReadOnlyList<PublishedRecruitmentVacancyRow>> BrowsePublicVacanciesAsync() => await PublicClient.GetFromJsonAsync<List<PublishedRecruitmentVacancyRow>>("/api/v1/public/recruitment/vacancies") ?? [];
    public async Task<OfficeVacanciesResponse?> GetOfficeVacanciesAsync(string officeSlug)
    {
        using var r = await PublicClient.GetAsync($"/api/v1/public/recruitment/office/{Uri.EscapeDataString(officeSlug)}/vacancies");
        if (r.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<OfficeVacanciesResponse>();
    }
    public async Task<PublicMeritSheetResponse?> GetPublicMeritSheetAsync(int publicationId)
    {
        using var r = await PublicClient.GetAsync($"/api/v1/public/recruitment/results/{publicationId}");
        if (r.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<PublicMeritSheetResponse>();
    }
    public async Task<ScreeningResultSheetDto?> GetPublicScreeningResultSheetAsync(int vacancyId, CancellationToken ct = default)
    {
        using var r = await PublicClient.GetAsync($"/api/v1/public/recruitment/screening-sheet/{vacancyId}", ct);
        if (r.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ScreeningResultSheetDto>(cancellationToken: ct);
    }
    public async Task<IReadOnlyList<RecruitmentOfficialTemplateRow>> GetOfficialTemplatesAsync(int policyVersionId, CancellationToken ct = default) => await http.GetFromJsonAsync<List<RecruitmentOfficialTemplateRow>>($"/api/v1/recruitment/official-templates/{policyVersionId}", ct) ?? [];
    public Task<HRM.DTOs.ServiceResult> RegisterOfficialTemplateAsync(RegisterRecruitmentOfficialTemplateInput input, CancellationToken ct = default) => PostAsync("/api/v1/recruitment/official-templates", input, ct);
    public async Task<HRM.DTOs.ServiceResult> UploadOfficialTemplateAsync(UploadRecruitmentOfficialTemplateInput input, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(input.RecruitmentPolicyVersionId.ToString()), "policyVersionId");
        form.Add(new StringContent(input.TemplateCode), "templateCode");
        form.Add(new StringContent(input.Name), "name");
        form.Add(new StringContent(input.SourceUrl ?? ""), "sourceUrl");
        form.Add(new StringContent(input.PositionCategory), "positionCategory");
        form.Add(new StringContent(input.TemplateDefinitionJson), "templateDefinitionJson");
        form.Add(new StringContent(input.Language ?? "en"), "language");
        form.Add(new StringContent(input.Direction ?? "ltr"), "direction");
        form.Add(new StringContent(input.VerifyImmediately ? "true" : "false"), "verifyImmediately");
        var fileContent = new ByteArrayContent(input.FileContent);
        if (!string.IsNullOrWhiteSpace(input.ContentType))
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(input.ContentType);
        form.Add(fileContent, "file", input.FileName);
        using var r = await http.PostAsync("/api/v1/recruitment/official-templates/upload", form, ct);
        if (r.StatusCode == System.Net.HttpStatusCode.Forbidden) return HRM.DTOs.ServiceResult.Fail("Recruitment.Manage permission is required.");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<HRM.DTOs.ServiceResult>(cancellationToken: ct) ?? HRM.DTOs.ServiceResult.Fail("No result returned.");
    }
    public async Task<HRM.DTOs.ServiceResult> AttachOfficialTemplateFileAsync(int templateId, string fileName, string? contentType, byte[] content, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);
        if (!string.IsNullOrWhiteSpace(contentType))
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", fileName);
        using var r = await http.PostAsync($"/api/v1/recruitment/official-templates/{templateId}/file", form, ct);
        if (r.StatusCode == System.Net.HttpStatusCode.Forbidden) return HRM.DTOs.ServiceResult.Fail("Recruitment.Manage permission is required.");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<HRM.DTOs.ServiceResult>(cancellationToken: ct) ?? HRM.DTOs.ServiceResult.Fail("No result returned.");
    }
    public Task<HRM.DTOs.ServiceResult> VerifyOfficialTemplateAsync(int templateId, CancellationToken ct = default) => PostAsync($"/api/v1/recruitment/official-templates/{templateId}/verify", new { }, ct);
    public async Task<RecruitmentOfficialTemplateFileResult> DownloadOfficialTemplateFileAsync(int templateId, CancellationToken ct = default)
    {
        using var r = await http.GetAsync($"/api/v1/recruitment/official-templates/{templateId}/file", ct);
        if (!r.IsSuccessStatusCode)
        {
            var err = await r.Content.ReadAsStringAsync(ct);
            return new RecruitmentOfficialTemplateFileResult(false, string.Empty, string.Empty, [], string.IsNullOrWhiteSpace(err) ? $"Download failed ({(int)r.StatusCode})." : err);
        }
        var bytes = await r.Content.ReadAsByteArrayAsync(ct);
        var contentType = r.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName = r.Content.Headers.ContentDisposition?.FileNameStar ?? r.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? $"template-{templateId}";
        return new RecruitmentOfficialTemplateFileResult(true, fileName, contentType, bytes);
    }
    public async Task<RecruitmentDraftDetail?> GetDraftAsync(int vacancy){using var r=await http.GetAsync($"/api/v1/recruitment/my-applications/vacancy/{vacancy}");if(r.StatusCode==System.Net.HttpStatusCode.NoContent)return null;r.EnsureSuccessStatusCode();return await r.Content.ReadFromJsonAsync<RecruitmentDraftDetail>();}
    public Task<HRM.DTOs.ServiceResult> SaveDraftAsync(SaveRecruitmentDraftInput input)=>PostAsync("/api/v1/recruitment/my-applications/draft",input,default);
    public Task<HRM.DTOs.ServiceResult> SubmitDraftAsync(int id,RecruitmentLifecycleActionInput input)=>PostAsync($"/api/v1/recruitment/my-applications/{id}/submit",input,default);
    public Task<HRM.DTOs.ServiceResult> WithdrawAsync(int id,RecruitmentLifecycleActionInput input)=>PostAsync($"/api/v1/recruitment/my-applications/{id}/withdraw",input,default);
    public async Task<HRM.DTOs.ServiceResult> UploadAsync(int id,string category,string fileName,byte[] content){using var form=new MultipartFormDataContent();form.Add(new StringContent(category),"category");form.Add(new ByteArrayContent(content),"file",fileName);using var r=await http.PostAsync($"/api/v1/recruitment/my-applications/{id}/documents",form);r.EnsureSuccessStatusCode();return await r.Content.ReadFromJsonAsync<HRM.DTOs.ServiceResult>()??HRM.DTOs.ServiceResult.Fail("No upload result returned.");}
    public async Task<IReadOnlyList<PublishedRecruitmentVacancyRow>> BrowseVacanciesAsync()=>await http.GetFromJsonAsync<List<PublishedRecruitmentVacancyRow>>("/api/v1/recruitment/applications/vacancies")??[];
    public Task<HRM.DTOs.ServiceResult> RegisterApplicantAsync(ApplicantRegistrationInput input)=>PostAsync("/api/v1/recruitment/applications/applicants",input,default);
    public Task<HRM.DTOs.ServiceResult> ApplyAsync(ApplyToRecruitmentVacancyInput input)=>PostAsync("/api/v1/recruitment/my-workspace/apply",input,default);
    public async Task<MyRecruitmentWorkspace> GetMyWorkspaceAsync()=>await http.GetFromJsonAsync<MyRecruitmentWorkspace>("/api/v1/recruitment/my-workspace")??new([],[]);
    public async Task<MyPublishedResultDetail?> GetMyPublishedResultAsync(int publicationId,int applicationId){using var r=await http.GetAsync($"/api/v1/recruitment/my-workspace/results/{publicationId}/{applicationId}");if(r.StatusCode==System.Net.HttpStatusCode.NotFound)return null;r.EnsureSuccessStatusCode();return await r.Content.ReadFromJsonAsync<MyPublishedResultDetail>();}
    public Task<HRM.DTOs.ServiceResult> AcceptMyOfferAsync(int id)=>PostAsync($"/api/v1/recruitment/my-workspace/offers/{id}/accept",new{},default);
    public Task<HRM.DTOs.ServiceResult> DeclineMyOfferAsync(int id, string? reason = null)=>PostAsync($"/api/v1/recruitment/my-workspace/offers/{id}/decline",new DeclineRecruitmentOfferInput(reason),default);
    public Task<HRM.DTOs.ServiceResult> SubmitMyObjectionAsync(SubmitRecruitmentObjectionInput input)=>PostAsync("/api/v1/recruitment/my-workspace/objections",input,default);
    public async Task<RecruitmentOfferWorkspace> GetOfferWorkspaceAsync(int vacancyId)=>await http.GetFromJsonAsync<RecruitmentOfferWorkspace>($"/api/v1/recruitment/offer-workspace/{vacancyId}")??new([],[]);
    public Task<HRM.DTOs.ServiceResult> CreateOfferAsync(CreateRecruitmentOfferInput input)=>PostAsync("/api/v1/recruitment/offers",input,default);
    public Task<HRM.DTOs.ServiceResult> SubmitOnboardingAsync(SubmitRecruitmentOnboardingInput input)=>PostAsync("/api/v1/recruitment/onboarding",input,default);
    public async Task<RecruitmentResultWorkspace> GetResultWorkspaceAsync(int vacancyId)=>await http.GetFromJsonAsync<RecruitmentResultWorkspace>($"/api/v1/recruitment/result-workspace/{vacancyId}")??new(null,false,null,[],[]);
    public Task<HRM.DTOs.ServiceResult> PublishResultsAsync(PublishRecruitmentResultsInput input)=>PostAsync("/api/v1/recruitment/results/publish",input,default);
    public Task<HRM.DTOs.ServiceResult> ResolveObjectionAsync(ResolveRecruitmentObjectionInput input)=>PostAsync("/api/v1/recruitment/results/objections/resolve",input,default);
    public Task<HRM.DTOs.ServiceResult> FinalizeResultsAsync(int id)=>PostAsync($"/api/v1/recruitment/results/{id}/finalize",new{},default);
    public async Task<RecruitmentAssessmentWorkspace> GetAssessmentWorkspaceAsync(int vacancyId)=>await http.GetFromJsonAsync<RecruitmentAssessmentWorkspace>($"/api/v1/recruitment/assessment-workspace/{vacancyId}")??new([],[]);
    public Task<HRM.DTOs.ServiceResult> CreateAssessmentAsync(CreateRecruitmentAssessmentInput input)=>PostAsync("/api/v1/recruitment/assessments",input,default);
    public Task<HRM.DTOs.ServiceResult> SubmitScorecardAsync(SubmitPanelScorecardInput input)=>PostAsync("/api/v1/recruitment/assessments/scorecards",input,default);
    public async Task<IReadOnlyList<RecruitmentScreeningApplicationRow>> GetScreeningApplicationsAsync(int vacancyId)=>await http.GetFromJsonAsync<List<RecruitmentScreeningApplicationRow>>($"/api/v1/recruitment/screening/applications/{vacancyId}")??[];
    public async Task<ScreeningResultSheetDto?> GetScreeningResultSheetAsync(int vacancyId, CancellationToken ct = default)
    {
        using var r = await http.GetAsync($"/api/v1/recruitment/screening/result-sheet/{vacancyId}", ct);
        if (r.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ScreeningResultSheetDto>(cancellationToken: ct);
    }
    public async Task<int> GetPendingScreeningCountAsync(CancellationToken ct = default)
    {
        using var r = await http.GetAsync("/api/v1/recruitment/screening/pending-count", ct);
        if (!r.IsSuccessStatusCode) return 0;
        return await r.Content.ReadFromJsonAsync<int>(cancellationToken: ct);
    }
    public async Task<HrRecruitmentApplicationDetail?> GetHrApplicationDetailAsync(int applicationId)
    {
        using var r = await http.GetAsync($"/api/v1/recruitment/screening/applications/{applicationId}/detail");
        if (r.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<HrRecruitmentApplicationDetail>();
    }
    public async Task<(bool Success, string FileName, string ContentType, byte[] Content, string? Error)> DownloadScreeningDocumentAsync(int documentId)
    {
        using var r = await http.GetAsync($"/api/v1/recruitment/screening/applications/documents/{documentId}");
        if (!r.IsSuccessStatusCode)
        {
            var err = await r.Content.ReadAsStringAsync();
            return (false, string.Empty, string.Empty, [], string.IsNullOrWhiteSpace(err) ? $"Download failed ({(int)r.StatusCode})." : err);
        }
        var bytes = await r.Content.ReadAsByteArrayAsync();
        var contentType = r.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName = r.Content.Headers.ContentDisposition?.FileNameStar ?? r.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? $"document-{documentId}";
        return (true, fileName, contentType, bytes, null);
    }
    public Task<HRM.DTOs.ServiceResult> ReturnApplicationForCorrectionAsync(ReturnRecruitmentApplicationInput input) => PostAsync($"/api/v1/recruitment/screening/applications/{input.ApplicationId}/return", input, default);
    public async Task<IReadOnlyList<RecruitmentConflictDeclarationRow>> GetConflictDeclarationsAsync(int vacancyId)=>await http.GetFromJsonAsync<List<RecruitmentConflictDeclarationRow>>($"/api/v1/recruitment/screening/conflicts/{vacancyId}")??[];
    public Task<HRM.DTOs.ServiceResult> RecordScreeningAsync(RecordScreeningInput input)=>PostAsync("/api/v1/recruitment/screening",input,default);
    public Task<HRM.DTOs.ServiceResult> DeclareConflictAsync(DeclareRecruitmentConflictInput input)=>PostAsync("/api/v1/recruitment/screening/conflicts",input,default);
    public async Task<IReadOnlyList<RecruitmentVacancyRow>> GetVacanciesAsync()=>await http.GetFromJsonAsync<List<RecruitmentVacancyRow>>("/api/v1/recruitment/vacancies")??[];
    public Task<HRM.DTOs.ServiceResult> CreateVacancyAsync(RecruitmentVacancyInput input)=>PostAsync("/api/v1/recruitment/requisitions/vacancies",new{input},default);
    public Task<HRM.DTOs.ServiceResult> PublishVacancyAsync(int id)=>PostAsync($"/api/v1/recruitment/requisitions/vacancies/{id}/publish",new{},default);
    public Task<HRM.DTOs.ServiceResult> CreateRequisitionAsync(RecruitmentRequisitionInput input, CancellationToken ct = default) => PostAsync("/api/v1/recruitment/requisitions", new { input }, ct);
    public async Task<IReadOnlyList<AvailablePositionMatrixOption>> GetAvailablePositionsAsync(CancellationToken ct = default) => await http.GetFromJsonAsync<List<AvailablePositionMatrixOption>>("/api/v1/recruitment/requisitions/available-positions", ct) ?? [];
    public async Task<VacancyDraftContextDto> GetVacancyDraftContextAsync(int requisitionId, CancellationToken ct = default) => await http.GetFromJsonAsync<VacancyDraftContextDto>($"/api/v1/recruitment/requisitions/{requisitionId}/vacancy-draft-context", ct) ?? throw new InvalidOperationException("Job matrix details could not be loaded for this requisition.");
    public Task<HRM.DTOs.ServiceResult> ApproveRequisitionAsync(int id, CancellationToken ct = default) => PostAsync($"/api/v1/recruitment/requisitions/{id}/approve", new { }, ct);
    public async Task<RecruitmentPolicyWorkspaceDto> GetPoliciesAsync(CancellationToken ct = default) => await http.GetFromJsonAsync<RecruitmentPolicyWorkspaceDto>("/api/v1/recruitment/requisitions/policies", ct) ?? new([], []);
    public Task<HRM.DTOs.ServiceResult> CreatePolicyAsync(CreateRecruitmentPolicyInput input, CancellationToken ct = default) => PostAsync("/api/v1/recruitment/requisitions/policies", input, ct);
    public Task<HRM.DTOs.ServiceResult> VerifyApprovePolicyAsync(int versionId, CancellationToken ct = default) => PostAsync($"/api/v1/recruitment/requisitions/policies/{versionId}/verify", new { }, ct);
    public Task<HRM.DTOs.ServiceResult> AssignPolicyAsync(AssignRecruitmentPolicyInput input, CancellationToken ct = default) => PostAsync("/api/v1/recruitment/requisitions/policies/assign", input, ct);
    public Task<HRM.DTOs.ServiceResult> TogglePolicyAssignmentAsync(int assignmentId, CancellationToken ct = default) => PostAsync($"/api/v1/recruitment/requisitions/policies/assignments/{assignmentId}/toggle", new { }, ct);
    private async Task<HRM.DTOs.ServiceResult> PostAsync<T>(string path, T input, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync(path, input, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden) return HRM.DTOs.ServiceResult.Fail("Recruitment.Manage permission is required for this action.");
        if (response.StatusCode is System.Net.HttpStatusCode.Conflict or System.Net.HttpStatusCode.BadRequest)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var detail = TryExtractErrorMessage(body);
            if (!string.IsNullOrWhiteSpace(detail))
                return HRM.DTOs.ServiceResult.Fail(detail);
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HRM.DTOs.ServiceResult>(cancellationToken: ct) ?? HRM.DTOs.ServiceResult.Fail("The server returned no action result.");
    }

    private static string? TryExtractErrorMessage(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (root.TryGetProperty("detail", out var d) && d.ValueKind == System.Text.Json.JsonValueKind.String && !string.IsNullOrWhiteSpace(d.GetString()))
                    return d.GetString();
                if (root.TryGetProperty("message", out var m) && m.ValueKind == System.Text.Json.JsonValueKind.String && !string.IsNullOrWhiteSpace(m.GetString()))
                    return m.GetString();
                if (root.TryGetProperty("title", out var t) && t.ValueKind == System.Text.Json.JsonValueKind.String && !string.IsNullOrWhiteSpace(t.GetString()))
                    return t.GetString();
            }
        }
        catch { }
        return null;
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
