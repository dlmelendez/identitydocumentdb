// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
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

        public virtual async Task AddClaimAsync(TUser user, Claim claim)
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
            await Task.Run(() =>
                {
                    TUserClaim item = Activator.CreateInstance<TUserClaim>();
                    item.UserId = user.Id;
                    item.ClaimType = claim.Type;
                    item.ClaimValue = claim.Value;
                    ((IGenerateKeys)item).GenerateKeys();
                    
                    user.Claims.Add(item);               
                });
        }

        public async virtual Task AddLoginAsync(TUser user, UserLoginInfo login)
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
            await Task.Run(() =>
            {
                TUserLogin item = Activator.CreateInstance<TUserLogin>();
                item.UserId = user.Id;
                item.ProviderKey = login.ProviderKey;
                item.LoginProvider = login.LoginProvider;
                ((IGenerateKeys)item).GenerateKeys();
                user.Logins.Add(item);
            });

        }

        public async virtual Task AddToRoleAsync(TUser user, string roleName)
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

            await Task.Run(() =>
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

            var doc = await Context.Client.CreateDocumentAsync(Context.UserDocumentCollection.DocumentsLink, user
                , Context.RequestOptions, true);
            Context.SetSessionTokenIfEmpty(doc.SessionToken);
            JsonConvert.PopulateObject(doc.Resource.ToString(), user);

        }

        public async virtual Task DeleteAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            await Context.Client.DeleteDocumentAsync(user.SelfLink,
                            Context.RequestOptions);
        }

        public void Dispose()
        {
            this.Dispose(true);
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

            var result = await Context.Client.ExecuteStoredProcedureAsync<IEnumerable<dynamic>>(Context.GetUserByLoginSproc.SelfLink,
                    new dynamic[] { loginId });
            if (result.Response != null)
            {
                Context.SetSessionTokenIfEmpty(result.SessionToken);
                return GetUserAggregate(result.Response.ToList());
            }
            return null;

        }

        public async Task<TUser> FindByEmailAsync(string plainEmail)
        {
            var result = await Context.Client.ExecuteStoredProcedureAsync<IEnumerable<dynamic>>(Context.GetUserByEmailSproc.SelfLink,
               new dynamic[] { plainEmail });

            if (result.Response != null)
            {
                Context.SetSessionTokenIfEmpty(result.SessionToken);
                return GetUserAggregate(result.Response.ToList());
            }
            return null;
        }

        public virtual async Task<TUser> FindByIdAsync(TKey userId)
        {
            this.ThrowIfDisposed();
            return await this.GetUserAggregateAsync(userId.ToString());
        }

        public async virtual Task<TUser> FindByNameAsync(string userName)
        {
            this.ThrowIfDisposed();
            var result = await Context.Client.ExecuteStoredProcedureAsync<IEnumerable<dynamic>>(Context.GetUserByUserNameSproc.SelfLink,
                    new dynamic[] { userName });
            if (result.Response != null)
            {
                Context.SetSessionTokenIfEmpty(result.SessionToken);
                return await Task.FromResult<TUser>(GetUserAggregate(result.Response.ToList()));
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


        public async virtual Task UpdateAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            
            var result = await Context.Client.ReplaceDocumentAsync(user.SelfLink, user,
                    Context.RequestOptions);
            Context.SetSessionTokenIfEmpty(result.SessionToken);
            user = Activator.CreateInstance<TUser>();
            JsonConvert.PopulateObject(result.Resource.ToString(), user);

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
