// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using ElCamino.AspNet.Identity.DocumentDB.Model;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ElCamino.AspNet.Identity.DocumentDB.Helpers;
using System.Diagnostics;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace ElCamino.AspNet.Identity.DocumentDB
{
    public class UserStore<TUser> : UserStore<TUser, IdentityRole, string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>, IUserStore<TUser>, IUserStore<TUser, string> where TUser : IdentityUser, new()
    {
        public UserStore()
            : this(new IdentityCloudContext<TUser>())
        {

        }

        public UserStore(IdentityCloudContext<TUser> context)
            : base(context)
        {
        }

        //Fixing code analysis issue CA1063
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    public class UserStore<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim> : IUserLoginStore<TUser, TKey>
        , IUserClaimStore<TUser, TKey>
        , IUserRoleStore<TUser, TKey>, IUserPasswordStore<TUser, TKey>
        , IUserSecurityStampStore<TUser, TKey>, IQueryableUserStore<TUser, TKey>
        , IUserEmailStore<TUser, TKey>, IUserPhoneNumberStore<TUser, TKey>
        , IUserTwoFactorStore<TUser, TKey>
        , IUserLockoutStore<TUser, TKey>
        , IUserStore<TUser, TKey>
        , IDisposable
        where TUser : IdentityUser<TKey, TUserLogin, TUserRole, TUserClaim>, new()
        where TRole : IdentityRole<TKey, TUserRole>, new()
        where TKey : IEquatable<TKey>
        where TUserLogin : IdentityUserLogin<TKey>, new()
        where TUserRole : IdentityUserRole<TKey>, new()
        where TUserClaim : IdentityUserClaim<TKey>, new()
    {
        private bool _disposed;
        private IQueryable<TUser> _users;


        public UserStore(IdentityCloudContext<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim> context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            this.Context = context;
            this._users = Context.Client.CreateDocumentQuery<TUser>(Context.UserDocumentCollection.DocumentsLink,
                    new FeedOptions() { SessionToken = Context.SessionToken });
        }

        //public async Task CreateTablesIfNotExists()
        //{
        //    await new TaskFactory().StartNew(() =>
        //    {
        //        Task<bool>[] tasks = new Task<bool>[] 
        //            { 
        //                Context.RoleTable.CreateIfNotExistsAsync(),
        //                Context.UserTable.CreateIfNotExistsAsync(),
        //                Context.IndexTable.CreateIfNotExistsAsync(),
        //            };
        //        Task.WaitAll(tasks);
        //    });
        //}

        public virtual Task AddClaimAsync(TUser user, Claim claim)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (claim == null)
            {
                throw new ArgumentNullException("claim");
            }
            return new TaskFactory().StartNew(() =>
                {
                    TUserClaim item = Activator.CreateInstance<TUserClaim>();
                    item.UserId = user.Id;
                    item.ClaimType = claim.Type;
                    item.ClaimValue = claim.Value;
                    ((IGenerateKeys)item).GenerateKeys();
                    
                    user.Claims.Add(item);

                    var docTask = Context.Client.CreateDocumentAsync(Context.UserDocumentCollection.DocumentsLink, item
                               , Context.RequestOptions, true);
                    docTask.Wait();
                    var doc = docTask.Result;
                    Context.SetSessionTokenIfEmpty(doc.SessionToken);
                    JsonConvert.PopulateObject(doc.Resource.ToString(), item);
               
                });
        }

        public virtual Task AddLoginAsync(TUser user, UserLoginInfo login)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (login == null)
            {
                throw new ArgumentNullException("login");
            }
            return new TaskFactory().StartNew(() =>
            {
                TUserLogin item = Activator.CreateInstance<TUserLogin>();
                item.UserId = user.Id;
                item.ProviderKey = login.ProviderKey;
                item.LoginProvider = login.LoginProvider;
                ((IGenerateKeys)item).GenerateKeys();

                var docTask = Context.Client.CreateDocumentAsync(Context.UserDocumentCollection.DocumentsLink, item
                           , Context.RequestOptions, true);
                docTask.Wait();
                var doc = docTask.Result;
                Context.SetSessionTokenIfEmpty(doc.SessionToken);
                JsonConvert.PopulateObject(doc.Resource.ToString(), item);
            });

        }

        public virtual Task AddToRoleAsync(TUser user, string roleName)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, "roleName");
            }

            return new TaskFactory().StartNew(() =>
                {
                    TRole roleT = Activator.CreateInstance<TRole>();
                    roleT.Name = roleName;
                    ((IGenerateKeys)roleT).GenerateKeys();

                    TUserRole userToRole = Activator.CreateInstance<TUserRole>();
                    userToRole.UserId = user.Id;
                    userToRole.RoleId = roleT.Id;
                    userToRole.RoleName = roleT.Name;
                    TUserRole item = userToRole;

                    ((IGenerateKeys)item).GenerateKeys();

                    user.Roles.Add(item);
                    roleT.Users.Add(item);

                    var docTask = Context.Client.CreateDocumentAsync(Context.UserDocumentCollection.DocumentsLink, item
                               , Context.RequestOptions, true);
                    docTask.Wait();
                    var doc = docTask.Result;
                    Context.SetSessionTokenIfEmpty(doc.SessionToken);
                    JsonConvert.PopulateObject(doc.Resource.ToString(), item);
                });

        }

        public async virtual Task CreateAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            ((IGenerateKeys)user).GenerateKeys();

            await new TaskFactory().StartNew(() =>
            {
                var docTask = Context.Client.CreateDocumentAsync(Context.UserDocumentCollection.DocumentsLink, user
                    , Context.RequestOptions, true);
                docTask.Wait();
                var doc = docTask.Result;
                Context.SetSessionTokenIfEmpty(doc.SessionToken);
                JsonConvert.PopulateObject(doc.Resource.ToString(), user);
            });

        }

        public async virtual Task DeleteAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            List<Task> tasks = new List<Task>(100);

            tasks.Add(Context.Client.DeleteDocumentAsync(user.SelfLink,
                            Context.RequestOptions));

            foreach (var userLogin in user.Logins)
            {
                tasks.Add(Context.Client.DeleteDocumentAsync(userLogin.SelfLink,
                            Context.RequestOptions));
            }

            foreach (var userRole in user.Roles)
            {
                tasks.Add(Context.Client.DeleteDocumentAsync(userRole.SelfLink,
                            Context.RequestOptions));
            }

            foreach (var userClaim in user.Claims)
            {
                tasks.Add(Context.Client.DeleteDocumentAsync(userClaim.SelfLink,
                            Context.RequestOptions));
            }

            await Task.WhenAll(tasks.ToArray());

        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (this.Context != null)
                {
                    this.Context.Dispose();
                }
                this._users = null;
                this.Context = null;
                this._disposed = true;
            }
        }

        public async virtual Task<TUser> FindAsync(UserLoginInfo login)
        {
            ThrowIfDisposed();
            if (login == null)
            {
                throw new ArgumentNullException("login");
            }

            string loginId = login.GenerateRowKeyUserLoginInfo();

            var task = Context.Client.ExecuteStoredProcedureAsync<IEnumerable<dynamic>>(Context.GetUserByLoginSproc.SelfLink,
                    new dynamic[] { loginId });
            task.Wait();
            if (task.Result.Response != null)
            {
                return await Task.FromResult<TUser>(GetUserAggregate(task.Result.Response.ToList()));
            }
            return null;

        }

        public async Task<TUser> FindByEmailAsync(string plainEmail)
        {
            var task = Context.Client.ExecuteStoredProcedureAsync<IEnumerable<dynamic>>(Context.GetUserByEmailSproc.SelfLink,
               new dynamic[] { plainEmail });
            task.Wait();
            if (task.Result.Response != null)
            {
                return await Task.FromResult<TUser>(GetUserAggregate(task.Result.Response.ToList()));
            }
            return null;
        }

        public virtual Task<TUser> FindByIdAsync(TKey userId)
        {
            this.ThrowIfDisposed();
            return FindByIdAsync(userId.ToString());
        }

        private Task<TUser> FindByIdAsync(string userId)
        {
            this.ThrowIfDisposed();
            return this.GetUserAggregateAsync(userId);
        }


        public async virtual Task<TUser> FindByNameAsync(string userName)
        {
            this.ThrowIfDisposed();
            var task = Context.Client.ExecuteStoredProcedureAsync<IEnumerable<dynamic>>(Context.GetUserByUserNameSproc.SelfLink,
                    new dynamic[] { userName });
            task.Wait();
            if (task.Result.Response != null)
            {
                return await Task.FromResult<TUser>(GetUserAggregate(task.Result.Response.ToList()));
            }
            return null;
        }

        public Task<int> GetAccessFailedCountAsync(TUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult<int>(user.AccessFailedCount);
        }

        public virtual Task<IList<Claim>> GetClaimsAsync(TUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult<IList<Claim>>(user.Claims.Select(c => new Claim(c.ClaimType, c.ClaimValue)).ToList());
        }

        public Task<string> GetEmailAsync(TUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult<string>(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(TUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult<bool>(user.EmailConfirmed);
        }

        public Task<bool> GetLockoutEnabledAsync(TUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult<bool>(user.LockoutEnabled);
        }

        public Task<DateTimeOffset> GetLockoutEndDateAsync(TUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult<DateTimeOffset>(user.LockoutEndDateUtc.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(user.LockoutEndDateUtc.Value, DateTimeKind.Utc)) : new DateTimeOffset());
        }

        public virtual Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult<IList<UserLoginInfo>>((from l in user.Logins select new UserLoginInfo(l.LoginProvider, l.ProviderKey)).ToList<UserLoginInfo>());
        }

        public Task<string> GetPasswordHashAsync(TUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult<string>(user.PasswordHash);
        }

        public Task<string> GetPhoneNumberAsync(TUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult<string>(user.PhoneNumber);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(TUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult<bool>(user.PhoneNumberConfirmed);
        }

        public virtual Task<IList<string>> GetRolesAsync(TUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            return Task.FromResult<IList<string>>(user.Roles.ToList().Select(r => r.RoleName).ToList());
        }

        public Task<string> GetSecurityStampAsync(TUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult<string>(user.SecurityStamp);
        }

        public Task<bool> GetTwoFactorEnabledAsync(TUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult<bool>(user.TwoFactorEnabled);
        }

        private  async Task<TUser> GetUserAggregateAsync(string userId)
        {
            var task = Context.Client.ExecuteStoredProcedureAsync<IEnumerable<dynamic>>(Context.GetUserByIdSproc.SelfLink,
                new dynamic[] { userId });
            task.Wait();
            if (task.Result.Response != null)
            {
                return await Task.FromResult<TUser>(GetUserAggregate(task.Result.Response.ToList()));
            }
            return null;

        }

        private TUser GetUserAggregate(List<dynamic> userResults)
        {
            TUser user = default(TUser);
            var vUser = userResults.Where(u => u.id == u.UserId).SingleOrDefault();

            if (vUser != null)
            {
                //User
                user = Activator.CreateInstance<TUser>();
                JsonConvert.PopulateObject(vUser.ToString(), user);

                Task[] tasks = new Task[]
                    { 
                        new TaskFactory().StartNew(()=>
                        {
                            //Roles
                            userResults.Where(u => u.id.ToString().StartsWith(Constants.RowKeyConstants.PreFixIdentityUserRole))
                                .ToList()
                                .ForEach(log =>
                                {
                                    TUserRole trole = Activator.CreateInstance<TUserRole>();
                                    JsonConvert.PopulateObject(log.ToString(), trole);
                                    user.Roles.Add(trole);
                                });
                        }),
                        new TaskFactory().StartNew(()=>
                        {
                            //Claims
                            userResults.Where(u => u.id.ToString().StartsWith(Constants.RowKeyConstants.PreFixIdentityUserClaim))
                                .ToList()
                                .ForEach(log =>
                                {
                                    TUserClaim tclaim = Activator.CreateInstance<TUserClaim>();
                                    JsonConvert.PopulateObject(log.ToString(), tclaim);
                                    user.Claims.Add(tclaim);
                                });
                            }),
                        new TaskFactory().StartNew(()=>
                        {
                            //Logins
                            userResults.Where(u => u.id.ToString().StartsWith(Constants.RowKeyConstants.PreFixIdentityUserLogin))
                                .ToList()
                                .ForEach(log =>
                                {
                                    TUserLogin tlogin = Activator.CreateInstance<TUserLogin>();
                                    JsonConvert.PopulateObject(log.ToString(), tlogin);
                                    user.Logins.Add(tlogin);
                                });
                         })
                    };
                Task.WaitAll(tasks);
            }
            return user;
        }


        public Task<bool> HasPasswordAsync(TUser user)
        {
            return Task.FromResult<bool>(user.PasswordHash != null);
        }

        public Task<int> IncrementAccessFailedCountAsync(TUser user)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            user.AccessFailedCount++;
            return Task.FromResult<int>(user.AccessFailedCount);
        }

        public virtual Task<bool> IsInRoleAsync(TUser user, string roleName)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, "roleName");
            }

            //Removing the live query. UserManager calls FindById to hydrate the user object first.
            //No need to go to the table again.
            return Task.FromResult<bool>(user.Roles.Any(r => r.Id == KeyHelper.GenerateRowKeyIdentityRole(roleName)));
        }

        public virtual Task RemoveClaimAsync(TUser user, Claim claim)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (claim == null)
            {
                throw new ArgumentNullException("claim");
            }

            if (string.IsNullOrWhiteSpace(claim.Type))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, "claim.Type");
            }

            if (string.IsNullOrWhiteSpace(claim.Value))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, "claim.Value");
            }

            return new TaskFactory().StartNew(() =>
                {
                    TUserClaim local = user.Claims.FirstOrDefault(uc => uc.Id == KeyHelper.GenerateRowKeyIdentityUserClaim(claim.Type, claim.Value));
                    {
                        user.Claims.Remove(local);
                        var delTask = Context.Client.DeleteDocumentAsync(local.SelfLink,
                            Context.RequestOptions);
                        delTask.Wait();
                        Context.SetSessionTokenIfEmpty(delTask.Result.SessionToken);
                    }

                });

        }

        public virtual Task RemoveFromRoleAsync(TUser user, string roleName)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException(IdentityResources.ValueCannotBeNullOrEmpty, "roleName");
            }
            return new TaskFactory().StartNew(() =>
            {
                TUserRole item = user.Roles.FirstOrDefault<TUserRole>(r => r.RoleName.ToUpper() == roleName.ToUpper());
                if (item != null)
                {
                    user.Roles.Remove(item);
                    var delTask = Context.Client.DeleteDocumentAsync(item.SelfLink,
                        Context.RequestOptions);
                    delTask.Wait();
                    Context.SetSessionTokenIfEmpty(delTask.Result.SessionToken);
                }

            });
        }

        public virtual Task RemoveLoginAsync(TUser user, UserLoginInfo login)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (login == null)
            {
                throw new ArgumentNullException("login");
            }
            string provider = login.LoginProvider;
            string key = login.ProviderKey;
            TUserLogin item = user.Logins.SingleOrDefault<TUserLogin>(l => (l.LoginProvider == provider) && (l.ProviderKey == key));
            if (item != null)
            {
                user.Logins.Remove(item);
                var delTask = Context.Client.DeleteDocumentAsync(item.SelfLink,
                    Context.RequestOptions);
                delTask.Wait();
                Context.SetSessionTokenIfEmpty(delTask.Result.SessionToken);
            }
            return Task.FromResult<int>(0);
        }

        public Task ResetAccessFailedCountAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            user.AccessFailedCount = 0;
            return Task.FromResult<int>(0);
        }

        public Task SetEmailAsync(TUser user, string email)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user.Email = email;
            return Task.FromResult<int>(0);
        }

        public Task SetEmailConfirmedAsync(TUser user, bool confirmed)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            user.EmailConfirmed = confirmed;
            return Task.FromResult<int>(0);
        }

        public Task SetLockoutEnabledAsync(TUser user, bool enabled)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            user.LockoutEnabled = enabled;
            return Task.FromResult<int>(0);
        }

        public Task SetLockoutEndDateAsync(TUser user, DateTimeOffset lockoutEnd)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            user.LockoutEndDateUtc = (lockoutEnd == DateTimeOffset.MinValue) ? null : new DateTime?(lockoutEnd.UtcDateTime);
            return Task.FromResult<int>(0);
        }

        public Task SetPasswordHashAsync(TUser user, string passwordHash)
        {
            this.ThrowIfDisposed();
            
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            user.PasswordHash = passwordHash;
            return Task.FromResult<int>(0);
        }

        public Task SetPhoneNumberAsync(TUser user, string phoneNumber)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            user.PhoneNumber = phoneNumber;
            return Task.FromResult<int>(0);
        }

        public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            user.PhoneNumberConfirmed = confirmed;
            return Task.FromResult<int>(0);
        }

        public Task SetSecurityStampAsync(TUser user, string stamp)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            user.SecurityStamp = stamp;
            return Task.FromResult<int>(0);
        }

        public Task SetTwoFactorEnabledAsync(TUser user, bool enabled)
        {
            this.ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            user.TwoFactorEnabled = enabled;
            return Task.FromResult<int>(0);
        }

        private void ThrowIfDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(base.GetType().Name);
            }
        }

        //private TUser ChangeUserName(TUser user)
        //{
        //    List<Task> taskList = new List<Task>(50);
        //    string userNameKey = KeyHelper.GenerateRowKeyUserName(user.UserName);
            
        //    Debug.WriteLine("Old User.Id: {0}", user.Id);
        //    Debug.WriteLine(string.Format("New User.Id: {0}", KeyHelper.GenerateRowKeyUserName(user.UserName)));
        //    //Get the old user
        //    var userRows = GetUserAggregateQuery(user.Id.ToString()).ToList();
        //    //Insert the new user name rows
        //    BatchOperationHelper insertBatchHelper = new BatchOperationHelper();
        //    foreach (DynamicTableEntity oldUserRow in userRows)
        //    {
        //        ITableEntity dte = null;
        //        if (oldUserRow.RowKey == user.Id.ToString())
        //        {
        //            IGenerateKeys ikey = (IGenerateKeys)user;
        //            ikey.GenerateKeys();
        //            dte = user;
        //        }
        //        else
        //        {
        //            dte = new DynamicTableEntity(userNameKey, oldUserRow.RowKey,
        //                Constants.ETagWildcard,
        //                oldUserRow.Properties);
        //        }
        //        insertBatchHelper.Add(TableOperation.Insert(dte));
        //    }
        //    taskList.Add(insertBatchHelper.ExecuteBatchAsync(_userTable));
        //    //Delete the old user
        //    BatchOperationHelper deleteBatchHelper = new BatchOperationHelper();
        //    foreach (DynamicTableEntity delUserRow in userRows)
        //    {
        //        deleteBatchHelper.Add(TableOperation.Delete(delUserRow));
        //    }
        //    taskList.Add(deleteBatchHelper.ExecuteBatchAsync(_userTable));

        //    // Create the new email index
        //    if (!string.IsNullOrWhiteSpace(user.Email))
        //    {
        //        IdentityUserIndex indexEmail = CreateEmailIndex(userNameKey, user.Email);

        //        taskList.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(indexEmail)));
        //    }

        //    // Update the external logins
        //    foreach (var login in user.Logins)
        //    {
        //        IdentityUserIndex indexLogin = CreateLoginIndex(userNameKey, login);
        //        taskList.Add(_indexTable.ExecuteAsync(TableOperation.InsertOrReplace(indexLogin)));
        //        login.PartitionKey = userNameKey;
        //    }

        //    // Update the claims partitionkeys
        //    foreach (var claim in user.Claims)
        //    {
        //        claim.PartitionKey = userNameKey;
        //    }

        //    // Update the roles partitionkeys
        //    foreach (var role in user.Roles)
        //    {
        //        role.PartitionKey = userNameKey;
        //    }

        //    Task.WaitAll(taskList.ToArray());
        //    return user;
        //}


        public async virtual Task UpdateAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            await new TaskFactory().StartNew(() =>
            {
                var updTask = Context.Client.ReplaceDocumentAsync(user.SelfLink, user,
                        Context.RequestOptions);
                updTask.Wait();
                Context.SetSessionTokenIfEmpty(updTask.Result.SessionToken);
                JsonConvert.PopulateObject(updTask.Result.Resource.ToString(), user);
            });

        }

        public IdentityCloudContext<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim> Context { get; private set; }


        public IQueryable<TUser> Users
        {
            get
            {
                ThrowIfDisposed();
                return _users;
            }
        }

        
    }
}
