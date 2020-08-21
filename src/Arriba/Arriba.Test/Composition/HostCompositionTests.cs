using Arriba.Communication;
using Arriba.Composition;
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

        [TestMethod]
        public void VerifyJsonWriterIsRegistered()
        {
            var host = new Arriba.Composition.Host();
            host.Compose();

            var service = host.GetService<ApplicationServer>();
            var writer = service.ReaderWriter.GetWriter("application/json", string.Empty);
            Assert.IsNotNull(writer);
        }
    }
}
