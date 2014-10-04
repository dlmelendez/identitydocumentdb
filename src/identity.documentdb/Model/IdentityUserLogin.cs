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
    public class IdentityUserLogin : IdentityUserLogin<string>, IGenerateKeys
    {
        public IdentityUserLogin() { }


        /// <summary>
        /// Generates  Id keys.
        /// </summary>
        public void GenerateKeys()
        {
            Id = PeekRowKey();
            KeyVersion = KeyHelper.KeyVersion;
        }

        [JsonProperty(PropertyName = "kver")]
        public double KeyVersion { get; set; }

        /// <summary>
        /// Generates the RowKey without setting it on the object.
        /// </summary>
        /// <returns></returns>
        public string PeekRowKey()
        {
            return KeyHelper.GenerateRowKeyIdentityUserLogin(LoginProvider, ProviderKey);
        }

        public override string UserId { get; set; }
    }

    public class IdentityUserLogin<TKey> 
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        public virtual string LoginProvider { get; set; }

        public virtual string ProviderKey { get; set; }

        public virtual TKey UserId { get; set; }

    }

}
