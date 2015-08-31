// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ElCamino.AspNet.Identity.DocumentDB;
using Microsoft.AspNet.Identity;
using System.Collections.Generic;
using System.Security.Claims;
using System.Linq;
using ElCamino.AspNet.Identity.DocumentDB.Model;
using System.Threading;
using System.Threading.Tasks;
using ElCamino.AspNet.Identity.DocumentDB.Helpers;
using ElCamino.Web.Identity.DocumentDB.Tests.ModelTests;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using System.Text;

namespace ElCamino.AspNet.Identity.DocumentDB.Tests
{
    [TestClass]
    public partial class UserStoreTests
    {
        #region Static and Const Members
        public static string DefaultUserPassword;
        private static IdentityUser User = null;
        private static List<string> NoCreateUserTests =
            new List<string>() { 
                "AddUserLogin",
                "SecurityStamp", 
                "CreateUser",
                "AddUserRole",
                "AddUserClaim",
                "UpdateApplicationUser",
                "AddRemoveUserClaim",
                "LockoutEnabled",
                "PhoneNumber",
                "TwoFactorEnabled",
                "PasswordHash",
                "Email",
                "EmailNone"
                };

        #endregion

        private TestContext testContextInstance;
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }


        #region Test Initialization
        [TestInitialize]
        public void Initialize()
        {

            DefaultUserPassword = Guid.NewGuid().ToString();

            //--Changes to speed up tests that don't require a new user, sharing a static user

            if(User == null &&
                !NoCreateUserTests.Any(t => t == TestContext.TestName))
            {
                CreateUser();
            }
            //--
        }
        #endregion

        private void WriteLineObject<t> (t obj)  where t : class
        {
            TestContext.WriteLine(typeof(t).Name);
            string strLine = obj == null ? "Null" : Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            TestContext.WriteLine("{0}", strLine);
        }

        private Claim GenAdminClaim()
        {
            return new Claim(Constants.AccountClaimTypes.AccountTestAdminClaim, Guid.NewGuid().ToString());
        }

        private Claim GenUserClaim()
        {
            return new Claim(Constants.AccountClaimTypes.AccountTestUserClaim, Guid.NewGuid().ToString());
        }
        private UserLoginInfo GenGoogleLogin()
        {
           return new UserLoginInfo(Constants.LoginProviders.GoogleProvider.LoginProvider,
                        Constants.LoginProviders.GoogleProvider.ProviderKey);
        }

        private IdentityUser GenTestUser()
        {
            Guid id = Guid.NewGuid();
            IdentityUser user = new IdentityUser()
            {
                Email = id.ToString() + "@live.com",
                UserName = id.ToString("N"),
                LockoutEnabled = false,
                LockoutEndDateUtc = null,
                PhoneNumber = "555-555-5555",
                TwoFactorEnabled = false,
            };

            return user;
        }

