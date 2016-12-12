// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using ElCamino.AspNet.Identity.DocumentDB.Helpers;
using Microsoft.AspNet.Identity;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElCamino.AspNet.Identity.DocumentDB.Model
{
    public class IdentityUser : IdentityUser<string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>
        , IUser
        , IUser<string>
        , IGenerateKeys
    {
        public IdentityUser() { }


        /// <summary>
        /// Generates Id key.
        /// All are the same in this case
        /// </summary>
        public void GenerateKeys()
        {
            Id = PeekRowKey();
            KeyVersion = KeyHelper.KeyVersion;
        }

        /// <summary>
        /// Generates the RowKey without setting it on the object.
        /// In this case, just returns a new guid
        /// </summary>
        /// <returns></returns>
        public string PeekRowKey()
        {
            return Guid.NewGuid().ToString().ToUpper();
        }

        [JsonProperty(PropertyName = "kver")]
        public double KeyVersion { get; set; }

        [JsonProperty(PropertyName = "id")]
        public override string Id
        {
            get
            {
                return base.Id;
            }
            set
            {
                base.Id = value;
            }
        }

        public string UserId
        {
            get
            {
                return base.Id;
            }
            set
            {
                base.Id = value;
            }
        }
        public override string UserName
        {
            get
            {
                return base.UserName;
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    base.UserName = value.Trim();
                }
            }
        }
    }

    public class IdentityUser<TKey, TLogin, TRole, TClaim> : Resource,
        IUser<TKey>
        where TLogin : IdentityUserLogin<TKey>
        where TRole : IdentityUserRole<TKey>
        where TClaim : IdentityUserClaim<TKey>
    {
        [JsonIgnore]
        private TKey _id;

        public IdentityUser()
        {
            this.Claims = new List<TClaim>(100);
            this.Roles = new List<TRole>(100);
            this.Logins = new List<TLogin>(100);
        }

        public virtual int AccessFailedCount { get; set; }

        [JsonProperty("Claims", NullValueHandling= NullValueHandling.Ignore)]
        public ICollection<TClaim> Claims { get; private set; }

        public virtual string Email { get; set; }

        public virtual bool EmailConfirmed { get; set; }

        public new virtual TKey Id
        {
            get { return _id; }
            set
            {
                _id = value;
                if (_id == null)
                {
                    base.Id = null;
                }
                else
                {
                    base.Id = value.ToString();
                }
            }
        }

        public virtual bool LockoutEnabled { get; set; }

        public virtual DateTime? LockoutEndDateUtc { get; set; }

        [JsonProperty("Logins", NullValueHandling= NullValueHandling.Ignore)]
        public ICollection<TLogin> Logins { get; private set; }

        public virtual string PasswordHash { get; set; }

        public virtual string PhoneNumber { get; set; }

        public virtual bool PhoneNumberConfirmed { get; set; }

        [JsonProperty("Roles", NullValueHandling = NullValueHandling.Ignore)]
        public ICollection<TRole> Roles { get; private set; }

        public virtual string SecurityStamp { get; set; }

        public virtual bool TwoFactorEnabled { get; set; }

        public virtual string UserName { get; set; }

    }

}
