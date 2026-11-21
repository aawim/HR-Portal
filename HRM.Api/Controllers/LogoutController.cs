using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
namespace HRM.Controllers
{
   
        [Route("Online/Auth")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting(HRM.Api.Infrastructure.HrmRateLimitPolicies.Auth)]
        public class LogoutController : Controller
        {
            // Interactive components obtain the token over HTTP so the browser
            // receives its matching antiforgery cookie before submitting logout.
            [HttpGet("LogoutToken")]
            [AllowAnonymous]
            public IActionResult LogoutToken([FromServices] IAntiforgery antiforgery)
            {
                Response.Headers.CacheControl = "no-store";
                var tokens = antiforgery.GetAndStoreTokens(HttpContext);
                return Json(new { formFieldName = tokens.FormFieldName, requestToken = tokens.RequestToken });
            }

            [HttpPost("Logout")]
            [AllowAnonymous]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Logout([FromServices] HRM.ActivityLogs.Abstractions.IActivityLogService? activityLogs = null)
            {
                if (activityLogs is not null && User?.Identity?.IsAuthenticated == true)
                {
                    await activityLogs.RecordAuthenticationAsync(
                        HRM.ActivityLogs.Models.ActivityTypes.UserSignedOut,
                        "User signed out of Digital HR.",
                        cancellationToken: HttpContext.RequestAborted);
                }

                // The URL the user should be sent to after the Identity Provider successfully logs them out.
                var callbackUrl = Url.Content("~/Online/Auth/login");

                // SignOut requires you to specify BOTH schemes to clear both the local and external sessions.
                return SignOut(
                    new AuthenticationProperties { RedirectUri = callbackUrl },
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    OpenIdConnectDefaults.AuthenticationScheme
                );
            }
        }
     
}
