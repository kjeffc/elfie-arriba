using Arriba.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Arriba.Test.Configuration
{
    [TestClass]
    public class ArribaConfigurationTest
    {

        [TestMethod]
        public void LoadExistentConfigurationUsingConfigurationName()
        {
            var args = new string[] { };
            var configurationLoader = new ArribaConfigurationLoader(args, GetBasePath("Louvau"));
            var configuration = configurationLoader.Bind<CrawlerConfiguration>("Arriba");
            Assert.IsNotNull(configuration);
            Assert.AreEqual("Louvau", configuration.ArribaTable);
        }

        [TestMethod]
        public void ThrowDirectoryNotFoundExceptionForANonExistenceBasePath()
        {
            Assert.ThrowsException<DirectoryNotFoundException>(
                () => new ArribaConfigurationLoader(new string[] { }, GetBasePath("foo")));
        }

        [TestMethod]
        public void LoadExistentConfigurationUsingConfigurationRoot()
        {
            var confRoot = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var configurationLoader = new ArribaConfigurationLoader(confRoot);
            Assert.IsNotNull(configurationLoader);
        }

        [TestMethod]
        public void GetEnvironmentVariableValue()
        {
            System.Environment.SetEnvironmentVariable("ArribaLoaderTest", "true");

            var args = new string[] { };
            var configurationLoader = new ArribaConfigurationLoader(args);
            Assert.IsNotNull(configurationLoader);
            Assert.AreEqual("true", configurationLoader.GetStringValue("ArribaLoaderTest"));
        }


        [TestMethod]
        public void AddNetJsonConfigurationFile()
        {
            var args = new string[] { "test=foo" };
            var configurationLoader = new ArribaConfigurationLoader(args);
            Assert.IsNotNull(configurationLoader);
            Assert.AreEqual("foo", configurationLoader.GetStringValue("test"));
            Assert.AreEqual("", configurationLoader.GetStringValue("Arriba:arribaTable", ""));
            configurationLoader.AddJsonSource(Path.Combine(GetBasePath("Louvau"), "appsettings.json"));
            Assert.AreEqual("Louvau", configurationLoader.GetStringValue("Arriba:arribaTable"));
        }

        [DataTestMethod]
        [DataRow(new string[] { "mode=load" })]
        [DataRow(new string[] { "name=\"First Document\"" })]
        [DataRow(new string[] { "mode=import", "table=Scratch", "select=\"Date, Adj Close\"", "take=20", "orderBy=Date", "load=true" })]
        public void ProcessCommandArgument(string[] args)
        {
            var configurationLoader = new ArribaConfigurationLoader(args);
            foreach (var arg in args)
            {
                var parts = arg.Split('=');
                Assert.AreEqual(parts[1], configurationLoader.GetStringValue(parts[0]));
            }            
        }

        private string GetBasePath(string configurationName)
        {
            var configSrcFolder = "src";
            var basePath = Directory.GetCurrentDirectory();
            basePath = basePath.Substring(0, basePath.LastIndexOf(configSrcFolder) + configSrcFolder.Length);
            return Path.Combine(basePath, "Arriba", "Databases", configurationName);
        }
    }
}
