using Arriba.Configuration;

namespace Arriba.Configuration
{
    public class ArribaServerConfiguration : IArribaServerConfiguration
    {        
        public IOAuthConfig OAuthConfig { get; set; }
        
        public string FrontendBaseUrl { get; set; }

        public bool EnabledAuthentication { get; set; }

        public string ApplicationVersion { get; set; }

        public string ServiceName { get; set; }
        public ArribaServerConfiguration()
        {
            OAuthConfig = new OAuthConfig();
        }
    }
}
