using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ElCamino.AspNet.Identity.DocumentDB.Model;

namespace ElCamino.Web.Identity.DocumentDB.Tests.ModelTests
{
    [TestClass]
    public class IdentityUserClaimTests
    {
        [TestMethod]
        [TestCategory("Identity.Azure.Model")]
        public void IdentityUserClaimGet_UserId()
        {
            var uc = new IdentityUserClaim();
            uc.GenerateKeys();
            Assert.AreEqual(uc.PartitionKey, uc.UserId, "PartitionKey and UserId are not equal.");
        }
    }
}
