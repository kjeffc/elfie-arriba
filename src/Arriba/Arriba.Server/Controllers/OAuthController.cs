using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Arriba.Configuration;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Arriba.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OAuthController : ControllerBase
    {
        private readonly IOAuthConfig _config;
        private readonly HttpClient _http;
        private readonly IArribaServerConfiguration _serverConfig;

        public OAuthController(IOAuthConfig config, IArribaServerConfiguration serverConfig)
        {
            _config = config;
            _serverConfig = serverConfig;
            _http = new HttpClient();
        }

        [HttpGet]
        public async Task<IActionResult> SignInAsync()
        {
            var uri = await GetAuthorizeUrlAsync(_config);
            return this.Redirect(uri.AbsoluteUri);
        }

        [HttpGet]
        [Route("auth-code")]
        public async Task<IActionResult> AuthCodeAsync([FromQuery]string code, [FromQuery] string state)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "client_id", _config.AudienceId },
                { "client_secret", _config.AppSecret },
                { "scope", GetScopes(_config.Scopes) },
                { "code", code },
                { "redirect_uri", _config.RedirectUrl },
            });

            var tokenResult = await ReadTokenResultAsync(await GetTokenUrlAsync(_config), content);
            return Redirect($"{_serverConfig.FrontendBaseUrl}/#access_token={tokenResult.AccessToken}");
        }

        private async Task<Uri> GetAuthorizeUrlAsync(IOAuthConfig config)
        {
            return await GetOAuthUrlAsync("authorize", config); 
        }

        private async Task<Uri> GetTokenUrlAsync(IOAuthConfig config)
        {
            return await GetOAuthUrlAsync("token", config);
        }

        private async Task<Uri> GetOAuthUrlAsync(string action, IOAuthConfig config)
        {
            var authorizeUrl = $"https://login.microsoftonline.com/{config.TenantId}/oauth2/v2.0/{action}";

            Random rnd = new Random();
            byte[] seedBytes = new byte[32];
            rnd.NextBytes(seedBytes);

            var ps = new Dictionary<string, string>
            {
                { "client_id", _config.AudienceId },
                { "response_type", "code" },
                { "redirect_uri", _config.RedirectUrl },
                { "response_mode", "query" },
                { "prompt", _config.Prompt },
                { "scope", GetScopes(_config.Scopes) },
                { "state", "012345" }
            };

            string dataString;
            using (var formContent = new FormUrlEncodedContent(ps))
            {
                dataString = await formContent.ReadAsStringAsync();
            }

            return new Uri($"{authorizeUrl}?{dataString}");
        }

        private async Task<OAuthTokenResult> ReadTokenResultAsync(Uri uri, HttpContent content)
        {
            OAuthTokenResult oAuthToken = null;
            var response = await _http.PostAsync(uri, content);
            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"ReadTokenResultAsync result: {result}");

            response.EnsureSuccessStatusCode();
            oAuthToken = ArribaConvert.FromJson<OAuthTokenResult>(result);

            return oAuthToken;
        }

        private string GetScopes(IList<string> scopes)
        {
            return string.Join(" ", scopes);
        }
    }

    public class OAuthTokenResult 
    {
        public string Value
        {
            get
            {
                return AccessToken;
            }
        }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string Token { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public string ExpriresIn { get; set; }

        [JsonProperty("ext_expires_in")]
        public string ExtExpiresIn { get; set; }

        [JsonProperty("expires_on")]
        public string ExpiresOn { get; set; }

        [JsonProperty("not_before")]
        public string NotBefore { get; set; }

        [JsonProperty("resource")]
        public string Resource { get; set; }
    }
}