        private ApplicationUser GetTestAppUser()
        {
            Guid id = Guid.NewGuid();
            ApplicationUser user = new ApplicationUser()
            {
                Email = id.ToString() + "@live.com",
                UserName = id.ToString("N"),
                LockoutEnabled = false,
                LockoutEndDateUtc = null,
                PhoneNumber = "555-555-5555",
                TwoFactorEnabled = false,
                FirstName = "Jim",
                LastName = "Bob"
            };
            return user;
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore")]
        public void UserStoreCtors()
        {
            try
            {
                new UserStore<IdentityUser>(null);
            }
            catch (ArgumentException) { }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore")]
        public void CreateUser()
        {
            User = CreateTestUser();
            WriteLineObject<IdentityUser>(User);
        }

        private IdentityUser CreateTestUser(bool createPassword = true, bool createEmail = true)
        {

            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var user = GenTestUser();
                    if (!createEmail)
                    {
                        user.Email = null;
                    }
                    var taskUser = createPassword ? 
                        manager.CreateAsync(user, DefaultUserPassword) :
                        manager.CreateAsync(user);
                    taskUser.Wait();
                    Assert.IsTrue(taskUser.Result.Succeeded, string.Concat(taskUser.Result.Errors));

                    for (int i = 0; i < 2; i++)
                    {
                        AddUserClaimHelper(manager, user, GenAdminClaim());
                        AddUserLoginHelper(manager, user, GenGoogleLogin());
                        AddUserRoleHelper(manager, user, string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N")));
                    }

                    try
                    {
                        var task = store.CreateAsync(null);
                        task.Wait();
                    }
                    catch (AggregateException agg) 
                    {
                        agg.ValidateAggregateException<ArgumentException>();
                    }

                    Thread.Sleep(2000);
                    var getUserTask = manager.FindByIdAsync(user.Id);
                    getUserTask.Wait();
                    return getUserTask.Result;
                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore")]
        public void DeleteUser()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var user = GenTestUser();

                    var taskUser = manager.CreateAsync(user, DefaultUserPassword);
                    taskUser.Wait();
                    Assert.IsTrue(taskUser.Result.Succeeded, string.Concat(taskUser.Result.Errors));


                    for (int i = 0; i < 5; i++)
                    {
                        AddUserClaimHelper(manager, user, GenAdminClaim());
                        AddUserLoginHelper(manager, user, GenGoogleLogin());
                        AddUserRoleHelper(manager, user, string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N")));
                    }

                    var findUserTask2 = manager.FindByIdAsync(user.Id);
                    findUserTask2.Wait();
                    user = findUserTask2.Result;
                    WriteLineObject<IdentityUser>(user);

                    TestContext.WriteLine("User Size in Bytes: {0}", Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(user)));

                    DateTime start = DateTime.UtcNow;
                    var taskUserDel = manager.DeleteAsync(user);
                    taskUserDel.Wait();
                    Assert.IsTrue(taskUserDel.Result.Succeeded, string.Concat(taskUser.Result.Errors));
                    TestContext.WriteLine("DeleteAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

                    Thread.Sleep(1000);

                    var findUserTask = manager.FindByIdAsync(user.Id);
                    findUserTask.Wait();
                    Assert.IsNull(findUserTask.Result, "Found user Id, user not deleted.");

                    try
                    {
                        var task = store.DeleteAsync(null);
                        task.Wait();
                    }
                    catch (AggregateException agg)
                    {
                        agg.ValidateAggregateException<ArgumentException>();
                    }
                }
            }
        }


        [TestMethod]
        [TestCategory("Identity.Azure.UserStore")]
        public void UpdateApplicationUser()
        {
            using (UserStore<ApplicationUser> store = new UserStore<ApplicationUser>())
            {
                using (UserManager<ApplicationUser> manager = new UserManager<ApplicationUser>(store))
                {
                    var user = GetTestAppUser();
                    WriteLineObject<ApplicationUser>(user);
                    var taskUser = manager.CreateAsync(user, DefaultUserPassword);
                    taskUser.Wait();
                    Assert.IsTrue(taskUser.Result.Succeeded, string.Concat(taskUser.Result.Errors));

                    string oFirstName = user.FirstName;
                    string oLastName = user.LastName;

                    var taskFind1 = manager.FindByNameAsync(user.UserName);
                    taskFind1.Wait();
                    Assert.AreEqual<string>(oFirstName, taskFind1.Result.FirstName, "FirstName not created.");
                    Assert.AreEqual<string>(oLastName, taskFind1.Result.LastName, "LastName not created.");                  

                    string cFirstName = string.Format("John_{0}", Guid.NewGuid());
                    string cLastName = string.Format("Doe_{0}", Guid.NewGuid());

                    user.FirstName = cFirstName;
                    user.LastName = cLastName;

                    var taskUserUpdate = manager.UpdateAsync(user);
                    taskUserUpdate.Wait();
                    Assert.IsTrue(taskUserUpdate.Result.Succeeded, string.Concat(taskUserUpdate.Result.Errors));

                    var taskFind = manager.FindByNameAsync(user.UserName);
                    taskFind.Wait();
                    Assert.AreEqual<string>(cFirstName, taskFind.Result.FirstName, "FirstName not updated.");
                    Assert.AreEqual<string>(cLastName, taskFind.Result.LastName, "LastName not updated.");                  
                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore")]
        public void UpdateUser()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var user = GenTestUser();
                    WriteLineObject<IdentityUser>(user);
                    var taskUser = manager.CreateAsync(user, DefaultUserPassword);
                    taskUser.Wait();
                    Assert.IsTrue(taskUser.Result.Succeeded, string.Concat(taskUser.Result.Errors));
                    
                    TestContext.WriteLine("User Size in Bytes: {0}", Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(user)));

                    var taskUserUpdate = manager.UpdateAsync(user);
                    taskUserUpdate.Wait();
                    Assert.IsTrue(taskUserUpdate.Result.Succeeded, string.Concat(taskUserUpdate.Result.Errors));

                    try
                    {
                        var t1 = store.UpdateAsync(null);
                        t1.Wait();
                    }
                    catch (AggregateException aggex) {
                        aggex.ValidateAggregateException<ArgumentNullException>();
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore")]
        public void ChangeUserName()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var firstUser = CreateTestUser();
                    TestContext.WriteLine("{0}", "Original User");
                    WriteLineObject<IdentityUser>(firstUser);
                    string originalPlainUserName = firstUser.UserName;
                    string originalUserId = firstUser.Id;
                    string userNameChange = Guid.NewGuid().ToString("N");
                    firstUser.UserName = userNameChange;

                    DateTime start = DateTime.UtcNow;
                    var taskUserUpdate = manager.UpdateAsync(firstUser);
                    taskUserUpdate.Wait();
                    TestContext.WriteLine("UpdateAsync(ChangeUserName): {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

                    Assert.IsTrue(taskUserUpdate.Result.Succeeded, string.Concat(taskUserUpdate.Result.Errors));

                    var taskUserChanged = manager.FindByNameAsync(userNameChange);
                    taskUserChanged.Wait();
                    var changedUser = taskUserChanged.Result;

                    TestContext.WriteLine("{0}", "Changed User");
                    WriteLineObject<IdentityUser>(changedUser);

                    Assert.IsNotNull(changedUser, "User not found by new username.");
                    Assert.IsFalse(originalPlainUserName.Equals(changedUser.UserName, StringComparison.OrdinalIgnoreCase), "UserName property not updated.");
                    
                    Assert.AreEqual<int>(firstUser.Roles.Count, changedUser.Roles.Count, "Roles count are not equal");
                    Assert.IsTrue(changedUser.Roles.All(r => r.UserId == changedUser.Id.ToString()), "Roles partition keys are not equal to the new user id");
                    
                    Assert.AreEqual<int>(firstUser.Claims.Count, changedUser.Claims.Count, "Claims count are not equal");
                    Assert.IsTrue(changedUser.Claims.All(r => r.UserId == changedUser.Id.ToString()), "Claims partition keys are not equal to the new user id");

                    Assert.AreEqual<int>(firstUser.Logins.Count, changedUser.Logins.Count, "Logins count are not equal");
                    Assert.IsTrue(changedUser.Logins.All(r => r.UserId == changedUser.Id.ToString()), "Logins partition keys are not equal to the new user id");

                    //Check email
                    var taskFindEmail = manager.FindByEmailAsync(changedUser.Email);
                    taskFindEmail.Wait();
                    Assert.IsNotNull(taskFindEmail.Result, "User not found by new email.");

                    //Check logins
                    foreach (var log in taskFindEmail.Result.Logins)
                    {
                        var taskFindLogin = manager.FindAsync(new UserLoginInfo(log.LoginProvider, log.ProviderKey));
                        taskFindLogin.Wait();
                        Assert.IsNotNull(taskFindLogin.Result, "User not found by login.");
                    }

                    try
                    {
                        var t1 = store.UpdateAsync(null);
                        t1.Wait();
                    }
                    catch (AggregateException aggex)
                    {
                        aggex.ValidateAggregateException<ArgumentNullException>();
                    }
                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore")]
        public void FindUserByEmail()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var user = User;
                    WriteLineObject<IdentityUser>(user);

                    DateTime start = DateTime.UtcNow;
                    var findUserTask = manager.FindByEmailAsync(user.Email);
                    findUserTask.Wait();
                    TestContext.WriteLine("FindByEmailAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

                    Assert.AreEqual<string>(user.Email, findUserTask.Result.Email, "Found user email not equal");
                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore")]
        public void FindUserById()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var user = User;
                    DateTime start = DateTime.UtcNow;
                    var findUserTask = manager.FindByIdAsync(user.Id);
                    findUserTask.Wait();
                    TestContext.WriteLine("FindByIdAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

                    Assert.AreEqual<string>(user.Id, findUserTask.Result.Id, "Found user Id not equal");
                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore")]
        public void FindUserByName()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var user = User;
                    WriteLineObject<IdentityUser>(user);
                    DateTime start = DateTime.UtcNow;
                    var findUserTask = manager.FindByNameAsync(user.UserName);
                    findUserTask.Wait();
                    TestContext.WriteLine("FindByNameAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

                    Assert.AreEqual<string>(user.UserName, findUserTask.Result.UserName, "Found user UserName not equal");
                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore")]
        public void AddUserLogin()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>(new IdentityCloudContext<IdentityUser>()))
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var user = CreateTestUser(false);
                    WriteLineObject<IdentityUser>(user);
                    AddUserLoginHelper(manager, user, GenGoogleLogin());
                }
            }
        }

        public void AddUserLoginHelper(UserManager<IdentityUser> manager, IdentityUser user, UserLoginInfo loginInfo)
        {
            var userAddLoginTask = manager.AddLoginAsync(user.Id, loginInfo);
            userAddLoginTask.Wait();
            Assert.IsTrue(userAddLoginTask.Result.Succeeded, string.Concat(userAddLoginTask.Result.Errors));

            var loginGetTask = manager.GetLoginsAsync(user.Id);
            loginGetTask.Wait();
            Assert.IsTrue(loginGetTask.Result
                .Any(log => log.LoginProvider == loginInfo.LoginProvider
                    & log.ProviderKey == loginInfo.ProviderKey), "LoginInfo not found: GetLoginsAsync");

            DateTime start = DateTime.UtcNow;
            var loginGetTask2 = manager.FindAsync(loginGetTask.Result.First());
            loginGetTask2.Wait();
            TestContext.WriteLine("FindAsync(By Login): {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
            Assert.IsNotNull(loginGetTask2.Result, "LoginInfo not found: FindAsync");
        }


        [TestMethod]
        [TestCategory("Identity.Azure.UserStore")]
        public void AddRemoveUserLogin()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var user = GenTestUser();
                    WriteLineObject<IdentityUser>(user);
                    var taskUser = manager.CreateAsync(user, DefaultUserPassword);
                    taskUser.Wait();
                    Assert.IsTrue(taskUser.Result.Succeeded, string.Concat(taskUser.Result.Errors));

                    var loginInfo = GenGoogleLogin();
                    var userAddLoginTask = manager.AddLoginAsync(user.Id, loginInfo);
                    userAddLoginTask.Wait();
                    Assert.IsTrue(userAddLoginTask.Result.Succeeded, string.Concat(userAddLoginTask.Result.Errors));

                    var loginGetTask = manager.GetLoginsAsync(user.Id);
                    loginGetTask.Wait();
                    Assert.IsTrue(loginGetTask.Result
                        .Any(log=> log.LoginProvider == loginInfo.LoginProvider
                            & log.ProviderKey == loginInfo.ProviderKey), "LoginInfo not found: GetLoginsAsync");

                    var loginGetTask2 = manager.FindAsync(loginGetTask.Result.First());
                    loginGetTask2.Wait();
                    Assert.IsNotNull(loginGetTask2.Result, "LoginInfo not found: FindAsync");

                    var userRemoveLoginTaskNeg1 = manager.RemoveLoginAsync(user.Id, new UserLoginInfo(string.Empty, loginInfo.ProviderKey));
                    userRemoveLoginTaskNeg1.Wait();

                    var userRemoveLoginTaskNeg2 = manager.RemoveLoginAsync(user.Id, new UserLoginInfo(loginInfo.LoginProvider, string.Empty));
                    userRemoveLoginTaskNeg2.Wait();

                    var userRemoveLoginTask = manager.RemoveLoginAsync(user.Id, loginInfo);
                    userRemoveLoginTask.Wait();
                    Assert.IsTrue(userRemoveLoginTask.Result.Succeeded, string.Concat(userRemoveLoginTask.Result.Errors));
                    var loginGetTask3 = manager.GetLoginsAsync(user.Id);
                    loginGetTask3.Wait();
                    Assert.IsTrue(!loginGetTask3.Result.Any(), "LoginInfo not removed");

                    //Negative cases

                    var loginFindNeg = manager.FindAsync(new UserLoginInfo("asdfasdf", "http://4343443dfaksjfaf"));
                    loginFindNeg.Wait();
                    Assert.IsNull(loginFindNeg.Result, "LoginInfo found: FindAsync");

                    try
                    {
                        var t1 =  store.AddLoginAsync(null, loginInfo);
                        t1.Wait();
                    }
                    catch (AggregateException aggex) {
                        aggex.ValidateAggregateException<ArgumentNullException>();
                    }

                    try
                    {
                        var t2 = store.AddLoginAsync(user, null);
                        t2.Wait();
                    }
                    catch (AggregateException aggex)
                    {
                        aggex.ValidateAggregateException<ArgumentNullException>();
                    }
                    try
                    {
                        var t3 = store.RemoveLoginAsync(null, loginInfo);
                        t3.Wait();
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        var t4 = store.RemoveLoginAsync(user, null);
                        t4.Wait();
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        var t5 = store.FindAsync(null);
                        t5.Wait();
                    }
                    catch (AggregateException aggex) {
                        aggex.ValidateAggregateException<ArgumentNullException>();
                    }
                    try
                    {
                        var t6 = store.GetLoginsAsync(null);
                        t6.Wait();
                    }
                    catch (ArgumentException) { }
                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore")]
        public void AddUserRole()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>(new IdentityCloudContext<IdentityUser>()))
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    string strUserRole = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));
                    WriteLineObject<IdentityUser>(User);
                    AddUserRoleHelper(manager, User, strUserRole);
                }
            }
        }

