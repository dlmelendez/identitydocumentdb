// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ElCamino.AspNet.Identity.DocumentDB.Helpers;

namespace ElCamino.AspNet.Identity.DocumentDB.Tests.HelperTests
{
    [TestClass]
    public class KeyHelperTests
    {
        [TestMethod]
        [TestCategory("Identity.Azure.Helper.KeyHelper")]
        public void Escape()
        {
            //Removing for now, with revist in 2.0
            string url = @"https://www.msn.com/hello/index.aspx?parm1=2341341%2345234598!@#$%^&*()&param3=(^%$#$%^&HJKKK}P{}|:\";
            string escaped = UriEncodeKeyHelper.EscapeKey(url);
            Assert.IsFalse(escaped.Contains(UriEncodeKeyHelper.ReplaceIllegalChar), "Contains illegal char for row or partition key");
            var keyVer = new UriEncodeKeyHelper().KeyVersion;
            Assert.IsNull(UriEncodeKeyHelper.EscapeKey(string.Empty), "Escape didn't return null with an empty string.");
        }
    }
}
