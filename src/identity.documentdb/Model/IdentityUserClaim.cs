// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using ElCamino.AspNet.Identity.DocumentDB.Helpers;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElCamino.AspNet.Identity.DocumentDB.Model
{
    public class IdentityUserClaim : IdentityUserClaim<string>, IGenerateKeys
    {
        public IdentityUserClaim() { }

        /// <summary>
        /// Generates Row and Id keys.
        /// Partition key is equal to the UserId
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
            return KeyHelper.GenerateRowKeyIdentityUserClaim(ClaimType, ClaimValue);
        }

        [JsonProperty(PropertyName = "kver")]
        public double KeyVersion { get; set; }

        public override string UserId { get; set; }
    }

    public class IdentityUserClaim<TKey> 
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        public virtual string ClaimType { get; set; }

        public virtual string ClaimValue { get; set; }

        public virtual TKey UserId { get; set; }

    }

}
