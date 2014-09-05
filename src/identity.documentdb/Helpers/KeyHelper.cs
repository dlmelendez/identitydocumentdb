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
    public static class KeyHelper
    {
        private static BaseKeyHelper baseHelper = new HashKeyHelper();

        public static string GenerateRowKeyUserLoginInfo(this UserLoginInfo info)
        {
            return baseHelper.GenerateRowKeyUserLoginInfo(info);
        }

        public static string GenerateRowKeyIdentityUserRole(string plainRoleName)
        {
            return baseHelper.GenerateRowKeyIdentityUserRole(plainRoleName);
        }

        public static string GenerateRowKeyIdentityRole(string plainRoleName)
        {
            return baseHelper.GenerateRowKeyIdentityRole(plainRoleName);
        }

        public static string GenerateRowKeyIdentityUserClaim(string claimType, string claimValue)
        {
            return baseHelper.GenerateRowKeyIdentityUserClaim(claimType, claimValue);
        }

        public static string GenerateRowKeyIdentityUserLogin(string loginProvider, string providerKey)
        {
            return baseHelper.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey);
        }

        public static double KeyVersion { get { return baseHelper.KeyVersion; } }
    }
}
