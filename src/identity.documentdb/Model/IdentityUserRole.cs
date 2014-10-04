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
    public class IdentityUserRole : IdentityUserRole<string>, IGenerateKeys
    {
        public IdentityUserRole() { }

        /// <summary>
        /// Generates Id key.
        /// </summary>
        public void GenerateKeys()
        {
            RoleId = PeekRowKey();
            KeyVersion = KeyHelper.KeyVersion;
        }

        /// <summary>
        /// Generates the RowKey without setting it on the object.
        /// </summary>
        /// <returns></returns>
        public string PeekRowKey()
        {
            return KeyHelper.GenerateRowKeyIdentityUserRole(RoleName);
        }

        [JsonProperty(PropertyName = "kver")]
        public double KeyVersion { get; set; }

        [JsonProperty(PropertyName = "id")]
        public override string Id
        {
            get
            {
                return base.RoleId;
            }
            set
            {
                base.RoleId = value;
            }
        }
    }


    public class IdentityUserRole<TKey>
    {
        [JsonProperty("id")]
        public virtual string Id { get; set; }

        public virtual TKey RoleId { get; set; }

        public virtual TKey UserId { get; set; }

        public string RoleName { get; set; }

    }

}
