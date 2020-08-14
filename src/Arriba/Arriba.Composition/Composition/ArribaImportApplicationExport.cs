using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using Arriba.Communication.Application;
using Arriba.Communication.Model;
using Arriba.Communication.Server.Application;
using Arriba.Model;
using Arriba.Server;
using Arriba.Server.Application;
using Arriba.Server.Authentication;
using Arriba.Server.Hosting;

namespace Arriba.Composition
{
    [Export(typeof(IRoutedApplication))]
    class ArribaImportApplicationExport : ArribaImportApplication
    {
        [ImportingConstructor]
        public ArribaImportApplicationExport(DatabaseFactory f, ClaimsAuthenticationService auth)
            : base(f, auth)
        {
        }
    }

    [Export(typeof(ClaimsAuthenticationService)), Shared]
    class ClaimsAuthenticationServiceExport : ClaimsAuthenticationService
    {
    }

    [Export(typeof(IArribaManagementService)), Shared]
    class ArribaManagementServiceExport : ArribaManagementService
    {
        [ImportingConstructor]
        public ArribaManagementServiceExport(SecureDatabase secureDatabase, CompositionComposedCorrectors composedCorrector)
            : base(secureDatabase, composedCorrector)
        {
        }
    }

    [Export(typeof(IRoutedApplication))]
    internal class ArribaQueryApplicationExport : ArribaQueryApplication
    {
        [ImportingConstructor]
        public ArribaQueryApplicationExport(DatabaseFactory f, ClaimsAuthenticationService auth)
            : base(f, auth)
        { }
    }

    [Export(typeof(IRoutedApplication))]
    internal class ArribaTableRoutesApplicationExport : ArribaTableRoutesApplication
    {
        [ImportingConstructor]
        public ArribaTableRoutesApplicationExport(DatabaseFactory f, ClaimsAuthenticationService auth, IArribaManagementService managementService)
            : base(f, auth, managementService)
        {
        }
    }
}
