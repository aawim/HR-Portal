// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/user-preference")]
public sealed class UserPreferenceController(global::HRM.Services.Contracts.UserPreferenceServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("get-theme")]
    public async Task<ActionResult<global::HRM.Services.UserThemePreference>> GetTheme()
    {
        return Ok(await service.GetThemeAsync());
    }

    [HttpPost("save-theme")]
    public async Task<IActionResult> SaveTheme([FromBody] UserPreferenceSaveThemeRequest request)
    {
        await service.SaveThemeAsync(request.theme, request.expectedUserId);
        return NoContent();
    }

    public sealed class UserPreferenceSaveThemeRequest
    {
        public global::System.String theme { get; set; } = default!;
        public global::System.Int32 expectedUserId { get; set; }
    }

    [HttpPost("save-font-size")]
    public async Task<IActionResult> SaveFontSize([FromBody] UserPreferenceSaveFontSizeRequest request)
    {
        await service.SaveFontSizeAsync(request.percent, request.expectedUserId);
        return NoContent();
    }

    public sealed class UserPreferenceSaveFontSizeRequest
    {
        public global::System.Int32 percent { get; set; }
        public global::System.Int32 expectedUserId { get; set; }
    }

}
