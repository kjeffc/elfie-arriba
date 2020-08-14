using Arriba.Communication.Model;
using Arriba.Model;
using Arriba.Model.Correctors;
using Arriba.ParametersCheckers;
using Arriba.Server.Authentication;

namespace Arriba.Communication.Server.Application
{
    public class ArribaManagementServiceFactory
    {
        private const string Table_People = "People";

        private readonly SecureDatabase secureDatabase;
        private readonly ClaimsAuthenticationService _claimsAuth;

        public ArribaManagementServiceFactory(SecureDatabase secureDatabase, ClaimsAuthenticationService claims)
        {
            ParamChecker.ThrowIfNull(secureDatabase, nameof(secureDatabase));
            ParamChecker.ThrowIfNull(claims, nameof(claims));

            this.secureDatabase = secureDatabase;
            _claimsAuth = claims;
        }

        public IArribaManagementService CreateArribaManagementService(string userAliasCorrectorTable = "")
        {
            if (string.IsNullOrWhiteSpace(userAliasCorrectorTable))
                userAliasCorrectorTable = Table_People;

            var correctors = new CompositionComposedCorrectors(new TodayCorrector(), new UserAliasCorrector(secureDatabase[userAliasCorrectorTable]));

            return new ArribaManagementService(secureDatabase, correctors, _claimsAuth);
        }
    }
}