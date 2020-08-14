using Arriba.Communication.Model;
using Arriba.Model;
using Arriba.Model.Correctors;
using Arriba.ParametersCheckers;

namespace Arriba.Communication.Server.Application
{
    public class ArribaManagementServiceFactory
    {
        private const string Table_People = "People";

        private readonly SecureDatabase secureDatabase;

        public ArribaManagementServiceFactory(SecureDatabase secureDatabase)
        {
            ParamChecker.ThrowIfNull(secureDatabase, nameof(secureDatabase));

            this.secureDatabase = secureDatabase;
        }

        public IArribaManagementService CreateArribaManagementService(string userAliasCorrectorTable = "")
        {
            if (string.IsNullOrWhiteSpace(userAliasCorrectorTable))
                userAliasCorrectorTable = Table_People;

            var correctors = new CompositionComposedCorrectors(new TodayCorrector(), new UserAliasCorrector(secureDatabase[userAliasCorrectorTable]));

            return new ArribaManagementService(secureDatabase, correctors);
        }
    }
}