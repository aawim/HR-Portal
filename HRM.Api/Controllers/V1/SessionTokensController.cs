using HRM.Api.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
namespace HRM.Api.Controllers.V1;

[AllowMissingEmployee]
[Route("api/v1/session")]
public sealed class SessionTokensController : HrmApiController
{
    [HttpGet("identity")]
    public ActionResult<HRM.Application.Transport.ApiIdentityDto> Identity() => Ok(new HRM.Application.Transport.ApiIdentityDto(
        User.Identity?.IsAuthenticated == true, User.FindFirst("idnumber")?.Value,
        User.FindFirst("full_name")?.Value ?? User.Identity?.Name));

    [HttpGet("antiforgery")]
    public IActionResult Antiforgery([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { requestToken = tokens.RequestToken, headerName = tokens.HeaderName });
    }
}
