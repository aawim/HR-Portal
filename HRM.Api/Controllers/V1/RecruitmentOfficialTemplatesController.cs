using HRM.Api.Infrastructure;
using HRM.Application.Contracts.Recruitment;
using HRM.DTOs;
using Microsoft.AspNetCore.Mvc;
namespace HRM.Api.Controllers.V1;
[Route("api/v1/recruitment/official-templates")]
public sealed class RecruitmentOfficialTemplatesController(IRecruitmentOfficialTemplateService service) : HrmApiController
{
    [HttpGet("{policyVersionId:int}")]
    public async Task<ActionResult<IReadOnlyList<RecruitmentOfficialTemplateRow>>> List(int policyVersionId, CancellationToken ct) =>
        Ok(await service.ListAsync(policyVersionId, ct));

    [HttpPost]
    public async Task<ActionResult<ServiceResult>> Register([FromBody] RegisterRecruitmentOfficialTemplateInput input, CancellationToken ct) =>
        Ok(await service.RegisterAsync(input, ct));

    [HttpPost("upload")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<ActionResult<ServiceResult>> Upload(
        [FromForm] int policyVersionId,
        [FromForm] string templateCode,
        [FromForm] string name,
        [FromForm] string? sourceUrl,
        [FromForm] string positionCategory,
        [FromForm] string templateDefinitionJson,
        [FromForm] string? language,
        [FromForm] string? direction,
        [FromForm] bool verifyImmediately,
        [FromForm] IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length <= 0 || file.Length > 15 * 1024 * 1024)
            return Ok(ServiceResult.Fail("Upload a file between 1 byte and 15 MB."));

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var input = new UploadRecruitmentOfficialTemplateInput(
            policyVersionId,
            templateCode,
            name,
            sourceUrl ?? "https://www.csc.gov.mv/",
            positionCategory,
            templateDefinitionJson,
            file.FileName,
            file.ContentType,
            ms.ToArray(),
            language ?? "en",
            direction ?? "ltr",
            verifyImmediately);
        return Ok(await service.UploadAndRegisterAsync(input, ct));
    }

    [HttpPost("{templateId:int}/file")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<ActionResult<ServiceResult>> AttachFile(int templateId, [FromForm] IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length <= 0 || file.Length > 15 * 1024 * 1024)
            return Ok(ServiceResult.Fail("Upload a file between 1 byte and 15 MB."));

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        return Ok(await service.AttachSourceFileAsync(templateId, file.FileName, file.ContentType, ms.ToArray(), ct));
    }

    [HttpPost("{templateId:int}/verify")]
    public async Task<ActionResult<ServiceResult>> Verify(int templateId, CancellationToken ct) =>
        Ok(await service.VerifyAsync(templateId, ct));

    [HttpGet("{templateId:int}/file")]
    public async Task<IActionResult> DownloadFile(int templateId, CancellationToken ct)
    {
        var result = await service.DownloadSourceFileAsync(templateId, ct);
        if (!result.Success) return NotFound(result.Error ?? "Template file not found.");
        return File(result.Content, result.ContentType, result.FileName);
    }
}