        public void AddUserRoleHelper(UserManager<IdentityUser> manager, IdentityUser user, string roleName)
        {
            using (RoleStore<IdentityRole> rstore = new RoleStore<IdentityRole>())
            {
                var userRole = rstore.FindByNameAsync(roleName);
                userRole.Wait();

                if (userRole.Result == null)
                {
                    var taskUser = rstore.CreateAsync(new IdentityRole(roleName));
                    taskUser.Wait();
                }
            }

            var userRoleTask = manager.AddToRoleAsync(user.Id, roleName);
            userRoleTask.Wait();
            Assert.IsTrue(userRoleTask.Result.Succeeded, string.Concat(userRoleTask.Result.Errors));

            var roles2Task = manager.IsInRoleAsync(user.Id, roleName);
            roles2Task.Wait();
            Assert.IsTrue(roles2Task.Result, "Role not found");

        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore")]
        public void AddRemoveUserRole()
        {
            string roleName = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestAdminRole, Guid.NewGuid().ToString("N"));

            using (RoleStore<IdentityRole> rstore = new RoleStore<IdentityRole>())
            {
                var taskAdmin = rstore.CreateAsync(new IdentityRole(roleName));
                taskAdmin.Wait();
                var adminRole = rstore.FindByNameAsync(roleName);
                adminRole.Wait();
            }

            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var user = User;
                    WriteLineObject<IdentityUser>(user);
                    var userRoleTask = manager.AddToRoleAsync(user.Id, roleName);
                    userRoleTask.Wait();
                    Assert.IsTrue(userRoleTask.Result.Succeeded, string.Concat(userRoleTask.Result.Errors));

                    var rolesTask = manager.GetRolesAsync(user.Id);
                    rolesTask.Wait();
                    Assert.IsTrue(rolesTask.Result.Contains(roleName), "Role not found");

                    var roles2Task = manager.IsInRoleAsync(user.Id, roleName);
                    roles2Task.Wait();
                    Assert.IsTrue(roles2Task.Result, "Role not found");

                    var userRemoveTask = manager.RemoveFromRoleAsync(user.Id, roleName);
                    userRemoveTask.Wait();
                    var rolesTask2 = manager.GetRolesAsync(user.Id);
                    rolesTask2.Wait();
                    Assert.IsFalse(rolesTask2.Result.Contains(roleName), "Role not removed.");

                    try
                    {
                        var t1 =  store.AddToRoleAsync(null, roleName);
                        t1.Wait();
                    }
                    catch (AggregateException aggex) {
                        aggex.ValidateAggregateException<ArgumentNullException>();
                    }

                    try
                    {
                        var t2 =  store.AddToRoleAsync(user, null);
                        t2.Wait();
                    }
                    catch (AggregateException aggex)
                    {
                        aggex.ValidateAggregateException<ArgumentNullException>();
                    }

                    try
                    {
                        var t3 =  store.AddToRoleAsync(user, Guid.NewGuid().ToString());
                        t3.Wait();
                    }
                    catch (AggregateException aggex)
                    {
                        aggex.ValidateAggregateException<ArgumentNullException>();
                    }

                    try
                    {
                        var t4 =  store.RemoveFromRoleAsync(null, roleName);
                        t4.Wait();
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        var t5 =  store.RemoveFromRoleAsync(user, null);
                        t5.Wait();
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        var t6 =  store.GetRolesAsync(null);
                        t6.Wait();
                    }
                    catch (ArgumentException) { }

                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore")]
        public void IsUserInRole()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>())
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var user = User;
                    WriteLineObject<IdentityUser>(user);
                    string roleName = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));

                    AddUserRoleHelper(manager, user, roleName);

                    DateTime start = DateTime.UtcNow;
                    var roles2Task = manager.IsInRoleAsync(user.Id, roleName);
                    roles2Task.Wait();
                    TestContext.WriteLine("IsInRoleAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
                    Assert.IsTrue(roles2Task.Result, "Role not found");

                   
                    try
                    {
                        store.IsInRoleAsync(null, roleName);
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.IsInRoleAsync(user, null);
                    }
                    catch (ArgumentException) { }
                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore")]
        public void AddUserClaim()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>(new IdentityCloudContext<IdentityUser>()))
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    WriteLineObject<IdentityUser>(User);
                    AddUserClaimHelper(manager, User, GenUserClaim());
                }
            }
        }

        private void AddUserClaimHelper(UserManager<IdentityUser> manager, IdentityUser user, Claim claim)
        {
            var userClaimTask = manager.AddClaimAsync(user.Id, claim);
            userClaimTask.Wait();
            Assert.IsTrue(userClaimTask.Result.Succeeded, string.Concat(userClaimTask.Result.Errors));
            var claimsTask = manager.GetClaimsAsync(user.Id);
            claimsTask.Wait();
            Assert.IsTrue(claimsTask.Result.Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore")]
        public void AddRemoveUserClaim()
        {
            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>(new IdentityCloudContext<IdentityUser>()))
            {
                using (UserManager<IdentityUser> manager = new UserManager<IdentityUser>(store))
                {
                    var user = User;
                    WriteLineObject<IdentityUser>(user);
                    Claim claim = GenAdminClaim();
                    var userClaimTask = manager.AddClaimAsync(user.Id, claim);
                    userClaimTask.Wait();
                    Assert.IsTrue(userClaimTask.Result.Succeeded, string.Concat(userClaimTask.Result.Errors));
                    var claimsTask = manager.GetClaimsAsync(user.Id);
                    claimsTask.Wait();
                    Assert.IsTrue(claimsTask.Result.Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");


                    var userRemoveClaimTask = manager.RemoveClaimAsync(user.Id, claim);
                    userRemoveClaimTask.Wait();
                    Assert.IsTrue(userClaimTask.Result.Succeeded, string.Concat(userClaimTask.Result.Errors));
                    var claimsTask2 = manager.GetClaimsAsync(user.Id);
                    claimsTask2.Wait();
                    Assert.IsTrue(!claimsTask2.Result.Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not removed");

                    try
                    {
                        var task = store.AddClaimAsync(null, claim);
                        task.Wait();
                    }
                    catch (AggregateException aggex) 
                    {
                        if(!(aggex.InnerException is ArgumentException))
                            throw;
                    }

                    try
                    {
                        store.AddClaimAsync(user, null);
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.RemoveClaimAsync(null, claim);
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.RemoveClaimAsync(user, null);
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.RemoveClaimAsync(user, new Claim(string.Empty, Guid.NewGuid().ToString()));
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.RemoveClaimAsync(user, new Claim(claim.Type, string.Empty));
                    }
                    catch (ArgumentException) { }

                    try
                    {
                        store.GetClaimsAsync(null);
                    }
                    catch (ArgumentException) { }
                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.UserStore")]
        public void ThrowIfDisposed()
        {
            UserStore<IdentityUser> store = new UserStore<IdentityUser>();
            store.Dispose();
            GC.Collect();
            try
            {
                var task = store.DeleteAsync(null);
            }
            catch (AggregateException agg)
            {
                agg.ValidateAggregateException<ArgumentException>();
            }
        }

    }
}
