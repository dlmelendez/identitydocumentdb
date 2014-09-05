// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ElCamino.AspNet.Identity.DocumentDB.Helpers
{
    public abstract class BaseKeyHelper
    {
        public abstract string GenerateRowKeyUserLoginInfo(UserLoginInfo info);

        public abstract string GenerateRowKeyIdentityUserRole(string plainRoleName);

        public abstract string GenerateRowKeyIdentityRole(string plainRoleName);

        public abstract string GenerateRowKeyIdentityUserClaim(string claimType, string claimValue);

        public abstract string GenerateRowKeyIdentityUserLogin(string loginProvider, string providerKey);

        public abstract double KeyVersion { get; }
    }
}
