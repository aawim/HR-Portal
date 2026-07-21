using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
namespace HRM.Controllers
{
   
        [Route("Online/Auth")]
        public class LogoutController : Controller
        {
            [HttpPost("Logout")]
            public IActionResult Logout()
            {
                // The URL the user should be sent to after the Identity Provider successfully logs them out.
                var callbackUrl = Url.Content("~/");

                // SignOut requires you to specify BOTH schemes to clear both the local and external sessions.
                return SignOut(
                    new AuthenticationProperties { RedirectUri = callbackUrl },
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    OpenIdConnectDefaults.AuthenticationScheme
                );
            }
        }
     
}
