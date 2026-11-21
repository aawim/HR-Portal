using HRM.Services.EdgeAttendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Api.Controllers.V1;

[ApiController]
[AllowAnonymous]
[Route("api/attendance/connectors")]
public sealed class AttendanceConnectorController(AttendanceConnectorGatewayService gateway) : ControllerBase
{
    [HttpPost("enroll")]
    [ProducesResponseType(typeof(ConnectorEnrollmentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConnectorEnrollmentResponse>> Enroll([FromBody] ConnectorEnrollmentRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await gateway.EnrollAsync(request, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("configuration")]
    [ProducesResponseType(typeof(ConnectorConfigurationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConnectorConfigurationResponse>> GetConfiguration([FromQuery] string agentId, CancellationToken ct)
    {
        var certificate = await HttpContext.Connection.GetClientCertificateAsync(ct);
        if (certificate is null) return Unauthorized();

        try
        {
            return Ok(await gateway.GetConfigurationAsync(certificate, agentId, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("events/batch")]
    [ProducesResponseType(typeof(EdgeAttendanceIngestionResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<EdgeAttendanceIngestionResult>> IngestBatch([FromBody] EdgeAttendanceBatchInput batch, CancellationToken ct)
    {
        var certificate = await HttpContext.Connection.GetClientCertificateAsync(ct);
        if (certificate is null) return Unauthorized();

        try
        {
            return Ok(await gateway.IngestBatchAsync(certificate, batch, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] ConnectorHeartbeatRequest request, CancellationToken ct)
    {
        var certificate = await HttpContext.Connection.GetClientCertificateAsync(ct);
        if (certificate is null) return Unauthorized();

        try
        {
            await gateway.HeartbeatAsync(certificate, request, ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("commands")]
    [ProducesResponseType(typeof(IReadOnlyList<ConnectorCommandPayloadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConnectorCommandPayloadDto>>> GetCommands([FromQuery] string agentId, CancellationToken ct)
    {
        var certificate = await HttpContext.Connection.GetClientCertificateAsync(ct);
        if (certificate is null) return Unauthorized();

        try
        {
            return Ok(await gateway.GetPendingCommandsAsync(certificate, agentId, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("commands/{id:long}/result")]
    public async Task<IActionResult> ReportCommandResult([FromRoute] long id, [FromBody] ConnectorCommandResultRequest request, CancellationToken ct)
    {
        var certificate = await HttpContext.Connection.GetClientCertificateAsync(ct);
        if (certificate is null) return Unauthorized();

        try
        {
            await gateway.ReportCommandResultAsync(certificate, id, request, ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("credentials/renew")]
    [ProducesResponseType(typeof(ConnectorCredentialRenewalResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConnectorCredentialRenewalResponse>> RenewCredentials([FromBody] ConnectorCredentialRenewalRequest request, CancellationToken ct)
    {
        var certificate = await HttpContext.Connection.GetClientCertificateAsync(ct);
        if (certificate is null) return Unauthorized();

        try
        {
            return Ok(await gateway.RenewCredentialsAsync(certificate, request, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
