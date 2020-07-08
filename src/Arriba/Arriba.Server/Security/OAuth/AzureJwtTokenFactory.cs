using Arriba.Configuration;
using Arriba.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Threading.Tasks;

namespace Arriba.Security.OAuth
{
    public class AzureJwtTokenFactory
    {
        private IOAuthConfig OAuthConfig { get; }
        private OpenIdConnectConfiguration Config { get; }

        public TokenValidationParameters TokenValidationParameters { get; }

        private AzureJwtTokenFactory(IOAuthConfig oauthConfig, OpenIdConnectConfiguration config)
        {
            this.OAuthConfig = oauthConfig;
            Config = config;
            TokenValidationParameters = new TokenValidationParameters()
            {
                ValidateLifetime = true,
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidIssuer = config.Issuer,
                IssuerSigningKeys = Config.SigningKeys,
                SignatureValidator = this.ValidateSignature
            };
        }

        public void Configure(JwtBearerOptions options)
        {
            options.Audience = OAuthConfig.AudienceId;
            options.TokenValidationParameters = this.TokenValidationParameters;
        }

        private SecurityToken ValidateSignature(string token, TokenValidationParameters validationParameters)
        {
            var jwt = new JwtSecurityToken(token);
            
            if (AreNotEqual(jwt.Issuer, Config.Issuer))
            {
                throw new SecurityTokenException("Invalid issuer");
            }

            return jwt;
        }

        private bool AreNotEqual(string issuer1, string issuer2)
        {
            return !String.Equals(issuer1, issuer2, StringComparison.OrdinalIgnoreCase);
        }

        public static async Task<AzureJwtTokenFactory> CreateAsync(IOAuthConfig authConfig)
        {
            string stsDiscoveryEndpoint = $"https://login.microsoftonline.com/{authConfig.TenantId}/v2.0/.well-known/openid-configuration";

            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(stsDiscoveryEndpoint, new OpenIdConnectConfigurationRetriever());

            var config = await configManager.GetConfigurationAsync();
            return new AzureJwtTokenFactory(authConfig, config);
        }
    }
}

