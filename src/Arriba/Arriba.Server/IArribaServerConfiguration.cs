using Arriba.Configuration;

namespace Arriba.Server
{
    public interface IArribaServerConfiguration : IArribaConfiguration
    {
        public bool EnabledAuthentication { get; }
        public IOAuthConfig OAuthConfig { get; }
        public string FrontendBaseUrl { get; }
    }
}
