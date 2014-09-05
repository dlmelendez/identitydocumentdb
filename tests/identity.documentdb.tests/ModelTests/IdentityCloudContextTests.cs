// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ElCamino.AspNet.Identity.DocumentDB.Model;
using System.Resources;

namespace ElCamino.AspNet.Identity.DocumentDB.Tests.ModelTests
{
    [TestClass]
    public class IdentityCloudContextTests
    {
        //[TestMethod]
        //[TestCategory("Identity.Azure.Model")]
        //public void IdentityCloudContextCtors()
        //{
        //    var ic = new IdentityCloudContext();
        //    Assert.IsNotNull(ic, "New IdentityCloudContext is null");

        //    //Pass in valid connection string
        //    string strValidConnection = CloudConfigurationManager.GetSetting(
        //        ElCamino.AspNet.Identity.DocumentDB.Constants.AppSettingsKeys.DefaultStorageConnectionStringKey);
        //    var icc = new IdentityCloudContext(strValidConnection);
        //    icc.Dispose();

        //    string strInvalidConnectionStringKey = Guid.NewGuid().ToString();

        //    try
        //    {
        //        ic = new IdentityCloudContext(strInvalidConnectionStringKey);
        //    }
        //    catch (System.FormatException) { }

        //    try
        //    {
        //        ic = new IdentityCloudContext(string.Empty);
        //    }
        //    catch (MissingManifestResourceException) {  }

        //    //----------------------------------------------
        //    var iucc = new IdentityCloudContext<IdentityUser>();
        //    Assert.IsNotNull(iucc, "New IdentityCloudContext is null");

        //    try
        //    {
        //        iucc = new IdentityCloudContext<IdentityUser>(strInvalidConnectionStringKey);
        //    }
        //    catch (System.FormatException) { }

        //    try
        //    {
        //        iucc = new IdentityCloudContext<IdentityUser>(string.Empty);
        //    }
        //    catch (MissingManifestResourceException) { }
            
        //    //------------------------------------------

        //    var i2 = new IdentityCloudContext<IdentityUser, IdentityRole, string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>();
        //    Assert.IsNotNull(i2, "New IdentityCloudContext is null");

        //    try
        //    {
        //        i2 = new IdentityCloudContext<IdentityUser, IdentityRole, string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>(Guid.NewGuid().ToString());
        //    }
        //    catch (System.FormatException) { }

        //    try
        //    {
        //        i2 = new IdentityCloudContext<IdentityUser, IdentityRole, string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>(string.Empty);
        //    }
        //    catch (MissingManifestResourceException) { }

        //    try
        //    {
        //        var i3 = new IdentityCloudContext<IdentityUser>();
        //        i3.Dispose();
        //        var table = i3.RoleTable;
        //    }
        //    catch (ObjectDisposedException) { }

        //    try
        //    {
        //        var i4 = new IdentityCloudContext<IdentityUser>();
        //        i4.Dispose();
        //        var table = i4.UserTable;
        //    }
        //    catch (ObjectDisposedException) { }

        //}
    }
}
