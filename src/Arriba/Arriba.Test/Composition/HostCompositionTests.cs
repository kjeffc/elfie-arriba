using Arriba.Communication;
using Arriba.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Composition
{
    [TestClass]
    public class HostCompositionTests
    {
        [TestMethod]
        public void VerifiedApplicationServerComposition()
        {
            var host = new Arriba.Composition.Host();
            host.Compose();

            var server = host.GetService<ApplicationServer>();
            Assert.IsNotNull(server);
        }
    }
}
