// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElCamino.AspNet.Identity.DocumentDB.Helpers
{
    public class UriEncodeKeyHelper : BaseKeyHelper
    {
        public const string ReplaceIllegalChar = "%";
        public const string NewCharForIllegalChar = "_";

        public override string GenerateRowKeyUserLoginInfo(UserLoginInfo info)
        {
            string strTemp = string.Format("{0}_{1}", EscapeKey(info.LoginProvider), EscapeKey(info.ProviderKey));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserLogin, strTemp);
        }

        public override string GenerateRowKeyIdentityUserRole(string plainRoleName)
        {
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserRole,
                EscapeKey(plainRoleName));
        }

        public override string GenerateRowKeyIdentityRole(string plainRoleName)
        {
            return string.Format(Constants.RowKeyConstants.FormatterIdentityRole,
                    EscapeKey(plainRoleName));
        }

        public override string GenerateRowKeyIdentityUserClaim(string claimType, string claimValue)
        {
            string strTemp = string.Format("{0}_{1}", EscapeKey(claimType), EscapeKey(claimValue));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserClaim, strTemp);

        }

        public static string EscapeKey(string keyUnsafe)
        {
            if (!string.IsNullOrWhiteSpace(keyUnsafe))
            {
                // Need to replace '%' because azure bug.
                return System.Uri.EscapeDataString(keyUnsafe).Replace(ReplaceIllegalChar, NewCharForIllegalChar).ToUpper();
            }
            return null;
        }

        public override string GenerateRowKeyIdentityUserLogin(string loginProvider, string providerKey)
        {
            string strTemp = string.Format("{0}_{1}", EscapeKey(loginProvider), EscapeKey(providerKey));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserLogin, strTemp);
        }

        public override double KeyVersion
        {
            get { return 1.2; }
        }
    }
}
