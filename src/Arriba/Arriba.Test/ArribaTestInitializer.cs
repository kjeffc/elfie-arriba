using System;
using System.Collections.Generic;
using System.Text;
using Arriba.Composition;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba
{
    [TestClass]
    public class ArribaTestInitializer
    {
        [AssemblyInitialize]
        public static void InitialEnvironment(TestContext context)
        {
            ArribaServices.Initialize();
        }
    }
}
