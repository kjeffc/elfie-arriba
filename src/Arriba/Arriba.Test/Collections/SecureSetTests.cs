using Arriba.Model.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Collections
{
    [TestClass]
    public class SecureSetTests
    {
        [TestMethod]
        public void SecureSetSerializesAsArray()
        {
            var expected = "[{\"key\":{\"scope\":\"Group\",\"name\":\"group\"},\"value\":\"group value\"},{\"key\":{\"scope\":\"User\",\"name\":\"user\"},\"value\":\"user value\"}]";

            var set = new SecuredSet<string>();

            set.Add(new SecurityIdentity(IdentityScope.Group, "group"), "group value");
            set.Add(new SecurityIdentity(IdentityScope.User, "user"), "user value");

            var actual = ArribaConvert.ToJson(set);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SecureSetDeserializesFromArray()
        {
            var expected = new SecuredSet<string>();
            expected.Add(new SecurityIdentity(IdentityScope.Group, "group"), "group value");
            expected.Add(new SecurityIdentity(IdentityScope.User, "user"), "user value");

            var input = "[{\"key\":{\"scope\":\"Group\",\"name\":\"group\"},\"value\":\"group value\"},{\"key\":{\"scope\":\"User\",\"name\":\"user\"},\"value\":\"user value\"}]";

            var actual = ArribaConvert.FromJson<SecuredSet<string>>(input);
            CollectionAssert.AreEquivalent(expected, actual);
        }
    }
}
