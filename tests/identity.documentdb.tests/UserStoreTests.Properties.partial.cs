// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ElCamino.AspNet.Identity.DocumentDB;
using Microsoft.AspNet.Identity;
using System.Collections.Generic;
using System.Security.Claims;
using System.Linq;
using ElCamino.AspNet.Identity.DocumentDB.Model;
using System.Threading.Tasks;

namespace ElCamino.AspNet.Identity.DocumentDB.Tests
{
    public partial class UserStoreTests
    {
        [TestMethod]
        [TestCategory("Identity.Azure.UserStore.Properties")]
        public async Task AccessFailedCount()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    manager.MaxFailedAccessAttemptsBeforeLockout = 2;
                    
                    var user = await CreateTestUser();
                    var taskUser = await manager.GetAccessFailedCountAsync(user.Id);
                    Assert.AreEqual<int>(user.AccessFailedCount, taskUser, "AccessFailedCount not equal");

                    var taskAccessFailed =  await manager.AccessFailedAsync(user.Id);
                    Assert.IsTrue(taskAccessFailed.Succeeded, string.Concat(taskAccessFailed.Errors));

                    user = await manager.FindByIdAsync(user.Id);
                    var taskAccessReset = await manager.ResetAccessFailedCountAsync(user.Id);
                    Assert.IsTrue(taskAccessReset.Succeeded, string.Concat(taskAccessReset.Errors));

                    try
                    {
                        var task = store.GetAccessFailedCountAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                    try
                    {
                        var task = store.IncrementAccessFailedCountAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                    try
                    {
                        var task = store.ResetAccessFailedCountAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                }
            }
        }

        private async Task SetValidateEmail(UserManager<IdentityUser> manager, 
            UserStore<IdentityUser> store,
            IdentityUser user, 
            string strNewEmail)
        {
            string originalEmail = user.Email;
            var taskUserSet = await manager.SetEmailAsync(user.Id, strNewEmail);
            Assert.IsTrue(taskUserSet.Succeeded, string.Concat(taskUserSet.Errors));

            var taskUser = await manager.GetEmailAsync(user.Id);
            Assert.AreEqual<string>(strNewEmail, taskUser, "GetEmailAsync: Email not equal");

            if (!string.IsNullOrWhiteSpace(strNewEmail))
            {
                var taskFind = await manager.FindByEmailAsync(strNewEmail);
                Assert.AreEqual<string>(strNewEmail, taskFind.Email, "FindByEmailAsync: Email not equal");
            }
            else
            {
                //TableQuery query = new TableQuery();
                //query.SelectColumns = new List<string>() { "Id" };
                //query.FilterString = TableQuery.GenerateFilterCondition("Id", QueryComparisons.Equal, user.Id);
                //query.Take(1);
                //var results = store.Context.IndexTable.ExecuteQuery(query);
                //Assert.IsTrue(results.Where(x=> x.RowKey.StartsWith("E_")).Count() == 0, string.Format("Email index not deleted for user {0}", user.Id));
            }
            //Should not find old by old email.
            if (!string.IsNullOrWhiteSpace(originalEmail))
            {
                var taskFind = await manager.FindByEmailAsync(originalEmail);
                Assert.IsNull(taskFind, "FindByEmailAsync: Old email should not yield a find result.");
            }

        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore.Properties")]
        public async Task EmailNone()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var user = await CreateTestUser(false, false);
                    await Task.Delay(1000);
                    string strNewEmail = string.Format("{0}@hotmail.com", Guid.NewGuid().ToString("N"));
                    await SetValidateEmail(manager, store, user, strNewEmail);

                    await SetValidateEmail(manager, store, user, string.Empty);

                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore.Properties")]
        public async Task Email()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var user = await CreateTestUser();
                    await Task.Delay(2000);
                    string strNewEmail = string.Format("{0}@gmail.com", Guid.NewGuid().ToString("N"));
                    await SetValidateEmail(manager, store, user, strNewEmail);

                    try
                    {
                        var task = store.GetEmailAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                    try
                    {
                        var task = store.SetEmailAsync(null, strNewEmail);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                    try
                    {
                        var task = store.SetEmailAsync(user, null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }
                }
            }
        }


        [TestMethod]
        [TestCategory("Identity.Azure.UserStore.Properties")]
        public async Task EmailConfirmed()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    manager.UserTokenProvider = new EmailTokenProvider<IdentityUser>();
                    var user = await CreateTestUser();

                    var taskUserSet = await manager.GenerateEmailConfirmationTokenAsync(user.Id);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(taskUserSet), "GenerateEmailConfirmationToken failed.");
                    string token = taskUserSet;

