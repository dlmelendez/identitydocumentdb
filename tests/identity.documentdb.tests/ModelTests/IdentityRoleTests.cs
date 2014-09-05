using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ElCamino.AspNet.Identity.DocumentDB.Model;

namespace ElCamino.Web.Identity.DocumentDB.Tests.ModelTests
{
    [TestClass]
    public class IdentityRoleTests
    {
        [TestMethod]
        [TestCategory("Identity.Azure.Model")]
        public void IdentityRoleSet_Id()
        {
            var role = new IdentityRole();
            role.Id = Guid.NewGuid().ToString();

            role.Id = null;
        }

        [TestMethod]
        [TestCategory("Identity.Azure.Model")]
        public void IdentityUserSet_Id()
        {
            var u = new IdentityUser();
            u.Id = Guid.NewGuid().ToString();

            u.Id = null;
        }

    }
}
