// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using ElCamino.AspNet.Identity.DocumentDB.Helpers;
using Microsoft.AspNet.Identity;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ElCamino.AspNet.Identity.DocumentDB.Model
{
    public class IdentityRole : IdentityRole<string, IdentityUserRole>, IGenerateKeys
    {
        public IdentityRole() : base() { }

        /// <summary>
        /// Generates Row and Id keys.
        /// </summary>
        public void GenerateKeys()
        {
            Id = PeekRowKey();
            KeyVersion = KeyHelper.KeyVersion;
        }

        /// <summary>
        /// Generates the RowKey without setting it on the object.
        /// </summary>
        /// <returns></returns>
        public string PeekRowKey()
        {
            return KeyHelper.GenerateRowKeyIdentityRole(Name);
        }

        [JsonProperty(PropertyName = "kver")]
        public double KeyVersion { get; set; }

        public IdentityRole(string roleName)
            : this()
        {
            base.Name = roleName;
        }

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
    }

    public class IdentityRole<TKey, TUserRole> : Resource,
         IRole<TKey> where TUserRole : IdentityUserRole<TKey>
    {
        [JsonIgnore]
        private TKey _id;
        public IdentityRole() : base()
        {
            this.Users = new List<TUserRole>();
        }


        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonIgnore]
        public ICollection<TUserRole> Users { get; private set; }

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

    }
}
