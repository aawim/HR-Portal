using System.Security.Cryptography.X509Certificates;
using HRM.Services.EdgeAttendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Api.Controllers.V1;

[ApiController]
[AllowAnonymous]
[Route("api/v1/edge-attendance/events")]
public sealed class EdgeAttendanceIngestionController(EdgeAttendanceIngestionService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(EdgeAttendanceIngestionResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<EdgeAttendanceIngestionResult>> Ingest([FromBody] EdgeAttendanceBatchInput batch, CancellationToken ct)
    {
        var certificate = await HttpContext.Connection.GetClientCertificateAsync(ct);
        if (certificate is null) return Unauthorized();
        try { return Ok(await service.AcceptAsync(certificate, batch, ct)); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
    }
}
