using HRM.Application.Transport;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace HRM.Api.Authentication;

public static class EfaasAuthentication
{
    public static IServiceCollection AddEfaasAuthentication(this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        var production = !environment.IsDevelopment();
        var origin = DeploymentOrigin.Require(config["Application:PublicOrigin"], "Application:PublicOrigin", production);
        var authority = config["Authentication:OpenIdConnect:Authority"];
        var client = config["Authentication:OpenIdConnect:ClientId"];
        var secret = config["Authentication:OpenIdConnect:ClientSecret"];
        if (production && CredentialExposure.IsExposed(secret))
            throw new InvalidOperationException("Rotate the eFaas client secret exposed in Git history before deploying to production.");
        if (!Uri.TryCreate(authority, UriKind.Absolute, out var issuer) || issuer.Scheme != "https" || !string.IsNullOrEmpty(issuer.UserInfo) ||
            !string.IsNullOrEmpty(issuer.Query) || !string.IsNullOrEmpty(issuer.Fragment) || production && issuer.IsLoopback || string.IsNullOrWhiteSpace(client) ||
            production && string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("Configure the HTTPS eFaas authority, registered client ID and production client secret.");
        services.AddDistributedMemoryCache(); // Replace IDistributedCache with shared storage before scaling the API to multiple instances.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ProtectedTicketStore>();
        services.AddAuthentication(options => { options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme; })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = AuthenticationSessionDefaults.CookieName;
                options.Cookie.Path = "/"; options.Cookie.HttpOnly = true; options.Cookie.SecurePolicy = CookieSecurePolicy.Always; options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromHours(8); options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context => { if (context.Request.Path.StartsWithSegments("/api")) context.Response.StatusCode = 401; else context.Response.Redirect("/Online/Auth/login"); return Task.CompletedTask; };
                options.Events.OnRedirectToAccessDenied = context => { if (context.Request.Path.StartsWithSegments("/api")) context.Response.StatusCode = 403; else context.Response.StatusCode = 403; return Task.CompletedTask; };
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = authority; options.ClientId = client; options.ClientSecret = secret;
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.CallbackPath = "/Online/Auth/Callback"; options.SignedOutRedirectUri = new Uri(origin, "Online/Auth/login").AbsoluteUri;
                options.ResponseType = "code"; options.UsePkce = true; options.RequireHttpsMetadata = true;
                options.SaveTokens = true; // Stored only in the protected server-side ticket store.
                options.GetClaimsFromUserInfoEndpoint = true;
                options.TokenValidationParameters = new TokenValidationParameters { NameClaimType = "full_name", ValidAudience = client, ValidateIssuer = true,
                    ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true, RequireSignedTokens = true };
                options.Scope.Clear(); foreach (var scope in new[] { "openid", "efaas.profile", "efaas.email", "efaas.mobile", "efaas.birthdate" }) options.Scope.Add(scope);
                foreach (var claim in new[] { "idnumber", "full_name", "first_name", "last_name", "email", "mobile", "dob", "photo", "user_type", "sub", "middle_name", "given_name", "family_name" }) options.ClaimActions.MapUniqueJsonKey(claim, claim);
                // Use the registered public origin, never a caller's Host/forwarded header, for protocol callbacks.
                options.Events.OnRedirectToIdentityProvider = context => { context.ProtocolMessage.RedirectUri = new Uri(origin, "Online/Auth/Callback").AbsoluteUri; return Task.CompletedTask; };
                options.Events.OnRedirectToIdentityProviderForSignOut = context => { context.ProtocolMessage.PostLogoutRedirectUri = new Uri(origin, "signout-callback-oidc").AbsoluteUri; return Task.CompletedTask; };
            });
        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ProtectedTicketStore>((options, store) => options.SessionStore = store);
        services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build());
        return services;
    }
}
