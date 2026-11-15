using HRM.Services.EdgeAttendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Api.Controllers.V1;

[ApiController]
[AllowAnonymous]
[Route("api/v1/edge-attendance/heartbeat")]
public sealed class EdgeAttendanceHeartbeatController(EdgeAttendanceIngestionService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Heartbeat([FromBody] EdgeAttendanceHeartbeatInput heartbeat, CancellationToken ct)
    {
        var certificate = await HttpContext.Connection.GetClientCertificateAsync(ct);
        if (certificate is null) return Unauthorized();
        try { await service.HeartbeatAsync(certificate, heartbeat, ct); return NoContent(); }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
    }
}
