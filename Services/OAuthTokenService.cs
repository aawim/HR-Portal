using System.Text.Json;
using HRM.Models.Archives;

namespace HRM.Services
{
    public class OAuthTokenService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OAuthTokenService> _logger;

        // Dependency Injection brings in the HttpClient, Config, and Logger automatically
        public OAuthTokenService(HttpClient httpClient, IConfiguration configuration, ILogger<OAuthTokenService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<TokenResponse> GetTokenAsync(string code)
        {
            // 1. Pull settings from appsettings.json
            string tokenEndpoint = _configuration["OAuthProvider:TokenEndpoint"];
            string clientId = _configuration["OAuthProvider:ClientId"];
            string clientSecret = _configuration["OAuthProvider:ClientSecret"];

            // This MUST match the exact callback URL you registered (e.g., https://localhost:xxxx/Online/Auth/Callback)
            string redirectUri = _configuration["OAuthProvider:RedirectUri"];

            // 2. Prepare the payload exactly how the provider expects it
            var postData = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUri },
                { "code", code }
            };

            var content = new FormUrlEncodedContent(postData);

            try
            {
                // 3. Make the POST request asynchronously
                var response = await _httpClient.PostAsync(tokenEndpoint, content);

                string responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Token exchange failed. Status Code: {StatusCode}. Response: {Response}",
                        response.StatusCode, responseString);

                    return null; // The controller handles the null and redirects to a failure page
                }

                // 4. Parse the JSON string into our TokenResponse C# object
                var tokenObj = JsonSerializer.Deserialize<TokenResponse>(responseString);

                return tokenObj;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "A fatal network error occurred while calling the Token Endpoint.");
                return null;
            }
        }
    }
}
