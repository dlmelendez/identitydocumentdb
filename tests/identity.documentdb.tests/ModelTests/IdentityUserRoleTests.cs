using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ElCamino.AspNet.Identity.DocumentDB.Model;

namespace ElCamino.Web.Identity.DocumentDB.Tests.ModelTests
{
    [TestClass]
    public class IdentityUserRoleTests
    {
        [TestMethod]
        [TestCategory("Identity.Azure.Model")]
        public void IdentityUserRoleGet_UserId()
        {
            var ur = new IdentityUserRole();
            ur.GenerateKeys();
            Assert.AreEqual(ur.PartitionKey, ur.UserId, "PartitionKey and UserId are not equal.");
        }
    }
}