                    var taskConfirm = await manager.ConfirmEmailAsync(user.Id, token);
                    Assert.IsTrue(taskConfirm.Succeeded, string.Concat(taskConfirm.Errors));

                    user = await manager.FindByEmailAsync(user.Email);
                    var taskConfirmGet = await store.GetEmailConfirmedAsync(user);
                    Assert.IsTrue(taskConfirmGet, "Email not confirmed");

                    try
                    {
                        var task = store.SetEmailConfirmedAsync(null, true);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                    try
                    {
                        var task = store.GetEmailConfirmedAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore.Properties")]
        public async Task LockoutEnabled()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    manager.UserTokenProvider = new EmailTokenProvider<IdentityUser>();

                    var user = await CreateTestUser();

                    var taskLockoutSet = await manager.SetLockoutEnabledAsync(user.Id, true);
                    Assert.IsTrue(taskLockoutSet.Succeeded, string.Concat(taskLockoutSet.Errors));

                    DateTimeOffset offSet = new DateTimeOffset(DateTime.UtcNow.AddMinutes(3));
                    var taskDateSet = await manager.SetLockoutEndDateAsync(user.Id, offSet);
                    Assert.IsTrue(taskDateSet.Succeeded, string.Concat(taskDateSet.Errors));

                    var taskEnabledGet = manager.GetLockoutEnabledAsync(user.Id);
                    taskEnabledGet.Wait();
                    Assert.IsTrue(taskEnabledGet.Result, "Lockout not true");

                    var taskDateGet = manager.GetLockoutEndDateAsync(user.Id);
                    taskDateGet.Wait();
                    Assert.AreEqual(offSet,taskDateGet.Result, "Lockout date incorrect");

                    DateTime tmpDate = DateTime.UtcNow.AddDays(1);
                    user.LockoutEndDateUtc = tmpDate;
                    var taskGet = store.GetLockoutEndDateAsync(user);
                    taskGet.Wait();
                    Assert.AreEqual<DateTimeOffset>(new DateTimeOffset(tmpDate), taskGet.Result, "LockoutEndDate not set");

                    user.LockoutEndDateUtc = null;
                    var taskGet2 = store.GetLockoutEndDateAsync(user);
                    taskGet2.Wait();
                    Assert.AreEqual<DateTimeOffset>(new DateTimeOffset(), taskGet2.Result, "LockoutEndDate not set");

                    var minOffSet = DateTimeOffset.MinValue;
                    var taskSet2 = store.SetLockoutEndDateAsync(user, minOffSet); 
                    taskSet2.Wait();
                    Assert.IsNull(user.LockoutEndDateUtc, "LockoutEndDate not null");


                    try
                    {
                        store.GetLockoutEnabledAsync(null);
                    }
                    catch (ArgumentException) { }
                   

                    try
                    {
                        store.GetLockoutEndDateAsync(null);
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.SetLockoutEndDateAsync(null, offSet);
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.SetLockoutEnabledAsync(null, false);
                    }
                    catch (ArgumentException) { }
                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore.Properties")]
        public async Task PhoneNumber()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var user = await CreateTestUser();

                    string strNewPhoneNumber = "542-887-3434";
                    var taskPhoneNumberSet = await manager.SetPhoneNumberAsync(user.Id, strNewPhoneNumber);
                    Assert.IsTrue(taskPhoneNumberSet.Succeeded, string.Concat(taskPhoneNumberSet.Errors));

                    var taskUser = await manager.GetPhoneNumberAsync(user.Id);
                    Assert.AreEqual<string>(strNewPhoneNumber, taskUser, "PhoneNumber not equal");

                    try
                    {
                        var task = store.GetPhoneNumberAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                    try
                    {
                        var task = store.SetPhoneNumberAsync(null, strNewPhoneNumber);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                    try
                    {
                        var task = store.SetPhoneNumberAsync(user, null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore.Properties")]
        public async Task PhoneNumberConfirmed()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    manager.UserTokenProvider = new PhoneNumberTokenProvider<IdentityUser>();
                    var user = await CreateTestUser();
                    string strNewPhoneNumber = "425-555-1111";
                    var taskUserSet = await manager.GenerateChangePhoneNumberTokenAsync(user.Id, strNewPhoneNumber);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(taskUserSet), "GeneratePhoneConfirmationToken failed.");
                    string token = taskUserSet;

                    var taskConfirm = await manager.ChangePhoneNumberAsync(user.Id, strNewPhoneNumber, token);
                    Assert.IsTrue(taskConfirm.Succeeded, string.Concat(taskConfirm.Errors));

                    user = await manager.FindByEmailAsync(user.Email);
                    var taskConfirmGet = await store.GetPhoneNumberConfirmedAsync(user);
                    Assert.IsTrue(taskConfirmGet, "Phone not confirmed");

                    try
                    {
                        var task = store.SetPhoneNumberConfirmedAsync(null, true);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                    try
                    {
                        var task = store.GetPhoneNumberConfirmedAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore.Properties")]
        public async Task TwoFactorEnabled()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var user = await CreateTestUser();

                    bool twoFactorEnabled = true;
                    var taskTwoFactorEnabledSet = await manager.SetTwoFactorEnabledAsync(user.Id, twoFactorEnabled);
                    Assert.IsTrue(taskTwoFactorEnabledSet.Succeeded, string.Concat(taskTwoFactorEnabledSet.Errors));

                    var taskUser = await manager.GetTwoFactorEnabledAsync(user.Id);
                    Assert.AreEqual<bool>(twoFactorEnabled, taskUser, "TwoFactorEnabled not true");

                    try
                    {
                        var task = store.GetTwoFactorEnabledAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                    try
                    {
                        var task = store.SetTwoFactorEnabledAsync(null, twoFactorEnabled);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore.Properties")]
        public async Task PasswordHash()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var user = await CreateTestUser();
                    string passwordPlain = Guid.NewGuid().ToString("N");
                    string passwordHash = manager.PasswordHasher.HashPassword(passwordPlain);

                    await store.SetPasswordHashAsync(user, passwordHash);

                    var taskHasHash = await manager.HasPasswordAsync(user.Id);
                    Assert.IsTrue(taskHasHash, "PasswordHash not set");

                    var taskUser = await store.GetPasswordHashAsync(user);
                    Assert.AreEqual<string>(passwordHash, taskUser, "PasswordHash not equal");
                    user.PasswordHash = passwordHash;
                    try
                    {
                        var task = store.GetPasswordHashAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                    try
                    {
                        var task = store.HasPasswordAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                    try
                    {
                        var task = store.SetPasswordHashAsync(null, passwordHash);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                    try
                    {
                        var task = store.SetPasswordHashAsync(user, null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore.Properties")]
        public void UsersProperty()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                Assert.IsNotNull(store.Users, "Users Property is null");
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore.Properties")]
        public async Task SecurityStamp()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var user = await CreateTestUser();

                    var taskUser = await manager.GetSecurityStampAsync(user.Id);
                    Assert.AreEqual<string>(user.SecurityStamp, taskUser, "SecurityStamp not equal");

                    string strNewSecurityStamp = Guid.NewGuid().ToString("N");
                    await store.SetSecurityStampAsync(user, strNewSecurityStamp);

                    try
                    {
                        var task = store.GetSecurityStampAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                    try
                    {
                        var task = store.SetSecurityStampAsync(null, strNewSecurityStamp);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }

                    try
                    {
                        var task = store.SetSecurityStampAsync(user, null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }
                }
            }
        }

    }
}
