// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Services;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElCamino.AspNet.Identity.DocumentDB.Model;
using Microsoft.Azure.Documents;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Collections.ObjectModel;

namespace ElCamino.AspNet.Identity.DocumentDB
{
    public class IdentityCloudContext : IdentityCloudContext<IdentityUser, IdentityRole, string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>
    {
        public IdentityCloudContext()
            : base()
        { }

        public IdentityCloudContext(string uri, string authKey, string database, ConnectionPolicy policy = null)
            : base(uri, authKey, database, policy)
        { }

    }

    public class IdentityCloudContext<TUser> : IdentityCloudContext<TUser, IdentityRole, string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim> where TUser : IdentityUser
    {
        public IdentityCloudContext()
            : base()
        { }

        public IdentityCloudContext(string uri, string authKey, string database, ConnectionPolicy policy = null)
            : base(uri, authKey, database, policy)
        {
        }

    }

    public class IdentityCloudContext<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim> : IDisposable
        where TUser : IdentityUser<TKey, TUserLogin, TUserRole, TUserClaim>
        where TRole : IdentityRole<TKey, TUserRole>
        where TUserLogin : IdentityUserLogin<TKey>
        where TUserRole : IdentityUserRole<TKey>
        where TUserClaim : IdentityUserClaim<TKey>
    {
        private DocumentClient _client = null;
        private Database _db = null;
        private DocumentCollection _roleDocumentCollection = new DocumentCollection() { Id = Constants.DocumentCollectionIds.RolesCollection };
        private DocumentCollection _userDocumentCollection = new DocumentCollection() { Id = Constants.DocumentCollectionIds.UsersCollection };
        private StoredProcedure _getUserByEmailSproc = null;
        private StoredProcedure _getUserByUserNameSproc = null;
        private StoredProcedure _getUserByIdSproc = null;
        private StoredProcedure _getUserByLoginSproc = null;

        public StoredProcedure GetUserByLoginSproc
        {
            get { return _getUserByLoginSproc; }
        }

        public StoredProcedure GetUserByIdSproc
        {
            get { return _getUserByIdSproc; }
        }

        public StoredProcedure GetUserByUserNameSproc
        {
            get { return _getUserByUserNameSproc; }
        }

        public StoredProcedure GetUserByEmailSproc
        {
            get { return _getUserByEmailSproc; }
        }
        private string _sessionToken = string.Empty;
        private bool _disposed = false;

        public IdentityCloudContext() :
            this(ConfigurationManager.AppSettings[Constants.AppSettingsKeys.DatabaseUriKey].ToString(),
            ConfigurationManager.AppSettings[Constants.AppSettingsKeys.DatabaseAuthKey].ToString(),
            ConfigurationManager.AppSettings[Constants.AppSettingsKeys.DatabaseNameKey].ToString(),
            null)
        {

        }

        public IdentityCloudContext(string uri, string authKey, string database, ConnectionPolicy policy = null)
        {
            _client = new DocumentClient(new Uri(uri), authKey, policy, ConsistencyLevel.Session);
            InitDatabase(database);
            InitCollections();
            InitStoredProcs();
        }

        private void InitDatabase(string database)
        {
            _db = _client.CreateDatabaseQuery().Where(d => d.Id == database).ToList().FirstOrDefault();
            if (_db == null)
            {
                var task = _client.CreateDatabaseAsync(new Database { Id = database });
                task.Wait();
                _db = task.Result;
            }
        }

        private void InitCollections()
        {
            Task[] tasks = new Task[2] {
            new TaskFactory().StartNew(() =>
                {
                    var uc = _client.CreateDocumentCollectionQuery(_db.CollectionsLink)
                        .Where(c => c.Id == Constants.DocumentCollectionIds.UsersCollection)
                        .ToList()
                        .FirstOrDefault();
                    if (uc == null)
                    {
                        _userDocumentCollection.IndexingPolicy.IndexingMode = IndexingMode.Lazy;
                        _userDocumentCollection.IndexingPolicy.IncludedPaths.Add(new IncludedPath()
                        {
                             Path = @"/",
                             Indexes= new Collection<Index>
                             {
                                 new HashIndex(DataType.String)
                             }
                        });
                        _userDocumentCollection.IndexingPolicy.IncludedPaths.Add(new IncludedPath()
                        {
                            Path = @"/""Email""/?",
                             Indexes= new Collection<Index>
                             {
                                 new HashIndex(DataType.String)
                             }
                        });
                        _userDocumentCollection.IndexingPolicy.IncludedPaths.Add(new IncludedPath()
                        {
                             Path = @"/""UserName""/?",
                             Indexes= new Collection<Index>
                             {
                                 new HashIndex(DataType.String)
                             }
                        });
                        _userDocumentCollection.IndexingPolicy.IncludedPaths.Add(new IncludedPath()
                        {
                            Path = @"/""UserId""/?",
                             Indexes= new Collection<Index>
                             {
                                 new HashIndex(DataType.String)
                             }
                        });
                        var ucTask = _client.CreateDocumentCollectionAsync(_db.SelfLink, _userDocumentCollection);
                        ucTask.Wait();
                        uc = ucTask.Result;
                    }
                    UserDocumentCollection = uc;
                }),
            new TaskFactory().StartNew(() =>
                {
                    var rc = _client.CreateDocumentCollectionQuery(_db.CollectionsLink)
                        .Where(c => c.Id == Constants.DocumentCollectionIds.RolesCollection)
                        .ToList()
                        .FirstOrDefault();
                    if (rc == null)
                    {
                        var rcTask = _client.CreateDocumentCollectionAsync(_db.SelfLink, _roleDocumentCollection);
                        rcTask.Wait();
                        rc = rcTask.Result;
                    }
                    RoleDocumentCollection = rc;
                })
            };
            Task.WaitAll(tasks);
        }

        private void InitStoredProcs()
        {
            InitGetUserByEmail();
            InitGetUserByUserName();
            InitGetUserById();
            InitGetUserByLogin();
        }

        private void InitGetUserByLogin()
        {
            string body = string.Empty;

            using (StreamReader sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "ElCamino.AspNet.Identity.DocumentDB.StoredProcs.getUserByLogin_sproc.js"), Encoding.UTF8))
            {
                body = sr.ReadToEnd();
            }
            string strId = "getUserByLogin_v1";
            _getUserByLoginSproc = _client.CreateStoredProcedureQuery(_userDocumentCollection.StoredProceduresLink,
                new FeedOptions() { SessionToken = SessionToken }).Where(s => s.Id == strId).ToList().FirstOrDefault();
            //if (_getUserByLoginSproc != null)
            //{
            //    var task = _client.DeleteStoredProcedureAsync(_getUserByLoginSproc.SelfLink,
            //        RequestOptions);
            //    task.Wait();
            //    _getUserByLoginSproc = null;
            //}
            if (_getUserByLoginSproc == null)
            {
                var task = _client.CreateStoredProcedureAsync(_userDocumentCollection.SelfLink,
                    new StoredProcedure()
                    {
                        Id = strId,
                        Body = body,
                    },
                    RequestOptions);
                task.Wait();
                _getUserByLoginSproc = task.Result;
            }
        }

        private void InitGetUserById()
        {
            string body = string.Empty;

            using (StreamReader sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "ElCamino.AspNet.Identity.DocumentDB.StoredProcs.getUserById_sproc.js"), Encoding.UTF8))
            {
                body = sr.ReadToEnd();
            }
            string strId = "getUserById_v1";
            _getUserByIdSproc = _client.CreateStoredProcedureQuery(_userDocumentCollection.StoredProceduresLink,
                new FeedOptions() { SessionToken = SessionToken }).Where(s => s.Id == strId).ToList().FirstOrDefault();
            //if (_getUserByIdSproc != null)
            //{
            //    var task = _client.DeleteStoredProcedureAsync(_getUserByIdSproc.SelfLink,
            //        RequestOptions);
            //    task.Wait();
            //    _getUserByIdSproc = null;
            //}
            if (_getUserByIdSproc == null)
            {
                var task = _client.CreateStoredProcedureAsync(_userDocumentCollection.SelfLink,
                    new StoredProcedure()
                    {
                        Id = strId,
                        Body = body,
                    },
                    RequestOptions);
                task.Wait();
                _getUserByIdSproc = task.Result;
            }
        }

        private void InitGetUserByUserName()
        {
            string body = string.Empty;

            using (StreamReader sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "ElCamino.AspNet.Identity.DocumentDB.StoredProcs.getUserByUserName_sproc.js"), Encoding.UTF8))
            {
                body = sr.ReadToEnd();
            }
            string strId = "getUserByUserName_v1";
            _getUserByUserNameSproc = _client.CreateStoredProcedureQuery(_userDocumentCollection.StoredProceduresLink,
                new FeedOptions() { SessionToken = SessionToken }).Where(s => s.Id == strId).ToList().FirstOrDefault();
            //if (_getUserByUserNameSproc != null)
            //{
            //    var task = _client.DeleteStoredProcedureAsync(_getUserByUserNameSproc.SelfLink,
            //        RequestOptions);
            //    task.Wait();
            //    _getUserByUserNameSproc = null;
            //}
            if (_getUserByUserNameSproc == null)
            {
                var task = _client.CreateStoredProcedureAsync(_userDocumentCollection.SelfLink,
                    new StoredProcedure()
                    {
                        Id = strId,
                        Body = body,
                    },
                    RequestOptions);
                task.Wait();
                _getUserByUserNameSproc = task.Result;
            }
        }

        private void InitGetUserByEmail()
        {
            string body = string.Empty;

            using (StreamReader sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "ElCamino.AspNet.Identity.DocumentDB.StoredProcs.getUserByEmail_sproc.js"), Encoding.UTF8))
            {
                body = sr.ReadToEnd();
            }
            string strId = "getUserByEmail_v1";
            _getUserByEmailSproc = _client.CreateStoredProcedureQuery(_userDocumentCollection.StoredProceduresLink,
                new FeedOptions() { SessionToken = SessionToken }).Where(s => s.Id == strId).ToList().FirstOrDefault();
            //if (_getUserByEmailSproc != null)
            //{
            //    var task = _client.DeleteStoredProcedureAsync(_getUserByEmailSproc.SelfLink,
            //        RequestOptions);
            //    task.Wait();
            //    _getUserByEmailSproc = null;
            //}
            if (_getUserByEmailSproc == null)
            {
                var task = _client.CreateStoredProcedureAsync(_userDocumentCollection.SelfLink,
                    new StoredProcedure()
                    {
                        Id = strId,
                        Body = body,
                    },
                    RequestOptions);
                task.Wait();
                _getUserByEmailSproc = task.Result;
            }
        }

        ~IdentityCloudContext()
        {
            this.Dispose(false);
        }

        public DocumentClient Client
        {
            get { ThrowIfDisposed(); return _client; }
        }

        public Database Database
        {
            get { ThrowIfDisposed(); return _db; }
        }


        public RequestOptions RequestOptions
        {
            get
            {
                return new RequestOptions()
                {
                    ConsistencyLevel = ConsistencyLevel.Session,
                    SessionToken = SessionToken
                };
            }
        }

        public string SessionToken
        {
            get { return _sessionToken; }
        }

        public void SetSessionTokenIfEmpty(string tokenNew)
        {
            if (string.IsNullOrWhiteSpace(_sessionToken))
            {
                _sessionToken = tokenNew;
            }
        }

        public DocumentCollection RoleDocumentCollection
        {
            get { ThrowIfDisposed(); return _roleDocumentCollection; }
            set { _roleDocumentCollection = value; }
        }

        public DocumentCollection UserDocumentCollection
        {
            get { ThrowIfDisposed(); return _userDocumentCollection; }
            set { _userDocumentCollection = value; }
        }

        private void ThrowIfDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(base.GetType().Name);
            }
        }
        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_client != null)
                {
                    _client.Dispose();
                }
                _client = null;
                _db = null;
                _disposed = true;
                _roleDocumentCollection = null;
                _userDocumentCollection = null;
                _sessionToken = null;
            }
        }
    }

}
