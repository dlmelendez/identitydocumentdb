// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNet.Identity;
using ElCamino.AspNet.Identity.DocumentDB.Helpers;
using ElCamino.AspNet.Identity.DocumentDB.Model;

namespace ElCamino.AspNet.Identity.DocumentDB.Tests.ModelTests
{
    [TestClass]
    public class IdentityUserTests
    {
        [TestMethod]
        [TestCategory("Identity.Azure.Model")]
        public void IdentityUserCtors()
        {

            Assert.IsNotNull(new IdentityUser(Guid.NewGuid().ToString()), "Identity User is null.");

        }
    }
}
