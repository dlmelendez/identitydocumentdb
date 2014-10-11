// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElCamino.AspNet.Identity.DocumentDB;
using ElCamino.AspNet.Identity.DocumentDB.Model;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Azure.Documents.Client;

namespace MigrateIdentityData
{
    class Program
    {
        private static DocumentCollection userCollection;
        private static int iUserSuccessConvert = 0;
        private static int iUserFailureConvert = 0;
        private static object ObjectLock = new object();
        private static ConcurrentBag<string> userIdFailures = new ConcurrentBag<string>();

        private static List<string> helpTokens = new List<string>() { "/?", "/help"};
        private static string previewToken = "/preview";
        private static string migrateToken = "/migrate";
        private static string nodeleteToken = "/nodelete";

        private static bool migrateOption = false;
        private static bool deleteOption = false;


        static void Main(string[] args)
        {
            if (!ValidateArgs(args))
            {
                return;
            }
            using (IdentityCloudContext<IdentityUser> ic = new IdentityCloudContext<IdentityUser>())
            {
                using (UserStore<IdentityUser> store = new UserStore<IdentityUser>(ic))
                {
                    userCollection = ic.Client.CreateDocumentCollectionQuery(ic.Database.CollectionsLink)
                        .Where(c => c.Id == Constants.DocumentCollectionIds.UsersCollection)
                        .ToList()
                        .FirstOrDefault();

                    if (userCollection != null)
                    {
                        List<Document> lroles = null;
                        List<Document> llogins = null;
                        List<Document> lclaims = null;
                        Dictionary<string, Document> lusers = null;

                        DateTime startLoad = DateTime.UtcNow;
                        var allDataList = new List<Document>(2000);

                        Task[] tasks = new Task[]{
                        new TaskFactory().StartNew(() =>
                            {
                                var qRoles = ic.Client.CreateDocumentQuery(userCollection.SelfLink,
                                    "SELECT VALUE r FROM root r WHERE r.RoleName != '' ",
                                        new Microsoft.Azure.Documents.Client.FeedOptions());
                                lroles = qRoles.ToList().Select(r => { Document d = ConvertDynamicToDoc(r); return d; }).ToList();
                                allDataList.AddRange(lroles);
                                Console.WriteLine("Roles to convert: {0}", lroles.Count);
                            }),
                        new TaskFactory().StartNew(()=>
                            {
                                var qLogins = ic.Client.CreateDocumentQuery(userCollection.SelfLink,
                                    "SELECT VALUE r FROM root r WHERE r.LoginProvider != '' ",
                                        new Microsoft.Azure.Documents.Client.FeedOptions());
                                llogins = qLogins.ToList().Select(r => { Document d = ConvertDynamicToDoc(r); return d; }).ToList();
                                allDataList.AddRange(llogins);
                                Console.WriteLine("Logins to convert: {0}", llogins.Count);
                            }),
                        new TaskFactory().StartNew(()=>
                            {
                                var qClaims = ic.Client.CreateDocumentQuery(userCollection.SelfLink,
                                    "SELECT VALUE r FROM root r WHERE r.ClaimType != '' ",
                                        new Microsoft.Azure.Documents.Client.FeedOptions());
                                lclaims = qClaims.ToList().Select(r => { Document d = ConvertDynamicToDoc(r); return d; }).ToList();
                                allDataList.AddRange(lclaims);
                                Console.WriteLine("Claims to convert: {0}", lclaims.Count);
                            }),
                        new TaskFactory().StartNew(()=>
                            {
                                var qUser = ic.Client.CreateDocumentQuery(userCollection.SelfLink,
                                    "SELECT VALUE r FROM root r WHERE r.id = r.UserId",
                                        new Microsoft.Azure.Documents.Client.FeedOptions());
                                lusers = qUser.ToList().Select(r => { Document d = ConvertDynamicToDoc(r); return d; }).ToDictionary(d=> d.Id);
                                Console.WriteLine("Total Users: {0}", lusers.Count);
                            })
                        };

                        Task.WaitAll(tasks);
                        Console.WriteLine("Load Roles, Claims, Logins and Users: {0} seconds", (DateTime.UtcNow - startLoad).TotalSeconds);

                        List<string> userIds = allDataList.Select(dl=> dl.GetPropertyValue<string>("UserId")).Distinct().ToList();
                        var result2 = Parallel.ForEach<string>(userIds, (userId) =>
                        {
                            Document user;
                            if (!lusers.TryGetValue(userId, out user))
                            {
                                Console.WriteLine("User document not found: {0}", userId);
                                return;
                            }
                            //Get all of the docs with the same UserId
                            Task<TempUser> tempTask = CreateTempUser(user, allDataList.Where(d => d.GetPropertyValue<string>("UserId") == user.Id).ToList());
                            tempTask.Wait();

                            TempUser temp = tempTask.Result;
                            if (temp.Roles.Count > 0
                                || temp.Claims.Count > 0
                                || temp.Logins.Count > 0)
                            {
                                if (migrateOption)
                                {
                                    ConvertUser(temp, ic, deleteOption).ContinueWith((tu) =>
                                    {
                                        return ConfirmUserConvert(tu.Result, store);
                                    }).Wait();
                                }
                            }
                        });

                        Console.WriteLine("");
                        Console.WriteLine("Elapsed time: {0} seconds", (DateTime.UtcNow - startLoad).TotalSeconds);
                        Console.WriteLine("Total Users To Convert: {0}", userIds.Count);

                        Console.WriteLine("");
                        if (migrateOption)
                        {
                            Console.WriteLine("Total Users Successfully Converted: {0}", iUserSuccessConvert);
                            Console.WriteLine("Total Users Failed to Convert: {0}", iUserFailureConvert);
                            if (iUserFailureConvert > 0)
                            {
                                Console.WriteLine("User Ids Failed:");
                                foreach (string s in userIdFailures)
                                {
                                    Console.WriteLine(s);
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Cannot find UserCollection. Check app.config appSettings for correct DocumentDB connection. If correct, no migration needed.");
                    }

                }
            }

            DisplayAnyKeyToExit();

        }
        private static Document ConvertDynamicToDoc(dynamic d)
        {
            string strJson = d.ToString();
            byte[] bytes = Encoding.UTF8.GetBytes(strJson);
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                ms.Position = 0;
                Document doc = Document.LoadFrom<Document>(ms);
                return doc;
            }
        }

        private static void DisplayAnyKeyToExit()
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static bool ValidateArgs(string[] args)
        {
            if (args.Length == 0 || args.Any(a => helpTokens.Any(h => h.Equals(a, StringComparison.OrdinalIgnoreCase))))
            {
                DisplayHelp();
                return false;
            }
            else
            {
                List<string> nonHelpTokens = new List<string>() { previewToken, migrateToken, nodeleteToken };
                if (!args.All(a => nonHelpTokens.Any(h => h.Equals(a, StringComparison.OrdinalIgnoreCase))))
                {
                    DisplayInvalidArgs(args.Where(a => !nonHelpTokens.Any(h => h.Equals(a, StringComparison.OrdinalIgnoreCase))).ToList());
                    return false;
                }
                bool isPreview = args.Any(a => a.Equals(previewToken, StringComparison.OrdinalIgnoreCase));
                bool isMigrate = args.Any(a => a.Equals(migrateToken, StringComparison.OrdinalIgnoreCase));
                if (isPreview && isMigrate)
                {
                    DisplayInvalidArgs(new List<string>() { previewToken, migrateToken, "Cannot define /preview and /migrate. Only one can be used." });
                    return false;
                }
                bool isNoDelete = args.Any(a => a.Equals(nodeleteToken, StringComparison.OrdinalIgnoreCase));
                if (isNoDelete && !isMigrate)
                {
                    DisplayInvalidArgs(new List<string>() { nodeleteToken, "/nodelete must be used with /migrate option." });
                    return false;
                }

                migrateOption = isMigrate;
                deleteOption = isMigrate && !isNoDelete;

                return true;
            }
        }

        private static void DisplayInvalidArgs(List<string> args)
        {
            if (args != null && args.Count > 0)
            {
                foreach (string a in args)
                {
                    Console.WriteLine("Invalid argument: {0}.", a);
                }
            }
            else
            {
                Console.WriteLine("Invalid argument(s).");
            }

            DisplayAnyKeyToExit();
        }
        private static void DisplayHelp()
        {
            Console.WriteLine("MigrateIdentityData.exe ");
            Console.WriteLine("Migrates or previews the non-user documents merged into the user subdocuments to be used in the current schema of the ElCamino.AspNet.Identity.DocumentDB provider. Make sure the MigrateIdentityData.exe.config has the correct DocumentDB connection information.");

            Console.WriteLine("noargs, /help or /? - shows this message.");
            Console.WriteLine("e.g. MigrateIdentityData.exe /?");
            Console.WriteLine("");
            Console.WriteLine("/preview - no data is modified. Shows how many user documents will be affected.");
            Console.WriteLine("e.g. MigrateIdentityData.exe /preview");
            Console.WriteLine("");
            Console.WriteLine("/migrate - migrates documents to the current schema. By default, will delete the old documents unless /nodelete is specified.");
            Console.WriteLine("e.g. MigrateIdentityData.exe /migrate");
            Console.WriteLine("");
            Console.WriteLine("/nodelete - must be used with /migrate. Will not delete non-user documents after they are merged into the user document.");
            Console.WriteLine("e.g. MigrateIdentityData.exe /migrate /nodelete");
            DisplayAnyKeyToExit();
        }

        private static void ProcessUser(ref TempUser temp)
        {
            lock (ObjectLock)
            {
                Console.WriteLine(migrateOption ? "Processing User: {0}" : "Analyzing User: {0}", temp.User.Id);
                Console.WriteLine("Roles: {0}, Claims: {1}, Logins: {2}", temp.Roles.Count, temp.Claims.Count, temp.Logins.Count);

                if (temp.Roles.Count > 0)
                {
                    var newRoles = temp.Roles.Select(r =>
                        {
                            var item = new IdentityUserRole();
                            JsonConvert.PopulateObject(r.ToString(), item);
                            return item;
                        }).ToList();

                    var eRoles = temp.User.GetPropertyValue<List<IdentityUserRole>>("Roles"); 
                    if (eRoles != null)
                    {
                        eRoles.AddRange(newRoles.Where(n => !eRoles.Any(er => er.Id == n.Id)));
                        temp.User.SetPropertyValue("Roles", eRoles.ToArray());
                    }
                    else
                    {
                        temp.User.SetPropertyValue("Roles", newRoles.ToArray());
                    }
                }
                if (temp.Claims.Count > 0)
                {
                    var newClaims = temp.Claims.Select(r =>
                    {
                        var item = new IdentityUserClaim();
                        JsonConvert.PopulateObject(r.ToString(), item);
                        return item;
                    }).ToList();

                    var eClaims = temp.User.GetPropertyValue<List<IdentityUserClaim>>("Claims"); 
                    if (eClaims != null)
                    {
                        eClaims.AddRange(newClaims.Where(n => !eClaims.Any(er => er.Id == n.Id)));
                        temp.User.SetPropertyValue("Claims", eClaims.ToArray());
                    }
                    else
                    {
                        temp.User.SetPropertyValue("Claims", newClaims.ToArray());
                    }

                }
                if (temp.Logins.Count > 0)
                {
                    var newLogins = temp.Logins.Select(r =>
                    {
                        var item = new IdentityUserLogin();
                        JsonConvert.PopulateObject(r.ToString(), item);
                        return item;
                    }).ToList();

                    var eLogins = temp.User.GetPropertyValue<List<IdentityUserLogin>>("Logins"); 
                    if (eLogins != null)
                    {
                        eLogins.AddRange(newLogins.Where(n => !eLogins.Any(er => er.Id == n.Id)));
                        temp.User.SetPropertyValue("Logins", eLogins.ToArray());
                    }
                    else
                    {
                        temp.User.SetPropertyValue("Logins", newLogins.ToArray());
                    }

                }
            }
        }
        private static async Task<TempUser> CreateTempUser(Document uDoc, List<Document> allUserList)
        {
            TempUser temp = new TempUser();
            temp.User = uDoc;
            Task[] tasks = new Task[] { 
                new TaskFactory().StartNew(() => 
                    {
                        temp.Roles = 
                            allUserList.Where(r => r.GetPropertyValue<string>("UserId") == uDoc.Id && r.Id.StartsWith(Constants.RowKeyConstants.PreFixIdentityUserRole)).ToList();
                    }),
                new TaskFactory().StartNew(() =>
                    {
                        temp.Claims = 
                            allUserList.Where(r => r.GetPropertyValue<string>("UserId") == uDoc.Id && r.Id.StartsWith(Constants.RowKeyConstants.PreFixIdentityUserClaim)).ToList();
                    }),
                new TaskFactory().StartNew(() =>
                    {
                        temp.Logins = 
                            allUserList.Where(r => r.GetPropertyValue<string>("UserId") == uDoc.Id && r.Id.StartsWith(Constants.RowKeyConstants.PreFixIdentityUserLogin)).ToList();
                    })};
            await Task.WhenAll(tasks);
            ProcessUser(ref temp);
            return temp;
        }

        private static async Task<TempUser> ConvertUser(TempUser user, IdentityCloudContext<IdentityUser> ic, bool deleteDocuments)
        {
            var response = await ic.Client.ReplaceDocumentAsync(user.User, new Microsoft.Azure.Documents.Client.RequestOptions()
                {
                     SessionToken = ic.SessionToken
                });
            ic.SetSessionTokenIfEmpty(response.SessionToken);

            if (deleteDocuments)
            {
                foreach (var r in user.Roles)
                {
                    await ic.Client.DeleteDocumentAsync(r.SelfLink);
                }

                foreach (var c in user.Claims)
                {
                    await ic.Client.DeleteDocumentAsync(c.SelfLink);
                }

                foreach (var l in user.Logins)
                {
                    await ic.Client.DeleteDocumentAsync(l.SelfLink);
                }
            }

            return user;
        }

        private static async Task ConfirmUserConvert(TempUser user, UserStore<IdentityUser> store)
        {
            var userFound = await store.FindByIdAsync(user.User.Id);

            if (userFound.Roles.Where(r => user.Roles.Any(ur => ur.Id == r.Id)).Count() == user.Roles.Count
                && userFound.Claims.Where(c => user.Claims.Any(uc => uc.Id == c.Id)).Count() == user.Claims.Count
                && userFound.Logins.Where(l => user.Logins.Any(ul => ul.Id == l.Id)).Count() == user.Logins.Count)
            {
                Interlocked.Increment(ref iUserSuccessConvert);
                Console.WriteLine("Completed User: {0}", user.User.Id);
            }
            else
            {
                Interlocked.Increment(ref iUserFailureConvert);
                userIdFailures.Add(user.User.Id);
            }
        }
    }
}
