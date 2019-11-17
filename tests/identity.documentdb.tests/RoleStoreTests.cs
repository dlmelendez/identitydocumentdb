// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ElCamino.AspNet.Identity.DocumentDB;
using Microsoft.AspNet.Identity;
using ElCamino.AspNet.Identity.DocumentDB.Model;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.Azure.Documents.Linq;
using System.Linq;
using System.Threading;
using System.Configuration;
using System.Threading.Tasks;

namespace ElCamino.AspNet.Identity.DocumentDB.Tests
{
    [TestClass]
    public class RoleStoreTests
    {
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

        [TestInitialize]
        public void Initialize()
        {
        }

        [TestMethod]
        [TestCategory("Identity.Azure.RoleStore")]
        public void RoleStoreCtors()
        {
            try
            {
                new RoleStore<IdentityRole>(null);
            }
            catch (ArgumentException) { }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.RoleStore")]
        public void CreateRoleScratch()
        {
            Guid id = Guid.NewGuid();
            using (IdentityCloudContext context = new IdentityCloudContext())
            {
                var doc = new { id = id.ToString(), SpiderMonkey = "Monkey baby" };
                var docTask = context.Client.CreateDocumentAsync(context.RoleDocumentCollection.SelfLink,
                    doc, context.RequestOptions);
                docTask.Wait();
                var docResult = docTask.Result;
                Debug.WriteLine(docResult.Resource.ToString());
                

                var docQrTask = context.Client.CreateDocumentQuery(context.RoleDocumentCollection.DocumentsLink
                    , new Microsoft.Azure.Documents.Client.FeedOptions() { SessionToken = context.SessionToken, MaxItemCount = 1 })
                    .Where(d => d.Id == doc.id)
                    .Select(s => s)
                    .ToList()
                    .FirstOrDefault();
                Debug.WriteLine(docQrTask.ToString());


            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.RoleStore")]
        public async Task CreateRoleSameCollection()
        {
            const string sameCol = "ur";
            //var ic = new IdentityCloudContext<IdentityUser>(sameCol, sameCol);

            using (RoleStore<IdentityRole> store = new RoleStore<IdentityRole>(new IdentityCloudContext(sameCol, sameCol)))
            {
                using (RoleManager<IdentityRole> manager = new RoleManager<IdentityRole>(store))
                {
                    Assert.IsNotNull(store.Context.Database);
                    var role = CreateRoleHelper(manager);
                    WriteLineObject<IdentityRole>(role);

                    var result = await store.Context.Client.DeleteDocumentCollectionAsync(store.Context.UserDocumentCollection.SelfLink);

                }
            }

        }

        [TestMethod]
        [TestCategory("Identity.Azure.RoleStore")]
        public void CreateQueryRoles()
        {

            using (RoleStore<IdentityRole> store = new RoleStore<IdentityRole>())
            {
                using (RoleManager<IdentityRole> manager = new RoleManager<IdentityRole>(store))
                {
                    var role = CreateRoleHelper(manager);
                    WriteLineObject<IdentityRole>(role);

                    var r = manager.Roles.AsQueryable().Where(o => o.Name == role.Name).ToList().FirstOrDefault();
                    Assert.IsNotNull(r);

                }
            }

        }

        [TestMethod]
        [TestCategory("Identity.Azure.RoleStore")]
        public void CreateRole()
        {    
            using (RoleStore<IdentityRole> store = new RoleStore<IdentityRole>())
            {
                using (RoleManager<IdentityRole> manager = new RoleManager<IdentityRole>(store))
                {
                    var role = CreateRoleHelper(manager);
                    WriteLineObject<IdentityRole>(role);

                    try
                    {
                        var task = store.CreateAsync(null);
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Assert.IsNotNull(ex, "Argument exception not raised");
                    }
                }
            }
        }

        private IdentityRole CreateRoleHelper(RoleManager<IdentityRole> manager )
        {
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
            var role = new IdentityRole(roleNew);
            var createTask = manager.CreateAsync(role);
            createTask.Wait();
            return role;
        }

        [TestMethod]
        [TestCategory("Identity.Azure.RoleStore")]
        public void ThrowIfDisposed()
        {
            using (RoleStore<IdentityRole> store = new RoleStore<IdentityRole>())
            {
                RoleManager<IdentityRole> manager = new RoleManager<IdentityRole>(store);
                manager.Dispose();

                try
                {
                    var task = store.DeleteAsync(null);
                }
                catch (ArgumentException) { }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.RoleStore")]
        public void UpdateRole()
        {
            using (RoleStore<IdentityRole> store = new RoleStore<IdentityRole>())
            {
                using (RoleManager<IdentityRole> manager = new RoleManager<IdentityRole>(store))
                {
                    string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());

                    var role = new IdentityRole(roleNew);
                    var createTask = manager.CreateAsync(role);
                    createTask.Wait();

                    role.Name = Guid.NewGuid() + role.Name;
                    var updateTask = manager.UpdateAsync(role);
                    updateTask.Wait();

                    var findTask = manager.FindByIdAsync(role.Id);

                    Assert.IsNotNull(findTask.Result, "Find Role Result is null");
                    //Assert.AreEqual<string>(role.RowKey, findTask.Result.RowKey, "RowKeys don't match.");
                    Assert.AreNotEqual<string>(roleNew, findTask.Result.Name, "Name not updated.");

                    try
                    {
                        var task = store.UpdateAsync(null);
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
        [TestCategory("Identity.Azure.RoleStore")]
        public void UpdateRole2()
        {
            using (RoleStore<IdentityRole> store = new RoleStore<IdentityRole>())
            {
                using (RoleManager<IdentityRole> manager = new RoleManager<IdentityRole>(store))
                {
                    string roleNew = string.Format("{0}_TestRole", Guid.NewGuid());

                    var role = new IdentityRole(roleNew);
                    var createTask = manager.CreateAsync(role);
                    createTask.Wait();

                    role.Name = role.Name + Guid.NewGuid();
                    var updateTask = manager.UpdateAsync(role);
                    updateTask.Wait();

                    var findTask = manager.FindByIdAsync(role.Id);
                    findTask.Wait();
                    Assert.IsNotNull(findTask.Result, "Find Role Result is null");
                    Assert.AreEqual<string>(role.Id, findTask.Result.Id, "RowKeys don't match.");
                    Assert.AreNotEqual<string>(roleNew, findTask.Result.Name, "Name not updated.");
                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.RoleStore")]
        public void DeleteRole()
        {
            using (RoleStore<IdentityRole> store = new RoleStore<IdentityRole>())
            {
                using (RoleManager<IdentityRole> manager = new RoleManager<IdentityRole>(store))
                {
                    string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
                    var role = new IdentityRole(roleNew);
                    var createTask = manager.CreateAsync(role);
                    createTask.Wait();

                    var delTask = manager.DeleteAsync(role);
                    delTask.Wait();

                    var findTask = manager.FindByIdAsync(role.Id);
                    findTask.Wait();
                    Assert.IsNull(findTask.Result, "Role not deleted ");

                    try
                    {
                        var task = store.DeleteAsync(null);
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
        [TestCategory("Identity.Azure.RoleStore")]
        public void FindRoleById()
        {
            using (RoleStore<IdentityRole> store = new RoleStore<IdentityRole>(new IdentityCloudContext(
                ConfigurationManager.AppSettings[ElCamino.AspNet.Identity.DocumentDB.Constants.AppSettingsKeys.DatabaseUriKey].ToString(),
            ConfigurationManager.AppSettings[ElCamino.AspNet.Identity.DocumentDB.Constants.AppSettingsKeys.DatabaseAuthKey].ToString(),
            ConfigurationManager.AppSettings[ElCamino.AspNet.Identity.DocumentDB.Constants.AppSettingsKeys.DatabaseNameKey].ToString(),
            null)))
            {
                using (RoleManager<IdentityRole> manager = new RoleManager<IdentityRole>(store))
                {
                    DateTime start = DateTime.UtcNow;
                    var role = CreateRoleHelper(manager);
                    var findTask = manager.FindByIdAsync(role.Id);
                    findTask.Wait();
                    TestContext.WriteLine("FindByIdAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

                    Assert.IsNotNull(findTask.Result, "Find Role Result is null");
                    WriteLineObject<IdentityRole>(findTask.Result);
                    Assert.AreEqual<string>(role.Id, findTask.Result.Id, "Role Ids don't match.");
                }
            }
        }

        [TestMethod]
        [TestCategory("Identity.Azure.RoleStore")]
        public void FindRoleByName()
        {
            using (RoleStore<IdentityRole> store = new RoleStore<IdentityRole>())
            {
                using (RoleManager<IdentityRole> manager = new RoleManager<IdentityRole>(store))
                {
                    var role = CreateRoleHelper(manager);
                    DateTime start = DateTime.UtcNow;
                    var findTask = manager.FindByNameAsync(role.Name);
                    findTask.Wait();
                    TestContext.WriteLine("FindByNameAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

                    Assert.IsNotNull(findTask.Result, "Find Role Result is null");
                    Assert.AreEqual<string>(role.Name, findTask.Result.Name, "Role names don't match.");
                }
            }
        }

        private void WriteLineObject<t>(t obj) where t : class
        {
            TestContext.WriteLine(typeof(t).Name);
            string strLine = obj == null ? "Null" : Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            TestContext.WriteLine("{0}", strLine);
        }

    }
}
